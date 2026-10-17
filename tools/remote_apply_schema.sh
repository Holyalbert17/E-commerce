#!/bin/bash
set -euo pipefail
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
SQL=/tmp/ecom-pg-setup/ecom_schema_dbo.sql
echo "=== applying schema as ecom on ecommerce:5432 ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -v ON_ERROR_STOP=1 <<EOSQL
CREATE SCHEMA IF NOT EXISTS dbo;
ALTER DATABASE ecommerce SET search_path TO dbo, public;
ALTER ROLE ecom IN DATABASE ecommerce SET search_path TO dbo, public;
SET search_path TO dbo, public;
CREATE TABLE IF NOT EXISTS dbo."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
\i $SQL
EOSQL

echo "=== table counts ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT schemaname, count(*) FROM pg_tables WHERE schemaname IN ('dbo','public') GROUP BY 1 ORDER BY 1;"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  'SELECT * FROM dbo."__EFMigrationsHistory";'
echo "DONE"
