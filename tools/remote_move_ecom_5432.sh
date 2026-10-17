#!/bin/bash
set -euo pipefail
echo "=== stop patroni (broken etcd; frees 5432) ==="
echo 'Dtmx.2025' | sudo -S systemctl stop patroni || true
sleep 1
ss -lntp 2>/dev/null | grep -E ':5432|:5433' || true

echo "=== move ecom cluster 5433 -> 5432 ==="
echo 'Dtmx.2025' | sudo -S pg_ctlcluster 17 ecom stop || true
CONF=/etc/postgresql/17/ecom/postgresql.conf
echo 'Dtmx.2025' | sudo -S sed -i "s/^port = .*/port = 5432/" "$CONF"
# also uncomment/set if only commented form exists
if ! grep -qE '^port = 5432' "$CONF"; then
  echo 'Dtmx.2025' | sudo -S bash -c "echo 'port = 5432' >> '$CONF'"
fi
echo 'Dtmx.2025' | sudo -S pg_ctlcluster 17 ecom start
sleep 2

echo "=== verify ==="
pg_lsclusters
ss -lntp 2>/dev/null | grep -E ':5432|:5433' || true
export PGPASSWORD="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  "SELECT schemaname, count(*) FROM pg_tables WHERE schemaname='dbo' GROUP BY 1;"
psql -h 127.0.0.1 -p 5432 -U ecom -d ecommerce -c \
  'SELECT * FROM dbo."__EFMigrationsHistory";'
echo "MOVED_TO_5432"
