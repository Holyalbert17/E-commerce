# Datamex E-Commerce

Plataforma de tienda online para empresas que ya operan con el ERP Datamex. El comprador usa la tienda, el operador usa el panel, y ambos hablan con la misma API. El catálogo, los precios, el stock, los pedidos y las cuentas viven en tablas con la misma forma que el ERP: no hay un esquema paralelo.

Cada empresa es un tenant (`IDEmpresa`). La tienda se elige por slug (`demo`, `casa-imperial`, …) con el header `X-Tenant-Slug` o `X-Tenant-ID`.

## Qué incluye

| Aplicación | Tecnología | Puerto local | Para quién |
|---|---|---|---|
| API | ASP.NET Core 8 | `5082` | Tienda, panel y sincronización |
| Storefront | Angular 19 | `4200` | Comprador B2C / B2B |
| Admin | Angular 19 | `4201` | Operador de la tienda |

La API persiste con Entity Framework Core 8. En una máquina nueva se puede arrancar con SQLite. El entorno pensado para operar es PostgreSQL, con las mismas migraciones.

## Documentación

| Documento | Contenido |
|---|---|
| [Puesta en marcha](docs/PUESTA-EN-MARCHA.md) | Requisitos, base de datos y cómo levantar las tres apps |
| [Arquitectura](docs/ARQUITECTURA.md) | Capas, tenant, modos de integración y datos |
| [Funcionalidades](docs/FUNCIONALIDADES.md) | Qué hace la tienda y qué hace el panel |
| [Subir a GitHub](docs/SUBIR-A-GITHUB.md) | Checklist antes de publicar el repositorio |
| [Swagger](http://localhost:5082/swagger) | Contrato HTTP cuando la API está corriendo |

En `docs/` también hay notas internas de QA, paridad con el ERP y estado de implementación. Esas notas son de trabajo; la tabla de arriba es la documentación para quien clona el repo.

## Estructura

```
E-commerce/
  Datamex.ECommerce.sln
  docker-compose.yml                 # PostgreSQL 18 opcional, puerto 5433
  src/
    Datamex.ECommerce.Api            # HTTP, Swagger, SignalR, auth
    Datamex.ECommerce.Application    # casos de uso
    Datamex.ECommerce.Domain         # entidades
    Datamex.ECommerce.Infrastructure # EF Core, seed, integraciones
  apps/
    storefront/                      # tienda
    admin/                           # panel
  tests/
    Datamex.ECommerce.Application.Tests
  tools/                             # scripts de sync, seed y operación
```

## Arranque rápido

Hace falta .NET 8 SDK, Node.js 20+ y, si no usás SQLite, Docker o un PostgreSQL accesible.

```powershell
# 1. Configuración local (no se sube a git)
copy src\Datamex.ECommerce.Api\appsettings.Example.json src\Datamex.ECommerce.Api\appsettings.json
copy src\Datamex.ECommerce.Api\appsettings.Development.Example.json src\Datamex.ECommerce.Api\appsettings.Development.json

# 2. API
cd src\Datamex.ECommerce.Api
dotnet run --launch-profile http

# 3. Tienda (otra terminal)
cd apps\storefront
npm install
npm start

# 4. Panel (otra terminal)
cd apps\admin
npm install
npm start
```

- API y Swagger: http://localhost:5082
- Tienda: http://localhost:4200
- Panel: http://localhost:4201
- Salud: http://localhost:5082/health

Con la base vacía, el seed crea la tienda `demo`. Usuario `demo`, contraseña `demo123`. Ese usuario es solo para desarrollo.

Si el navegador responde 503 contra `localhost`, el proxy del sistema está interceptando el loopback. Agregá `localhost` y `127.0.0.1` a las excepciones del proxy.

## Modos de integración

`ECommerce:IntegrationMode`:

- **Standalone.** La tienda vive sola. Los pedidos no se escriben en el ERP.
- **Integrated.** La empresa usa el mismo `IDEmpresa` del ERP. El catálogo puede entrar por sincronización y, si se habilita, el pedido puede volver al ERP.
- **External.** La plataforma se conecta a un ERP fuera de este proceso.

La lectura masiva ERP → tienda no corre dentro de esta API: la hace el proceso aparte `ecom-etl`. Esta API sí puede hacer el write-back de pedidos cuando `Erp:AllowWriteBack` está en `true`.

## Canales

- **B2C.** Precio de contado minorista.
- **B2B.** Precio mayorista, cuando la cuenta está aprobada.

El canal viaja en las consultas de catálogo, precios y carrito (`canal=B2C` o `canal=B2B`).

## Antes de publicar

Los archivos `appsettings.json`, `appsettings.Development.json` y `appsettings.Production.json` están en `.gitignore` porque llevan cadenas de conexión y claves. En el repositorio quedan las plantillas `*.Example.json`.

Antes de un build de producción, reemplazá las URLs de ejemplo en:

- `apps/storefront/src/environments/environment.production.ts`
- `apps/admin/src/environments/environment.production.ts`

Los pasos completos están en [docs/SUBIR-A-GITHUB.md](docs/SUBIR-A-GITHUB.md).
