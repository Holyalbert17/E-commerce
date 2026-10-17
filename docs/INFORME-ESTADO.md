# Informe general — Datamex E-Commerce

**Fecha:** 4 de agosto de 2026  
**Alcance:** Panel administrador · Storefront · Backend · Relación con ERP  
**Modo actual:** Standalone operativo · Integrated (ERP live) sync lectura · write-back OFF  
**Informe funcional (storefront vs admin):** [`INFORME-FUNCIONAL-COMPLETO.md`](./INFORME-FUNCIONAL-COMPLETO.md)  
**Plan + prompts P0/P1/P2:** [`PLAN-PROMPTS-FUNCIONALIDADES.md`](./PLAN-PROMPTS-FUNCIONALIDADES.md)  
**Roadmap features:** [`ROADMAP-FUNCIONALIDADES.md`](./ROADMAP-FUNCIONALIDADES.md)

---

## 1. Resumen ejecutivo

La plataforma es un **e-commerce multi-tenant** (Angular storefront + Angular admin + ASP.NET 8) diseñado con dos principios a la vez:

1. **Acorde al ERP Datamex** — mismas formas de tablas (`articulo`, `listaprecio`, `pedidomovil`, `cuenta`…), mismos códigos de permiso `Site_App_Ecommerce_*`, mismos conceptos (canal B2C/B2B, embalajes, depósitos, PedidoMovil, mayorista).
2. **Independiente del ERP** — el comprador y el backoffice operan contra la base e-com local (SQLite/Postgres). No hay llamadas al Postgres ERP en el hot-path. El acoplamiento Integrated se hace por **ETL/CDC + write-back**, no por acceso directo.

**Estado hoy:** el ciclo comercial completo y el backoffice operativo funcionan en Standalone, incluyendo **personalización marketing** (`ConfiguracionPersonalizacion`) y **timeline de tracking** con push SignalR. La paridad Integrated (sync real + INSERT PedidoMovil + Bancard Live) está documentada y scaffolded, pero **no está lista para producción ERP**.

### Avance estimado (1 ago 2026)

| Vista | % generado |
|---|---|
| **Global** (hacia Integrated completo) | **~83%** |
| **Standalone** (tienda + admin sin ERP live) | **~96%** |
| **Integrated** (sync + write-back + Bancard Live) | **~35%** |

Checklist: [`CHECKLIST-INFORME.md`](./CHECKLIST-INFORME.md) · Tracking/personalización: [`SPEC-TRACKING-PERSONALIZACION.md`](./SPEC-TRACKING-PERSONALIZACION.md).

| Superficie | Madurez | Comentario |
|---|---|---|
| Storefront | **Alta** | Compra, cuenta, chat, branding, B2B, tracking visual, tema/layout |
| Admin | **Media–alta** | Ops + `/personalizacion`; reportes/lifecycle más finos |
| Backend local | **Alta** | APIs, tenancy, JWT, media, CRM, personalización |
| Vínculo ERP live | **Baja / parcial** | Sync stub · write-back con INSERT si hay connstring |

**Puertos de trabajo habituales:** API `http://localhost:5082` · Storefront `:4200` · Admin `:4201`.

---

## 2. Arquitectura y principio ERP ↔ independencia

```
                    ┌─────────────────────┐
   (futuro)         │   ERP Postgres      │
   ETL / CDC   ───► │  (Casa Imperial…)   │
                    └──────────┬──────────┘
                               │ write-back PedidoMovil (futuro)
                               ▼
┌──────────────┐    ┌─────────────────────┐    ┌──────────────┐
│  Storefront  │───►│  Ecom API + Ecom DB │◄───│    Admin     │
│   :4200      │    │  (SQLite/Postgres)  │    │    :4201     │
└──────────────┘    └─────────────────────┘    └──────────────┘
```

- **Standalone (default):** si `ControlaStockEnCarrito`, al confirmar baja `TotalExistencia` y sube `TotalPedidoVenta` (como Pedido ERP); al cancelar no migrado se restaura; pedidos locales; migración ERP puede **simularse**.
- **Integrated (objetivo):** maestros vienen del ERP; stock lo mueve el ERP al facturar (e-com no muta); e-com escribe `PedidoMovil` real.
- **External:** previsto para catálogos/pasarelas externas (menos usado hoy).

Tenant = `IDEmpresa`, resuelto por `X-Tenant-Slug` / `X-Tenant-ID`.

### Qué es espejo ERP vs solo e-com

| Espejo / híbrido ERP | Solo plataforma e-com |
|---|---|
| Empresa, Tienda, Articulo, Embalaje, Imagen | Chat CRM, Reseñas, Cupones, Favoritos |
| ListaPrecio, Stock, Deposito | Branding visual (colores, logo, banner) |
| Carrito, PedidoMovil, Cuenta, UsuarioEcommerce | SyncCursor, MigracionPedidoLog |
| TarifaDelivery, Agencia, Horario, Geo | AdminUsuario / Rol / Funcionalidad (códigos ERP, tablas locales) |
| Config ecom (Bancard keys, ProcesaPedidosERP…) | UnidadMedidaEcommerce (helper local) |

