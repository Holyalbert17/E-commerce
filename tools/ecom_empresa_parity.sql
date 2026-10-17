START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ALTER COLUMN "MontoUmbralMayorista" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ALTER COLUMN "MontoPedidoMinimo" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ALTER COLUMN "ClavePublicaProcesadoraTarjeta" TYPE character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ALTER COLUMN "ClavePrivadaProcesadoraTarjeta" TYPE character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ADD "NombreServidorCorreoElectronico" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ADD "NombreUsuarioCorreoElectronico" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ADD "PasswordCorreoElectronico" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ADD "PuertoCorreoElectronico" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ADD "TiempoAproxEntregaDelivery" time without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce ADD "TiempoAproxEntregaPickup" time without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ALTER COLUMN "RazonSocial" TYPE character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ALTER COLUMN "NumeroDocumento" TYPE character varying(15);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ALTER COLUMN "NombreFantasia" TYPE character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ALTER COLUMN "ID" DROP IDENTITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "CodEmpresa" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "DireccionEmpresa" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "EMail" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "Fax" character varying(15);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "IDPais" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "LogoEmpresa" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "RepresentanteLegal" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    ALTER TABLE dbo.empresa ADD "Telefono" character varying(15);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    CREATE TABLE dbo.empresaconfiguracionecommerceredsocial (
        "ID" bigint NOT NULL,
        "IDEmpresaConfiguracionEcommerce" bigint NOT NULL,
        "NombreRedSocial" character varying(255) NOT NULL,
        "DireccionURLRedSocial" text NOT NULL,
        "LogoRedSocial" text NOT NULL,
        "Activo" boolean NOT NULL,
        "UltModificacion" timestamp with time zone NOT NULL,
        "UserName" text,
        CONSTRAINT "PK_empresaconfiguracionecommerceredsocial" PRIMARY KEY ("ID"),
        CONSTRAINT "FK_empresaconfiguracionecommerceredsocial_empresaconfiguracion~" FOREIGN KEY ("IDEmpresaConfiguracionEcommerce") REFERENCES dbo.empresaconfiguracionecommerce ("ID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    CREATE INDEX "IX_empresaconfiguracionecommerceredsocial_IDEmpresaConfiguraci~" ON dbo.empresaconfiguracionecommerceredsocial ("IDEmpresaConfiguracionEcommerce");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805160529_EmpresaErpParity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805160529_EmpresaErpParity', '8.0.11');
    END IF;
END $EF$;
COMMIT;

