#!/usr/bin/env bash
# Prepara carpeta de medios ecom en el mismo host que Postgres (205)
# y sirve estáticos en :8090 → http://HOST:8090/media/...
set -euo pipefail

MEDIA_ROOT=/var/lib/ecommerce
MEDIA_DIR="${MEDIA_ROOT}/media"
PORT=8090
SERVICE=ecom-media

echo "==> Creando ${MEDIA_DIR}"
mkdir -p "${MEDIA_DIR}"
chown -R userdb:userdb "${MEDIA_ROOT}"
chmod -R u+rwX,g+rX,o+rX "${MEDIA_ROOT}"

# Servidor estático simple (python3) bajo systemd
cat >/usr/local/bin/ecom-media-serve.sh <<'EOF'
#!/usr/bin/env bash
cd /var/lib/ecommerce
exec /usr/bin/python3 -m http.server 8090 --bind 0.0.0.0
EOF
chmod +x /usr/local/bin/ecom-media-serve.sh

cat >/etc/systemd/system/${SERVICE}.service <<EOF
[Unit]
Description=Datamex E-Commerce media static files
After=network.target

[Service]
Type=simple
User=userdb
WorkingDirectory=${MEDIA_ROOT}
ExecStart=/usr/local/bin/ecom-media-serve.sh
Restart=on-failure
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now ${SERVICE}.service
systemctl --no-pager --full status ${SERVICE}.service || true

# smoke
sleep 1
curl -sI "http://127.0.0.1:${PORT}/media/" | head -n 5 || true
ls -la "${MEDIA_DIR}"
echo "OK media root=${MEDIA_DIR} public=http://$(hostname -I | awk '{print $1}'):${PORT}/media/"
