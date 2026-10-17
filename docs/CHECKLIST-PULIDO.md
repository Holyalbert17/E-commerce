# Checklist — pulir y mejorar

Fecha: 2026-07-30 (actualizado)

Leyenda: `[ ]` pendiente · `[~]` parcial · `[x]` hecho

---

## Storefront (e-com principal)

### Chat / atención al cliente
- [x] Botón flotante abajo-derecha “Atención al cliente”
- [x] Panel chat estilo WhatsApp (texto + foto)
- [x] Sonido / beep al recibir respuesta del vendedor (panel cerrado)
- [x] Badge de no leídos en el FAB con poll en background
- [x] Historial de varias conversaciones + “Nueva” (`forzarNueva`, sin status 0)
- [x] Cerrar conversación desde el cliente
- [x] Preview de imagen antes de enviar (máx. 5 MB documentado)
- [x] Fotos: URL relativa + `resolveMediaUrl` (5081→5082); upload verificado
- [~] Indicador “escribiendo…” (typing) — pendiente SignalR; hoy poll calmado
- [x] Abrir chat pre-cargado desde un pedido (“Consultar” en Mis pedidos)
- [x] Tras login desde el FAB, reabrir el chat automáticamente
- [x] Escape cierra el panel · foco al abrir · aria-modal
- [x] Scroll sticky WhatsApp (sigue abajo; chip “Nuevos mensajes” si subís)
- [x] Agente visible (Callbell): “Te atiende {nombre}” + autor en burbujas
- [x] Rate limit dev subido (evitar 429 en poll)

### UX / conversión
- [x] Estados vacíos en carrito con CTA al catálogo
- [x] Skeleton loaders en catálogo
- [x] Skeleton PDP + `document.title` SEO
- [x] Feedback toast unificado (host global + login)
- [x] Validación suave documento (RUC/CI) en checkout
- [x] Cupón aplicado y desglose de descuento en checkout
- [x] Validar horarios de pickup por sucursal antes de confirmar
- [x] Link mapa de sucursal en modo pickup (si hay lat/lng)
- [x] Cancelar pedido desde cliente (no migrado)
- [x] Filtros catálogo persistentes en URL
- [x] Branding dinámico: logo, favicon, banner, colores, redes (admin `/branding`)
- [x] Alias CSS `--dx-muted` / `--dx-font-display` (fix Mi cuenta)
- [x] Cambio canal B2B/B2C refresca precios/carrito
- [x] `resolveMediaUrl` en catálogo, PDP, carrito, favoritos, seguidos
- [x] Bloqueo checkout / CTA carrito si hay stock insuficiente
- [x] MOQ / incremento de venta al agregar al carrito
- [x] Activar cuenta rehidrata estado mayorista
- [~] Emails: `SmtpEmailSender` listo; activar con `Smtp:Enabled=true` (hoy logging)

### Catálogo / PDP
- [x] Embalajes: stock estimado por presentación + stock depósito
- [x] Filtros persistentes en URL (marca, stock, q, page)
- [x] SEO title + meta description PDP
- [x] Zoom de imagen desktop + touch en mobile

### Auth / cuenta
- [x] Tras login desde el FAB, reabrir el chat automáticamente
- [~] Carrito guest se conserva por sesión al login
- [ ] Preferencias de notificación (email / push)

---

## Panel administrador

### CRM Chat
- [x] Inbox tipo WhatsApp (`/crm`)
- [x] Header/toolbar compacto + composer sticky
- [x] Cerrar chat desde admin
- [x] Badge no leídos en menú lateral
- [x] Filtro todas / abiertas / sin asignar / mis asignadas
- [x] Respuestas rápidas (plantillas)
- [ ] Adjuntar desde galería de pedidos / facturas
- [x] Métricas CRM (abiertas + en curso en header)
- [x] Poll calmado + scroll sticky + chip nuevos mensajes
- [x] Agente Callbell: chip en inbox, “Atendido por”, autor en burbujas
- [x] Filtro “cerradas” + reabrir
- [x] Reabrir chat cerrado (composer bloqueado mientras cerrada)
- [x] Badge no leídos sidebar refresca al responder / poll
- [x] Asignarme usa id admin de sesión (no id falso)
- [ ] Notificación desktop / sonido mensaje nuevo

### Operación
- [x] Alta tarifas delivery + selector ciudad/barrio
- [x] Mayorista + selector lista al aprobar
- [x] Migrar PedidoMovil ERP + logs
- [x] Planillas CSV clientes / listas / artículos
- [x] Reportes ventas (`/reportes`)
- [x] Export abandonados (CSV 24h+)
- [x] Módulo Diseño / logo (`/branding`) + auto-test `scripts/test-branding.ps1`
- [~] Sync ERP continuo (job + cursores; CS ERP requerida)
- [~] Write-back PedidoMovil (código listo; CS ERP + ProcesaPedidosERP)
- [ ] Campaña email recovery abandonados
- [ ] Multi-depósito / stock por agencia en UI
- [~] Bancard claves tenant + Mode en appsettings

### Moderación
- [~] Reseñas / devoluciones en Marketplace
- [ ] Responder reseñas públicamente
- [ ] Fotos en devoluciones + workflow depósito

---

## Integración ERP (crítico producto)

- [~] Slot `ConnectionStrings:Erp` en appsettings
- [ ] Health check ERP por tenant
- [ ] Sync delta catálogo / precios / stock real
- [ ] Redis stock hot-path
- [ ] PedidoMovil write-back contra Postgres vivo
- [ ] Pull tracking desde ERP
- [ ] Lista precio cuenta ERP al login B2B (asignación local hecha)
- [ ] Bancard Live UAT

---

## Calidad / ops

- [ ] Tests e2e chat + checkout
- [~] Logs sync/migración
- [x] Upload chat documentado en UI
- [x] A11y básica panel chat
- [ ] Lazy routes admin CRM / build prod tuning

---

## Cómo verificar (local)

| Servicio | URL |
|----------|-----|
| Storefront | http://localhost:4200 |
| Admin | http://localhost:4201 |
| API / Swagger | http://localhost:5082/swagger |
| Health | http://localhost:5082/health |

Demo: tenant `demo` · user `demo` / `demo123` · admin `admin` / `Admin12!`

### Probar ahora
1. FAB → `+ Nueva conversación` → mensaje (sin error status 0)  
2. Adjuntar foto → se ve en cliente y admin `/crm`  
3. **Cerrar** (cliente) / **Cerrar chat** (admin)  
4. Admin header bajo + composer siempre visible  
5. Checkout pickup / Mis pedidos Consultar-Cancelar  

### Prioridad siguiente
1. SignalR chat + typing  
2. `ConnectionStrings:Erp` + write-back Casa Imperial  
3. Sync delta real  
4. SMTP real + recovery abandonados  
5. Responder reseñas públicamente  
6. Tests e2e chat + checkout  

Detalle chat: `docs/CHECKLIST-CHAT.md` · ERP: `docs/GAPS-ERP-PARITY.md`
