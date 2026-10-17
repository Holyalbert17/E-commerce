# Informe funcional — E-commerce vs Panel administrativo

**Fecha:** 4 de agosto de 2026  
**Producto:** Datamex E-Commerce (Standalone / ERP-ready)  
**Audiencia:** producto, operaciones, desarrollo  
**Apps:** Storefront Angular `:4200` · Admin Angular `:4201` · API ASP.NET 8 `:5082`

---

## 1. Resumen

La plataforma separa dos mundos con roles distintos:

| Superficie | Quién la usa | Qué hace |
|---|---|---|
| **E-commerce (storefront)** | Comprador B2C / mayorista B2B | Descubre, compra, paga, sigue pedidos, chatea |
| **Panel administrativo** | Operador de tienda / backoffice | Configura la tienda, catálogo, precios, envíos, CRM, sync ERP |

Ambas hablan con la **misma API y base e-com local** (SQLite/Postgres). El tenant se identifica por slug (`demo`, `casa-imperial`, …) vía header `X-Tenant-Slug` / `?tenant=`.

**Principio:** el hot-path comercial **no depende** del Postgres ERP. El ERP se acopla por sync de lectura (pull) y, opcionalmente, write-back de pedidos (hoy **apagado** con `Erp:AllowWriteBack=false`).

```
Comprador ──► Storefront :4200 ──┐
                                 ├──► API :5082 ──► EcomDb local
Operador  ──► Admin :4201     ──┘         │
                                          └── (opcional) lectura ERP
```

---

## 2. Multi-tenant y sesión — comportamiento común

| Concepto | Comportamiento |
|---|---|
| Tenant | `IDEmpresa` + `SlugTienda` (ej. `casa-imperial` = empresa 41) |
| Storefront | `?tenant=slug` al abrir (desde admin “Tienda”); chip de tenant en header |
| Admin | Login pide slug + usuario admin; header muestra slug y `#empresa` |
| Auth comprador | JWT de `UsuarioEcommerce` |
| Auth admin | JWT de `AdminUsuario` + permisos `Site_App_Ecommerce_*` / `manager:all` |
| Canales | **B2C** (contado minorista) y **B2B** (mayorista, si la cuenta está aprobada) |

---

## 3. E-commerce (storefront) — funcionalidades y comportamiento

Puerto típico: `http://localhost:4200`  
Rutas: `apps/storefront/src/app/app.routes.ts`

### 3.1 Mapa de rutas

| Ruta | Auth | Función |
|---|---|---|
| `/catalogo` | No | Home / vitrina |
| `/buscar` | No | Búsqueda con filtros (mismo componente, modo search) |
| `/producto/:id` | No* | Ficha de producto (PDP) |
| `/carrito` | No** | Carrito |
| `/checkout` | Condicional | Confirmar pedido (delivery/pickup, mapa, cupón) |
| `/pago/:nro` | Sí | Pago Bancard (modo stub/live según config) |
| `/pedido/:nro` | Sí | Detalle / confirmación de pedido |
| `/seguimiento/:token` | No | Tracking público por token |
| `/mis-pedidos` | Sí | Historial del comprador |
| `/favoritos` | Sí | Listas de favoritos |
| `/seguidos` | Sí | Productos seguidos (reposición) |
| `/mi-cuenta` | Sí | Hub de cuenta (datos, direcciones, mayorista…) |
| `/login`, `/registro`, `/recuperar`, `/activar` | No | Ciclo de cuenta |

\* Ver precios B2B / stock sensible puede requerir sesión.  
\*\* Carrito anónimo o autenticado según flujo.

### 3.2 Catálogo y descubrimiento

**Qué hace**
- Muestra solo artículos con **`ActivoEcommerce = true`** (vitrina).
- Hero / branding dinámico (nombre tienda, logo, colores, tagline).
- Colecciones, slots de marketing, flyers/promos, marcas.
- Filtros: texto, marca, grupo/colección, solo con stock, orden.
- Canal B2C/B2B: al cambiar canal se refrescan precios y carrito.
- Empty state: distingue “filtros sin resultado” vs “catálogo sin publicar” y muestra el slug del tenant.

**Comportamiento**
- Si no hay productos en vitrina → mensaje + pista de tenant (el operador debe Activar en admin).
- Imágenes vía `resolveMediaUrl` (media local / URLs).
- Recomendaciones / engagement alimentan bloques de home.

