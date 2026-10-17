#!/bin/bash
set -eu
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
echo "=== dbo table count ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT schemaname, count(*) AS tables FROM pg_tables WHERE schemaname IN ('dbo','public') GROUP BY 1 ORDER BY 1;"
echo "=== sample tables ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT tablename FROM pg_tables WHERE schemaname='dbo' ORDER BY 1 LIMIT 20;"
echo "=== migrations ==="
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c 'SELECT * FROM dbo."__EFMigrationsHistory";'
echo "=== pg_hba vpn lines ==="
sudo grep -E '10\.74\.74|10\.99\.99' /etc/postgresql/17/ecom/pg_hba.conf || true
echo "=== listen / port ==="
sudo grep -E '^listen_addresses|^port' /etc/postgresql/17/ecom/postgresql.conf || true
ss -lntp 2>/dev/null | grep 5432 || netstat -lntp 2>/dev/null | grep 5432 || true
echo "OK"
