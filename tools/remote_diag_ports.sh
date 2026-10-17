#!/bin/bash
set -eu
echo "=== listeners 5432/5433 ==="
ss -lntp 2>/dev/null | grep -E ':5432|:5433' || netstat -lntp 2>/dev/null | grep -E ':5432|:5433' || true
echo "=== clusters ==="
pg_lsclusters 2>/dev/null || true
echo "=== patroni ==="
systemctl is-active patroni 2>/dev/null || true
systemctl is-enabled patroni 2>/dev/null || true
echo "=== patroni.yml listen/port (grep) ==="
sudo grep -nE 'port:|listen|connect_address|data_dir|scope' /etc/patroni/patroni.yml 2>/dev/null | head -40 || \
  sudo grep -nE 'port:|listen|connect_address|data_dir|scope' /etc/patroni/*.yml 2>/dev/null | head -40 || true
echo "=== who owns 5432 data ==="
sudo ls -la /var/lib/postgresql/data 2>/dev/null | head -5 || true
sudo ls -la /var/lib/postgresql/17/ 2>/dev/null || true
echo "OK"
