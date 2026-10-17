-- Resync identity sequences after ERP sync / manual ID inserts.
-- Usage:
--   python tools/fix_identity_sequences.py
--   psql -h 10.99.99.205 -p 5432 -U ecom -d ecommerce -f tools/fix_identity_sequences.sql

SELECT setval(
  pg_get_serial_sequence('dbo.usuarioecommerce', 'ID'),
  GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.usuarioecommerce), 1),
  true
) AS usuarioecommerce_seq;

SELECT setval(
  pg_get_serial_sequence('dbo.usuarioecommercecuenta', 'ID'),
  GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.usuarioecommercecuenta), 1),
  true
) AS usuarioecommercecuenta_seq;

SELECT setval(
  pg_get_serial_sequence('dbo.cuenta', 'ID'),
  GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.cuenta), 1),
  true
) AS cuenta_seq;
