-- Adjunta foto a devoluciones (storefront RMA)
ALTER TABLE dbo.devolucionecommerce
  ADD COLUMN IF NOT EXISTS "UrlAdjunto" character varying(800),
  ADD COLUMN IF NOT EXISTS "MimeType" character varying(100),
  ADD COLUMN IF NOT EXISTS "NombreArchivo" character varying(260);
