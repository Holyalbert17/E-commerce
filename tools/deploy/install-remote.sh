#!/usr/bin/env bash
# Instala / actualiza Datamex E-Commerce en el host de aplicación.
set -euo pipefail

DEPLOY_USER="${DEPLOY_USER:-userdb}"
INSTALL_ROOT="/opt/datamex-ecom"
MEDIA_ROOT="/var/lib/ecommerce/media"
API_PORT=5082

echo "=== packages ==="
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq nginx curl iptables-persistent >/dev/null 2>&1 || apt-get install -y -qq nginx curl >/dev/null

echo "=== directories ==="
mkdir -p "${INSTALL_ROOT}/api" "${INSTALL_ROOT}/storefront" "${INSTALL_ROOT}/admin"
mkdir -p "${MEDIA_ROOT}"
chown -R "${DEPLOY_USER}:${DEPLOY_USER}" /var/lib/ecommerce "${INSTALL_ROOT}"
chmod +x "${INSTALL_ROOT}/api/Datamex.ECommerce.Api"

echo "=== firewall ==="
for p in 80 5082 4200 4201 8787; do
  ufw allow "${p}/tcp" 2>/dev/null || true
  iptables -C INPUT -p tcp --dport "${p}" -j ACCEPT 2>/dev/null || iptables -I INPUT -p tcp --dport "${p}" -j ACCEPT
  for cidr in 10.99.99.0/24 10.74.74.0/24; do
    iptables -C INPUT -p tcp -s "${cidr}" --dport "${p}" -j ACCEPT 2>/dev/null \
      || iptables -I INPUT -p tcp -s "${cidr}" --dport "${p}" -j ACCEPT
  done
done
mkdir -p /etc/iptables 2>/dev/null || true
iptables-save > /etc/iptables/rules.v4 2>/dev/null || true

echo "=== nginx ==="
cat >/etc/nginx/sites-available/datamex-ecom <<EOF
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;

    location /api/ {
        proxy_pass http://127.0.0.1:${API_PORT}/api/;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location /hubs/ {
        proxy_pass http://127.0.0.1:${API_PORT}/hubs/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
    }

    location /media/ {
        proxy_pass http://127.0.0.1:${API_PORT}/media/;
        proxy_set_header Host \$host;
    }

    location /health {
        proxy_pass http://127.0.0.1:${API_PORT}/health;
    }

    location /swagger/ {
        proxy_pass http://127.0.0.1:${API_PORT}/swagger/;
        proxy_set_header Host \$host;
    }

    location /admin/ {
        alias ${INSTALL_ROOT}/admin/browser/;
        try_files \$uri \$uri/ /admin/index.html;
    }

    location = /admin {
        return 301 /admin/;
    }

    location / {
        root ${INSTALL_ROOT}/storefront/browser;
        try_files \$uri \$uri/ /index.html;
    }
}

server {
    listen 4200;
    listen [::]:4200;
    server_name _;
    root ${INSTALL_ROOT}/storefront/browser;
    index index.html;
    location / { try_files \$uri \$uri/ /index.html; }
}

server {
    listen 4201;
    listen [::]:4201;
    server_name _;
    root ${INSTALL_ROOT}/admin/browser;
    index index.html;
    location / { try_files \$uri \$uri/ /index.html; }
}
EOF

rm -f /etc/nginx/sites-enabled/default
ln -sf /etc/nginx/sites-available/datamex-ecom /etc/nginx/sites-enabled/datamex-ecom
nginx -t
systemctl enable nginx
systemctl restart nginx

echo "=== systemd API ==="
cat >/etc/systemd/system/datamex-ecom-api.service <<EOF
[Unit]
Description=Datamex E-Commerce API
After=network.target

[Service]
Type=simple
User=${DEPLOY_USER}
WorkingDirectory=${INSTALL_ROOT}/api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:${API_PORT}
Environment=DOTNET_EnableDiagnostics=0
ExecStart=${INSTALL_ROOT}/api/Datamex.ECommerce.Api
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable datamex-ecom-api
systemctl restart datamex-ecom-api

echo "=== smoke ==="
sleep 3
curl -fsS "http://127.0.0.1/health" || curl -fsS "http://127.0.0.1:${API_PORT}/health"
echo
curl -fsS -H 'X-Tenant-Slug: casa-imperial' http://127.0.0.1/api/catalog/ping || true
echo
curl -fsSI http://127.0.0.1/ | head -n 1 || true
curl -fsSI http://127.0.0.1/admin/ | head -n 1 || true
systemctl --no-pager --full status datamex-ecom-api nginx | head -n 15 || true
echo "OK http://HOST/  admin=http://HOST/admin/  api=http://HOST/api/"
