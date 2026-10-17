#!/bin/bash
# Status del cluster standalone e-commerce (17/ecom en 5432).
# Patroni queda disabled en este host — no arrancar sin coordinar.
set -eu

echo "=== cluster ecom (systemd) ==="
systemctl is-active postgresql@17-ecom 2>&1 || true
systemctl is-enabled postgresql@17-ecom 2>&1 || true
systemctl is-active patroni 2>&1 || true
systemctl is-enabled patroni 2>&1 || true

echo "=== listen 5432 ==="
ss -lntp 2>/dev/null | grep 5432 || netstat -lntp 2>/dev/null | grep 5432 || true

echo "=== postgresql.conf / hba (17/ecom) ==="
sudo grep -E '^listen_addresses|^port' /etc/postgresql/17/ecom/postgresql.conf || true
sudo grep -E '10\.74\.74|10\.99\.99|127\.0\.0\.1' /etc/postgresql/17/ecom/pg_hba.conf || true

echo "=== local ecom as ecom user ==="
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
timeout 15 psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT current_database(), current_user, current_setting('search_path');" 2>&1 | head -20
timeout 15 psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT schemaname, count(*) AS tables FROM pg_tables WHERE schemaname IN ('dbo','public') GROUP BY 1 ORDER BY 1;" 2>&1 | head -20

echo "=== synccursor empresa 41 ==="
timeout 15 psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  'SELECT "EntityKey","LastStatus","RowsProcessed","LastError","LastRunUtc" FROM dbo.synccursor WHERE "IDEmpresa"=41 ORDER BY 1;' 2>&1 | head -40

echo "=== patroni (solo info; debe estar down) ==="
timeout 5 curl -s http://127.0.0.1:8008/patroni 2>&1 | head -c 400 || echo "(patroni REST no responde — esperado)"
echo
echo DONE
