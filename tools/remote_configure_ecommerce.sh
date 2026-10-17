#!/bin/bash
set -eu

PATRONI_CFG=/etc/patroni/config.yml
HBA=/var/lib/postgresql/data/pg_hba.conf
DB_NAME=ecommerce
DB_USER=ecom
DB_PASS="${ECOM_PG_PASSWORD:?Definí ECOM_PG_PASSWORD}"

echo "=== node role ==="
timeout 5 curl -s http://127.0.0.1:8008/patroni || true
echo

echo "=== patch patroni config for VPN 10.74.74.0/24 ==="
python3 - <<'PY'
from pathlib import Path
p = Path("/etc/patroni/config.yml")
text = p.read_text()
if "10.74.74.0/24" in text:
    print("patroni config already has VPN CIDR")
else:
    needle = "        - hostssl all             all          10.99.99.0/24  md5"
    extra = (
        "        - hostssl all             all          10.99.99.0/24  md5\n"
        "        - hostssl all             all          10.74.74.0/24  md5\n"
        "        - host    all             all          10.74.74.0/24  md5\n"
        "        - hostssl replication     replicator  10.74.74.0/24  md5"
    )
    if needle in text:
        p.write_text(text.replace(needle, extra, 1))
        print("patched bootstrap pg_hba")
    elif "pg_hba:" in text:
        p.write_text(
            text.replace(
                "pg_hba:\n",
                "pg_hba:\n"
                "        - hostssl all             all          10.74.74.0/24  md5\n"
                "        - host    all             all          10.74.74.0/24  md5\n",
                1,
            )
        )
        print("appended into first pg_hba section")
    else:
        raise SystemExit("Could not locate pg_hba in patroni config")
PY

if ! grep -q "10.74.74.0/24" "$HBA"; then
  echo "=== appending runtime pg_hba ==="
  cat >> "$HBA" <<'EOF'
# ecommerce VPN access
hostssl all             all          10.74.74.0/24  md5
host    all             all          10.74.74.0/24  md5
hostssl replication     replicator  10.74.74.0/24  md5
EOF
else
  echo "runtime hba already has VPN CIDR"
fi

echo "=== reload hba ==="
timeout 15 sudo -u postgres psql -d postgres -c "SELECT pg_reload_conf();" || true

IN_RECOVERY=$(timeout 10 sudo -u postgres psql -d postgres -Atc "SELECT pg_is_in_recovery();")
echo "pg_is_in_recovery=$IN_RECOVERY"
if [ "$IN_RECOVERY" = "t" ]; then
  echo "ERROR: replica node. Configure must run on PRIMARY."
  timeout 15 patronictl -c "$PATRONI_CFG" list || true
  exit 2
fi

echo "=== create role/db/schema ==="
timeout 30 sudo -u postgres psql -d postgres <<SQL
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

EXISTS=$(timeout 10 sudo -u postgres psql -d postgres -Atc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'")
if [ "$EXISTS" != "1" ]; then
  timeout 30 sudo -u postgres psql -d postgres -c "CREATE DATABASE ${DB_NAME} OWNER ${DB_USER};"
fi

timeout 30 sudo -u postgres psql -d "${DB_NAME}" <<SQL
CREATE SCHEMA IF NOT EXISTS dbo AUTHORIZATION ${DB_USER};
ALTER DATABASE ${DB_NAME} SET search_path TO dbo, public;
GRANT ALL ON SCHEMA dbo TO ${DB_USER};
GRANT ALL ON SCHEMA public TO ${DB_USER};
ALTER ROLE ${DB_USER} IN DATABASE ${DB_NAME} SET search_path TO dbo, public;
SQL

echo "=== verify ==="
timeout 15 sudo -u postgres psql -d "${DB_NAME}" -c "\dn+"
grep "10.74.74" "$HBA" || true
echo DONE
