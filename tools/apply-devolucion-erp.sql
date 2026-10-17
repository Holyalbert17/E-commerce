-- Campos resultado write-back ERP en devoluciones
ALTER TABLE dbo.devolucionecommerce
  ADD COLUMN IF NOT EXISTS "ErpMensaje" character varying(500),
  ADD COLUMN IF NOT EXISTS "IdNotaCreditoErp" bigint,
  ADD COLUMN IF NOT EXISTS "ErpProcesado" boolean NOT NULL DEFAULT false;
