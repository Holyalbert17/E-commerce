using Datamex.ECommerce.Domain.Common;

namespace Datamex.ECommerce.Domain.Entities;

/// <summary>Espejo parcial de tipodocumento (registro ecom).</summary>
public class TipoDocumento : EntidadConEmpresa
{
    public int CodTipoDocumento { get; set; }
    public string DescripcionTipoDocumento { get; set; } = string.Empty;
    public bool EsRuc { get; set; }
}

/// <summary>
/// Espejo mínimo de cuenta para standalone: dirección + coords + vínculo usuario.
/// En Integrated el ERP es dueño de la cuenta completa.
/// </summary>
public class Cuenta : EntidadConEmpresa
{
    public long NroCuenta { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreCuenta { get; set; }
    public string? ApellidoCuenta { get; set; }
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public long? IDCiudad { get; set; }
    public long? IDBarrio { get; set; }
    public long? IDDivisionPolitica { get; set; }
    public long? IDListaPrecio { get; set; }
    public long? IDAgencia { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}

/// <summary>Espejo parcial de agencia — pickup / centro mapa / tarifa→depósito.</summary>
public class Agencia : EntidadConEmpresa
{
    public int CodAgencia { get; set; }
    public string DescripcionAgencia { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public long? IDCiudad { get; set; }
    public long? IDDeposito { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public bool EsSucursalEcommerce { get; set; } = true;

    public ICollection<HorarioAtencion> Horarios { get; set; } = new List<HorarioAtencion>();
}

/// <summary>
/// Espejo de horarioatencion (ERP). DiaSemana: 0=Dom … 6=Sáb.
/// Turnos opcionales: segunda franja puede ser 00:00–00:00 si no aplica.
/// </summary>
public class HorarioAtencion : EntidadConEmpresa
{
    public long IDAgencia { get; set; }
    public byte DiaSemana { get; set; }
    public TimeSpan HoraPrimeraApertura { get; set; }
    public TimeSpan HoraPrimerCierre { get; set; }
    public TimeSpan HoraSegundaApertura { get; set; }
    public TimeSpan HoraSegundoCierre { get; set; }

    public Agencia? Agencia { get; set; }
}