### 3.3 Ficha de producto (PDP)

**Qué hace**
- Galería de imágenes, descripción, marca/línea.
- Selección de **embalaje** (unidad, caja…) y cantidad con **MOQ**.
- Precio según canal y lista aplicable.
- Stock: disponible / insuficiente / consultar (según config `ControlaStockEnCarrito` y existencias).
- Reseñas, favorito, seguir producto.
- Agregar al carrito con validación de stock y embalaje.

### 3.4 Carrito

**Qué hace**
- Alta / cambio cantidad / baja de ítems.
- Pickup vs delivery (flags de empresa).
- Reordenar desde un pedido anterior.
- Vaciar carrito.
- Gate de stock si la empresa controla stock en carrito.

**Comportamiento Standalone**
- Al confirmar pedido: baja `TotalExistencia` y sube `TotalPedidoVenta` (paridad Pedido ERP).
- Al cancelar pedido no migrado: restaura stock.

### 3.5 Checkout

**Qué hace**
- Datos de entrega / retiro en agencia.
- Mapa Leaflet → lat/lng en carrito/pedido.
- Cálculo de tarifa delivery por barrio/ciudad.
- Monto mínimo de pedido (config empresa).
- Cupón (engagement).
- Confirmación → crea `PedidoMovil` local + semilla de tracking.

**Comportamiento Integrated (objetivo)**
- Con `AllowWriteBack=true` el pedido puede insertarse en ERP `pedidomovil`.
- Hoy en dev: write-back **OFF** → el pedido vive solo en EcomDb.

### 3.6 Pago

**Qué hace**
- Flujo Bancard (checkout URL / confirm).
- Modo **Stub** por defecto (útil demo/UAT sin pasarela real).
- Modo **Live** si `Bancard:Mode=Live` + claves de empresa.

### 3.7 Cuenta del comprador

| Función | Comportamiento |
|---|---|
| Registro | Crea `UsuarioEcommerce` (+ cuenta asociada según flujo) |
| Activación | Link/token → marca cuenta activa; rehidrata mayorista si aplica |
| Login / recuperar | JWT + reset (SMTP real si `Smtp:Enabled`) |
| Mi cuenta | Perfil, documento, teléfonos, preferencias |
| Mis pedidos | Lista + acceso a detalle / reordenar |
| Favoritos | Secciones + ítems; alta al carrito desde lista |
| Seguidos | Productos a avisar (alertas email = roadmap, depende SMTP) |
| Mayorista | Solicitud → aprobación en admin → canal B2B |

### 3.8 Tracking de pedido

**Qué hace**
- Timeline de estados (`SeguimientoPedidoEcommerce`) en detalle y página pública `/seguimiento/:token`.
- Push SignalR / auto-refresh en UI de tracking.
- Admin puede asignar **chofer** (`IDChofer`) y avanzar estados.

### 3.9 Chat CRM (lado comprador)

**Qué hace**
- Widget flotante: conversaciones, mensajes, adjunto foto.
- Marcar leídos / cerrar conversación.
- Polling (+ hub SignalR disponible en API).

### 3.10 Branding y personalización visibles en tienda

Todo lo que el admin guarda en **Branding** / **Personalización** se refleja en runtime:
- Logo, favicon, banner, colores, redes, footer (dirección, mapa, WhatsApp…).
- Layout / temas / textos de marketing (`ConfiguracionPersonalizacion`).

### 3.11 Límites actuales del storefront

- Bancard Live y SMTP prod no son default.
- Stock de tenants sincronizados puede venir en 0 si el ERP no tiene filas en el depósito e-com.
- Imágenes ERP: sync de meta; binarios/export completo aún parcial.
- Chat: poll como fuente primaria; typing SignalR todavía pulible.

---

## 4. Panel administrativo — funcionalidades y comportamiento

Puerto típico: `http://localhost:4201`  
Rutas: `apps/admin/src/app/app.routes.ts`  
UI: AdminLTE + toolbars `dx-*`, menú alineado al ERP Site (`Site_App_Ecommerce_*`).

### 4.1 Acceso y shell

