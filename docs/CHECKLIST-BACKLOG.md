# Checklist — backlog de mejoras

Fecha: 2026-08-03 (corregido vs código)  
Leyenda: `[ ]` pendiente · `[~]` parcial / config · `[x]` hecho · **P0** crítico · **P1** producto · **P2** polish

Documento vivo alineado al repo. Relacionados: `INFORME-ESTADO.md`, `CHECKLIST-PULIDO.md`, `GAPS-ERP-PARITY.md`, `CHECKLIST-CHAT.md`.

---

## Hecho recientemente (storefront UX)

- [x] Buscador → `/buscar` con filtros + grilla completa
- [x] Carrito: toasts, precio unitario, sticky móvil
- [x] Checkout: toasts de validación
- [x] Catálogo: debounce, empty + limpiar, “Desde”
- [x] PDP: barra móvil + toast
- [x] Mis pedidos: filtros, stats, cards
- [x] Footer branding (redes, mapa, contacto) + seed demo
- [x] Favoritos → **Al carrito**
- [x] Seguidos → **Al carrito** + precio + stock (API)
- [x] Tracking público: auto-refresh 20s
- [x] Tracking en `/pedido/:nro` vía SignalR hub
- [x] **Bugs de flujo (2026-08-03):** mapa checkout, coords obligatorias, búsqueda `/buscar`, qty carrito, MOQ PDP/listas, canal update carrito, reorder delivery, tracking pago, chat pedido, sesión mayorista al boot, Bancard amount InvariantCulture, IDCuenta sin fallback `1`

---

## 1. Storefront UX

### Conversión
- [ ] **P1** Quick-add en cards del catálogo
- [ ] **P1** Mini-cart / drawer al agregar
- [ ] **P1** Persistir datos de checkout entre pedidos
- [ ] **P2** Comparador de productos
- [ ] **P2** Wishlist compartible
- [ ] **P2** Cupones visibles en vitrina

### Búsqueda
- [x] Página `/buscar`
- [ ] **P1** Sugerencias: marcas + categorías
- [ ] **P1** Historial de búsquedas (localStorage)
- [ ] **P2** SEO `/marca/:slug` · `/coleccion/:id`
- [ ] **P2** Facetas con counts

### Engagement
- [x] Favoritos → carrito
- [x] Seguidos → carrito + precio/stock
- [ ] **P1** Alertas email reposición / baja precio (requiere SMTP)
- [ ] **P2** Preferencias de notificación
- [ ] **P2** Vistos recientemente

### Cuenta / postventa
- [~] Mi cuenta funcional, aún densa
- [ ] **P1** Mi cuenta en tabs
- [x] **P1** PDF comprobante / factura (Pentaho: Mis pedidos + detalle pedido + admin N°)
- [ ] **P2** NPS post-entrega

### Auth
- [x] Registro + activación + reset (código listo)
- [~] **P0** SMTP: `SmtpEmailSender` existe; falta `Smtp:Enabled=true` + credenciales en prod
- [ ] **P1** OAuth (opcional)
- [ ] **P2** Tip onboarding primer pedido

### Mobile / PWA
- [ ] **P2** Manifest + SW
- [ ] **P2** Push seguimiento
- [ ] **P2** Offline queue
- [ ] **P2** Compresión foto chat

---

## 2. Pagos (Bancard)

- [~] Stub funciona en demo (`Bancard:Mode` default Stub)
- [ ] **P0** Live UAT (`Mode=Live` + `HttpBancardClient`)
- [ ] **P0** Confirm / rollback / webhook prod
- [ ] **P1** UX estados pago (pendiente / OK / fallo / retry)
- [ ] **P1** Multi-buy + process_id estable en UI
- [~] Claves por tenant en config empresa — validar E2E Live
- [ ] **P2** Ocultar simular pago fuera de non-prod

---

## 3. Integración ERP

- [~] Write-back `PedidoMovil`: **INSERT real** en código si hay CS ERP — falta UAT Postgres + FKs
- [x] **Inbound**: app independiente `ecom-etl` (multiempresa, COPY/upsert). Checklist: `../../ecom-etl/docs/CHECKLIST.md`
- [x] C# `ErpSyncHostedService` **off** en prod inbound (`Erp:Enabled=false`) — no duplicar pull
- [ ] Cerrar gaps ecom-etl vs C# (`detallelistaprecio`, cuentas, horarios, detalle grupo, imagenes, config empresa) — ver checklist B*
- [ ] **P0** UAT write-back contra ERP vivo
- [ ] **P1** Health ERP por tenant en admin (API ya tiene test CS; ETL `doctor --tenants multi`)
- [~] **P1** Pull estados/tracking desde ERP → dominio `order_status` en ecom-etl
- [ ] **P1** Lista precio cuenta al login B2B (`listapreciocuenta` en ETL)
- [ ] **P1** UI logs sync/migración accionables (cursors ya en `/sync`)
- [x] Cuenta local + `ICuentaErpWriter` en activación (si ERP CS)
- [x] **P2** CDC/ETL → reemplazado por ecom-etl continuous
- [ ] **P2** Mapeo ruta/chofer ERP

