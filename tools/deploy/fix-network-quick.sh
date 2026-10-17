#!/bin/bash
set -e
INSTALL_ROOT="/opt/datamex-ecom"
API_PORT=5082
for p in 80 5082 4200 4201 8787; do
  iptables -C INPUT -p tcp --dport "$p" -j ACCEPT 2>/dev/null || iptables -I INPUT -p tcp --dport "$p" -j ACCEPT
done
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
    }
    location /hubs/ {
        proxy_pass http://127.0.0.1:${API_PORT}/hubs/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
    }
    location /media/ { proxy_pass http://127.0.0.1:${API_PORT}/media/; proxy_set_header Host \$host; }
    location /health { proxy_pass http://127.0.0.1:${API_PORT}/health; }
    location /swagger/ { proxy_pass http://127.0.0.1:${API_PORT}/swagger/; proxy_set_header Host \$host; }
    location /admin/ {
        alias ${INSTALL_ROOT}/admin/browser/;
        try_files \$uri \$uri/ /admin/index.html;
    }
    location = /admin { return 301 /admin/; }
    location / {
        root ${INSTALL_ROOT}/storefront/browser;
        try_files \$uri \$uri/ /index.html;
    }
}
EOF
ln -sf /etc/nginx/sites-available/datamex-ecom /etc/nginx/sites-enabled/datamex-ecom
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl restart nginx
curl -fsS http://127.0.0.1/health; echo
curl -fsS -H 'X-Tenant-Slug: casa-imperial' http://127.0.0.1/api/catalog/ping; echo
echo DONE