| Elemento | Comportamiento |
|---|---|
| `/login` | Usuario admin + password + **slug tenant** |
| Header | Slug, `#empresa`, **Tienda** (abre storefront con `?tenant=`), **Sync**, salir |
| Menú | Filtrado por permisos de sesión (API también exige permiso) |
| Demo | `demo` / `admin` / `Admin12!` · Casa Imperial: `casa-imperial` / `admin` / `Admin12!` |

### 4.2 Menú ERP-paridad (núcleo operativo)

| Ruta admin | Permiso típico | Función y comportamiento |
|---|---|---|
| `/` Dashboard | Acceso e-com | KPIs, checklist ops, atajos (activar, sync, abrir tienda) |
| `/configuracion` | Empresa / Agencia / Horario | Delivery/pickup, monto mínimo, depósito, Bancard keys, **agencias** (lat/lng/depósito), **horarios** |
| `/branding` | Empresa | Logo, favicon, banner, colores, redes → impacta storefront al instante |
| `/personalizacion` | Personalización | Schemas layout/tema/textos; versiones y programación |
| `/parametros-producto` | Productos | Maestros: MARCA, LINEA, TIPO, EMBALAJE, PROVEEDOR, PAIS, REGIMEN |
| `/catalogo` | Productos | Listado, filtros vitrina, editar, stock, “ver tienda” |
| `/catalogo/nuevo` · `…/editar` | Add/Edit | Alta tipo ArticuloDTO ERP; **no publica vitrina** hasta Activar |
| `/catalogo/activar` | Activar/Inactivar | Bulk publicar/ocultar `ActivoEcommerce` |
| `/catalogo/:id/imagenes` | Imágenes | Upload, reorder, principal |
| `/catalogo/:id/embalajes` · `/unidades` | Productos | Embalajes, UOM, códigos de barra |
| `/promociones` | Productos | Grupos / flyers, imagen, destino en tienda |
| `/listas-precios` | ListasPrecios | Contado / cheque / crédito, detalles, autorizar/anular, cambio vigencia, CSV |
| `/tarifas` | TarifaDelivery | Tarifas por zona; flujo de autorización |
| `/clientes` | Clientes | Cuentas, usuarios e-com, solicitudes **mayorista** (aprobar/rechazar) |
| `/carritos` · `/carritos/:id` | CarritoCompras | Carritos abiertos, export/trazabilidad |

### 4.3 Ops / extras

| Ruta | Función y comportamiento |
|---|---|
| `/sync` | **Pull solo lectura** ERP → EcomDb (catálogo, precios, stock, clientes, shipping, promos…). No modifica ERP. Botón **Publicar todo lo fuera de vitrina** + link Activar + Ver tienda |
| `/pedidos` · `/pedidos/:id` | Pedidos locales; detalle; tracking; asignación chofer; migrar ERP (simulado o write-back si flag ON) |
| `/choferes` | Maestro de choferes para fulfillment |
| `/crm` | Inbox chat: asignar, no leídos (badge), cerrar/reabrir, adjuntos |
| `/marketplace` | Moderación reseñas, cupones, devoluciones |
| `/reportes` | Ventas por rango/día (v1) |
| `/empresas` | Multi-tenant (provisioning / listado) |
| `/manager` | RBAC: usuarios admin, roles, funcionalidades (`Site_App_Ecommerce_*`) |

### 4.4 Comportamiento clave del backoffice

1. **Lo que activás en vitrina aparece en la tienda** (`ActivoEcommerce`). El sync trae artículos; muchos llegan OFF → hay que publicar.
2. **Branding/personalización** no son tablas ERP: son capa e-com; cambian la experiencia del comprador sin tocar el ERP.
3. **Listas de precios** gobiernan lo que ve B2C/B2B; la autorización de listas/tarifas refleja el flujo ERP.
4. **Stock admin** edita espejo local; en Integrated el ERP es la fuente de verdad al facturar.
5. **Sync** conserva IDs ERP para facilitar corte Integrated; `AllowWriteBack=false` impide INSERT en Postgres ERP.
6. **CRM** es nativo e-com (no ERP): cola tipo WhatsApp/Callbell para la tienda.

### 4.5 Límites actuales del admin

- Reportes aún básicos.
- Pedidos: sin máquina de estados de packing/fulfillment completa tipo WMS.
- Nav filtra por permiso; no todas las rutas tienen guard fino por código (la API sí valida).
- Planillas: CSV (Excel EPPlus = roadmap).
- Imágenes/stock desde ERP pueden quedar incompletos según datos del depósito e-com.

