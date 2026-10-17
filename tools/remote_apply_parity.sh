#!/bin/bash
set -euo pipefail
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
SQL=/tmp/ecom-pg-setup/ecom_empresa_parity.sql
echo "=== applying EmpresaErpParity on ecommerce:5432 ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -v ON_ERROR_STOP=1 <<EOSQL
SET search_path TO dbo, public;
\i $SQL
EOSQL
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c 'SELECT * FROM dbo."__EFMigrationsHistory" ORDER BY 1;'
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT column_name FROM information_schema.columns WHERE table_schema='dbo' AND table_name='empresaconfiguracionecommerce' AND column_name ILIKE '%correo%' OR (table_schema='dbo' AND table_name='empresaconfiguracionecommerce' AND column_name ILIKE '%tiempo%') ORDER BY 1;"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT tablename FROM pg_tables WHERE schemaname='dbo' AND tablename LIKE '%redsocial%';"
echo "DONE"
