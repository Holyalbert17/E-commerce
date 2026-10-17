# Puesta en marcha

Guía para dejar corriendo la API, la tienda y el panel en una máquina de desarrollo.

## Requisitos

- .NET SDK 8
- Node.js 20 o superior y npm
- Docker Desktop, solo si vas a usar el PostgreSQL del `docker-compose.yml`
- Angular CLI no hace falta instalarlo global: los scripts usan el CLI local del proyecto

Comprobación:

```powershell
dotnet --version
node --version
npm --version
```

## 1. Configuración

La API no trae secretos en el repositorio. Copiá las plantillas una sola vez:

```powershell
cd E-commerce
copy src\Datamex.ECommerce.Api\appsettings.Example.json src\Datamex.ECommerce.Api\appsettings.json
copy src\Datamex.ECommerce.Api\appsettings.Development.Example.json src\Datamex.ECommerce.Api\appsettings.Development.json
```

Esos dos archivos quedan ignorados por git. El de Development de ejemplo deja la API en **SQLite** (`App_Data/ecom.db`) y en modo **Standalone**, con el tenant `demo`.

Si ya tenés un `appsettings.Development.json` apuntando a un PostgreSQL propio, no lo pises: la API usa ese archivo cuando `ASPNETCORE_ENVIRONMENT=Development`.

### PostgreSQL local con Docker

```powershell
docker compose up -d
```

El contenedor publica PostgreSQL 18 en el puerto **5433** (el 5432 del host queda libre). Base `datamex_ecom`, usuario `ecom`, contraseña `ecom_dev_pass`.

En `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "EcomDb": "Host=localhost;Port=5433;Database=datamex_ecom;Username=ecom;Password=ecom_dev_pass;SSL Mode=Prefer;Search Path=dbo,public"
  },
  "Database": {
    "Provider": "Postgres"
  }
}
```

La API aplica las migraciones de `src/Datamex.ECommerce.Infrastructure/Persistence/Migrations` al arrancar (`Database.Migrate()`).

Para aplicarlas a mano:

```powershell
$env:EcomDbConnection = "Host=localhost;Port=5433;Database=datamex_ecom;Username=ecom;Password=ecom_dev_pass;SSL Mode=Prefer;Search Path=dbo,public"
$env:Database__Provider = "Postgres"
dotnet ef database update --project src/Datamex.ECommerce.Infrastructure --startup-project src/Datamex.ECommerce.Api
```

`dotnet ef` no tiene una conexión por defecto. Si falta `EcomDbConnection`, el comando termina con un error que explica cómo definirla.

## 2. API

```powershell
cd src\Datamex.ECommerce.Api
dotnet run --launch-profile http
```

El perfil `http` de `launchSettings.json` escucha en **http://localhost:5082** y abre Swagger. El storefront y el admin de desarrollo apuntan a `http://127.0.0.1:5082`.

Comprobaciones:

- http://localhost:5082/swagger
- http://localhost:5082/health

La primera ejecución crea el esquema y, si la base está vacía, la tienda demo.

| Dato | Valor |
|---|---|
| Slug | `demo` |
| Usuario tienda | `demo` |
| Contraseña | `demo123` |

Ese seed no pisa un tenant que ya exista (por ejemplo `casa-imperial`).

## 3. Tienda

```powershell
cd apps\storefront
npm install
npm start
```

http://localhost:4200 abre el catálogo. Para otra empresa agregá `?tenant=slug` en la URL.

## 4. Panel

```powershell
cd apps\admin
npm install
npm start
```

http://localhost:4201/login pide slug, usuario y contraseña de un administrador de esa empresa. El alta de empresas está en `/empresas` y exige un usuario con permiso de manager.

## Variables que importan

| Clave | Efecto |
|---|---|
| `Database:Provider` | `Sqlite` o `Postgres` |
| `ConnectionStrings:EcomDb` | Base de la tienda |
| `ConnectionStrings:Erp` | Base del ERP, solo si hay sync o write-back |
| `ECommerce:IntegrationMode` | `Standalone`, `Integrated` o `External` |
| `ECommerce:DefaultTenantSlug` | Tenant si el request no trae header |
| `ECommerce:JwtSigningKey` | Firma del JWT. Cambiala fuera de desarrollo |
| `ECommerce:CorsOrigins` | Orígenes permitidos. En dev ya entran 4200 y 4201 |
| `Erp:AllowWriteBack` | Si el pedido confirmado se inserta en el ERP |
| `Bancard:Mode` | `Stub` simula el pago. `Live` usa vPOS |
| `Media:RootPath` | Carpeta de imágenes. Por defecto `App_Data/media`, servida en `/media` |

## Tests

```powershell
dotnet test tests/Datamex.ECommerce.Application.Tests
```

En el admin, el spec de configuración de empresa:

```powershell
cd apps\admin
npm run test:empresa-config
```

## Problemas frecuentes

**503 en el navegador y la API sí está arriba.** Un proxy corporativo está interceptando `localhost`. Excluí `localhost` y `127.0.0.1`.

**La tienda carga y el catálogo está vacío.** El tenant no tiene artículos con `ActivoEcommerce`. Entrá al panel, catálogo, y activá productos para la vitrina. Confirmá que el slug de la URL es el de esa empresa.

**El panel no llega a la API.** Los environments de desarrollo usan el puerto 5082. Si cambiaste el puerto de la API, actualizá `apiBaseUrl` en `apps/storefront/src/environments` y `apps/admin/src/environments`.

**`dotnet ef` pide `EcomDbConnection`.** Es intencional: la fábrica de diseño no guarda una conexión. Definí la variable en esa terminal y volvé a correr el comando.
