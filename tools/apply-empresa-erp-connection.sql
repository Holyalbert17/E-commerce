-- Conexión ERP por empresa (stock continuo + write-back).
-- Idempotente: solo agrega columnas si faltan.

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'dbo' AND table_name = 'empresaconfiguracionecommerce'
      AND column_name = 'ErpAllowWriteBack'
  ) THEN
    ALTER TABLE dbo.empresaconfiguracionecommerce
      ADD COLUMN "ErpAllowWriteBack" boolean NOT NULL DEFAULT false,
      ADD COLUMN "ErpHost" character varying(200) NULL,
      ADD COLUMN "ErpPort" integer NULL,
      ADD COLUMN "ErpDatabase" character varying(120) NULL,
      ADD COLUMN "ErpUsername" character varying(120) NULL,
      ADD COLUMN "ErpPasswordProtected" character varying(500) NULL,
      ADD COLUMN "ErpSslMode" character varying(40) NULL;
  END IF;
END $$;

INSERT INTO dbo."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260810180000_EmpresaErpConnection', '8.0.11'
WHERE NOT EXISTS (
  SELECT 1 FROM dbo."__EFMigrationsHistory"
  WHERE "MigrationId" = '20260810180000_EmpresaErpConnection'
);
