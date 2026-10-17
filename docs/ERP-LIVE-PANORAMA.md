# Panorama ERP vivo (conexión verificada)

Fecha: 2026-08-03  
Host: `10.99.99.74`  
DB Site/Ecom: `erp_cervepar_pruebas` (schema **`dbo`**)  
Usuario app: `iis_user` (Web.config `DefaultConnection`)

> No se documentan claves Bancard / SMTP / passwords aquí.

---

## Tenant e-commerce real

| Campo | Valor ERP |
|---|---|
| Empresa | **id=41 · CASA IMPERIAL** (RUC 80002212-2) |
| Delivery / Pickup | ambos `true` |
| Cobra entrega / al pedir | ambos `true` |
| Monto mínimo | **5000** |
| `ProcesaPedidosERP` | **true** |
| `ProcesaPedidosPorPlanilla` | **true** |
| `ControlaStockEnCarrito` | **false** |
| `IDDeposito` | **1** · CENTRAL MINGA GUAZU |
| `IDArticuloDelivery` | **467647** (SKU 8094 · artículo ecom activo) |
| Últ. nro carrito | 52 |
| Bancard | claves + comercio/sucursal configurados |
| Checkout URL | apunta al ecom ERP Site |

También hay config ecom en `erp_cmsa_desarrollo` (empresa id=31, mínimo 150000, stock control off).

---

## Volúmenes dbo (cervepar_pruebas)

| Tabla | Filas (aprox.) | Nota |
|---|---:|---|
| articulo | 249 668 | solo **5** con `activoecommerce=true` |
| articuloembalaje | 257 706 | |
| articuloimagen | 2 209 | |
| marca / linea / tipoarticulo | 4 015 / 4 081 / 2 289 | maestros reales |
| proveedor / pais / regimenimpositivo | 50 259 / 257 / 193 | |
| listaprecio | 181 | **1** con `eslistaprecioecommerce` (id 483, estado 2 AUTORIZADO) |
| detallelistapreciocondicionventa | 5 | **solo 5 precios** en lista ecom |
| stock | 257 240 | buckets ERP: existencia, pedidoVenta, venta, reserva… |
| deposito | 1 639 | |
| carritocompra | 38 | todos idempresa **41** |
| pedidomovil | 769 238 | origen 0≈732k, 1≈37k, **2≈283** (ecom) |
| usuarioecommerce | 13 | todos empresa 41 |
| tarifadelivery | 126 | |
| grupoarticuloecommerce | 5 | |
| agencia / horarioatencion / chofer | 152 / 18 / 369 | |
| cuenta | 257 994 | |

---

## Columnas artículo ERP relevantes vs admin nuevo

ERP `dbo.articulo` (ecom):  
`nroarticulo, sku, descripciones, codreferenciaproveedor, idmarca, idlinea, idproveedor, idpais, activo*, activoecommerce`

Implica: el ABM e-com nuevo (marca/línea/proveedor/país/régimen + activar) está alineado a columnas reales.  
`idtipoarticulo` existe en maestro `tipoarticulo` (usar como en ERP Create).

---

## Implicaciones para la plataforma nueva

1. **Integrated debe apuntar a schema `dbo`** (no `public`) en `erp_cervepar_pruebas` (o réplica).
2. Tenant demo ≠ Casa Imperial: en vivo el catálogo ecom publicado es **mínimo (5 SKUs + 5 precios)**.
3. En producción Casa Imperial **no controla stock en carrito**; el movimiento fuerte es Pedido/facturación ERP.
4. Write-back PedidoMovil debe usar `OrigenPedido` ecom (valor **2** en esta DB) + `IDEmpresa=41`.
5. Lista ecom autorizada id **483** es la fuente de precios vitrina.
6. Sync inbound debe priorizar: articulo(+activoecommerce), embalaje, imagen, listaprecio 483 + detalles, stock depósito 1, tarifas, agencias/horarios, usuarios ecom.

---

## Conexión de referencia (sin secretos)

```
Server=10.99.99.74; Database=erp_cervepar_pruebas; User Id=iis_user; …
search_path efectivo de tablas ERP Site: dbo
```
