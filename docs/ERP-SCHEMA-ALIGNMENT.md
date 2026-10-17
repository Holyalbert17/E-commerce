# Alineación de tablas ERP ↔ E-com (flujo + plan)

Fecha: 2026-08-03  
Fuente viva: `erp_cervepar_pruebas` schema **`dbo`**, columnas **lowercase**.

---

## Veredicto

El e-com nuevo ya **espeja los nombres de tabla** del ERP (`articulo`, `carritocompra`, `pedidomovil`, …), pero **no es plug-and-play** para pasar datos:

| Capa | ERP vivo | E-com actual |
|---|---|---|
| Schema | `dbo` | `dbo` (Postgres e-com) |
| Columnas | `activoecommerce`, `id`, … | EF PascalCase quoted (`"ActivoEcommerce"`); sync SQL lee ERP en lowercase |
| IDs | mismos PK del ERP | sync ID-preserving; demos propios |
| Extras e-com | no existen en ERP | `EstadoEcommerce`, `NombreMarca`, branding, cupones, RBAC admin, `synccursor`, … |
| Tabla tracking | **no existe** `seguimientopedidoecommerce` | sí en e-com |
| Precios carrito ERP | usa también `detallelistaprecio.precio` | solo `detallelistapreciocondicionventa` |
| Régimen en artículo | **no** hay `idregimen` en `dbo.articulo` | campo local + `catalogoparametro` |

Write-back actual (`PedidoErpMigrationService`) inserta `"ID"`, `"Activo"`… → **falla** en ERP (`column "ID" does not exist`). Debe usar `dbo.pedidomovil` + `id`, `activo`, …

---

## Flujo ERP (referencia operativa)

```
UsuarioEcommerce / Cuenta
        │
        ▼
CarritoCompra + DetalleCarritoCompra
  · lista: Cuenta.IDListaPrecio  o  ListaPrecio.EsListaPrecioEcommerce
  · precio catálogo: DetalleListaPrecioCondicionVenta.PrecioContado
  · precio add-to-cart ERP: DetalleListaPrecio.Precio  (divergencia histórica)
  · stock check (si ControlaStockEnCarrito): Stock.TotalExistencia @ IDDeposito config
        │  Checkout / Bancard
        ▼
PedidoMovil (OrigenPedido=2) + DetallePedidoMovil
  · NO mueve TotalPedidoVenta en el checkout ecom
  · stock de compromiso lo hace el Pedido ERP al migrar/procesar
        │
        ▼
ProcesaPedidosERP / planilla → Pedido comercial → Stock.TotalPedidoVenta +=
```

Config por empresa: `empresaconfiguracionecommerce` (PK = id empresa). Casa Imperial = **41**.

**Paridad config (ago-5):** e-com espeja campos ERP de `empresa` + `empresaconfiguracionecommerce` (SMTP, ETA pickup/delivery, Bancard keys, depósito, flags) + tabla `empresaconfiguracionecommerceredsocial`. Extensiones locales: slug, branding, mayorista, `ModoIntegracion`. Sync pull completo en `SyncEmpresaAsync` (aún con `Erp:Enabled=false` hasta decisión).

---

## Flujo e-com nuevo (hoy)

```
Standalone (SQLite/Postgres propio)
  Checkout → pedidomovil local
           → si ControlaStockEnCarrito: TotalExistencia− / TotalPedidoVenta+
  Sync: stub (cuenta filas locales)
  Migración ERP: INSERT crudo (casing incorrecto) o simulado

Integrated (objetivo)
  Leer maestros dbo.*  ·  No mutar stock local  ·  Write-back PedidoMovil lowercase
```

---

## Mapa de tablas (núcleo compartible)

