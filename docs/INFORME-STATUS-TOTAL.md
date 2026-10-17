# Informe de status total — Casa Imperial / Datamex E-Commerce

**Fecha:** 6 de agosto de 2026 (snapshot ~09:50 ART)  
**Tenant:** `casa-imperial` · **Empresa ERP/ecom:** `41` · **Depósito vitrina:** `305`  
**Modo:** Integrated (pull ERP → ecom) · **`AllowWriteBack=false`** (solo lectura hacia ERP)  
**Fuentes:** `python tools/remote_pg_setup.py` (`status` / `verify` / `sync-progress` / `price-counts`) + API local `:5082`

---

## 1. Resumen ejecutivo

| Área | Estado | Nota |
|---|---|---|
| Postgres e-com `10.99.99.205:5432` | **OK** | Cluster standalone `17/ecom` active/enabled |
| Patroni en el mismo host | **OFF** | `inactive` / `disabled` (no arrancar sin coordinar) |
| Schema / migraciones | **OK** | `dbo` · **64** tablas · 3 migraciones EF |
| Sync ERP → ecom (empresa 41) | **OK** | 8/8 entidades `LastStatus=ok` |
| Precios en vitrina | **OK** | Lista operativa **#108** (~20.5k detalles); API página 1: **24/24** con `precioDesde` |
| API local | **UP** | `http://localhost:5082` → 200 |
| Storefront / Admin Angular | **DOWN** (esta máquina) | `:4200` / `:4201` no responden ahora |
| Write-back PedidoMovil | **OFF** | Hard rule vigente |

**Veredicto:** infraestructura e-com y espejo ERP **operativos** para catálogo/stock/precios de lectura. La tienda Angular hay que levantarla localmente para UI; el backend ya sirve datos con precios.

---

## 2. Infraestructura Postgres (`10.99.99.205`)

| Ítem | Valor |
|---|---|
| Servicio | `postgresql@17-ecom` → **active / enabled** |
| Data / conf | `/var/lib/postgresql/17/ecom` · `/etc/postgresql/17/ecom/` |
| Puerto | **5432** · `listen_addresses=*` |
| Database | `ecommerce` |
| Schema | `dbo` (+ `public` en search_path) |
| User app | `ecom` / search_path `dbo, public` |
| Tablas `dbo` | **64** |
| Patroni | **inactive / disabled** (data histórico conservado; no competir por 5432) |

### `pg_hba` (red)

- `127.0.0.1/32`
- `10.74.74.0/24` + `10.74.74.108/32` (VPN)
- `10.99.99.0/24` (LAN)

### Connection string (app)

```
Host=10.99.99.205;Port=5432;Database=ecommerce;Username=ecom;Password=***;SSL Mode=Prefer;Search Path=dbo,public
```

### Migraciones aplicadas

| MigrationId | EF |
|---|---|
| `20260805153448_InitialPostgres` | 8.0.11 |
| `20260805160529_EmpresaErpParity` | 8.0.11 |
| `20260805191212_AdminUsuarioFuncionalidadHierarchy` | 8.0.11 |

Doc operativa: [`POSTGRES-SERVER-205.md`](./POSTGRES-SERVER-205.md).

---

## 3. Datos tenant Casa Imperial (empresa 41)

| Entidad | Count |
|---|---|
| `empresa` (global ecom) | 2 |
| `empresaconfiguracionecommerce` | 1 (`casa-imperial`) |
| `articulo` | **23 760** |
| `articulo` ActivoEcommerce | **15 564** |
| Catálogo API (`totalCount` B2C) | **12 545** |
| `stock` | **15 203** |
| `listaprecio` | 9 |
| `detallelistaprecio` | **75 381** |
| `detallelistapreciocondicionventa` | 5 |
| `cuenta` | 7 |

Config ecom (post `ConfigureCasaImperial`): slug `casa-imperial`, display **CASA IMPERIAL**, modo Integrated, depósito **305**.

---

## 4. Sync ERP → ecom (`synccursor`)

Todos **ok** al snapshot:

| EntityKey | Status | RowsProcessed | LastRunUtc (UTC) |
|---|---|---|---|
| empresa | ok | 1 | 2026-08-06 12:51:31 |
| catalog | ok | 48 179 | 2026-08-06 12:40:12 |
| prices | ok | 75 395 | 2026-08-06 12:44:01 |
| stock | ok | 15 202 | 2026-08-06 12:49:51 |
| customers | ok | 29 | 2026-08-06 12:51:05 |
| shipping | ok | 149 | 2026-08-06 12:51:14 |
| promotions | ok | 12 | 2026-08-06 12:51:24 |
| order_status | ok | 0 | 2026-08-06 12:51:29 |

