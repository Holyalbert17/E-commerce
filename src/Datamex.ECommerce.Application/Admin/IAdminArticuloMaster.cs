namespace Datamex.ECommerce.Application.Admin;

/// <summary>Detalle maestro (paridad ERP ArticuloDTO e-com).</summary>
public record AdminArticuloDetailDto(
    long Id,
    int NroArticulo,
    string? Sku,
    string Descripcion,
    string? DescripcionAbreviada,
    string? DescripcionExtensa,
    string? CodReferenciaProveedor,
    long IdMarca,
    string? NombreMarca,
    long? IdLinea,
    string? NombreLinea,
    long? IdTipoArticulo,
    string? NombreTipoArticulo,
    long? IdProveedor,
    string? NombreProveedor,
    long? IdPais,
    string? NombrePais,
    long? IdRegimenImpositivo,
    string? NombreRegimenImpositivo,
    bool Activo,
    bool ActivoEcommerce,
    string EstadoEcommerce,
    string? CodigoBarra,
    long? IdEmbalajeParametro,
    string? DescripcionEmbalaje,
    string CodigoUnidadMedida,
    double CantidadPorEmbalaje,
    long? IdEmbalajeArticulo,
    string? ImagenUrl,
    decimal StockExistencia,
    decimal? PrecioDesdeB2C,
    string? FichaTecnicaUrl,
    string? VideoUrl,
    string? SlugSeo = null,
    string OrigenDatos = "Local",
    bool IsReadOnly = false);

/// <summary>Alta artículo e-com (paridad AddArticuloECommerce).</summary>
public record CreateAdminArticuloRequest(
    string Sku,
    string Descripcion,
    string? CodReferenciaProveedor,
    long IdMarca,
    long IdLinea,
    long IdTipoArticulo,
    long IdProveedor,
    long IdEmbalaje,
    string CodigoUnidadMedida,
    double CantidadPorEmbalaje,
    long? IdPais = null,
    long? IdRegimenImpositivo = null,
    int? NroArticulo = null,
    string? CodigoBarra = null,
    string? ImagenUrl = null,
    string? DescripcionExtensa = null,
    string? FichaTecnicaUrl = null,
    string? VideoUrl = null,
    decimal? StockInicial = null,
    string? SlugSeo = null);

/// <summary>Edición completa del maestro (paridad EditArticuloECommerce).</summary>
public record UpsertAdminArticuloRequest(
    string Sku,
    string Descripcion,
    string? CodReferenciaProveedor,
    long IdMarca,
    long IdLinea,
    long IdTipoArticulo,
    long IdProveedor,
    long IdEmbalaje,
    string CodigoUnidadMedida,
    double CantidadPorEmbalaje,
    long? IdPais = null,
    long? IdRegimenImpositivo = null,
    int? NroArticulo = null,
    string? CodigoBarra = null,
    string? ImagenUrl = null,
    string? DescripcionAbreviada = null,
    string? DescripcionExtensa = null,
    string? FichaTecnicaUrl = null,
    string? VideoUrl = null,
    bool? Activo = null,
    bool? ActivoEcommerce = null,
    string? EstadoEcommerce = null,
    string? SlugSeo = null);

public record CatalogoParametroDto(
    long Id,
    string Tipo,
    int Codigo,
    string Descripcion,
    bool Activo);

public record UpsertCatalogoParametroRequest(
    string Tipo,
    string Descripcion,
    int? Codigo = null,
    bool Activo = true);

public interface IAdminParametrosProductoService
{
    Task<IReadOnlyList<CatalogoParametroDto>> ListAsync(string tipo, bool incluirInactivos = false, CancellationToken ct = default);
    Task<CatalogoParametroDto> CreateAsync(UpsertCatalogoParametroRequest request, CancellationToken ct = default);
    Task<CatalogoParametroDto> UpdateAsync(long id, UpsertCatalogoParametroRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
