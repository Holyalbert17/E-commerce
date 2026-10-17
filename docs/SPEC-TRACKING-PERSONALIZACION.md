# Spec — Tracking estilo Amazon + Personalización marketing

**Fecha:** 1 de agosto de 2026  
**Estado:** Base implementada en Standalone (sin ERP live).

## 1. Tracking

### Ya existe
- `SeguimientoPedidoEcommerce` (hitos), `PedidoMovil.IDChofer`, `Chofer`
- Timeline visual storefront (`dx-tracking-timeline`): barra de progreso + detalle
- Push en vivo: SignalR `JoinPedido` / evento `trackingUpdated` al agregar hito en admin
- Mapa Leaflet en checkout (coordenadas guardadas); **no** mapa live post-despacho

### Gaps vs Amazon (pendientes)
| Ítem | Estado |
|---|---|
| ETA dinámico | Pendiente |
| Excepciones (`demorado` / `excepcion`) | Hecho (máquina de estados + UI admin) |
| Preferencias de notificación por usuario | Pendiente |
| Link público compartible (token) | Hecho (`TokenTrackingPublico` + `/seguimiento/:token`) |
| Multi-ítem con ETAs independientes | Pendiente |
| Mapa en vivo en reparto | Pendiente |
| Push/WhatsApp “mapa disponible” | Pendiente |

### Prompt de sistema (asistente tracking)

Usar el bloque del informe de producto (sección 2 del brief 31/07/2026): experto visual+funcional ASP.NET 8 + Angular, entregables con DTOs/endpoints/migración/componente, priorizar funcional sobre decorativo.

## 2. Personalización marketing

### Entidad
`ConfiguracionPersonalizacion` — clave JSON versionada por `IDEmpresa`, vigencia opcional.

Claves v1:
- `home.layout` — secciones visibles/orden + banner mayorista
- `tema.activo` — paleta / tipografía campaña
- `checkout.textos` — microcopy checkout / mensaje éxito
- `widgets.toggles` — chat, reseñas, favoritos, seguidos, banner mayorista

### Admin UX
- Formularios tipados por pestaña (JSON avanzado opcional)
- Historial de versiones + restaurar
- Preview iframe de la tienda
- Programar campaña por fechas

### API
- Admin: `GET/PUT /api/admin/personalizacion/{clave}`, `POST .../programar`, `GET .../schemas`, `GET .../versiones`
- Storefront: `GET /api/storefront/personalizacion` (público, cacheable por tenant)
- Permiso: `Site_App_Ecommerce_Personalizacion` (rol MARKETING seed)

### UI
- Admin `/personalizacion` — editar JSON, publicar o programar campaña
- Storefront aplica layout (home), tema (CSS vars), textos checkout

## 3. Próximos pasos
1. Schemas tipados + formularios dinámicos (hoy JSON libre validado)
2. Preview borrador en iframe
3. ETA + share-token tracking
4. Reusar SignalR para notificaciones de excepción