**Reglas vigentes**

- Pull **solo lectura** hacia ERP (`AllowWriteBack=false`).
- Sync de embalajes mapea UOM (metro/kg/…) cuando la descripción ERP lo permite.
- Flags `EsListaPrecioEcommerce` promovidos por `configure-ci` **no se demotan** en el siguiente sync de precios.

Herramientas: `tools/ErpSyncOnce`, `tools/ConfigureCasaImperial`, `tools/remote_pg_setup.py`.

---

## 5. Precios / lista e-com

### Situación ERP vs vitrina

| ID | Nro | Descripción | EsLista e-com | Detalles |
|---|---|---|---|---|
| **108** | 1 | LISTA PRECIO NRO. 1 | **sí (operativa)** | **20 504** |
| 109 | 2 | LISTA PRECIO NRO. 2 | no | 18 193 |
| 110 | 3 | LISTA PRECIO NRO. 3 | no | 18 590 |
| 111 | 4 | LISTA PRECIO NRO. 4 | no | 18 077 |
| 483 | 5 | LISTA PRECIO ECOMMERCE | no (desmarcada) | 17 (+ 5 CV) |
| 484+ | … | otras / ofertas / prueba | no | 0 |

**Acción 6 ago:** `ConfigureCasaImperial` desmarcó #483 (casi vacía) y promovió **#108** como lista e-com vigente (`Estado=2`, canal `BOTH`).  
**Smoke API:** `GET /api/catalog/products?canal=B2C&page=1&pageSize=24` → **24/24** con `precioDesde` (tenant `casa-imperial`).

Pendiente de negocio ERP (opcional): poblar de verdad la lista #483 en el ERP si se quiere volver a usarla como lista “oficial” ecommerce.

---

## 6. Aplicaciones (esta estación de trabajo)

| Servicio | Puerto | Status snapshot |
|---|---|---|
| API ASP.NET | **5082** | **UP** (HTTP 200) |
| API alt | 5081 | down |
| Storefront Angular | 4200 | **down** |
| Admin Angular | 4201 | **down** |

Credenciales admin (dev): slug `casa-imperial` / `admin` / `Admin12!`.

Para UI: levantar storefront/admin (`ng serve`) apuntando a la API en 5082.

---

## 7. Cambios recientes relevantes (sesión)

### Storefront UX
- Campo **tenant oculto** en login/registro/forgot; auto-carga `casa-imperial` (`demo` → migra).
- Home tipo Nissei: franja de confianza + “Ofertas del día”.
- PDP: venta por metros (paso, mínimo, subtotal) si embalaje permite fracción / UOM M.

### Backend / ops
- Sync embalajes: UOM desde ERP + heurística metro/tela.
- `ConfigureCasaImperial`: remapeo automático de lista e-com por cobertura de precios.
- Sync precios: preserva promoción local `configure-ci`.
- Scripts remotos alineados a cluster **17/ecom** (`remote_status.sh`, `price-counts`).

### Admin
- Árbol de operaciones / RBAC (paridad TreeEmpresaUsuarioOperacion) ya migrado (`AdminUsuarioFuncionalidadHierarchy`).

---

## 8. Riesgos y pendientes

| Prioridad | Ítem |
|---|---|
| P0 ops | No reactivar **Patroni** en 205 sin coordinar (conflicto de puerto/data con `17/ecom`). |
| P1 negocio | Decidir si #483 se carga en ERP o se mantiene #108 como lista vitrina. |
| P1 prod | Rotar la contraseña de Postgres antes de exposición amplia. |
| P2 | Optimizar sync de precios (hoy N+1 `FindAsync`; lento en corridas grandes). |
| P2 | `AllowWriteBack=true` solo tras UAT PedidoMovil. |
| P2 | CV (`detallelistapreciocondicionventa`) casi vacío; vitrina usa fallback `detallelistaprecio` (OK hoy). |

---

## 9. Comandos útiles

```bash
# Status / verify / sync / precios (SSH a 205)
python tools/remote_pg_setup.py status
python tools/remote_pg_setup.py verify
python tools/remote_pg_setup.py sync-progress
python tools/remote_pg_setup.py price-counts

# Pull ERP → ecom
dotnet run --project tools/ErpSyncOnce -- empresa catalog prices stock

# Remap branding / lista e-com / RBAC admin
dotnet run --project tools/ConfigureCasaImperial
```

---

## 10. Conclusión

El **status total** del entorno Casa Imperial es **verde en datos e infraestructura**: Postgres standalone sano, migraciones al día, sync completo en OK, lista de precios operativa con cobertura real y API sirviendo catálogo con precio. Falta en esta máquina levantar **storefront/admin**; el write-back a ERP sigue deliberadamente apagado.
