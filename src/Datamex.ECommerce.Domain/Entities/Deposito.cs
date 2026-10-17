using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de tipodeposito (flag Normal = depósitos facturables/visibles en ecom).</summary>
public class TipoDeposito : EntidadConEmpresa
{
    public int CodTipoDeposito { get; set; }
    public string DescripcionTipoDeposito { get; set; } = string.Empty;
    public bool Normal { get; set; } = true;
    public bool Consolidado { get; set; } = true;
    public bool Facturable { get; set; } = true;
    public bool PermiteFacturarBajoCosto { get; set; }
    public bool Oferta { get; set; }
    public bool Remate { get; set; }
    public bool PuntoVenta { get; set; }

    public ICollection<Deposito> Depositos { get; set; } = new List<Deposito>();
}

/// <summary>Espejo de deposito. Ecom usa el de EmpresaConfiguracionEcommerce.IDDeposito.</summary>
public class Deposito : EntidadConEmpresa
{
    public int CodDeposito { get; set; }
    public long IDTipoDeposito { get; set; }
    public string DescripcionDeposito { get; set; } = string.Empty;
    public bool DepositoEntrega { get; set; } = true;
    public bool FacturacionMoviles { get; set; }
    public bool DepositoDelCliente { get; set; }

    /// <summary>Extensión ecom: marca depósito preferido de vitrina (además de config.IDDeposito).</summary>
    public bool EsDepositoEcommerce { get; set; } = true;

    public TipoDeposito? TipoDeposito { get; set; }
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}

/// <summary>
/// Espejo de stock (tabla ERP). Existencia ecom = SUM(TotalExistencia) por IDEmpresa+IDDeposito+IDArticulo.
/// Misma forma para levantar standalone o sincronizar/ETL desde ERP sin remapear columnas.
/// </summary>
public class Stock : EntidadConEmpresa
{
    public long IDDeposito { get; set; }
    public long IDArticulo { get; set; }
    public long? IDContenedor { get; set; }
    public long? IDLoteArticulo { get; set; }

    public double CostoGestionUltimo { get; set; }
    public double CostoGestionPromedio { get; set; }
    public double CostoGestionFIFO { get; set; }
    public double CostoGestionLIFO { get; set; }
    public double CostoGestionNIFO { get; set; }
    public double CostoUltimo { get; set; }
    public double CostoPromedio { get; set; }
    public double CostoFIFO { get; set; }
    public double CostoLIFO { get; set; }
    public double CostoNIFO { get; set; }

    public double PuntoReposicion { get; set; }
    public double LoteReposicion { get; set; }

    public double TotalCompra { get; set; }
    public double TotalDevolucionCompra { get; set; }
    public double TotalPedidoVenta { get; set; }
    public double TotalVenta { get; set; }
    public double TotalDevolucionVenta { get; set; }
    public double TotalTransferenciaSalida { get; set; }
    public double TotalTransferenciaEntrada { get; set; }
    public double TotalIndustrializacionIngreso { get; set; }
    public double TotalIndustrializacionEgreso { get; set; }
    public double TotalAjustePositivo { get; set; }
    public double TotalAjusteNegativo { get; set; }
    public double TotalTransito { get; set; }

    /// <summary>Cantidad física disponible (lo que consulta el ecom ERP).</summary>
    public double TotalExistencia { get; set; }

    public bool ExistenciaCongelada { get; set; }
    public double TotalExistenciaInicial { get; set; }
    public decimal CostoUnitarioInicial { get; set; }

    /// <summary>Reservas (pedidos/OD). Ecom ERP muestra TotalExistencia; al checkout se puede usar TotalReserva.</summary>
    public decimal TotalReserva { get; set; }

    public Deposito? Deposito { get; set; }
    public Articulo? Articulo { get; set; }
}
