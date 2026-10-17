#!/bin/bash
set -eu

echo "=== processes ==="
ps aux | grep -E 'postgres|patroni' | grep -v grep | head -40 || true
echo "=== ports ==="
ss -lntp | grep -E '5432|5433|8008' || true
echo "=== patroni service ==="
systemctl is-active patroni || true
systemctl status patroni --no-pager -l | head -40 || true
echo "=== clusters ==="
pg_lsclusters || true