---

## 3. Backend

### Capas

| Proyecto | Rol |
|---|---|
| `Datamex.ECommerce.Domain` | Entidades, enums (`IntegrationMode`) |
| `Datamex.ECommerce.Application` | Contratos, DTOs, options |
| `Datamex.ECommerce.Infrastructure` | EF Core, servicios, seed, sync |
| `Datamex.ECommerce.Api` | Controllers, JWT, tenancy, permisos |

### Auth

| Contexto | Endpoint | Usuario |
|---|---|---|
| Storefront | `POST /api/auth/login` | `UsuarioEcommerce` |
| Admin | `POST /api/admin/auth/login` | `AdminUsuario` + RBAC |

Permisos admin: `[RequirePermission("Site_App_Ecommerce_…")]`. Rol `manager:all` bypass.

### Superficie API (agrupada)

**Pública / comprador:** `/api/catalog`, `/pricing`, `/inventory`, `/cart`, `/checkout`, `/shipping`, `/geography`, `/auth`, `/account`, `/payments/bancard`, `/favoritos`, `/reviews`, `/engagement` (chat, cupones, seguidos…).

**Admin:** dashboard, products (+ imágenes/embalajes), orders, carritos, listas-precios, clientes/mayorista, promotions, branding/empresa, tarifas, planillas CSV, marketplace (reseñas/cupones/dev), CRM chat, sync, reportes ventas, tenants, unidades, manager RBAC.

### Sync / migración ERP (realidad actual)

| Pieza | Estado |
|---|---|
| Job `ErpSyncHostedService` (5 min) | Corre; sin `ConnectionStrings:Erp` → skipped |
| `ErpSyncService` | Contadores locales / watermark — **no lee ERP** |
| `PedidoErpMigrationService` | Sin ERP CS → simula éxito; con CS → stub (`SELECT 1`), **no INSERT** |
| Planillas CSV | Mután la DB e-com (útil Standalone e Integrated como staging) |
| Provisioning `from-erp` | One-shot de tenant con PK ERP |

---

## 4. Panel administrador

Consola **AdminLTE + toolbars `dx-*`**, español ERP, ~22 rutas protegidas.

### Inventario

| Ruta | Estado | Función |
|---|---|---|
| `/` | OK | KPIs dashboard |
| `/catalogo`, `/activar`, `/:id/imagenes`, `/:id/embalajes` | OK | Ops catálogo / media / UOM |
| `/listas-precios` | OK | Contado, cheque, crédito + CSV |
| `/promociones` | OK | Flyers, upload imagen, destino |
| `/branding` | OK | Logo, favicon, banner, colores, redes |
| `/crm` | OK | Inbox, asignar, cerrar/reabrir, badge |
| `/clientes` | OK | Cuentas + solicitudes mayorista |
| `/tarifas` | OK | Alta + autorización delivery |
| `/carritos`, `/pedidos` | Parcial | Listado/detalle; migrar stub |
| `/reportes` | Parcial | Ventas por rango/día |
| `/marketplace` | Parcial | Reseñas, cupones, devoluciones (chat → CRM) |
| `/unidades`, `/configuracion` | OK | UOM + empresa/agencias/horarios |
| `/empresas`, `/manager` | OK | Multi-tenant + RBAC |

### Fortalezas

- Media profesional (producto, branding, flyers, adjuntos CRM).
- CRM cercano a Callbell/WhatsApp (colas, no leídos, reabrir).
- Precios y mayorista alineados a conceptos ERP.
- Planillas y autorización de tarifas/listas.

### Brechas

- Pedidos poco accionables (sin máquina de estados de fulfillment).
- Reportes v1.
- Navegación **no** filtra por permiso (aunque el API sí).
- Dashboard con atajos incompletos.
- Dualidad visual AdminLTE vs pantallas custom.

---

## 5. Storefront

SPA con design system propio (`--dx-*`), tipografía expresiva, branding aplicado en runtime.

### Inventario

| Ruta / superficie | Estado |
|---|---|
| `/catalogo` — hero, categorías, flyers, filtros, canal | OK |
| `/producto/:id` — galería, embalajes, MOQ, reseñas | OK |
| `/carrito` → `/checkout` — stock gate, mapa, pickup, cupón | OK |
| `/pago/:nro`, `/pedido/:nro` | OK (Bancard stub) |
| `/mis-pedidos`, `/favoritos`, `/seguidos`, `/mi-cuenta` | OK |
| Factura PDF (Pentaho) en historial si `nrofactura` | OK (ago-4) |
| Auth modal (`/login`, `/registro`, `/recuperar`) | OK |
| Widget chat flotante | OK (poll + foto) |

### Fortalezas UX recientes

