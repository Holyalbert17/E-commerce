namespace Datamex.ECommerce.Application.Options;

public class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>Carpeta física local de medios (relativa al ContentRoot o absoluta). Backup si SFTP falla.</summary>
    public string RootPath { get; set; } = "App_Data/media";

    /// <summary>Prefijo URL público, ej. /media</summary>
    public string RequestPath { get; set; } = "/media";

    /// <summary>Base pública (API o servidor de medios en el host de la BD).</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>Máximo por archivo (bytes). Default 20 MB.</summary>
    public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "image/avif",
        "image/x-icon",
        "image/vnd.microsoft.icon"
    ];

    /// <summary>Si true, guarda en el mismo host que Postgres vía SFTP (Media:Sftp).</summary>
    public bool UseRemoteSftp { get; set; }

    /// <summary>
    /// Raíz de archivos de imagen del ERP (PersistentImagesPath).
    /// El campo dbo.articuloimagen.imagen es el nombre de archivo en disco (GUID.ext),
    /// no bytea. Ruta típica: {ErpImagesRootPath}/Articulos/{archivo}.
    /// </summary>
    public string ErpImagesRootPath { get; set; } = "";

    /// <summary>Subcarpeta bajo ErpImagesRootPath (ERP Site usa "Articulos").</summary>
    public string ErpImagesSubdirectory { get; set; } = "Articulos";

    public MediaSftpOptions Sftp { get; set; } = new();
}

public class MediaSftpOptions
{
    public string Host { get; set; } = "10.99.99.205";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "userdb";
    public string Password { get; set; } = "";
    /// <summary>Raíz remota que contiene la carpeta media (ej. /var/lib/ecommerce).</summary>
    public string RemoteRoot { get; set; } = "/var/lib/ecommerce";
}