| Tabla ERP `dbo` | En e-com | Rol | Notas para migrar datos |
|---|---|---|---|
| `empresaconfiguracionecommerce` | sí | config tienda | OK core; extras branding solo e-com |
| `articulo` | sí (subset) | catálogo | Filtrar `activoecommerce`; **IDs deben coincidir** en Integrated |
| `articuloembalaje` / `articuloimagen` | sí | UOM / media | sync por `id` |
| `marca` `linea` `tipoarticulo` `proveedor` `pais` | ERP sí / e-com vía `catalogoparametro` + nombres denorm. | maestros | Mejor sync FK reales o vistas |
| `listaprecio` | sí | vitrina | flag `eslistaprecioecommerce` |
| `detallelistapreciocondicionventa` | sí | precios | OK |
| `detallelistaprecio` | **falta en e-com** | precio simple carrito ERP | Agregar si se quiere paridad exacta |
| `stock` / `deposito` | sí | disponibilidad | depósito config |
| `cuenta` / `usuarioecommerce` / `usuarioecommercecuenta` | sí | clientes | |
| `carritocompra` / `detallecarritocompra` | sí | carrito | operativo e-com |
| `pedidomovil` / `detallepedidomovil` | sí | pedido | write-back crítico |
| `tarifadelivery` `agencia` `horarioatencion` | sí | entrega | |
| `grupoarticuloecommerce` | sí | promos | |
| `logpagotarjeta` | sí | Bancard | |
| `listapreciocuenta` | sí | historial listas | OK (ago-5): sync customers + admin historial |
| `seguimientopedidoecommerce` | **solo e-com** | tracking UI | quedarse en DB e-com |

---

## Arquitectura recomendada (pasar datos fácil)

**No** mezclar columnas solo-ecom dentro de tablas ERP productivas.

```
┌─────────────────────────────┐     sync ID-preserving      ┌──────────────────────┐
│  ERP Postgres dbo.*         │ ◄──────────────────────────► │  Ecom DB             │
│  articulo, stock, listas,   │   pull maestros / push      │  espejo lowercase    │
│  cuenta, pedidomovil, …     │   PedidoMovil               │  + schema ecom_ext   │
└─────────────────────────────┘                             │  (branding, cupones, │
                                                            │   seguimiento, RBAC) │
                                                            └──────────────────────┘
```

### Reglas

1. **Tablas núcleo** = mismos nombres + **columnas lowercase** + mismos tipos/IDs que ERP.  
2. **Extensiones** (`EstadoEcommerce`, `NombreMarca`, branding, cupones, admin, sync cursors) → tablas `*ecommerce` / schema `ecom` con `idarticulo` / `idempresa` FK lógica.  
3. **Integrated**: `search_path=dbo` o `dbo.` en todo SQL; no mutar `stock` desde e-com (seguir flag ERP).  
4. **Standalone**: sigue con DB propia (demo), pero el **modelo** ya alineado para un `pg_dump`/ETL por tabla.

### Fases de implementación

| Fase | Qué | Resultado |
|---|---|---|
| **0** | Write-back SQL lowercase + `AllowWriteBack=false` | Sin tocar ERP por defecto |
| **1** | Sync pull real (empresa/catalog/prices/stock/customers/shipping/promotions) | Datos en EcomDb local id=41 |
| **2** | Preservar IDs ERP al upsert local | Hecho en sync |
| **3** | Entidad `DetalleListaPrecio` | Hecho |
| **4** | Extensiones fuera de tablas núcleo | Parcial |
| **5** | (Opcional) Integrated hot-path | Pendiente |

**Garantía:** con `Erp:AllowWriteBack=false` el inbound sync y los writers **no hacen INSERT/UPDATE en ERP**.


---

## Checklist de paridad de flujo

- [x] Nombres de tablas núcleo  
- [ ] Casing columnas = ERP lowercase  
- [ ] Schema `dbo` en conexión Integrated / write-back  
- [ ] Sync pull real (hoy stub)  
- [x] `OrigenPedido = 2` ecom  
- [ ] Write-back lowercase (fase 0)  
- [ ] No aplicar stock local si `ControlaStockEnCarrito=false` / Integrated  
- [ ] `detallelistaprecio` en modelo e-com  
- [ ] Tracking / branding solo en tablas e-com  