- Cambio B2C/B2B refresca precios/carrito.
- `resolveMediaUrl` en catálogo, PDP, carrito, favoritos.
- Bloqueo por stock insuficiente; MOQ al agregar.
- Activación de cuenta rehidrata mayorista.
- Banner mayorista distingue modo activo.
- Buscador → `/buscar` con filtros + grilla completa.
- Mis pedidos / favoritos / seguidos modernizados (carrito desde listas).
- Tracking público con auto-refresh.
- Footer branding (redes, mapa, contacto) editable en admin.

### Brechas

- Chat sin SignalR / typing como fuente primaria.
- SMTP aún en logging por defecto.
- Bancard: Stub OK + confirm/rollback endurecidos (idempotencia + restore stock); UAT Live con claves reales pendiente.
- Densidad alta en Mi cuenta.
- Sync inbound ERP **híbrido** (job + lazy PDP); alertas de seguidos aún incompletas.

Ver backlog completo: [`CHECKLIST-BACKLOG.md`](./CHECKLIST-BACKLOG.md).

### Paridad ERP reciente (stock / barcodes / tracking / choferes)

| Pieza ERP | E-com | Estado |
|---|---|---|
| `articulo` → `articuloembalaje` → stock por depósito | Igual forma | OK (ya existía) |
| Pedido: Existencia− / PedidoVenta+ + reverso cancel | `ApplyPedidoStockAsync` / `RestorePedidoStockAsync` | OK Standalone |
| `articuloembalajecodigobarra` | Tabla `articuloembalajecodigobarra` + sync desde embalaje | OK |
| Tracking (`TrazabilidadCarritoCompra`) | `SeguimientoPedidoEcommerce` + seed en checkout + UI admin/tienda | OK |
| Mapa checkout → lat/lng | Leaflet en `/checkout` → `Carrito`/`PedidoMovil` | OK |
| `Chofer` + asignación | Maestro admin `/choferes` + `PedidoMovil.IDChofer` | OK |

---

## 6. Matriz de madurez por módulo

| Módulo | Admin | Storefront | API | Nota |
|---|---|---|---|---|
| Catálogo + PDP + UOM | Alto | Alto | Alto | Espejo ERP + vitrina |
| Carrito / checkout / pago | Medio | Alto | Alto | Bancard stub + mapa |
| Precios B2C/B2B + listas | Alto | Alto | Alto | Gate mayorista aprobado |
| Branding / flyers | Alto | Alto | Alto | Independiente ERP |
| CRM chat | Alto | Alto | Alto | Poll |
| Envíos / tarifas | Medio | Alto | Alto | Alta + auth |
| Tracking + choferes | Alto | Alto | Alto | Paridad ERP web |
| Pedidos / migración ERP | Alto | Alto | Alto | INSERT ERP si hay CS; UAT vivo pendiente |
| Sync continuo ERP | Bajo | — | Parcial | Job cuenta local; sin pull ERP |
| Reportes | Bajo | — | Medio | Solo ventas |
| RBAC Manager | Medio | — | Alto | Nav filtra; rutas sin guard por permiso |

---

## 7. Roadmap priorizado

### P0 — paridad Integrated / producción ERP

2. Sync inbound real (catálogo, precios, stock, clientes) desde ERP — **híbrido:** job periódico + lazy-pull PDP si falta en local.
2. UAT write-back `PedidoMovil` contra Postgres vivo (INSERT ya está en código).
3. Bancard Live (multi-buy, confirm, rollback).
4. Stock continuo por depósito desde ERP.
5. SMTP prod (`Smtp:Enabled=true` + credenciales).

### P1 — producto / ops

6. SignalR como fuente primaria en chat + typing.
7. Route guards admin por permiso (nav ya filtra).
8. Reportes ricos + bulk / packing en pedidos.
9. Lista precio por cuenta al login B2B (pull ERP si Integrated).
10. Alertas de seguidos por email (depende SMTP).

### P2 — polish

11. Dashboard / marketplace UX.
12. Notificaciones desktop CRM.
13. PWA / SEO colecciones.
14. Mi cuenta en tabs.

---

## 8. Conclusión

El e-commerce **ya es usable de punta a punta sin ERP**, con un backoffice alineado al lenguaje y al modelo del ERP Datamex. Esa independencia permite demo, UAT Standalone y operación multi-tenant sin depender de la disponibilidad del Postgres ERP.

El siguiente salto de plataforma no es “más pantallas de tienda”, sino **cerrar el circuito Integrated**: maestros desde ERP, pedidos de vuelta al ERP, pagos Live. Hasta entonces, el producto debe presentarse con claridad como:

> **E-commerce Standalone con esquema ERP-ready**, preparado para enganchar ETL y write-back cuando `ConnectionStrings:Erp` y los jobs dejen de ser stub.

### Demo

| Rol | Credencial |
|---|---|
| Tenant | `demo` |
| Comprador | `demo` / `demo123` |
| Admin | `admin` / `Admin12!` |

Documentos relacionados: `docs/CHECKLIST-INFORME.md`, `docs/GAPS-ERP-PARITY.md`, `docs/CHECKLIST-PULIDO.md`, `docs/CHECKLIST-CHAT.md`, `docs/CHECKLIST-BACKLOG.md`.
