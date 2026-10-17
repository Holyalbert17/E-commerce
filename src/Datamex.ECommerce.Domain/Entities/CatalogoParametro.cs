using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>
/// Parámetros de producto e-com (paridad ERP ParametrosProducto):
/// MARCA | LINEA | TIPO | EMBALAJE | PROVEEDOR
/// </summary>
public class CatalogoParametro : EntidadConEmpresa
{
    public string Tipo { get; set; } = CatalogoParametroTipos.Marca;
    public int Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public static class CatalogoParametroTipos
{
    public const string Marca = "MARCA";
    public const string Linea = "LINEA";
    public const string Tipo = "TIPO";
    public const string Embalaje = "EMBALAJE";
    public const string Proveedor = "PROVEEDOR";
    public const string Pais = "PAIS";
    public const string Regimen = "REGIMEN";

    public static readonly string[] All =
    [
        Marca, Linea, Tipo, Embalaje, Proveedor, Pais, Regimen
    ];

    public static bool IsValid(string? tipo) =>
        !string.IsNullOrWhiteSpace(tipo)
        && All.Contains(tipo.Trim().ToUpperInvariant());
}
