namespace Datamex.ECommerce.Domain.Common;

/// <summary>Espejo de Datamex.ERP.Core.Models.EntidadBase</summary>
public abstract class EntidadBase
{
    public long ID { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime UltModificacion { get; set; } = DateTime.UtcNow;
    public string? UserName { get; set; }
}

public abstract class EntidadConEmpresa : EntidadBase
{
    public long IDEmpresa { get; set; }
}

public abstract class EntidadSinEmpresa : EntidadBase
{
}
