# Gaps de paridad ERP ↔ E-commerce nuevo

Última actualización: 2026-08-05

Referencia ERP Site: `Areas/ECommerce` + menú `_SideBarMenu.cshtml`.
Plataforma nueva: Standalone (Postgres `dbo`; ERP opcional cuando `Erp:Enabled`).

---

## Menú admin = menú ERP ECommerce (hecho)

| ERP | Admin nuevo |
|---|---|
| Empresa / Agencias / Horarios | `/configuracion` (alta+**edición** agencia con lat/lng/depósito) |
| Parámetros producto | `/parametros-producto` (+ País, Régimen) |
| Productos CRUD | `/catalogo`, `/catalogo/nuevo`, `/catalogo/:id/editar` |
| Activar / inactivar | `/catalogo/activar` |
| Grupo de productos | `/promociones` |
| Unidades / embalajes | `/unidades`, `/catalogo/:id/embalajes` |
| Listas de precios (+ descuentos / vigencia) | `/listas-precios` + `POST …/cambio-vigencia` |
| Tarifas delivery | `/tarifas` |
| Clientes / cuentas | `/clientes` |
| Carritos export/trazabilidad | `/carritos` |

---

## Stock Standalone = Pedido ERP (hecho)

`ApplyPedidoStockAsync` / `RestorePedidoStockAsync` (Existencia− / PedidoVenta+).

---

## Artículo e-com = ArticuloDTO ERP (hecho)

SKU, descripción, ref proveedor, marca, línea, tipo, proveedor, **país**, **régimen**, embalaje, UOM, cantidad, barra, imagen. Alta sin publicar vitrina.

---

## Write-back PedidoMovil / Cuenta (paridad ERP — ago-4)

Insert/update **deben** seguir `CarritoCompraService` / `UsuarioEcommerceService.ActivarUsuario`:

| Campo | Valor ERP correcto |
|---|---|
| `tipofactura` | Delivery→`REPARTO(0)`, Pickup→`SALON(1)` |
| `tipopedido` | `VENTA(1)` |
| `migrado` (fila ERP) | `false` (planilla lo procesa); local e-com `Migrado=true` = “ya escrito” |
| `iddetallelistaprecio` | FK → `dbo.detallelistaprecio` (nunca CondicionVenta) |
| `origenpedido` | `ECOMMERCE(2)` |
| cuenta | Cliente + FKs plantilla (vendedor/zona/geo/canal…) o link por email/documento |
| `cuentadocumento` / `cuentatelefono` | Insertados al alta (paridad ActivarUsuario) |

Auto push: `Erp:AutoWriteBack` · INSERT real: `Erp:AllowWriteBack=true`.

---

## Factura PDF vía Pentaho (ago-4)

Cuando el pedido está **emitido/facturado** (`nrofactura` en ERP), el comprador puede verlo y descargar PDF en **Mis pedidos**:

| Pieza | Detalle |
|---|---|
| API | `GET /api/account/invoices` · `GET /api/account/orders/{nro}/factura.pdf` · admin `GET /api/admin/orders/{idPm}/factura.pdf` |
| Motor | Misma URL Pentaho que ERP Site/PortalDE: `…/reports_erp:comercial[:sub]:{reporte}.prpt/generatedContent?idpedido=&idempresa=&output-target=pageable/pdf` |
| Reportes | `facturasalon` / `facturasalonelectronica` / `facturareparto` / `facturaservicio`… |
| Config | `Pentaho:Enabled` · `Pentaho:UrlReportPentaho` (misma base que `urlReportPentaho` del ERP) |
| Sync | `order_status` trae `nrofactura` de `pedidomovil` y de `pedido` vinculado |

Storefront: filtro **Facturados** + botón **Descargar factura** (Mis pedidos y detalle `/pedido/:nro`).
Admin: badge N° factura + **Descargar factura** (Pentaho) en detalle de pedido.

---

## Pendiente (requiere ERP core / Integrated)

1. Sync inbound real — **híbrido (ago-4):** job periódico (`Erp:SyncIntervalMinutes`) + **lazy-pull** PDP (`EnsureArticuloFromErpAsync` / `Erp:HybridLazyPull`) si falta el artículo en local; upsert art+emb+precio+stock; `AllowWriteBack=false`  
2. Write-back PedidoMovil — **sin ETL + paridad enums/FK (ago-4):** auto al checkout/pago + retry; INSERT real con `AllowWriteBack=true`; cuenta ActivarUsuario-like; UAT vivo pendiente de flag  
3. Bancard Live — **parcial (ago-4):** confirm idempotente, rollback+restore Standalone, logs rejected; Stub intacto; UAT claves Live por tenant pendiente  
4. Stock IntegrationMode **por tenant** (P0.4) — `ModoIntegracion` en empresa; Integrated no muta stock local  
5. Templates Excel EPPlus / historial `listapreciocuenta` — **historial hecho (ago-5)** · **Excel EPPlus hecho (ago-5):** export `?format=xlsx` clientes/artículos/listas  
6. Documento / teléfono cuenta en write-back — **hecho (ago-4):** link por documento + INSERT `cuentadocumento` / `cuentatelefono`  
7. Factura PDF Pentaho — **hecho (ago-4)**  
8. Búsqueda multinivel — **hecho (ago-5):** índice local + Redis/OpenSearch opcionales + Map/IndexedDB/Web Worker · [`SEARCH-ARCHITECTURE.md`](./SEARCH-ARCHITECTURE.md)  

Plan: [`PLAN-PROMPTS-FUNCIONALIDADES.md`](./PLAN-PROMPTS-FUNCIONALIDADES.md)  

Panorama: [`ERP-LIVE-PANORAMA.md`](./ERP-LIVE-PANORAMA.md) · Alineación: [`ERP-SCHEMA-ALIGNMENT.md`](./ERP-SCHEMA-ALIGNMENT.md) · Features: [`ROADMAP-FUNCIONALIDADES.md`](./ROADMAP-FUNCIONALIDADES.md)

---

## Arquitectura Integrated (objetivo)

```
ERP dbo (lowercase) ──sync ID-preserving──► Ecom espejo + ecom_ext
Checkout ──PedidoMovil──► write-back dbo.pedidomovil (lowercase)
```
