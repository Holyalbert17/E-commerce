#!/bin/bash
set -eu

# Standalone Postgres for Datamex E-Commerce (does NOT touch Patroni data dir).
VERSION=17
CLUSTER=ecom
PORT=5432
DATADIR=/var/lib/postgresql/${VERSION}/${CLUSTER}
DB_NAME=ecommerce
DB_USER=ecom
DB_PASS="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"
VPN_CIDR='10.74.74.0/24'
# also allow the exact host the user mentioned
VPN_HOST='10.74.74.108/32'

echo "=== ensure packages ==="
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq postgresql-${VERSION} postgresql-client-${VERSION} >/dev/null

echo "=== create cluster if missing ==="
if ! pg_lsclusters | awk '{print $1,$2}' | grep -q "^${VERSION} ${CLUSTER}$"; then
  # Avoid colliding with Patroni data at /var/lib/postgresql/data
  pg_createcluster ${VERSION} ${CLUSTER} --port=${PORT} --datadir=${DATADIR} --start
else
  echo "cluster exists"
  pg_ctlcluster ${VERSION} ${CLUSTER} start || true
fi

CONF_DIR=/etc/postgresql/${VERSION}/${CLUSTER}
HBA=${CONF_DIR}/pg_hba.conf
POSTGRESQL_CONF=${CONF_DIR}/postgresql.conf

echo "=== listen addresses ==="
sed -i "s/^#\?listen_addresses.*/listen_addresses = '*'/" "$POSTGRESQL_CONF"
# ensure port
sed -i "s/^#\?port.*/port = ${PORT}/" "$POSTGRESQL_CONF"

echo "=== pg_hba VPN ==="
# keep local trust for admin, add VPN md5
cp -a "$HBA" "${HBA}.bak.$(date +%s)"
cat > "$HBA" <<EOF
# Database administrative login by Unix domain socket
local   all             postgres                                peer
local   all             all                                     peer
# IPv4 local connections:
host    all             all             127.0.0.1/32            scram-sha-256
host    all             all             ::1/128                 scram-sha-256
# VPN ecommerce clients
host    all             all             ${VPN_CIDR}             scram-sha-256
host    all             all             ${VPN_HOST}             scram-sha-256
# Datamex LAN (optional useful)
host    all             all             10.99.99.0/24           scram-sha-256
EOF

echo "=== restart cluster ==="
pg_ctlcluster ${VERSION} ${CLUSTER} restart

echo "=== create role/db/schema ==="
sudo -u postgres psql -p ${PORT} -d postgres <<SQL
DO \$\$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_USER}') THEN
    CREATE ROLE ${DB_USER} LOGIN PASSWORD '${DB_PASS}';
  ELSE
    ALTER ROLE ${DB_USER} WITH LOGIN PASSWORD '${DB_PASS}';
  END IF;
END
\$\$;
SQL

EXISTS=$(sudo -u postgres psql -p ${PORT} -d postgres -Atc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'")
if [ "$EXISTS" != "1" ]; then
  sudo -u postgres psql -p ${PORT} -d postgres -c "CREATE DATABASE ${DB_NAME} OWNER ${DB_USER};"
fi

sudo -u postgres psql -p ${PORT} -d ${DB_NAME} <<SQL
CREATE SCHEMA IF NOT EXISTS dbo AUTHORIZATION ${DB_USER};
ALTER DATABASE ${DB_NAME} SET search_path TO dbo, public;
GRANT ALL ON SCHEMA dbo TO ${DB_USER};
GRANT ALL ON SCHEMA public TO ${DB_USER};
ALTER ROLE ${DB_USER} IN DATABASE ${DB_NAME} SET search_path TO dbo, public;
SQL

# enable on boot
systemctl enable postgresql@${VERSION}-${CLUSTER} 2>/dev/null || true

echo "=== verify ==="
pg_lsclusters
ss -lntp | grep ${PORT} || true
sudo -u postgres psql -p ${PORT} -d ${DB_NAME} -c "\dn+"
PGPASSWORD="${DB_PASS}" psql -h 127.0.0.1 -p ${PORT} -U ${DB_USER} -d ${DB_NAME} -c "SHOW search_path;"
echo "CONN=Host=10.99.99.205;Port=${PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Prefer"
echo DONE