---

## 5. Matriz: quién hace qué (impacto cruzado)

| Acción en Admin | Efecto en E-commerce |
|---|---|
| Activar producto | Aparece en `/catalogo` y búsqueda |
| Ocultar producto | Desaparece de vitrina (sigue en admin) |
| Cambiar precio en lista | Nuevo precio en PDP/carrito según canal |
| Subir logo / colores | Header/footer/tema de la tienda |
| Personalización marketing | Home, slots, textos |
| Aprobar mayorista | Comprador puede usar canal B2B |
| Crear tarifa delivery | Checkout calcula envío |
| Avanzar tracking / chofer | Comprador ve timeline / seguimiento público |
| Responder CRM | Mensajes en widget del storefront |
| Sync ERP + publicar | Catálogo/tenant real (ej. Casa Imperial) en vitrina |
| Write-back OFF | Pedido del comprador **no** llega al ERP |

| Acción en E-commerce | Efecto en Admin |
|---|---|
| Agregar al carrito | Visible en `/carritos` |
| Confirmar checkout | Pedido en `/pedidos`; stock local si aplica |
| Solicitar mayorista | Cola en `/clientes` |
| Enviar chat | Inbox `/crm` + badge |
| Dejar reseña / devolución | `/marketplace` |
| Seguir / favorito | Datos engagement (ops / futuro marketing) |

---

## 6. Ciclo comercial de punta a punta (comportamiento esperado)

```
Admin: Sync (opcional) → Activar vitrina → Branding OK
        ↓
Comprador: Catálogo → PDP → Carrito → Checkout → Pago → Pedido
        ↓
Admin: Ve pedido → (opcional) chofer/tracking → (opcional) migrar ERP
        ↓
Comprador: Mis pedidos / seguimiento público / chat
```

**Demo local**

| Rol | Credencial |
|---|---|
| Tenant demo | slug `demo` · comprador `demo` / `demo123` · admin `admin` / `Admin12!` |
| Tenant ERP sync | slug `casa-imperial` · admin `admin` / `Admin12!` · tienda `?tenant=casa-imperial` |

---

## 7. Relación con el ERP Datamex (contexto)

| Capa | Estado |
|---|---|
| Esquema / menú / permisos / conceptos | Alineados (espejo de tablas + `Site_App_Ecommerce_*`) |
| Sync inbound lectura | **ecom-etl** (app independiente multiempresa). C# `ErpSyncHostedService` off (`Erp:Enabled=false`). Checklist: `ecom-etl/docs/CHECKLIST.md` |
| Write-back PedidoMovil | Código listo; **gate** `AllowWriteBack` (default false) |
| Bancard Live / SMTP prod | Configurables; no default de desarrollo |
| Paridad menú admin ↔ ERP Site | Documentada en `GAPS-ERP-PARITY.md` |

---

## 8. Conclusión

- El **e-commerce** es la experiencia de compra completa: vitrina, precios por canal, carrito, checkout con mapa, pago, cuenta, favoritos, tracking y chat.
- El **panel administrativo** es el centro de operación de la tienda: maestros ERP-like, vitrina, precios, envíos, clientes, pedidos, CRM, branding y sync.
- Juntos forman un **e-commerce Standalone usable hoy**, con forma ERP-ready para Integrated cuando se habilite write-back y se complete el circuito de stock/imágenes/pagos Live.

### Documentos relacionados

| Doc | Contenido |
|---|---|
| [`INFORME-ESTADO.md`](./INFORME-ESTADO.md) | Estado técnico / madurez / arquitectura |
| [`GAPS-ERP-PARITY.md`](./GAPS-ERP-PARITY.md) | Paridad menú y gaps Integrated |
| [`ROADMAP-FUNCIONALIDADES.md`](./ROADMAP-FUNCIONALIDADES.md) | Backlog priorizado de features |
| [`SPEC-TRACKING-PERSONALIZACION.md`](./SPEC-TRACKING-PERSONALIZACION.md) | Tracking + personalización |
| [`ERP-LIVE-PANORAMA.md`](./ERP-LIVE-PANORAMA.md) | Panorama tenant ERP vivo |
| [`ERP-SCHEMA-ALIGNMENT.md`](./ERP-SCHEMA-ALIGNMENT.md) | Alineación de esquema |
