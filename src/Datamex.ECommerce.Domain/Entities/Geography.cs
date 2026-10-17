using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo de divisionpolitica (departamento) — EntidadSinEmpresa como ERP.</summary>
public class DivisionPolitica : EntidadSinEmpresa
{
    public int CodDivisionPolitica { get; set; }
    public string DescripcionDivisionPolitica { get; set; } = string.Empty;
    public string NombreAbreviado { get; set; } = string.Empty;
    public long? IDPais { get; set; } = 1;

    public ICollection<Ciudad> Ciudades { get; set; } = new List<Ciudad>();
}

/// <summary>Espejo de ciudad.</summary>
public class Ciudad : EntidadSinEmpresa
{
    public int CodCiudad { get; set; }
    public string DescripcionCiudad { get; set; } = string.Empty;
    public long IDDivisionPolitica { get; set; }
    public string? CodigoSipap { get; set; }

    public DivisionPolitica? DivisionPolitica { get; set; }
    public ICollection<Localidad> Localidades { get; set; } = new List<Localidad>();
}

/// <summary>Espejo de localidad (cadena ciudad → barrio en ERP).</summary>
public class Localidad : EntidadSinEmpresa
{
    public int CodLocalidad { get; set; }
    public string DescripcionLocalidad { get; set; } = string.Empty;
    public long IDCiudad { get; set; }

    public Ciudad? Ciudad { get; set; }
    public ICollection<Distrito> Distritos { get; set; } = new List<Distrito>();
}

/// <summary>Espejo de distrito.</summary>
public class Distrito : EntidadSinEmpresa
{
    public int CodDistrito { get; set; }
    public string DescripcionDistrito { get; set; } = string.Empty;
    public long IDLocalidad { get; set; }

    public Localidad? Localidad { get; set; }
    public ICollection<Barrio> Barrios { get; set; } = new List<Barrio>();
}

/// <summary>Espejo de barrio — FK a distrito; ciudad se resuelve vía localidad.</summary>
public class Barrio : EntidadSinEmpresa
{
    public int CodBarrio { get; set; }
    public string DescripcionBarrio { get; set; } = string.Empty;
    public long? IDDistrito { get; set; }

    public Distrito? Distrito { get; set; }
}
