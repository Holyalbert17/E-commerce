# Arquitectura

## Idea

Datamex E-Commerce es una tienda multi-empresa apoyada en el modelo del ERP Datamex. Las tablas (`articulo`, `listaprecio`, `stock`, `carritocompra`, `pedidomovil`, `cuenta`, `empresa`, …) tienen la misma forma que en el ERP, en minúsculas. Así un pedido o un precio se pueden leer de los dos lados sin una traducción de esquema.

```
Comprador  →  Storefront :4200  ─┐
                                 ├─→  API :5082  →  EcomDb (SQLite o PostgreSQL)
Operador   →  Admin :4201      ─┘         │
                                          ├─→  archivos en /media
                                          ├─→  SignalR /hubs/chat
                                          └─→  ERP (opcional: sync de entrada y write-back de pedidos)
```

La sincronización masiva de catálogo desde el ERP hacia la tienda corre en un proceso separado (`ecom-etl`), no en el request del comprador. La API atiende el camino de compra aunque el ERP no esté disponible.

## Capas

| Proyecto | Responsabilidad |
|---|---|
| `Datamex.ECommerce.Domain` | Entidades y enums. Sin infraestructura. |
| `Datamex.ECommerce.Application` | Casos de uso: catálogo, precios, carrito, checkout, cuenta, admin, sync. |
| `Datamex.ECommerce.Infrastructure` | EF Core, migraciones, seed, envío de mail, pagos, acceso a datos. |
| `Datamex.ECommerce.Api` | Controladores, JWT, resolución de tenant, Swagger, SignalR, health check, archivos estáticos de media. |

Las apps Angular no hablan con la base. Todo pasa por la API.

## Tenant

Una empresa del ERP es una tienda.

1. El middleware lee `X-Tenant-ID` o `X-Tenant-Slug`.
2. Resuelve `IDEmpresa` y el resto de la request queda acotada a esa empresa.
3. El alta de una empresa (`/api/admin/tenants`) no exige slug: es una operación de plataforma.

Hay dos altas, y las dos escriben las mismas tablas:

| Vía | Endpoint | Resultado |
|---|---|---|
| Manual | `POST /api/admin/tenants/manual` | `IDEmpresa` local, `ProcesaPedidosERP=false`, datos mínimos y un admin |
| Desde ERP | `POST /api/admin/tenants/from-erp` | Reutiliza el `IDEmpresa` del ERP, `ProcesaPedidosERP=true` |

El panel las ofrece en **Empresas** (`/empresas`).

## Autenticación

Hay dos JWT distintos.

- **Comprador.** `UsuarioEcommerce`. Login, registro, activación por mail y recuperación. El carrito de invitado usa el header `X-Cart-Session` hasta que la cuenta se vincula.
- **Operador.** `AdminUsuario`, con permisos `Site_App_Ecommerce_*` y permisos de plataforma `manager:*`. Cada ruta del panel declara qué permiso exige.

La firma sale de `ECommerce:JwtSigningKey`. SignalR acepta el token en el query `access_token` del hub `/hubs/chat`.

Límites por IP: la API general, el login y el formulario de contacto tienen ventanas distintas. En Development el techo de la API es más alto porque tienda y panel comparten la misma IP.

## Modos

`ECommerce:IntegrationMode`:

| Modo | Comportamiento |
|---|---|
| Standalone | La tienda es la fuente. Al confirmar un pedido baja `TotalExistencia` y sube `TotalPedidoVenta`. Cancelar un pedido no migrado devuelve el stock. |
| Integrated | Misma empresa que el ERP. El catálogo puede venir del sync. Con `Erp:AllowWriteBack=true` el pedido puede insertarse en `pedidomovil` del ERP (`OrigenPedido` de e-commerce). |
| External | Conexión a un ERP que no es el proceso local. |

`Erp:Enabled` y `Erp:HybridLazyPull` quedan apagados en la API: la entrada de datos del ERP la hace `ecom-etl`.

## Datos

| Entorno | Provider | Dónde se configura |
|---|---|---|
| Desarrollo aislado | SQLite | `Database:Provider=Sqlite` y `Data Source=...` |
| Desarrollo compartido o producción | PostgreSQL (Npgsql) | `Database:Provider=Postgres` y `ConnectionStrings:EcomDb` |

El historial de migraciones en PostgreSQL vive en el esquema `dbo` (`__EFMigrationsHistory`). `docker-compose.yml` levanta PostgreSQL 18 en el puerto 5433 para no chocar con un Postgres del host.

Imágenes y adjuntos van a `Media:RootPath` (por defecto `App_Data/media`) y se publican en `/media`. Esa carpeta es datos de runtime: no entra al repositorio.

## Compra

1. El catálogo lista solo artículos con `ActivoEcommerce`.
2. El precio sale de la lista del canal (`listaprecio` + detalle, marcada para e-commerce).
3. El carrito valida cantidad contra `SUM(stock.TotalExistencia)` del depósito configurado en la empresa, si `ControlaStockEnCarrito` está activo.
4. El envío aplica una `tarifadelivery` por barrio o tarifa y agrega la línea del artículo de delivery.
5. El checkout cierra el carrito y crea el `pedidomovil`.
6. El pago Bancard puede ir en `Stub` (sin cargo real) o `Live` (vPOS).

La geografía de entrega sigue la cadena del ERP: división política → ciudad → localidad → distrito → barrio. El checkout de la tienda pide ciudad y barrio y guarda lat/lng.

## Tiempo real y búsqueda

- **Chat y seguimiento.** SignalR en `/hubs/chat`. El comprador puede abrir un tracking público por token, sin login.
- **Búsqueda.** Motor local por defecto (`Search:Engine=Local`), con cache corta. Redis u OpenSearch se configuran y quedan vacíos hasta que hagan falta.

## Qué no está en este repositorio

- El job `ecom-etl` que copia catálogo, precios y stock desde el ERP.
- Certificados, contraseñas y URLs de producción. Van en la configuración local o en el entorno del servidor.
- Los binarios de corridas anteriores (`_run-api*`, `artifacts`, `bin`, `obj`). Se regeneran con `dotnet build` y `ng build`.
