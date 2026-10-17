# Postgres e-commerce — servidor 10.99.99.205



**Fecha:** 5 ago 2026  

**Estado:** cluster standalone en **5432** · schema `dbo` · tenant **Casa Imperial** (`casa-imperial` / empresa **41**) operativo



## Por qué antes era 5433



En este host ya existía **Patroni HA** (PG 17) con data en `/var/lib/postgresql/data` y puerto estándar **5432**, pero etcd de otros nodos no responde → Patroni no levanta Postgres y **5432 quedaba vacío**.



Para no tocar el data dir de Patroni se creó el cluster Ubuntu `17/ecom` en **5433**.  

A pedido, se **movió a 5432** y se detuvo Patroni (sigue `disabled` al boot) para liberar el puerto. El data de Patroni **no se borró**.



## Cluster e-commerce (actual)



| Ítem | Valor |

|---|---|

| Host | `10.99.99.205` |

| Puerto | **5432** |

| Cluster | `17/ecom` |

| Data | `/var/lib/postgresql/17/ecom` |

| Database | `ecommerce` |

| Schema | **`dbo`** (+ `public`) |

| User | `ecom` |

| Password | definida en el entorno, no en el repositorio |

| `pg_hba` | `10.74.74.0/24`, `10.74.74.108/32`, `10.99.99.0/24`, localhost |

| listen | `*` |

| Tablas | **62** en `dbo` · migraciones `InitialPostgres` + `EmpresaErpParity` |



Connection string:



```

Host=SERVIDOR;Port=5432;Database=ecommerce;Username=ecom;Password=CAMBIAR;SSL Mode=Prefer;Search Path=dbo,public

```



## App QA en servidor (ago 2026)

| Rol | Host | URL |
|-----|------|-----|
| Postgres + ecom-etl | `10.99.99.205` | panel `http://10.99.99.205:8787/` |
| API + vitrina + admin | `10.99.99.189` | `http://10.99.99.189/` · admin `/admin/` |

La API Production usa `EcomDb` → este host (`10.99.99.205:5432`). El ETL **no** corre en 189.

## App local — Casa Imperial



- `appsettings.json` + `appsettings.Development.json` → **Postgres :5432**

- Development: **`Erp:Enabled=true`**, `IDEmpresa=41`, full catalog + all lists + publish vitrina

- `AllowWriteBack=false` (solo lectura hacia ERP)

- `ECommerce:DefaultTenantSlug=casa-imperial` (middleware + Angular admin/storefront)

- Admin: slug `casa-imperial` / `admin` / `Admin12!`

- Vitrina: `http://localhost:4200` (default slug) o `?tenant=casa-imperial`

- Precios: vitrina usa lista `EsListaPrecioEcommerce` (CV + fallback `detallelistaprecio`)



### Herramientas



```bash

# Sync full (o entidades sueltas)

# Inbound: ecom-etl (no ErpSyncOnce)
# cd ../../ecom-etl && ecom-etl -c config/local.yaml once --mode full --tenants multi



# Ajustar slug/branding/listas e-com + admin RBAC

dotnet run --project tools/ConfigureCasaImperial

```



## Scripts remotos Postgres



```bash

python tools/remote_pg_setup.py diag

python tools/remote_pg_setup.py verify

python tools/remote_pg_setup.py sync-progress

```



SSH: `userdb` / `Dtmx.2025`



## Pendiente



1. Si se repara etcd/Patroni: **no** arrancar Patroni sin coordinar (chocaría con ecom en 5432).  

2. Rotar la contraseña de Postgres antes de publicar el repositorio.  

3. `AllowWriteBack=true` solo tras UAT de PedidoMovil.



## ERP / Independent



- Sin CS Erp o `Erp:Enabled=false` → Standalone.  

- Con CS + `Enabled=true` → pull híbrido / write-back gateado.  

- Schema `dbo` alineado al ERP Datamex. **Nunca escribir en la base ERP** desde ecom.


