# Funcionalidades

Dos superficies, una API. El detalle de cada endpoint está en Swagger (`/swagger`) con la API levantada.

## Tienda (`apps/storefront`, puerto 4200)

| Ruta | Sesión | Qué hace |
|---|---|---|
| `/catalogo` | No | Vitrina. Solo artículos activos para e-commerce. Filtros por texto, marca, colección y stock. Canal B2C o B2B. |
| `/buscar` | No | La misma vitrina en modo búsqueda. |
| `/producto/:id` | No | Ficha: galería, embalaje, cantidad mínima, precio del canal, stock, reseñas, favorito y seguimiento. |
| `/carrito` | No | Alta, cambio de cantidad y baja. Invitado con `X-Cart-Session` o usuario logueado. |
| `/checkout` | Según el flujo | Entrega o retiro, mapa, tarifa por barrio, mínimo de pedido y cupón. Confirma y crea el pedido. |
| `/pago/:nro` | Sí | Pago Bancard (simulado o real, según `Bancard:Mode`). |
| `/pedido/:nro` | Sí | Confirmación y detalle. |
| `/seguimiento/:token` | No | Seguimiento público del envío. |
| `/mis-pedidos` | Sí | Historial de la cuenta. |
| `/devoluciones` | Sí | Solicitudes de devolución. |
| `/favoritos` | Sí | Listas de favoritos. |
| `/seguidos` | Sí | Productos seguidos para aviso de reposición. |
| `/mi-cuenta` | Sí | Datos, direcciones y alta mayorista. |
| `/login`, `/registro`, `/recuperar`, `/activar` | No | Alta, activación y acceso. |
| `/sucursales` | No | Agencias / sucursales. |
| `/contacto` | No | Formulario de contacto. |
| `/faq`, `/terminos`, `/como-comprar` | No | Páginas de contenido editables desde el panel. |

El branding (nombre, logo, colores, texto de portada) sale de la configuración de la empresa, no está fijo en el front.

## Panel (`apps/admin`, puerto 4201)

El login pide el slug de la tienda. Cada pantalla exige un permiso. `manager:all` cubre la operación de plataforma.

| Ruta | Para qué |
|---|---|
| `/` | Tablero. |
| `/empresas` | Alta manual o desde el ERP, y listado de tiendas. |
| `/configuracion` | Datos de la empresa, agencias y horario. |
| `/branding` | Identidad visual de la tienda. |
| `/personalizacion` | Piezas de la home y bloques comerciales. |
| `/contenido` | FAQ, términos, cómo comprar y otras páginas. |
| `/catalogo` | Artículos publicados, alta, edición, activar/inactivar. |
| `/catalogo/:id/imagenes` | Galería del producto. |
| `/catalogo/:id/embalajes` | Unidades y embalajes. |
| `/catalogo/:id/movimientos` | Movimientos de stock. |
| `/parametros-producto` | Atributos del catálogo. |
| `/listas-precios` | Listas y condiciones por canal. |
| `/promociones` | Promociones y grupos. |
| `/tarifas` | Tarifas de delivery. |
| `/carritos` | Carritos abiertos. |
| `/pedidos` | Pedidos de la tienda y su detalle. |
| `/choferes` | Choferes asociados a la entrega. |
| `/clientes` | Cuentas compradoras. |
| `/marketplace` | Reseñas, cupones y devoluciones. |
| `/crm` | Bandeja de chat. |
| `/contacto` | Mensajes del formulario. |
| `/reportes` | Reportes de operación. |
| `/sync` | Estado de la sincronización con el ERP. |
| `/manager` | Roles, usuarios y funcionalidades del panel. |

## API

Prefijos principales. Todos los que son de una tienda esperan el tenant, salvo el alta de empresas.

| Prefijo | Uso |
|---|---|
| `/api/catalog` | Productos y colecciones |
| `/api/search` | Búsqueda |
| `/api/pricing` | Lista de precios del canal |
| `/api/inventory` | Depósito y existencia |
| `/api/cart` | Carrito |
| `/api/checkout` | Confirmar pedido y consultarlo |
| `/api/shipping` | Tarifas y aplicar envío al carrito |
| `/api/geography` | Ciudades, barrios, documentos, agencias |
| `/api/auth` | Registro, activación, login y recuperación del comprador |
| `/api/account` | Perfil y pedidos del comprador |
| `/api/payments/bancard` | Inicio y confirmación de pago |
| `/api/favoritos` | Favoritos |
| `/api/reviews` | Reseñas |
| `/api/engagement` | Cupones, seguimiento de productos y piezas de home |
| `/api/contacto` | Contacto público |
| `/api/storefront/content` | Páginas CMS públicas |
| `/api/storefront/personalizacion` | Branding público |
| `/api/admin/auth` | Login del operador |
| `/api/admin` | Tablero, productos, pedidos, promociones, sync |
| `/api/admin/tenants` | Empresas |
| `/api/admin/empresa` | Configuración de la empresa |
| `/api/admin/listas-precios` | Listas |
| `/api/admin/clientes` | Clientes |
| `/api/admin/carritos` | Carritos del operador |
| `/api/admin/content` | CMS |
| `/api/admin/personalizacion` | Personalización |
| `/api/admin/manager` | Usuarios y permisos |
| `/api/admin/reports` | Reportes |
| `/hubs/chat` | SignalR de chat y tracking |
| `/health` | Estado de la API y de la base |

Headers habituales:

| Header | Cuándo |
|---|---|
| `X-Tenant-Slug` o `X-Tenant-ID` | Casi todas las operaciones de una tienda |
| `Authorization: Bearer {jwt}` | Comprador u operador ya autenticado |
| `X-Cart-Session` | Carrito de invitado |

## Reglas de negocio que se notan en la UI

- Sin `ActivoEcommerce` el producto no entra a la vitrina, aunque exista en la base.
- El precio cambia al pasar de B2C a B2B. B2B pide una cuenta aprobada.
- Si la empresa controla stock en el carrito, la cantidad no puede pasar la existencia del depósito configurado.
- El pedido de la tienda queda como `pedidomovil`. En Standalone mueve el stock local. En Integrated puede además escribirse en el ERP.
- El mail de activación y el de confirmación, en desarrollo, se registran en el log. El envío SMTP se configura aparte.
