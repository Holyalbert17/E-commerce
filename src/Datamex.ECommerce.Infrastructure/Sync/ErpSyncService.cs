using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Datamex.ECommerce.Infrastructure.Sync;

/// <summary>
/// Pull ID-preserving desde ERP Postgres (schema dbo, columnas lowercase)
/// hacia el espejo local EcomDb. Solo lectura hacia el ERP.
/// </summary>
public sealed class ErpSyncService : IErpSyncService
{
    public static readonly IReadOnlyList<string> DefaultEntityKeys =
    [
        "empresa", "catalog", "prices", "stock", "customers", "shipping", "promotions", "order_status"
    ];

    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _config;
    private readonly IErpConnectionResolver _erpCs;
    private readonly ILogger<ErpSyncService> _log;
    private PullAcc? _acc;

    public ErpSyncService(
        EcomDbContext db,
        ITenantContext tenant,
        IConfiguration config,
        IErpConnectionResolver erpCs,
        ILogger<ErpSyncService> log)
    {
        _db = db;
        _tenant = tenant;
        _config = config;
        _erpCs = erpCs;
        _log = log;
    }

    public async Task<ErpSyncStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var idEmpresa = ResolveTargetEmpresaId();
        var erpConfigured = !string.IsNullOrWhiteSpace(await GetErpConnectionStringAsync(idEmpresa, ct));
        var cursors = await LoadCursorsAsync(idEmpresa, ct);
        var source = await _erpCs.ResolveSourceAsync(idEmpresa, ct);
        var writeBack = await _erpCs.AllowWriteBackAsync(idEmpresa, ct);
        return new ErpSyncStatusDto(erpConfigured, cursors, source, writeBack, "ecom-etl");
    }

    public Task<ErpEnsureArticuloResult> EnsureArticuloFromErpAsync(
        long? articuloId = null,
        string? sku = null,
        CancellationToken ct = default)
    {
        // Lazy-pull C# deshabilitado: catálogo/stock los alimenta ecom-etl.
        return Task.FromResult(new ErpEnsureArticuloResult(
            false,
            articuloId,
            false,
            "Inbound deshabilitado en API. Usá ecom-etl (python) para sincronizar catálogo/stock."));
    }

    public async Task<ErpSyncRunResult> RunDeltaAsync(
        IReadOnlyList<string>? entityKeys = null, CancellationToken ct = default)
    {
        // Inbound deshabilitado: el espejo lo mantiene ecom-etl (1 ERP → N e-com).
        _log.LogWarning(
            "RunDeltaAsync rechazado: inbound solo vía ecom-etl. Keys={Keys}",
            entityKeys is null ? "all" : string.Join(',', entityKeys));
        var cursors = await LoadCursorsAsync(ResolveTargetEmpresaId(), ct);
        return new ErpSyncRunResult(0, 0, cursors, 0);
    }

    private sealed class PullAcc
    {
        public int Read;
        public int Inserted;
        public int Updated;

        public void NoteInsert()
        {
            Read++;
            Inserted++;
        }

        public void NoteUpdate()
        {
            Read++;
            Updated++;
        }
    }

    private async Task<int> RunEntityPullAsync(
        string erpCs, long idEmpresa, string entityKey, SyncCursor cursor, DateTime now, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(erpCs);
        await conn.OpenAsync(ct);
        await using (var sp = new NpgsqlCommand("SET search_path TO dbo, public", conn))
            await sp.ExecuteNonQueryAsync(ct);

        return entityKey switch
        {
            "empresa" => await SyncEmpresaAsync(conn, idEmpresa, now, ct),
            "catalog" => await SyncCatalogAsync(conn, idEmpresa, now, ct),
            "prices" => await SyncPricesAsync(conn, idEmpresa, now, ct),
            "stock" => await SyncStockAsync(conn, idEmpresa, now, ct),
            "customers" => await SyncCustomersAsync(conn, idEmpresa, now, ct),
            "shipping" => await SyncShippingAsync(conn, idEmpresa, now, ct),
            "promotions" => await SyncPromotionsAsync(conn, idEmpresa, now, ct),
            "order_status" => await SyncOrderStatusAsync(conn, idEmpresa, cursor, now, ct),
            _ => 0
        };
    }

    private async Task<int> SyncEmpresaAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT e.id, e.codempresa, e.nombreempresa, e.direccionempresa, e.telefono, e.fax, e.email,
                   e.ruc, e.representantelegal, e.idpais, e.logoempresa,
                   e.activo, e.ultmodificacion, e.username,
                   c.realizadelivery, c.realizapickup, c.cobraenlaentrega, c.cobraalhacerpedido,
                   c.montopedidominimo, c.procesapedidosporplanilla, c.procesapedidoserp,
                   c.nroultpedidocarritocompra, c.idarticulodelivery, c.controlastockencarrito,
                   c.clavepublicaprocesadoratarjeta, c.claveprivadaprocesadoratarjeta,
                   c.ultidprocesonegocio, c.direccionurlcheckout, c.codigocomercio, c.codigosucursal,
                   c.tiempoaproxentregapickup, c.tiempoaproxentregadelivery,
                   c.iddeposito, c.direccionurlfacebook, c.direccionurltwitter, c.direccionurlinstagram,
                   c.direccionurlyoutube, c.direccionurlwhatsapp,
                   c.nombreservidorcorreoelectronico, c.nombreusuariocorreoelectronico,
                   c.passwordcorreoelectronico, c.puertocorreoelectronico
            FROM dbo.empresa e
            LEFT JOIN dbo.empresaconfiguracionecommerce c ON c.id = e.id
            WHERE e.id = @id
            """, conn);
        cmd.Parameters.AddWithValue("id", idEmpresa);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            throw new InvalidOperationException($"Empresa ERP id={idEmpresa} no encontrada.");

        var nombre = r.GetString(r.GetOrdinal("nombreempresa"));
        var ruc = ReadString(r, "ruc");
        var activo = r.GetBoolean(r.GetOrdinal("activo"));
        var ult = ReadDateTime(r, "ultmodificacion") ?? now;
        var user = ReadString(r, "username");

        var empresa = await _db.Empresas.FindAsync([idEmpresa], ct);
        if (empresa is null)
        {
            empresa = new Empresa { ID = idEmpresa };
            _db.Empresas.Add(empresa);
            _acc?.NoteInsert();
        }
        else
        {
            _acc?.NoteUpdate();
        }

        empresa.CodEmpresa = r.IsDBNull(r.GetOrdinal("codempresa"))
            ? (int)idEmpresa
            : Convert.ToInt32(r.GetValue(r.GetOrdinal("codempresa")));
        empresa.RazonSocial = nombre;
        empresa.NombreFantasia ??= nombre;
        empresa.NumeroDocumento = ruc;
        empresa.DireccionEmpresa = ReadString(r, "direccionempresa");
        empresa.Telefono = ReadString(r, "telefono");
        empresa.Fax = ReadString(r, "fax");
        empresa.EMail = ReadString(r, "email");
        empresa.RepresentanteLegal = ReadString(r, "representantelegal");
        empresa.IDPais = ReadLongNull(r, "idpais");
        empresa.LogoEmpresa = ReadString(r, "logoempresa");
        empresa.Activo = activo;
        empresa.UltModificacion = ult;
        empresa.UserName = user ?? "erp-sync";

        var cfg = await _db.EmpresaConfiguracionesEcommerce.FindAsync([idEmpresa], ct);
        if (cfg is null)
        {
            cfg = new EmpresaConfiguracionEcommerce { ID = idEmpresa };
            _db.EmpresaConfiguracionesEcommerce.Add(cfg);
            _acc?.NoteInsert();
        }
        else
        {
            _acc?.NoteUpdate();
        }

        if (!r.IsDBNull(r.GetOrdinal("realizadelivery")))
        {
            cfg.RealizaDelivery = r.GetBoolean(r.GetOrdinal("realizadelivery"));
            cfg.RealizaPickup = r.GetBoolean(r.GetOrdinal("realizapickup"));
            cfg.CobraEnLaEntrega = r.GetBoolean(r.GetOrdinal("cobraenlaentrega"));
            cfg.CobraAlHacerPedido = r.GetBoolean(r.GetOrdinal("cobraalhacerpedido"));
            cfg.MontoPedidoMinimo = r.GetDecimal(r.GetOrdinal("montopedidominimo"));
            cfg.ProcesaPedidosPorPlanilla = r.GetBoolean(r.GetOrdinal("procesapedidosporplanilla"));
            cfg.ProcesaPedidosERP = r.GetBoolean(r.GetOrdinal("procesapedidoserp"));
            cfg.NroUltPedidoCarritoCompra = r.GetInt64(r.GetOrdinal("nroultpedidocarritocompra"));
            cfg.IDArticuloDelivery = ReadLongNull(r, "idarticulodelivery");
            cfg.ControlaStockEnCarrito = r.GetBoolean(r.GetOrdinal("controlastockencarrito"));
            cfg.ClavePublicaProcesadoraTarjeta = ReadString(r, "clavepublicaprocesadoratarjeta");
            cfg.ClavePrivadaProcesadoraTarjeta = ReadString(r, "claveprivadaprocesadoratarjeta");
            cfg.UltIDProcesoNegocio = ReadLong(r, "ultidprocesonegocio");
            cfg.DireccionURLCheckout = ReadString(r, "direccionurlcheckout");
            cfg.CodigoComercio = ReadLongNull(r, "codigocomercio");
            cfg.CodigoSucursal = ReadLongNull(r, "codigosucursal");
            cfg.TiempoAproxEntregaPickup = ReadTimeNull(r, "tiempoaproxentregapickup");
            cfg.TiempoAproxEntregaDelivery = ReadTimeNull(r, "tiempoaproxentregadelivery");
            var erpDep = ReadLong(r, "iddeposito");
            // No pisar depósito local si ya hay existencia sincronizada en otro depósito
            // (p.ej. ERP config=1 pero stock e-com real en 305).
            if (erpDep > 0)
            {
                var localHasStock = cfg.IDDeposito > 0
                    && cfg.IDDeposito != erpDep
                    && await _db.Stocks.AsNoTracking().AnyAsync(s =>
                        s.IDEmpresa == idEmpresa
                        && s.Activo
                        && s.IDDeposito == cfg.IDDeposito
                        && s.TotalExistencia > 0, ct);
                if (!localHasStock)
                    cfg.IDDeposito = erpDep;
            }
            cfg.DireccionURLFacebook = ReadString(r, "direccionurlfacebook");
            cfg.DireccionURLTwitter = ReadString(r, "direccionurltwitter");
            cfg.DireccionURLInstagram = ReadString(r, "direccionurlinstagram");
            cfg.DireccionURLYoutube = ReadString(r, "direccionurlyoutube");
            cfg.DireccionURLWhatsapp = ReadString(r, "direccionurlwhatsapp");
            cfg.NombreServidorCorreoElectronico = ReadString(r, "nombreservidorcorreoelectronico");
            cfg.NombreUsuarioCorreoElectronico = ReadString(r, "nombreusuariocorreoelectronico");
            cfg.PasswordCorreoElectronico = ReadString(r, "passwordcorreoelectronico");
            cfg.PuertoCorreoElectronico = ReadLongNull(r, "puertocorreoelectronico");

        // En Integrated el stock lo mueve el ERP vía sync; no forzar ControlaStockEnCarrito=false
        // solo por ProcesaPedidosERP — la vitrina lee el espejo y valida cantidades igual.
        cfg.ModoIntegracion = (int)Datamex.ECommerce.Domain.Enums.IntegrationMode.Integrated;
        cfg.UsarListaPrecioCuentaErp = true;
        }

        // Contacto / logo footer desde maestro ERP (sin pisar branding ya editado en admin si ya hay valor local distinto).
        cfg.DireccionLocal ??= empresa.DireccionEmpresa;
        cfg.TelefonoContacto ??= empresa.Telefono;
        cfg.EmailContacto ??= empresa.EMail;
        if (string.IsNullOrWhiteSpace(cfg.LogoUrl) && !string.IsNullOrWhiteSpace(empresa.LogoEmpresa))
            cfg.LogoUrl = empresa.LogoEmpresa;

        cfg.Activo = true;
        cfg.UltModificacion = now;
        cfg.UserName = "erp-sync";
        // Casa Imperial (y cualquier tenant ERP): slug fijo operativo para admin + vitrina.
        cfg.SlugTienda = idEmpresa == 41 ? "casa-imperial" : (cfg.SlugTienda ?? $"empresa-{idEmpresa}");
        cfg.NombreTiendaDisplay = string.IsNullOrWhiteSpace(cfg.NombreTiendaDisplay) ? nombre : cfg.NombreTiendaDisplay;

        await r.DisposeAsync();
        await _db.SaveChangesAsync(ct);

        await SyncEmpresaRedesSocialesAsync(conn, idEmpresa, now, ct);

        // Admin operable para el tenant sincronizado (no toca ERP).
        try
        {
            await DemoSeed.EnsureBackofficeRbacAsync(_db, idEmpresa, _log, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo asegurar admin RBAC para empresa {Empresa}", idEmpresa);
        }

        return 1;
    }

    private async Task SyncEmpresaRedesSocialesAsync(
        NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        // ERP migration usa "nombreredsocial" (Nombre+Red+Social), no "nombreredesocial".
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, nombreredsocial, direccionurlredsocial, logoredsocial,
                   activo, ultmodificacion, username
            FROM dbo.empresaconfiguracionecommerceredsocial
            WHERE idempresaconfiguracionecommerce = @e
            """, conn);
        cmd.Parameters.AddWithValue("e", idEmpresa);

        var seen = new HashSet<long>();
        var buffer = new List<(long Id, string Nombre, string Url, string Logo, bool Activo, DateTime? Ult, string? User)>();
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct))
            {
                buffer.Add((
                    r.GetInt64(r.GetOrdinal("id")),
                    ReadString(r, "nombreredsocial") ?? "",
                    ReadString(r, "direccionurlredsocial") ?? "",
                    ReadString(r, "logoredsocial") ?? "",
                    !r.IsDBNull(r.GetOrdinal("activo")) && r.GetBoolean(r.GetOrdinal("activo")),
                    ReadDateTime(r, "ultmodificacion"),
                    ReadString(r, "username")));
            }
        }

        foreach (var item in buffer)
        {
            seen.Add(item.Id);
            var row = await _db.EmpresaConfiguracionEcommerceRedSociales.FindAsync([item.Id], ct);
            if (row is null)
            {
                row = new EmpresaConfiguracionEcommerceRedSocial { ID = item.Id };
                _db.EmpresaConfiguracionEcommerceRedSociales.Add(row);
                _acc?.NoteInsert();
            }
            else
            {
                _acc?.NoteUpdate();
            }

            row.IDEmpresaConfiguracionEcommerce = idEmpresa;
            row.NombreRedSocial = item.Nombre;
            row.DireccionURLRedSocial = item.Url;
            row.LogoRedSocial = item.Logo;
            row.Activo = item.Activo;
            row.UltModificacion = item.Ult ?? now;
            row.UserName = item.User ?? "erp-sync";
        }

        var stale = await _db.EmpresaConfiguracionEcommerceRedSociales
            .Where(x => x.IDEmpresaConfiguracionEcommerce == idEmpresa && !seen.Contains(x.ID))
            .ToListAsync(ct);
        if (stale.Count > 0)
            _db.EmpresaConfiguracionEcommerceRedSociales.RemoveRange(stale);

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncCatalogAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        var full = _config.GetValue("Erp:SyncFullCatalog", false);
        var allLists = _config.GetValue("Erp:SyncAllPriceLists", false);
        var artIds = new HashSet<long>();
        var embIds = new HashSet<long>();

        // Artículos: ecom publicados, o todo el activo si SyncFullCatalog
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.articulo
                         WHERE idempresa = @e AND activo = true
                           AND (@full OR activoecommerce = true)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("full", full);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                artIds.Add(r.GetInt64(0));
        }

        // Embalajes / arts referenciados por listas (ecom o todas si SyncAllPriceLists)
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT DISTINCT ae.id, ae.idarticulo
                         FROM dbo.articuloembalaje ae
                         JOIN dbo.detallelistapreciocondicionventa d ON d.idarticuloembalaje = ae.id
                         JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
                         WHERE l.idempresa = @e AND l.activo = true
                           AND (l.eslistaprecioecommerce = true OR @all)
                         UNION
                         SELECT DISTINCT ae.id, ae.idarticulo
                         FROM dbo.articuloembalaje ae
                         JOIN dbo.detallelistaprecio d ON d.idarticuloembalaje = ae.id
                         JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
                         WHERE l.idempresa = @e AND l.activo = true
                           AND (l.eslistaprecioecommerce = true OR @all)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", allLists);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                embIds.Add(r.GetInt64(0));
                artIds.Add(r.GetInt64(1));
            }
        }

        if (artIds.Count == 0)
            return 0;

        var rows = 0;
        rows += await UpsertArticulosAsync(conn, idEmpresa, artIds, now, ct);
        rows += await UpsertEmbalajesForArtsAsync(conn, idEmpresa, artIds, embIds, now, ct);
        rows += await UpsertCatalogoParametrosFromArtsAsync(conn, idEmpresa, artIds, now, ct);
        rows += await UpsertImagenMetaAsync(conn, idEmpresa, artIds, now, ct);
        return rows;
    }

    private async Task<int> UpsertArticulosAsync(
        NpgsqlConnection conn, long idEmpresa, HashSet<long> artIds, DateTime now, CancellationToken ct)
    {
        var ids = artIds.ToArray();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT a.id, a.nroarticulo, a.sku, a.descripcionarticulo, a.descripcionarticuloabreviada,
                   a.descripcionarticuloextensa, a.codreferenciaproveedor,
                   a.idmarca, a.idlinea, a.idtipoarticulo, a.idproveedor, a.idpais,
                   a.activoecommerce, a.activo, a.ultmodificacion, a.username,
                   m.descripcionmarca, li.descripcionlinea, t.descripciontipoarticulo,
                   COALESCE(p.nombreproveedor, p.razonsocial, '') AS nombreproveedor,
                   pa.descripcionpais
            FROM dbo.articulo a
            LEFT JOIN dbo.marca m ON m.id = a.idmarca
            LEFT JOIN dbo.linea li ON li.id = a.idlinea
            LEFT JOIN dbo.tipoarticulo t ON t.id = a.idtipoarticulo
            LEFT JOIN dbo.proveedor p ON p.id = a.idproveedor
            LEFT JOIN dbo.pais pa ON pa.id = a.idpais
            WHERE a.id = ANY(@ids)
            """, conn);
        cmd.Parameters.AddWithValue("ids", ids);

        var n = 0;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var id = r.GetInt64(r.GetOrdinal("id"));
            var a = await _db.Articulos.FindAsync([id], ct);
            if (a is null)
            {
                a = new Articulo { ID = id, IDEmpresa = idEmpresa };
                _db.Articulos.Add(a);
                _acc?.NoteInsert();
            }
            else
            {
                _acc?.NoteUpdate();
            }

            a.IDEmpresa = idEmpresa;
            a.NroArticulo = r.GetInt32(r.GetOrdinal("nroarticulo"));
            a.SKU = ReadString(r, "sku");
            a.DescripcionArticulo = Trunc(ReadString(r, "descripcionarticulo") ?? "", 100);
            a.DescripcionArticuloAbreviada = Trunc(ReadString(r, "descripcionarticuloabreviada") ?? "", 50);
            a.DescripcionArticuloExtensa = ReadString(r, "descripcionarticuloextensa") ?? "";
            a.CodReferenciaProveedor = ReadString(r, "codreferenciaproveedor");
            a.IDMarca = ReadLong(r, "idmarca");
            a.IDLinea = ReadLongNull(r, "idlinea");
            a.IDTipoArticulo = ReadLongNull(r, "idtipoarticulo");
            a.IDProveedor = ReadLongNull(r, "idproveedor");
            a.IDPais = ReadLongNull(r, "idpais");
            a.NombreMarca = Trunc(ReadString(r, "descripcionmarca"), 100);
            a.NombreLinea = Trunc(ReadString(r, "descripcionlinea"), 100);
            a.NombreTipoArticulo = Trunc(ReadString(r, "descripciontipoarticulo"), 100);
            a.NombreProveedor = Trunc(ReadString(r, "nombreproveedor"), 100);
            a.NombrePais = Trunc(ReadString(r, "descripcionpais"), 100);
            var erpActivoEcom = r.GetBoolean(r.GetOrdinal("activoecommerce"));
            var publishAll = _config.GetValue("Erp:PublishSyncedCatalogToStorefront", false)
                             || _config.GetValue("Erp:SyncFullCatalog", false);
            // Full catalog → vitrina ve todos los activos ERP (Casa Imperial).
            a.ActivoEcommerce = publishAll ? r.GetBoolean(r.GetOrdinal("activo")) : erpActivoEcom;
            a.EstadoEcommerce = a.ActivoEcommerce ? "activo" : "oculto";
            a.Activo = r.GetBoolean(r.GetOrdinal("activo"));
            a.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
            a.UserName = ReadString(r, "username") ?? "erp-sync";
            n++;
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task<int> UpsertEmbalajesForArtsAsync(
        NpgsqlConnection conn, long idEmpresa, HashSet<long> artIds, HashSet<long> extraEmbIds,
        DateTime now, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT ae.id, ae.idarticulo, ae.idembalaje, ae.cantidadxembalaje,
                   ae.embalajeparafacturacion, ae.embalajeparacontrolstock,
                   ae.activo, ae.ultmodificacion, ae.username,
                   e.descripcionembalaje,
                   u.codunidadmedida AS uom_cod,
                   u.descripcionunidadmedida AS uom_desc
            FROM dbo.articuloembalaje ae
            LEFT JOIN dbo.embalaje e ON e.id = ae.idembalaje
            LEFT JOIN dbo.unidadmedida u ON u.id = ae.idunidadmedidaembalaje
            WHERE ae.idarticulo = ANY(@arts) OR ae.id = ANY(@embs)
            """, conn);
        cmd.Parameters.AddWithValue("arts", artIds.ToArray());
        cmd.Parameters.AddWithValue("embs", extraEmbIds.Count == 0 ? new long[] { -1 } : extraEmbIds.ToArray());

        var n = 0;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var id = r.GetInt64(r.GetOrdinal("id"));
            var emb = await _db.ArticuloEmbalajes.FindAsync([id], ct);
            if (emb is null)
            {
                emb = new ArticuloEmbalaje { ID = id, IDEmpresa = idEmpresa };
                _db.ArticuloEmbalajes.Add(emb);
            }

            emb.IDEmpresa = idEmpresa;
            emb.IDArticulo = r.GetInt64(r.GetOrdinal("idarticulo"));
            emb.IDEmbalaje = r.GetInt64(r.GetOrdinal("idembalaje"));
            emb.CantidadxEmbalaje = r.GetDouble(r.GetOrdinal("cantidadxembalaje"));
            emb.EmbalajeParaFacturacion = r.GetBoolean(r.GetOrdinal("embalajeparafacturacion"));
            emb.EmbalajeParaControlStock = r.GetBoolean(r.GetOrdinal("embalajeparacontrolstock"));
            emb.DescripcionEmbalaje = Trunc(ReadString(r, "descripcionembalaje"), 100);
            emb.Activo = r.GetBoolean(r.GetOrdinal("activo"));
            emb.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
            emb.UserName = ReadString(r, "username") ?? "erp-sync";

            var uomCod = r.IsDBNull(r.GetOrdinal("uom_cod")) ? (int?)null : r.GetInt32(r.GetOrdinal("uom_cod"));
            var uomDesc = ReadString(r, "uom_desc");
            ApplyUnidadMedidaFromErp(emb, uomCod, uomDesc);
            n++;
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    /// <summary>
    /// Mapea unidad ERP → códigos ecom (UN/M/M2/KG/L). Si es metro/kg/litro habilita fracción y paso de venta.
    /// </summary>
    private static void ApplyUnidadMedidaFromErp(ArticuloEmbalaje emb, int? codUnidad, string? descUnidad)
    {
        var blob = $"{descUnidad} {emb.DescripcionEmbalaje}".Trim().ToLowerInvariant();
        var (codigo, fraccion, incremento, minima) = ResolveUom(blob, codUnidad);

        emb.CodigoUnidadMedida = codigo;
        emb.PermiteFraccion = fraccion;
        emb.IncrementoVenta = incremento;
        emb.CantidadMinimaVenta = minima;
    }

    private static (string Codigo, bool Fraccion, decimal Incremento, decimal Minima) ResolveUom(
        string blob, int? codUnidad)
    {
        if (blob.Contains("metro cuadrado") || blob.Contains("m²") || blob.Contains("metros cuadrados")
            || Regex.IsMatch(blob, @"\bm2\b") || Regex.IsMatch(blob, @"\bm²\b"))
            return ("M2", true, 0.5m, 0.5m);

        if (blob.Contains("metro") || blob.Contains("por metro") || blob.Contains("mt lineal")
            || blob.Contains("metro lineal") || Regex.IsMatch(blob, @"\bml\b"))
            return ("M", true, 0.5m, 1m);

        // Heurística de catálogo textil / alfombra (mismo criterio que seed demo).
        if (blob.Contains("tela") || blob.Contains("alfombra") || blob.Contains("vinilo")
            || blob.Contains("piso flotante"))
            return ("M", true, 0.5m, 1m);

        if (blob.Contains("kilogram") || blob.Contains("kilo") || Regex.IsMatch(blob, @"\bkg\b"))
            return ("KG", true, 0.1m, 0.1m);

        if (blob.Contains("litro") || Regex.IsMatch(blob, @"\blitros?\b"))
            return ("L", true, 0.1m, 0.1m);

        _ = codUnidad;
        return ("UN", false, 1m, 1m);
    }

    /// <summary>
    /// Lista promovida en ecom por ConfigureCasaImperial (cobertura real de precios).
    /// Conservar aunque ERP diga que no es e-com.
    /// </summary>
    private static bool IsLocalEcommerceListaPromotion(ListaPrecio lp) =>
        string.Equals(lp.UserName, "configure-ci", StringComparison.Ordinal);

    /// <summary>
    /// Aplica flags e-com desde ERP sin demotar una promoción local (lista operativa con precios).
    /// Tampoco vuelve a marcar como e-com la lista ERP esquelética si hay promoción local.
    /// </summary>
    private static void ApplyListaEcommerceFlagsFromErp(ListaPrecio lp, bool erpEcom, int erpEstado)
    {
        if (IsLocalEcommerceListaPromotion(lp))
        {
            // Mantener vitrina operativa aunque el ERP no la marque.
            lp.EsListaPrecioEcommerce = true;
            if (lp.Estado != 2) lp.Estado = 2;
            if (string.IsNullOrWhiteSpace(lp.CanalEcommerce) || lp.CanalEcommerce is not ("B2C" or "B2B" or "BOTH"))
                lp.CanalEcommerce = "BOTH";
            return;
        }

        lp.EsListaPrecioEcommerce = erpEcom;
        lp.Estado = erpEstado;
        if (lp.EsListaPrecioEcommerce
            && (string.IsNullOrWhiteSpace(lp.CanalEcommerce)
                || lp.CanalEcommerce is not ("B2C" or "B2B" or "BOTH")))
        {
            lp.CanalEcommerce = "BOTH";
        }
    }

    private async Task<int> UpsertCatalogoParametrosFromArtsAsync(
        NpgsqlConnection conn, long idEmpresa, HashSet<long> artIds, DateTime now, CancellationToken ct)
    {
        var specs = new (string Tipo, string Sql)[]
        {
            ("MARCA", """
                SELECT DISTINCT m.id, m.descripcionmarca AS d
                FROM dbo.marca m
                JOIN dbo.articulo a ON a.idmarca = m.id
                WHERE a.id = ANY(@ids)
                """),
            ("LINEA", """
                SELECT DISTINCT li.id, li.descripcionlinea AS d
                FROM dbo.linea li
                JOIN dbo.articulo a ON a.idlinea = li.id
                WHERE a.id = ANY(@ids)
                """),
            ("TIPO", """
                SELECT DISTINCT t.id, t.descripciontipoarticulo AS d
                FROM dbo.tipoarticulo t
                JOIN dbo.articulo a ON a.idtipoarticulo = t.id
                WHERE a.id = ANY(@ids)
                """),
            ("PROVEEDOR", """
                SELECT DISTINCT p.id, COALESCE(p.nombreproveedor, p.razonsocial, p.id::text) AS d
                FROM dbo.proveedor p
                JOIN dbo.articulo a ON a.idproveedor = p.id
                WHERE a.id = ANY(@ids)
                """),
            ("PAIS", """
                SELECT DISTINCT pa.id, pa.descripcionpais AS d
                FROM dbo.pais pa
                JOIN dbo.articulo a ON a.idpais = pa.id
                WHERE a.id = ANY(@ids)
                """)
        };

        var n = 0;
        var ids = artIds.ToArray();
        foreach (var (tipo, sql) in specs)
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("ids", ids);
            try
            {
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    var id = r.GetInt64(0);
                    var desc = Trunc(r.IsDBNull(1) ? id.ToString() : r.GetString(1), 100) ?? id.ToString();
                    var codigo = id > int.MaxValue ? (int)(id % int.MaxValue) : (int)id;

                    var p = await _db.CatalogoParametros
                        .FirstOrDefaultAsync(x => x.IDEmpresa == idEmpresa && x.Tipo == tipo && x.Codigo == codigo, ct);
                    if (p is null)
                    {
                        // Prefer preserve ERP id when free
                        var byId = await _db.CatalogoParametros.FindAsync([id], ct);
                        if (byId is null)
                        {
                            p = new CatalogoParametro
                            {
                                ID = id,
                                IDEmpresa = idEmpresa,
                                Tipo = tipo,
                                Codigo = codigo,
                                Descripcion = desc,
                                Activo = true,
                                UltModificacion = now,
                                UserName = "erp-sync"
                            };
                            _db.CatalogoParametros.Add(p);
                        }
                        else
                        {
                            p = byId;
                            p.IDEmpresa = idEmpresa;
                            p.Tipo = tipo;
                            p.Codigo = codigo;
                            p.Descripcion = desc;
                            p.Activo = true;
                            p.UltModificacion = now;
                            p.UserName = "erp-sync";
                        }
                    }
                    else
                    {
                        p.Descripcion = desc;
                        p.Activo = true;
                        p.UltModificacion = now;
                        p.UserName = "erp-sync";
                    }

                    n++;
                }
            }
            catch (PostgresException ex)
            {
                _log.LogWarning(ex, "Skip catalogo parametro tipo={Tipo}", tipo);
            }

            await _db.SaveChangesAsync(ct);
        }

        return n;
    }

    private async Task<int> UpsertImagenMetaAsync(
        NpgsqlConnection conn, long idEmpresa, HashSet<long> artIds, DateTime now, CancellationToken ct)
    {
        // ERP guarda bytea en imagen; solo sincronizamos metadatos / nombre archivo como título.
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, idarticulo, nroorden, nombrearchivoimagen, activo, ultmodificacion, username
            FROM dbo.articuloimagen
            WHERE idarticulo = ANY(@ids) AND activo = true
            """, conn);
        cmd.Parameters.AddWithValue("ids", artIds.ToArray());

        var n = 0;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var id = r.GetInt64(r.GetOrdinal("id"));
            var img = await _db.ArticuloImagenes.FindAsync([id], ct);
            if (img is null)
            {
                img = new ArticuloImagen { ID = id, IDEmpresa = idEmpresa };
                _db.ArticuloImagenes.Add(img);
            }

            var file = ReadString(r, "nombrearchivoimagen");
            img.IDEmpresa = idEmpresa;
            img.IDArticulo = r.GetInt64(r.GetOrdinal("idarticulo"));
            img.Orden = r.IsDBNull(r.GetOrdinal("nroorden")) ? 0 : Convert.ToInt32(r.GetValue(r.GetOrdinal("nroorden")));
            img.Titulo = Trunc(file, 100);
            img.Url = string.IsNullOrWhiteSpace(file) ? $"/media/erp/id/{id}" : $"/media/erp/{file}";
            img.EsPrincipal = img.Orden <= 1;
            img.Activo = r.GetBoolean(r.GetOrdinal("activo"));
            img.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
            img.UserName = ReadString(r, "username") ?? "erp-sync";
            n++;
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    /// <summary>Precios e-com solo para embalajes de los artículos dados (lazy-pull).</summary>
    private async Task<int> UpsertEcommercePricesForArtsAsync(
        NpgsqlConnection conn, long idEmpresa, HashSet<long> artIds, DateTime now, CancellationToken ct)
    {
        var n = 0;
        // Cabeceras de listas ecom (mismas que sync batch).
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id, nrolistaprecio, descripcion, eslistaprecioecommerce, precioconimpuestos,
                                fecharegistro, fechainiciovigencia, fechafinvigencia, estado, activo,
                                ultmodificacion, username
                         FROM dbo.listaprecio
                         WHERE idempresa = @e AND (eslistaprecioecommerce = true OR @all)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", _config.GetValue("Erp:SyncAllPriceLists", false));
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var lp = await _db.ListasPrecio.FindAsync([id], ct);
                if (lp is null)
                {
                    lp = new ListaPrecio { ID = id, IDEmpresa = idEmpresa };
                    _db.ListasPrecio.Add(lp);
                    _acc?.NoteInsert();
                }
                else
                {
                    _acc?.NoteUpdate();
                }

                lp.IDEmpresa = idEmpresa;
                lp.NroListaPrecio = r.GetInt32(r.GetOrdinal("nrolistaprecio"));
                lp.Descripcion = Trunc(ReadString(r, "descripcion") ?? "", 255)!;
                ApplyListaEcommerceFlagsFromErp(lp, r.GetBoolean(r.GetOrdinal("eslistaprecioecommerce")),
                    r.IsDBNull(r.GetOrdinal("estado")) ? 1 : Convert.ToInt32(r.GetValue(r.GetOrdinal("estado"))));
                lp.PrecioConImpuestos = r.GetBoolean(r.GetOrdinal("precioconimpuestos"));
                lp.FechaRegistro = ReadDateTime(r, "fecharegistro") ?? now;
                lp.FechaInicioVigencia = ReadDateTime(r, "fechainiciovigencia");
                lp.FechaFinVigencia = ReadDateTime(r, "fechafinvigencia");
                lp.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                lp.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                if (!IsLocalEcommerceListaPromotion(lp))
                    lp.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT d.id, d.idlistaprecio, d.idarticuloembalaje, d.preciocontado, d.preciocheque,
                                d.preciocredito, d.cantidadminimamayorista, d.pordescuentominorista,
                                d.pordescuentomayorista, d.esautomatico, d.activo, d.ultmodificacion, d.username
                         FROM dbo.detallelistapreciocondicionventa d
                         JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
                         JOIN dbo.articuloembalaje ae ON ae.id = d.idarticuloembalaje
                         WHERE l.idempresa = @e AND (l.eslistaprecioecommerce = true OR @all)
                           AND ae.idarticulo = ANY(@arts)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", _config.GetValue("Erp:SyncAllPriceLists", false));
            cmd.Parameters.AddWithValue("arts", artIds.ToArray());
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var d = await _db.DetallesListaPrecioCondicionVenta.FindAsync([id], ct);
                if (d is null)
                {
                    d = new DetalleListaPrecioCondicionVenta { ID = id, IDEmpresa = idEmpresa };
                    _db.DetallesListaPrecioCondicionVenta.Add(d);
                    _acc?.NoteInsert();
                }
                else
                {
                    _acc?.NoteUpdate();
                }

                d.IDEmpresa = idEmpresa;
                d.IDListaPrecio = r.GetInt64(r.GetOrdinal("idlistaprecio"));
                d.IDArticuloEmbalaje = r.GetInt64(r.GetOrdinal("idarticuloembalaje"));
                d.PrecioContado = r.GetDecimal(r.GetOrdinal("preciocontado"));
                d.PrecioCheque = r.GetDecimal(r.GetOrdinal("preciocheque"));
                d.PrecioCredito = r.GetDecimal(r.GetOrdinal("preciocredito"));
                d.CantidadMinimaMayorista = r.GetDecimal(r.GetOrdinal("cantidadminimamayorista"));
                d.PorDescuentoMinorista = r.GetDecimal(r.GetOrdinal("pordescuentominorista"));
                d.PorDescuentoMayorista = r.GetDecimal(r.GetOrdinal("pordescuentomayorista"));
                d.EsAutomatico = !r.IsDBNull(r.GetOrdinal("esautomatico")) && r.GetBoolean(r.GetOrdinal("esautomatico"));
                d.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                d.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                d.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT d.id, d.idlistaprecio, d.idarticuloembalaje, d.precio,
                                d.activo, d.ultmodificacion, d.username
                         FROM dbo.detallelistaprecio d
                         JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
                         JOIN dbo.articuloembalaje ae ON ae.id = d.idarticuloembalaje
                         WHERE l.idempresa = @e AND (l.eslistaprecioecommerce = true OR @all)
                           AND ae.idarticulo = ANY(@arts)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", _config.GetValue("Erp:SyncAllPriceLists", false));
            cmd.Parameters.AddWithValue("arts", artIds.ToArray());
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var d = await _db.DetallesListaPrecio.FindAsync([id], ct);
                if (d is null)
                {
                    d = new DetalleListaPrecio { ID = id, IDEmpresa = idEmpresa };
                    _db.DetallesListaPrecio.Add(d);
                    _acc?.NoteInsert();
                }
                else
                {
                    _acc?.NoteUpdate();
                }

                d.IDEmpresa = idEmpresa;
                d.IDListaPrecio = r.GetInt64(r.GetOrdinal("idlistaprecio"));
                d.IDArticuloEmbalaje = r.GetInt64(r.GetOrdinal("idarticuloembalaje"));
                d.Precio = r.GetDecimal(r.GetOrdinal("precio"));
                d.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                d.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                d.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task<int> UpsertStockForArtsAsync(
        NpgsqlConnection conn, long idEmpresa, List<long> artIds, DateTime now, CancellationToken ct)
    {
        if (artIds.Count == 0) return 0;

        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var idDeposito = cfg?.IDDeposito ?? 1;
        await EnsureDepositoAsync(conn, idEmpresa, idDeposito, now, ct);

        var n = await PullStockForDepositoAsync(conn, idEmpresa, idDeposito, artIds, now, ct);
        if (n > 0) return n;

        // Fallback: cualquier depósito con stock de esos arts
        await using var diag = new NpgsqlCommand(
            """
            SELECT iddeposito, COUNT(*)::int AS n
            FROM dbo.stock
            WHERE idarticulo = ANY(@arts) AND activo = true
            GROUP BY iddeposito
            ORDER BY n DESC
            LIMIT 1
            """, conn);
        diag.Parameters.AddWithValue("arts", artIds.ToArray());
        var best = await diag.ExecuteScalarAsync(ct);
        if (best is null || best is DBNull) return 0;

        var altDep = Convert.ToInt64(best);
        if (altDep == idDeposito) return 0;
        await EnsureDepositoAsync(conn, idEmpresa, altDep, now, ct);
        return await PullStockForDepositoAsync(conn, idEmpresa, altDep, artIds, now, ct);
    }

    private async Task<int> SyncPricesAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        var n = 0;
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id, nrolistaprecio, descripcion, eslistaprecioecommerce, precioconimpuestos,
                                fecharegistro, fechainiciovigencia, fechafinvigencia, estado, activo,
                                ultmodificacion, username
                         FROM dbo.listaprecio
                         WHERE idempresa = @e AND (eslistaprecioecommerce = true OR @all)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", _config.GetValue("Erp:SyncAllPriceLists", false));
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var lp = await _db.ListasPrecio.FindAsync([id], ct);
                if (lp is null)
                {
                    lp = new ListaPrecio { ID = id, IDEmpresa = idEmpresa };
                    _db.ListasPrecio.Add(lp);
                }

                lp.IDEmpresa = idEmpresa;
                lp.NroListaPrecio = r.GetInt32(r.GetOrdinal("nrolistaprecio"));
                lp.Descripcion = Trunc(ReadString(r, "descripcion") ?? "", 255)!;
                ApplyListaEcommerceFlagsFromErp(lp, r.GetBoolean(r.GetOrdinal("eslistaprecioecommerce")),
                    r.IsDBNull(r.GetOrdinal("estado")) ? 1 : Convert.ToInt32(r.GetValue(r.GetOrdinal("estado"))));
                lp.PrecioConImpuestos = r.GetBoolean(r.GetOrdinal("precioconimpuestos"));
                lp.FechaRegistro = ReadDateTime(r, "fecharegistro") ?? now;
                lp.FechaInicioVigencia = ReadDateTime(r, "fechainiciovigencia");
                lp.FechaFinVigencia = ReadDateTime(r, "fechafinvigencia");
                lp.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                lp.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                if (!IsLocalEcommerceListaPromotion(lp))
                    lp.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT d.id, d.idlistaprecio, d.idarticuloembalaje, d.preciocontado, d.preciocheque,
                                d.preciocredito, d.cantidadminimamayorista, d.pordescuentominorista,
                                d.pordescuentomayorista, d.esautomatico, d.activo, d.ultmodificacion, d.username
                         FROM dbo.detallelistapreciocondicionventa d
                         JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
                         WHERE l.idempresa = @e AND (l.eslistaprecioecommerce = true OR @all)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", _config.GetValue("Erp:SyncAllPriceLists", false));
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var d = await _db.DetallesListaPrecioCondicionVenta.FindAsync([id], ct);
                if (d is null)
                {
                    d = new DetalleListaPrecioCondicionVenta { ID = id, IDEmpresa = idEmpresa };
                    _db.DetallesListaPrecioCondicionVenta.Add(d);
                }

                d.IDEmpresa = idEmpresa;
                d.IDListaPrecio = r.GetInt64(r.GetOrdinal("idlistaprecio"));
                d.IDArticuloEmbalaje = r.GetInt64(r.GetOrdinal("idarticuloembalaje"));
                d.PrecioContado = r.GetDecimal(r.GetOrdinal("preciocontado"));
                d.PrecioCheque = r.GetDecimal(r.GetOrdinal("preciocheque"));
                d.PrecioCredito = r.GetDecimal(r.GetOrdinal("preciocredito"));
                d.CantidadMinimaMayorista = r.GetDecimal(r.GetOrdinal("cantidadminimamayorista"));
                d.PorDescuentoMinorista = r.GetDecimal(r.GetOrdinal("pordescuentominorista"));
                d.PorDescuentoMayorista = r.GetDecimal(r.GetOrdinal("pordescuentomayorista"));
                d.EsAutomatico = !r.IsDBNull(r.GetOrdinal("esautomatico")) && r.GetBoolean(r.GetOrdinal("esautomatico"));
                d.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                d.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                d.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT d.id, d.idlistaprecio, d.idarticuloembalaje, d.precio,
                                d.activo, d.ultmodificacion, d.username
                         FROM dbo.detallelistaprecio d
                         JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
                         WHERE l.idempresa = @e AND (l.eslistaprecioecommerce = true OR @all)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("all", _config.GetValue("Erp:SyncAllPriceLists", false));
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var d = await _db.DetallesListaPrecio.FindAsync([id], ct);
                if (d is null)
                {
                    d = new DetalleListaPrecio { ID = id, IDEmpresa = idEmpresa };
                    _db.DetallesListaPrecio.Add(d);
                }

                d.IDEmpresa = idEmpresa;
                d.IDListaPrecio = r.GetInt64(r.GetOrdinal("idlistaprecio"));
                d.IDArticuloEmbalaje = r.GetInt64(r.GetOrdinal("idarticuloembalaje"));
                d.Precio = r.GetDecimal(r.GetOrdinal("precio"));
                d.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                d.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                d.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task<int> SyncStockAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var idDepositoCfg = cfg?.IDDeposito ?? 1;

        var artIds = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo)
            .Select(a => a.ID)
            .ToListAsync(ct);
        if (artIds.Count == 0)
            return 0;

        // Alinear vitrina multi-sucursal: pull todos los depósitos ecommerce / retiro.
        var depIds = await _db.Agencias.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa && a.Activo && a.EsSucursalEcommerce && a.IDDeposito != null)
            .Select(a => a.IDDeposito!.Value)
            .Distinct()
            .ToListAsync(ct);

        var ecomDeps = await _db.Depositos.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.Activo && d.EsDepositoEcommerce)
            .Select(d => d.ID)
            .ToListAsync(ct);

        foreach (var d in ecomDeps)
        {
            if (!depIds.Contains(d))
                depIds.Add(d);
        }

        if (idDepositoCfg > 0 && !depIds.Contains(idDepositoCfg))
            depIds.Add(idDepositoCfg);

        if (depIds.Count == 0)
            depIds.Add(idDepositoCfg);

        var total = 0;
        foreach (var idDeposito in depIds)
        {
            await EnsureDepositoAsync(conn, idEmpresa, idDeposito, now, ct);
            total += await PullStockForDepositoAsync(conn, idEmpresa, idDeposito, artIds, now, ct);
        }

        if (total > 0)
            return total;

        // Fallback: depósito con más filas en ERP si los e-com no tienen stock espejado.
        var alts = new List<(long Dep, int N)>();
        await using (var diag = new NpgsqlCommand(
                         """
                         SELECT iddeposito, COUNT(*)::int AS n
                         FROM dbo.stock
                         WHERE idarticulo = ANY(@arts) AND activo = true
                         GROUP BY iddeposito
                         ORDER BY n DESC
                         LIMIT 5
                         """, conn))
        {
            diag.Parameters.AddWithValue("arts", artIds.ToArray());
            await using var r = await diag.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                alts.Add((r.GetInt64(0), r.GetInt32(1)));
        }

        if (alts.Count == 0)
        {
            _log.LogWarning(
                "Stock sync: 0 filas en ERP para deps e-com [{Deps}] y arts={Arts} (empresa {E}).",
                string.Join(",", depIds), artIds.Count, idEmpresa);
            return 0;
        }

        var best = alts[0];
        if (depIds.Contains(best.Dep))
            return total;

        _log.LogWarning(
            "Stock sync: deps e-com sin filas útiles. Alternativas ERP: {Alts}. Usando {Best} ({N} filas).",
            string.Join(", ", alts.Select(a => $"{a.Dep}:{a.N}")),
            best.Dep, best.N);

        await EnsureDepositoAsync(conn, idEmpresa, best.Dep, now, ct);
        var cfgTracked = await _db.EmpresaConfiguracionesEcommerce
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (cfgTracked is not null && cfgTracked.IDDeposito != best.Dep)
        {
            cfgTracked.IDDeposito = best.Dep;
            cfgTracked.UltModificacion = now;
            cfgTracked.UserName = "erp-sync";
            await _db.SaveChangesAsync(ct);
            _log.LogInformation(
                "Stock sync: IDDeposito empresa {E} actualizado {Old} → {New} (depósito con stock ERP).",
                idEmpresa, idDepositoCfg, best.Dep);
        }

        return await PullStockForDepositoAsync(conn, idEmpresa, best.Dep, artIds, now, ct);
    }

    private async Task<int> PullStockForDepositoAsync(
        NpgsqlConnection conn, long idEmpresa, long idDeposito, List<long> artIds, DateTime now, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, iddeposito, idarticulo, idcontenedor, idlotearticulo,
                   totalexistencia, totalpedidoventa, totalreserva, totalventa,
                   activo, ultmodificacion, username
            FROM dbo.stock
            WHERE iddeposito = @dep AND idarticulo = ANY(@arts)
            """, conn);
        cmd.Parameters.AddWithValue("dep", idDeposito);
        cmd.Parameters.AddWithValue("arts", artIds.ToArray());

        var n = 0;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var id = r.GetInt64(r.GetOrdinal("id"));
            var s = await _db.Stocks.FindAsync([id], ct);
            if (s is null)
            {
                s = new Stock { ID = id, IDEmpresa = idEmpresa };
                _db.Stocks.Add(s);
                _acc?.NoteInsert();
            }
            else
            {
                _acc?.NoteUpdate();
            }

            s.IDEmpresa = idEmpresa;
            s.IDDeposito = r.GetInt64(r.GetOrdinal("iddeposito"));
            s.IDArticulo = r.GetInt64(r.GetOrdinal("idarticulo"));
            s.IDContenedor = ReadLongNull(r, "idcontenedor");
            s.IDLoteArticulo = ReadLongNull(r, "idlotearticulo");
            s.TotalExistencia = r.IsDBNull(r.GetOrdinal("totalexistencia")) ? 0 : Convert.ToDouble(r.GetValue(r.GetOrdinal("totalexistencia")));
            s.TotalPedidoVenta = r.IsDBNull(r.GetOrdinal("totalpedidoventa")) ? 0 : Convert.ToDouble(r.GetValue(r.GetOrdinal("totalpedidoventa")));
            s.TotalReserva = r.IsDBNull(r.GetOrdinal("totalreserva"))
                ? 0
                : Convert.ToDecimal(r.GetValue(r.GetOrdinal("totalreserva")));
            s.TotalVenta = r.IsDBNull(r.GetOrdinal("totalventa")) ? 0 : Convert.ToDouble(r.GetValue(r.GetOrdinal("totalventa")));
            s.Activo = r.GetBoolean(r.GetOrdinal("activo"));
            s.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
            s.UserName = ReadString(r, "username") ?? "erp-sync";
            n++;
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task EnsureDepositoAsync(
        NpgsqlConnection conn, long idEmpresa, long idDeposito, DateTime now, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT d.id, d.coddeposito, d.descripciondeposito, d.idtipodeposito, d.activo,
                   d.ultmodificacion, d.username,
                   t.codtipodeposito, t.descripciontipodeposito, t.normal, t.facturable
            FROM dbo.deposito d
            LEFT JOIN dbo.tipodeposito t ON t.id = d.idtipodeposito
            WHERE d.id = @id
            """, conn);
        cmd.Parameters.AddWithValue("id", idDeposito);

        long idTipo;
        int? codTipo;
        string? descTipo;
        bool? tipoNormal;
        bool? tipoFacturable;
        int? codDep;
        string? descDep;
        bool activo;
        DateTime? ult;
        string? user;
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct))
                return;

            idTipo = ReadLong(r, "idtipodeposito");
            codTipo = r.IsDBNull(r.GetOrdinal("codtipodeposito"))
                ? null
                : Convert.ToInt32(r.GetValue(r.GetOrdinal("codtipodeposito")));
            descTipo = ReadString(r, "descripciontipodeposito");
            tipoNormal = r.IsDBNull(r.GetOrdinal("normal")) ? null : r.GetBoolean(r.GetOrdinal("normal"));
            tipoFacturable = r.IsDBNull(r.GetOrdinal("facturable")) ? null : r.GetBoolean(r.GetOrdinal("facturable"));
            codDep = r.IsDBNull(r.GetOrdinal("coddeposito"))
                ? null
                : Convert.ToInt32(r.GetValue(r.GetOrdinal("coddeposito")));
            descDep = ReadString(r, "descripciondeposito");
            activo = r.GetBoolean(r.GetOrdinal("activo"));
            ult = ReadDateTime(r, "ultmodificacion");
            user = ReadString(r, "username");
        }

        long resolvedTipoId = 0;
        if (idTipo > 0)
        {
            var tipo = await _db.TiposDeposito.FindAsync([idTipo], ct);
            if (tipo is null && codTipo is int c)
            {
                // Ya puede existir un tipo seed/local con mismo código y otro ID.
                tipo = await _db.TiposDeposito.FirstOrDefaultAsync(
                    t => t.IDEmpresa == idEmpresa && t.CodTipoDeposito == c, ct);
            }

            if (tipo is null)
            {
                tipo = new TipoDeposito { ID = idTipo, IDEmpresa = idEmpresa };
                _db.TiposDeposito.Add(tipo);
            }

            tipo.IDEmpresa = idEmpresa;
            tipo.CodTipoDeposito = codTipo ?? tipo.CodTipoDeposito;
            if (tipo.CodTipoDeposito <= 0)
                tipo.CodTipoDeposito = 1;
            tipo.DescripcionTipoDeposito = Trunc(descTipo ?? "Normal", 100)!;
            tipo.Normal = tipoNormal ?? true;
            tipo.Facturable = tipoFacturable ?? true;
            tipo.Activo = true;
            tipo.UltModificacion = now;
            tipo.UserName = "erp-sync";
            resolvedTipoId = tipo.ID;
        }

        var dep = await _db.Depositos.FindAsync([idDeposito], ct);
        if (dep is null)
        {
            dep = new Deposito { ID = idDeposito, IDEmpresa = idEmpresa };
            _db.Depositos.Add(dep);
        }

        dep.IDEmpresa = idEmpresa;
        dep.CodDeposito = codDep ?? (int)idDeposito;
        dep.DescripcionDeposito = Trunc(descDep ?? $"Dep {idDeposito}", 100)!;
        if (resolvedTipoId > 0)
            dep.IDTipoDeposito = resolvedTipoId;
        else if (idTipo > 0)
            dep.IDTipoDeposito = idTipo;
        dep.EsDepositoEcommerce = true;
        dep.Activo = activo;
        dep.UltModificacion = ult ?? now;
        dep.UserName = user ?? "erp-sync";

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncCustomersAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        var n = 0;

        // Tipos documento usados
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT DISTINCT t.id, t.codtipodocumento, t.descripciontipodocumento, t.esruc, t.activo
                         FROM dbo.tipodocumento t
                         JOIN dbo.usuarioecommerce u ON u.idtipodocumento = t.id
                         WHERE u.idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            try
            {
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    var id = r.GetInt64(0);
                    var t = await _db.TiposDocumento.FindAsync([id], ct);
                    if (t is null)
                    {
                        t = new TipoDocumento { ID = id, IDEmpresa = idEmpresa };
                        _db.TiposDocumento.Add(t);
                    }

                    t.IDEmpresa = idEmpresa;
                    t.CodTipoDocumento = r.IsDBNull(1) ? 1 : Convert.ToInt32(r.GetValue(1));
                    t.DescripcionTipoDocumento = Trunc(r.IsDBNull(2) ? "Doc" : r.GetString(2), 100)!;
                    t.EsRuc = !r.IsDBNull(3) && r.GetBoolean(3);
                    t.Activo = r.IsDBNull(4) || r.GetBoolean(4);
                    t.UltModificacion = now;
                    t.UserName = "erp-sync";
                    n++;
                }
            }
            catch (PostgresException ex)
            {
                _log.LogWarning(ex, "tipodocumento sync parcial");
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id, codigousuario, usuario, password, nombre, apellido, idtipodocumento,
                                numerodocumento, email, numerotelefono, numeromovil, fecharegistro,
                                activado, bloqueado, codigoactivacion, checksumactivacion, checksum,
                                activo, ultmodificacion, username
                         FROM dbo.usuarioecommerce
                         WHERE idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var u = await _db.UsuariosEcommerce.FindAsync([id], ct);
                if (u is null)
                {
                    u = new UsuarioEcommerce { ID = id, IDEmpresa = idEmpresa };
                    _db.UsuariosEcommerce.Add(u);
                }

                u.IDEmpresa = idEmpresa;
                u.CodigoUsuario = ReadString(r, "codigousuario") ?? id.ToString();
                u.Usuario = ReadString(r, "usuario") ?? id.ToString();
                u.Password = ReadString(r, "password") ?? "";
                u.Nombre = ReadString(r, "nombre") ?? "";
                u.Apellido = ReadString(r, "apellido") ?? "";
                u.IDTipoDocumento = ReadLongNull(r, "idtipodocumento");
                u.NumeroDocumento = ReadString(r, "numerodocumento");
                u.Email = ReadString(r, "email");
                u.NumeroTelefono = ReadString(r, "numerotelefono");
                u.NumeroMovil = ReadString(r, "numeromovil");
                u.FechaRegistro = ReadDateTime(r, "fecharegistro") ?? now;
                u.Activado = !r.IsDBNull(r.GetOrdinal("activado")) && r.GetBoolean(r.GetOrdinal("activado"));
                u.Bloqueado = !r.IsDBNull(r.GetOrdinal("bloqueado")) && r.GetBoolean(r.GetOrdinal("bloqueado"));
                u.CodigoActivacion = ReadString(r, "codigoactivacion");
                u.ChecksumActivacion = ReadString(r, "checksumactivacion");
                u.CheckSum = ReadString(r, "checksum");
                u.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                u.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                u.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT c.id, c.nrocuenta, c.razonsocial, c.nombrecuenta, c.apellidocuenta,
                                c.direccion, c.email, c.idciudad, c.idbarrio, c.iddivisionpolitica,
                                c.idlistaprecio, c.idagencia, c.latitud, c.longitud,
                                c.activo, c.ultmodificacion, c.username
                         FROM dbo.cuenta c
                         WHERE c.id IN (
                           SELECT uc.idcuenta FROM dbo.usuarioecommercecuenta uc
                           JOIN dbo.usuarioecommerce u ON u.id = uc.idusuarioecommerce
                           WHERE u.idempresa = @e
                         ) OR c.idempresa = @e AND c.id IN (
                           SELECT idcuenta FROM dbo.carritocompra WHERE idempresa = @e
                         )
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var c = await _db.Cuentas.FindAsync([id], ct);
                if (c is null)
                {
                    c = new Cuenta { ID = id, IDEmpresa = idEmpresa };
                    _db.Cuentas.Add(c);
                }

                c.IDEmpresa = idEmpresa;
                c.NroCuenta = r.IsDBNull(r.GetOrdinal("nrocuenta"))
                    ? id
                    : Convert.ToInt64(r.GetValue(r.GetOrdinal("nrocuenta")));
                c.RazonSocial = Trunc(ReadString(r, "razonsocial") ?? "", 255)!;
                c.NombreCuenta = ReadString(r, "nombrecuenta");
                c.ApellidoCuenta = ReadString(r, "apellidocuenta");
                c.Direccion = ReadString(r, "direccion");
                c.Email = ReadString(r, "email");
                c.IDCiudad = ReadLongNull(r, "idciudad");
                c.IDBarrio = ReadLongNull(r, "idbarrio");
                c.IDDivisionPolitica = ReadLongNull(r, "iddivisionpolitica");
                c.IDListaPrecio = ReadLongNull(r, "idlistaprecio");
                c.IDAgencia = ReadLongNull(r, "idagencia");
                c.Latitud = ReadDecimalNull(r, "latitud");
                c.Longitud = ReadDecimalNull(r, "longitud");
                c.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                c.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                c.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT uc.id, uc.idusuarioecommerce, uc.idcuenta,
                                uc.activo, uc.ultmodificacion, uc.username, uc.idempresa
                         FROM dbo.usuarioecommercecuenta uc
                         JOIN dbo.usuarioecommerce u ON u.id = uc.idusuarioecommerce
                         WHERE u.idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var link = await _db.UsuariosEcommerceCuenta.FindAsync([id], ct);
                if (link is null)
                {
                    link = new UsuarioEcommerceCuenta { ID = id, IDEmpresa = idEmpresa };
                    _db.UsuariosEcommerceCuenta.Add(link);
                }

                link.IDEmpresa = idEmpresa;
                link.IDUsuarioEcommerce = r.GetInt64(r.GetOrdinal("idusuarioecommerce"));
                link.IDCuenta = r.GetInt64(r.GetOrdinal("idcuenta"));
                link.EsPrincipal = true;
                link.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                link.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                link.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        // listapreciocuenta: después de cuentas + listas (prices).
        var listaIds = await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa)
            .Select(l => l.ID)
            .ToListAsync(ct);
        var cuentaIds = await _db.Cuentas.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .Select(c => c.ID)
            .ToListAsync(ct);
        var listaSet = listaIds.ToHashSet();
        var cuentaSet = cuentaIds.ToHashSet();

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT lpc.id, lpc.idlistaprecio, lpc.idcuenta, lpc.activo,
                                lpc.ultmodificacion, lpc.username
                         FROM dbo.listapreciocuenta lpc
                         WHERE lpc.idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var idLista = r.GetInt64(r.GetOrdinal("idlistaprecio"));
                var idCuenta = r.GetInt64(r.GetOrdinal("idcuenta"));
                if (!listaSet.Contains(idLista) || !cuentaSet.Contains(idCuenta))
                    continue;

                var id = r.GetInt64(r.GetOrdinal("id"));
                var row = await _db.ListasPrecioCuenta.FindAsync([id], ct);
                if (row is null)
                {
                    row = new ListaPrecioCuenta { ID = id, IDEmpresa = idEmpresa };
                    _db.ListasPrecioCuenta.Add(row);
                    _acc?.NoteInsert();
                }
                else
                {
                    _acc?.NoteUpdate();
                }

                row.IDEmpresa = idEmpresa;
                row.IDListaPrecio = idLista;
                row.IDCuenta = idCuenta;
                row.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                row.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                row.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task<int> SyncShippingAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        var n = 0;
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id, codagencia, descripcionagencia, direccionagencia, idciudad,
                                latitud, longitud, activo, ultmodificacion, username
                         FROM dbo.agencia
                         WHERE idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var a = await _db.Agencias.FindAsync([id], ct);
                if (a is null)
                {
                    a = new Agencia { ID = id, IDEmpresa = idEmpresa };
                    _db.Agencias.Add(a);
                }

                a.IDEmpresa = idEmpresa;
                a.CodAgencia = r.IsDBNull(r.GetOrdinal("codagencia"))
                    ? (int)id
                    : Convert.ToInt32(r.GetValue(r.GetOrdinal("codagencia")));
                a.DescripcionAgencia = Trunc(ReadString(r, "descripcionagencia") ?? "", 100)!;
                a.Direccion = ReadString(r, "direccionagencia");
                a.IDCiudad = ReadLongNull(r, "idciudad");
                a.Latitud = ReadDecimalNull(r, "latitud");
                a.Longitud = ReadDecimalNull(r, "longitud");
                a.EsSucursalEcommerce = true;
                a.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                a.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                a.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT h.id, h.idagencia, h.diasemana, h.horaprimeraapertura, h.horaprimercierre,
                                h.horasegundaapertura, h.horasegundocierre, h.activo, h.ultmodificacion, h.username
                         FROM dbo.horarioatencion h
                         JOIN dbo.agencia a ON a.id = h.idagencia
                         WHERE a.idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            try
            {
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    var id = r.GetInt64(r.GetOrdinal("id"));
                    var h = await _db.HorariosAtencion.FindAsync([id], ct);
                    if (h is null)
                    {
                        h = new HorarioAtencion { ID = id, IDEmpresa = idEmpresa };
                        _db.HorariosAtencion.Add(h);
                    }

                    h.IDEmpresa = idEmpresa;
                    h.IDAgencia = r.GetInt64(r.GetOrdinal("idagencia"));
                    h.DiaSemana = Convert.ToByte(r.GetValue(r.GetOrdinal("diasemana")));
                    h.HoraPrimeraApertura = ReadTime(r, "horaprimeraapertura");
                    h.HoraPrimerCierre = ReadTime(r, "horaprimercierre");
                    h.HoraSegundaApertura = ReadTime(r, "horasegundaapertura");
                    h.HoraSegundoCierre = ReadTime(r, "horasegundocierre");
                    h.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                    h.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                    h.UserName = ReadString(r, "username") ?? "erp-sync";
                    n++;
                }
            }
            catch (PostgresException ex)
            {
                _log.LogWarning(ex, "horarioatencion sync skipped/partial");
            }
        }

        await _db.SaveChangesAsync(ct);

        // Recolectar geo IDs de tarifas y crear stubs locales (cadena barrio→distrito→…).
        var geoRows = new List<(long Div, long Ciudad, long Barrio, string? NomCiudad, string? NomBarrio)>();
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT DISTINCT t.iddivisionpolitica, t.idciudad, t.idbarrio,
                                c.descripcionciudad, b.descripcionbarrio
                         FROM dbo.tarifadelivery t
                         LEFT JOIN dbo.ciudad c ON c.id = t.idciudad
                         LEFT JOIN dbo.barrio b ON b.id = t.idbarrio
                         WHERE t.idempresa = @e OR t.idagencia IN (SELECT id FROM dbo.agencia WHERE idempresa = @e)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                geoRows.Add((
                    ReadLong(r, "iddivisionpolitica"),
                    ReadLong(r, "idciudad"),
                    ReadLong(r, "idbarrio"),
                    ReadString(r, "descripcionciudad"),
                    ReadString(r, "descripcionbarrio")));
            }
        }

        foreach (var g in geoRows)
            await EnsureGeoStubAsync(g.Div, g.Ciudad, g.Barrio, g.NomCiudad, g.NomBarrio, now, ct);

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT t.id, t.iddivisionpolitica, t.idciudad, t.idbarrio, t.fechainiciovigencia, t.fechafinvigencia,
                                t.importedelivery, t.estado, t.idagencia, t.activo, t.ultmodificacion, t.username,
                                c.descripcionciudad, b.descripcionbarrio
                         FROM dbo.tarifadelivery t
                         LEFT JOIN dbo.ciudad c ON c.id = t.idciudad
                         LEFT JOIN dbo.barrio b ON b.id = t.idbarrio
                         WHERE t.idempresa = @e OR t.idagencia IN (SELECT id FROM dbo.agencia WHERE idempresa = @e)
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var t = await _db.TarifasDelivery.FindAsync([id], ct);
                if (t is null)
                {
                    t = new TarifaDelivery { ID = id, IDEmpresa = idEmpresa };
                    _db.TarifasDelivery.Add(t);
                }

                t.IDEmpresa = idEmpresa;
                t.IDDivisionPolitica = ReadLong(r, "iddivisionpolitica");
                if (t.IDDivisionPolitica <= 0) t.IDDivisionPolitica = 1;
                t.IDCiudad = ReadLong(r, "idciudad");
                t.IDBarrio = ReadLong(r, "idbarrio");
                t.FechaInicioVigencia = ReadDateTime(r, "fechainiciovigencia") ?? now;
                t.FechaFinVigencia = ReadDateTime(r, "fechafinvigencia") ?? now.AddYears(1);
                t.ImporteDelivery = r.IsDBNull(r.GetOrdinal("importedelivery"))
                    ? 0
                    : r.GetDecimal(r.GetOrdinal("importedelivery"));
                t.Estado = r.IsDBNull(r.GetOrdinal("estado")) ? 1 : Convert.ToInt32(r.GetValue(r.GetOrdinal("estado")));
                t.IDAgencia = ReadLongNull(r, "idagencia");
                t.NombreCiudad = Trunc(ReadString(r, "descripcionciudad") ?? $"Ciudad {t.IDCiudad}", 100)!;
                t.NombreBarrio = Trunc(ReadString(r, "descripcionbarrio") ?? $"Barrio {t.IDBarrio}", 100)!;
                t.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                t.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                t.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task EnsureGeoStubAsync(
        long idDiv, long idCiudad, long idBarrio,
        string? nomCiudad, string? nomBarrio, DateTime now, CancellationToken ct)
    {
        if (idDiv <= 0) idDiv = 1;
        if (idCiudad <= 0 || idBarrio <= 0)
            return;

        var div = await _db.DivisionesPoliticas.FindAsync([idDiv], ct);
        if (div is null)
        {
            div = new DivisionPolitica
            {
                ID = idDiv,
                CodDivisionPolitica = (int)Math.Min(idDiv, int.MaxValue),
                DescripcionDivisionPolitica = $"Div {idDiv}",
                NombreAbreviado = "XX",
                Activo = true,
                UltModificacion = now,
                UserName = "erp-sync"
            };
            _db.DivisionesPoliticas.Add(div);
            await _db.SaveChangesAsync(ct);
        }

        var ciudad = await _db.Ciudades.FindAsync([idCiudad], ct);
        if (ciudad is null)
        {
            ciudad = new Ciudad
            {
                ID = idCiudad,
                CodCiudad = (int)Math.Min(idCiudad, int.MaxValue),
                DescripcionCiudad = Trunc(nomCiudad ?? $"Ciudad {idCiudad}", 100)!,
                IDDivisionPolitica = idDiv,
                Activo = true,
                UltModificacion = now,
                UserName = "erp-sync"
            };
            _db.Ciudades.Add(ciudad);
            await _db.SaveChangesAsync(ct);
        }

        var barrio = await _db.Barrios.FindAsync([idBarrio], ct);
        if (barrio is not null)
            return;

        // Cadena mínima localidad→distrito→barrio con IDs sintéticos negativos evitados:
        // reutilizamos idBarrio para localidad/distrito stub únicos por barrio.
        var idLoc = 900_000_000L + idBarrio;
        var idDist = 910_000_000L + idBarrio;

        if (await _db.Localidades.FindAsync([idLoc], ct) is null)
        {
            _db.Localidades.Add(new Localidad
            {
                ID = idLoc,
                CodLocalidad = 1,
                DescripcionLocalidad = $"Loc {idBarrio}",
                IDCiudad = idCiudad,
                Activo = true,
                UltModificacion = now,
                UserName = "erp-sync"
            });
            await _db.SaveChangesAsync(ct);
        }

        if (await _db.Distritos.FindAsync([idDist], ct) is null)
        {
            _db.Distritos.Add(new Distrito
            {
                ID = idDist,
                CodDistrito = 1,
                DescripcionDistrito = $"Dist {idBarrio}",
                IDLocalidad = idLoc,
                Activo = true,
                UltModificacion = now,
                UserName = "erp-sync"
            });
            await _db.SaveChangesAsync(ct);
        }

        _db.Barrios.Add(new Barrio
        {
            ID = idBarrio,
            CodBarrio = (int)Math.Min(idBarrio, int.MaxValue),
            DescripcionBarrio = Trunc(nomBarrio ?? $"Barrio {idBarrio}", 100)!,
            IDDistrito = idDist,
            Activo = true,
            UltModificacion = now,
            UserName = "erp-sync"
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncPromotionsAsync(NpgsqlConnection conn, long idEmpresa, DateTime now, CancellationToken ct)
    {
        var n = 0;
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id, codgrupoarticulo, descripciongrupoarticulo, fechainiciovigencia, fechafinvigencia,
                                tipogrupo, imagen, estado, activo, ultmodificacion, username
                         FROM dbo.grupoarticuloecommerce
                         WHERE idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(r.GetOrdinal("id"));
                var g = await _db.GruposArticuloEcommerce.FindAsync([id], ct);
                if (g is null)
                {
                    g = new GrupoArticuloEcommerce { ID = id, IDEmpresa = idEmpresa };
                    _db.GruposArticuloEcommerce.Add(g);
                }

                g.IDEmpresa = idEmpresa;
                g.CodGrupoArticulo = r.IsDBNull(r.GetOrdinal("codgrupoarticulo"))
                    ? (int)id
                    : Convert.ToInt32(r.GetValue(r.GetOrdinal("codgrupoarticulo")));
                g.DescripcionGrupoArticulo = Trunc(ReadString(r, "descripciongrupoarticulo") ?? "", 200)!;
                g.FechaInicioVigencia = ReadDateTime(r, "fechainiciovigencia") ?? now;
                g.FechaFinVigencia = ReadDateTime(r, "fechafinvigencia") ?? now.AddYears(1);
                g.TipoGrupo = r.IsDBNull(r.GetOrdinal("tipogrupo")) ? 0 : Convert.ToInt32(r.GetValue(r.GetOrdinal("tipogrupo")));
                g.Imagen = ReadString(r, "imagen");
                g.Estado = r.IsDBNull(r.GetOrdinal("estado")) ? 1 : Convert.ToInt32(r.GetValue(r.GetOrdinal("estado")));
                g.Orden = 0;
                g.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                g.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                g.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT d.id, d.idgrupoarticuloecommerce, d.idarticulo, d.activo, d.ultmodificacion, d.username, d.idempresa
                         FROM dbo.detallegrupoarticuloecommerce d
                         JOIN dbo.grupoarticuloecommerce g ON g.id = d.idgrupoarticuloecommerce
                         WHERE g.idempresa = @e
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            var localArtIds = await _db.Articulos.AsNoTracking()
                .Where(a => a.IDEmpresa == idEmpresa)
                .Select(a => a.ID)
                .ToListAsync(ct);
            var artSet = localArtIds.ToHashSet();

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var idArt = r.GetInt64(r.GetOrdinal("idarticulo"));
                if (!artSet.Contains(idArt))
                    continue;

                var id = r.GetInt64(r.GetOrdinal("id"));
                var d = await _db.DetallesGrupoArticuloEcommerce.FindAsync([id], ct);
                if (d is null)
                {
                    d = new DetalleGrupoArticuloEcommerce { ID = id, IDEmpresa = idEmpresa };
                    _db.DetallesGrupoArticuloEcommerce.Add(d);
                }

                d.IDEmpresa = idEmpresa;
                d.IDGrupoArticuloEcommerce = r.GetInt64(r.GetOrdinal("idgrupoarticuloecommerce"));
                d.IDArticulo = idArt;
                d.Activo = r.GetBoolean(r.GetOrdinal("activo"));
                d.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                d.UserName = ReadString(r, "username") ?? "erp-sync";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private async Task<int> SyncOrderStatusAsync(
        NpgsqlConnection conn, long idEmpresa, SyncCursor cursor, DateTime now, CancellationToken ct)
    {
        // Solo metadatos de estado de pedidos ecom recientes (no reescribe detalle local).
        var since = cursor.LastWatermarkUtc ?? now.AddDays(-30);
        var n = 0;
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id, estado, migrado, nrofactura, tipofactura, ultmodificacion
                         FROM dbo.pedidomovil
                         WHERE idempresa = @e AND origenpedido = 2 AND ultmodificacion > @since
                         ORDER BY ultmodificacion
                         LIMIT 500
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("since", since);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(0);
                var local = await _db.PedidosMovil.FindAsync([id], ct);
                if (local is null)
                    continue;

                local.Estado = Convert.ToInt32(r.GetValue(1));
                local.Migrado = r.GetBoolean(2);
                if (!r.IsDBNull(3))
                    local.NroFactura = Convert.ToInt64(r.GetValue(3));
                if (!r.IsDBNull(4))
                    local.TipoFactura = Convert.ToInt32(r.GetValue(4));
                local.UltModificacion = ReadDateTime(r, "ultmodificacion") ?? now;
                local.UserName = "erp-sync";
                n++;
            }
        }

        // Facturas emitidas en dbo.pedido (nrofactura) aunque pedidomovil no se haya tocado.
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT pm.id, p.nrofactura, p.tipofactura,
                                COALESCE(p.ultmodificacion, pm.ultmodificacion) AS ult
                         FROM dbo.pedidomovil pm
                         INNER JOIN dbo.pedido p
                           ON p.idpedidomovil = pm.id AND p.idempresa = pm.idempresa
                         WHERE pm.idempresa = @e
                           AND pm.origenpedido = 2
                           AND p.nrofactura IS NOT NULL
                           AND COALESCE(p.ultmodificacion, pm.ultmodificacion) > @since
                         ORDER BY ult
                         LIMIT 500
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("since", since);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetInt64(0);
                var local = await _db.PedidosMovil.FindAsync([id], ct);
                if (local is null)
                    continue;

                if (!r.IsDBNull(1))
                    local.NroFactura = Convert.ToInt64(r.GetValue(1));
                if (!r.IsDBNull(2))
                    local.TipoFactura = Convert.ToInt32(r.GetValue(2));
                local.UltModificacion = ReadDateTime(r, "ult") ?? now;
                local.UserName = "erp-sync-factura";
                n++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return n;
    }

    private Task<string?> GetErpConnectionStringAsync(long idEmpresa, CancellationToken ct) =>
        _erpCs.ResolveAsync(idEmpresa, ct);

    private long ResolveTargetEmpresaId()
    {
        // Preferir tenant resuelto (job por empresa / request admin-vitrina).
        if (_tenant.IsResolved && _tenant.IDEmpresa.HasValue)
            return _tenant.IDEmpresa.Value;
        var configured = _config.GetValue<long?>("Erp:IDEmpresa");
        if (configured is > 0)
            return configured.Value;
        throw new InvalidOperationException("Configure Erp:IDEmpresa o resuelva tenant.");
    }

    private async Task<SyncCursor> UpsertCursorAsync(long idEmpresa, string entityKey, CancellationToken ct)
    {
        var cursor = await _db.SyncCursors
            .FirstOrDefaultAsync(c => c.IDEmpresa == idEmpresa && c.EntityKey == entityKey, ct);

        if (cursor is null)
        {
            cursor = new SyncCursor
            {
                IDEmpresa = idEmpresa,
                EntityKey = entityKey,
                Activo = true,
                UltModificacion = DateTime.UtcNow,
                UserName = "sync"
            };
            _db.SyncCursors.Add(cursor);
        }

        return cursor;
    }

    private async Task<IReadOnlyList<SyncCursorStatusDto>> LoadCursorsAsync(long idEmpresa, CancellationToken ct)
    {
        var existing = await _db.SyncCursors.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .ToListAsync(ct);

        var map = existing.ToDictionary(c => c.EntityKey, StringComparer.OrdinalIgnoreCase);
        return DefaultEntityKeys
            .Select(key => map.TryGetValue(key, out var c)
                ? MapCursor(c)
                : new SyncCursorStatusDto(key, null, null, null, null, null, 0))
            .ToList();
    }

    private static IReadOnlyList<string> NormalizeKeys(IReadOnlyList<string>? entityKeys)
    {
        if (entityKeys is null || entityKeys.Count == 0)
            return DefaultEntityKeys;

        return entityKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private static SyncCursorStatusDto MapCursor(SyncCursor c) =>
        new(c.EntityKey, c.LastWatermarkUtc, c.LastId, c.LastRunUtc,
            c.LastStatus, c.LastError, c.RowsProcessed,
            c.RowsRead, c.RowsInserted, c.RowsUpdated, c.DurationMs);

    private static string? ReadString(DbDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : Convert.ToString(r.GetValue(r.GetOrdinal(col)));

    private static long ReadLong(DbDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? 0 : Convert.ToInt64(r.GetValue(r.GetOrdinal(col)));

    private static long? ReadLongNull(DbDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : Convert.ToInt64(r.GetValue(r.GetOrdinal(col)));

    private static decimal? ReadDecimalNull(DbDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : Convert.ToDecimal(r.GetValue(r.GetOrdinal(col)));

    private static DateTime? ReadDateTime(DbDataReader r, string col)
    {
        if (r.IsDBNull(r.GetOrdinal(col))) return null;
        var v = r.GetValue(r.GetOrdinal(col));
        return v switch
        {
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeOffset dto => dto.UtcDateTime,
            _ => Convert.ToDateTime(v)
        };
    }

    private static TimeSpan ReadTime(DbDataReader r, string col)
    {
        if (r.IsDBNull(r.GetOrdinal(col))) return TimeSpan.Zero;
        var v = r.GetValue(r.GetOrdinal(col));
        return v switch
        {
            TimeSpan ts => ts,
            DateTime dt => dt.TimeOfDay,
            _ => TimeSpan.TryParse(Convert.ToString(v), out var t) ? t : TimeSpan.Zero
        };
    }

    private static TimeSpan? ReadTimeNull(DbDataReader r, string col)
    {
        if (r.IsDBNull(r.GetOrdinal(col))) return null;
        return ReadTime(r, col);
    }

    private static string? Trunc(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