---

## 4. Admin / Ops

### Pedidos
- [x] Asignar chofer desde detalle pedido
- [x] Agregar hitos de seguimiento (preparando → … → entregado)
- [~] Fulfillment usable en detalle; falta flujo guiado / bulk / packing
- [ ] **P1** Packing slip / etiqueta
- [ ] **P1** Bulk actions
- [ ] **P1** ETA estimado en UI
- [ ] **P2** Impersonar cliente

### Catálogo / stock
- [x] Ciclo stock Standalone = Pedido ERP (Existencia−/PedidoVenta+; restore al cancelar)
- [x] ABM artículos admin (alta/edición/baja) paridad ERP e-com
- [x] Parámetros producto (marca/línea/tipo/embalaje/proveedor)
- [x] Import planilla que cree artículos (SKU + DescripcionArticulo)
- [ ] **P1** Multi-depósito editable en UI
- [ ] **P1** Edición masiva precios / activar
- [ ] **P2** Preview storefront (iframe)
- [ ] **P0** Sync stock continuo desde ERP (Integrated)

### CRM / marketplace
- [~] Chat CRM (poll; hub SignalR existe)
- [ ] **P1** Responder reseñas
- [ ] **P1** Workflow devoluciones (aprobar/rechazar)
- [ ] **P1** Notif desktop + sonido CRM
- [ ] **P2** Transferir / notas internas
- [ ] **P2** Plantillas · SLA

### Reportes / dashboard
- [~] Ventas básicas
- [ ] **P1** Conversión, ticket, top SKU, abandono
- [ ] **P1** Dashboard atajos (hoy / stock bajo / chats)
- [ ] **P2** Export CSV
- [ ] **P2** Recovery carritos

### Branding
- [x] Footer redes / mapa / contacto
- [~] Personalización secciones (claves) — sin editor drag visual
- [ ] **P1** Editor orden secciones visual
- [ ] **P2** Preview mobile · CMS legales

### RBAC
- [x] Nav admin filtra con `session.can(...)`
- [ ] **P1** Route guards por permiso (hoy solo `adminAuthGuard`)
- [ ] **P2** Roles más granulares · auditoría precios

---

## 5. CRM / Chat (técnico)

- [~] Hub SignalR presente; cliente aún prioriza poll
- [ ] **P1** SignalR como fuente primaria (+ poll fallback)
- [ ] **P1** Typing indicator
- [ ] **P1** Entrega / lectura
- [ ] **P2** Adjuntar PDF
- [ ] **P2** Abrir chat desde push

Ver `CHECKLIST-CHAT.md`.

---

## 6. Seguridad / calidad / ops

- [ ] **P1** Rate limit por JWT
- [ ] **P1** Magic-bytes / antivirus uploads
- [ ] **P1** `/health` con ERP + Bancard + SMTP
- [ ] **P1** E2E registro→checkout→pago→tracking
- [ ] **P2** Jobs visibles · CDN/S3 · APM

---

## 7. Prioridad sugerida

| Sprint | Foco | Ítems reales |
|--------|------|----------------|
| A | Confianza | Activar SMTP prod, Bancard Live UAT, UX estados pago |
| B | Integrated | Pull ERP catálogo/stock/precios, UAT write-back PedidoMovil |
| C | Ops | Guards RBAC por ruta, reportes ricos, bulk pedidos |
| D | Engagement | Alertas seguidos (SMTP), SignalR+typing, Mi cuenta tabs |
| E | Growth | PWA, SEO colecciones, recovery carritos |

---

## Correcciones vs versión anterior del checklist

| Antes (incorrecto) | Ahora |
|--------------------|--------|
| Nav admin “sin filtro” | Nav **sí** filtra; faltan **route guards** |
| Write-back “stub” | INSERT **implementado**; falta UAT ERP |
| Sync “scaffold vacío” | Job corre pero **no lee ERP** (cuenta local) |
| SMTP “falta código” | Código listo; falta **config** |
| Chofer / tracking admin pendientes | **Hecho** en detalle pedido |
| Tracking `/pedido` pendiente | **Hecho** (hub) |
| Seguidos sin precio/stock | **Hecho** |

---

## Cómo usar

1. Marcar `[x]` al cerrar y referenciar PR.
2. No mezclar P0 Integrated con polish P2 en el mismo release.
3. Actualizar `INFORME-ESTADO.md` §6–7 cuando cambie un P0/P1.
