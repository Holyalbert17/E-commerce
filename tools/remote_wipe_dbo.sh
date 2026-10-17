#!/bin/bash
set -euo pipefail
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
echo "=== wipe dbo tables (empty bootstrap) ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -v ON_ERROR_STOP=1 <<'EOSQL'
DROP SCHEMA IF EXISTS dbo CASCADE;
CREATE SCHEMA dbo;
GRANT ALL ON SCHEMA dbo TO ecom;
ALTER DATABASE ecommerce SET search_path TO dbo, public;
ALTER ROLE ecom IN DATABASE ecommerce SET search_path TO dbo, public;
EOSQL
echo "WIPED"
