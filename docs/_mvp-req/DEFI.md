# Documento de Arranque Funcional y Técnico

**Proyecto:** Plataforma de E-commerce Datamex  
**Versión:** 0.3  
**Estado:** Documento de arranque para alineación de producto y arquitectura  
**Fecha:** 2026-06-10

---

## 1. Propósito del documento

Este documento define el punto de partida funcional y técnico del proyecto de e-commerce de Datamex. Su objetivo es alinear a negocio, producto y desarrollo sobre:

- qué problema resuelve la plataforma
- cuál es el alcance inicial
- cuáles son los módulos principales
- qué reglas de negocio deben respetarse
- qué decisiones técnicas se recomiendan para el MVP
- cuáles son los próximos pasos concretos

Este documento no reemplaza historias de usuario, wireframes ni diseño técnico detallado, pero sí debe servir como base confiable para iniciar planificación, modelado y construcción.

---

## 2. Objetivo del proyecto

Construir una plataforma de comercio electrónico robusta, escalable, integrable y altamente personalizable, pensada como producto reusable para múltiples clientes de Datamex.

La plataforma debe poder operar de dos formas:

- **modo autónomo:** el e-commerce administra por sí mismo catálogo, inventario, pedidos, clientes y operación comercial
- **modo integrado:** el e-commerce expone endpoints y mecanismos de sincronización para alimentarse desde sistemas externos, especialmente un ERP web

Adicionalmente, el producto debe poder adaptarse visualmente para distintos clientes mediante branding y theming, manteniendo un core funcional común.

La meta inicial no es cubrir todos los escenarios avanzados del e-commerce desde el día uno, sino lanzar un MVP sólido, extensible y técnicamente sano, evitando deuda estructural temprana.

---

## 3. Principios rectores

- El MVP debe priorizar confiabilidad operativa por encima de complejidad arquitectónica innecesaria.
- Las reglas críticas de negocio deben quedar definidas antes de implementar flujos sensibles como stock, checkout y pagos.
- La arquitectura debe permitir crecer sin forzar microservicios prematuros.
- La experiencia de compra debe ser simple, rápida y consistente en desktop y mobile.
- La seguridad, trazabilidad e idempotencia son obligatorias en autenticación, pagos, webhooks y actualización de stock.
- La plataforma debe poder funcionar de forma autónoma o integrada con sistemas externos sin reescribir el core.
- El producto debe ser fácilmente tematizable para distintos clientes sin duplicar lógica de negocio.
- Cada cliente debe poder desplegarse de forma aislada, con su propia configuración, base de datos, dominio y recursos.

---

## 4. Alcance del MVP

El MVP debe cubrir de forma funcional y usable:

- navegación pública de catálogo
- búsqueda y filtrado de productos
- detalle de producto
- carrito persistente sin login obligatorio
- checkout con validaciones
- integración con al menos una pasarela de pago
- creación y seguimiento básico de pedidos
- panel administrativo para gestión de catálogo e inventario
- panel de backoffice para operación y administración del ecommerce
- autenticación y autorización por roles
- configuración visual básica por cliente
- endpoints base para integración con sistemas externos

Queda fuera del MVP, salvo decisión explícita posterior:

- marketplace multi-vendedor completo
- motor avanzado de promociones complejas
- multi-moneda real con liquidación por país
- sistema de devoluciones automatizado
- recomendaciones personalizadas con IA
- arquitectura distribuida con microservicios independientes
- multi-tenant compartido a nivel de base de datos o aplicación

---

## 5. Stack tecnológico definido

- **Backend:** C# / .NET 10 con API REST
- **Frontend:** Angular SPA
- **Base de datos:** PostgreSQL
- **Contenerización:** Docker

Tecnologías complementarias recomendadas para el arranque:

- **ORM:** Entity Framework Core
- **Autenticación:** JWT con refresh tokens
- **Documentación API:** OpenAPI / Swagger
- **Manejo de archivos e imágenes:** almacenamiento desacoplado del backend local
- **Observabilidad:** logging estructurado y trazabilidad de requests

---

## 6. Enfoque arquitectónico recomendado

### 6.1 Decisión recomendada para el MVP

Se recomienda construir el sistema como un **modular monolith** desde el inicio.

Esto permite:

- menor complejidad operativa
- despliegue más simple
- una sola base de datos transaccional
- mayor velocidad de desarrollo
- separación lógica clara por dominios
- posibilidad de extraer módulos a servicios independientes más adelante si el negocio lo requiere

Este enfoque también favorece un producto white-label desplegable por cliente, ya que permite mantener un único código base con configuraciones y branding variables por instalación.

### 6.2 Decisión descartada para la etapa inicial

No se recomienda iniciar con microservicios en el MVP, ya que introducirían complejidad adicional en:

- transacciones distribuidas
- observabilidad
- despliegue
- mensajería y consistencia eventual
- debugging y costos operativos

### 6.3 Módulos del sistema

El backend debe organizarse por módulos de dominio claramente separados:

- **Auth:** usuarios, login, permisos, recuperación de contraseña
- **Catalog:** productos, categorías, atributos, variantes, imágenes
- **Inventory:** stock, movimientos, disponibilidad
- **Cart:** carrito persistente, ítems, totales provisorios
- **Checkout:** validaciones previas, direcciones, cálculo final
- **Payments:** integración con pasarela, intentos, confirmaciones y webhooks
- **Orders:** pedido, detalle, estados e historial
- **Admin:** operaciones de backoffice y gestión interna
- **Integrations:** endpoints externos, sincronización con ERP, importación/exportación y trazabilidad de integraciones
- **Configuration:** branding, theming, parámetros comerciales y ajustes por cliente

El módulo **Admin** no debe tratarse como una utilidad menor, sino como una aplicación interna del producto con valor propio.

Debe permitir operar el e-commerce tanto en:

- **modo autónomo**, donde el backoffice administra la mayor parte de la operación comercial
- **modo integrado**, donde el backoffice administra la capa digital, monitorea sincronizaciones con ERP y controla aquello que siga siendo responsabilidad del ecommerce

### 6.4 Estrategia de producto para múltiples clientes

La plataforma debe diseñarse como un producto **white-label con despliegue aislado por cliente**.

Esto significa:

- un mismo código base reutilizable
- una instancia desplegable independiente por cliente
- base de datos independiente por cliente
- variables de entorno y configuración independientes por cliente
- dominio propio por cliente
- posibilidad de alojar múltiples clientes en un mismo servidor mediante contenedores separados

### 6.5 Decisión explícita sobre multi-tenant

Para este producto, **no se recomienda multi-tenant compartido** en la etapa inicial ni como estrategia principal.

La recomendación es un esquema de:

- **single-tenant por despliegue**

Ventajas de este enfoque:

- aislamiento más claro entre clientes
- menor riesgo de fuga de datos entre tenants
- personalización más simple por cliente
- operación más predecible para branding, dominios e integraciones específicas
- despliegue flexible en una o varias máquinas virtuales

La escalabilidad horizontal debe resolverse agregando nuevas máquinas o redistribuyendo clientes entre hosts según carga y necesidad operativa.

---

## 7. Actores y roles

### 7.1 Cliente

Puede:

- navegar catálogo
- buscar y filtrar productos
- gestionar carrito
- iniciar checkout
- pagar
- consultar sus pedidos
- guardar direcciones

### 7.2 Administrador

Puede:

- gestionar productos, categorías y variantes
- actualizar stock
- ver pedidos y su estado
- gestionar usuarios internos según permisos
- revisar eventos de pago y errores operativos
- gestionar contenido visible del home y navegación
- monitorear integraciones y sincronizaciones externas

### 7.3 Vendedor

Rol opcional para el MVP. Si se incluye, debe tener permisos restringidos a:

- administración de su catálogo asignado
- consulta de stock
- consulta de pedidos relacionados

Si este rol no es una necesidad inmediata del negocio, debe posponerse para reducir complejidad inicial.

---

## 8. Requerimientos funcionales

### 8.1 Módulo de usuarios y autenticación

- Registro con correo electrónico y contraseña.
- Login con correo electrónico y contraseña.
- Recuperación de contraseña por correo.
- Gestión de sesión con JWT y refresh token.
- Gestión de roles y permisos.
- Posibilidad de incorporar SSO en una fase posterior.
- Protección de endpoints administrativos y operativos.

### 8.2 Módulo de catálogo

- CRUD de productos.
- CRUD de categorías.
- Gestión de atributos y variantes.
- Soporte de múltiples imágenes por producto.
- Definición de producto activo/inactivo.
- Soporte para slug SEO por producto y categoría.
- Búsqueda por texto.
- Filtros por categoría, precio, atributos y disponibilidad.
- Paginación de resultados.
- Posibilidad de que catálogo, precios o stock sean administrados localmente o sincronizados desde un sistema externo.

### 8.3 Módulo de inventario

- Control de stock por **SKU o variante**, no por producto agregado.
- Registro de movimientos de inventario.
- Validación de disponibilidad antes de confirmar la compra.
- Descuento de stock únicamente al momento definido por la regla de negocio del checkout.
- Protección contra sobreventa mediante operaciones atómicas.

### 8.4 Módulo de carrito

- Carrito persistente para usuarios anónimos y autenticados.
- Asociación del carrito a sesión y posterior fusión con usuario autenticado si corresponde.
- Alta, baja y modificación de cantidades.
- Recalculo de subtotales y totales.
- Validación de vigencia de precio y disponibilidad.

### 8.5 Módulo de checkout

- Captura o selección de dirección de envío.
- Captura o selección de dirección de facturación.
- Resumen final de compra antes de pagar.
- Validación final de stock, precios e impuestos.
- Bloqueo de inconsistencias antes de crear la orden.

### 8.6 Módulo de pagos

- Integración inicial con al menos una pasarela.
- El frontend no debe manipular ni almacenar datos sensibles completos de tarjeta en servidores propios.
- El backend debe trabajar con tokens, referencias o intents provistos por la pasarela.
- Confirmación de pago vía webhook y/o consulta segura al proveedor.
- Registro de intentos, errores y resultado final del pago.
- Manejo idempotente de webhooks.

### 8.7 Módulo de órdenes

- Creación de pedido con snapshot de datos relevantes al momento de compra.
- Persistencia de ítems, precios, impuestos, descuentos y total final.
- Historial de estados del pedido.
- Consulta de pedido por cliente.
- Consulta administrativa de pedidos.

### 8.8 Módulo de integraciones externas

- Exposición de endpoints para alimentación desde ERP u otros sistemas externos.
- Autenticación segura para integraciones máquina a máquina.
- Soporte para operaciones idempotentes.
- Registro de requests y respuestas de integración.
- Trazabilidad de sincronizaciones, errores y reintentos.
- Definición de ownership por entidad: catálogo, stock, precio, cliente, pedido.
- Posibilidad de importar y exportar datos según reglas de negocio.

### 8.9 Módulo de configuración y white-label

- Configuración de branding por cliente.
- Cambio de logo, paleta, tipografía y assets principales sin modificar el core.
- Configuración por cliente de textos, banners y piezas visuales principales.
- Soporte para dominio propio por cliente.
- Separación clara entre lógica de negocio y capa visual.

### 8.10 Módulo de backoffice

- Panel administrativo interno accesible por roles.
- Dashboard operativo básico con resumen de pedidos, catálogo, stock e integraciones.
- Gestión de catálogo, categorías, variantes e imágenes.
- Gestión de inventario según el modo operativo del cliente.
- Gestión de pedidos y consulta de estados.
- Gestión de clientes y usuarios internos según permisos.
- Gestión de contenido del home, banners, bloques y navegación.
- Monitoreo de sincronizaciones con ERP u otros sistemas externos.
- Trazabilidad de errores de integración, webhooks e importaciones.
- Configuración operativa del sitio por cliente.

### 8.11 Comportamiento del backoffice según modo de operación

#### Modo autónomo

El backoffice debe permitir administrar directamente:

- catálogo
- precios
- stock
- contenido
- pedidos
- clientes

#### Modo integrado con ERP

El backoffice debe seguir existiendo, pero con foco en:

- visualización del estado del catálogo sincronizado
- monitoreo de stock y precios provenientes del ERP
- administración de contenido del canal digital
- gestión de pedidos generados en el e-commerce
- monitoreo y auditoría de integraciones

En este modo, algunas acciones pueden quedar:

- bloqueadas
- limitadas
- o solo informativas

según qué sistema sea maestro por entidad.

---

## 9. Reglas de negocio críticas

### 9.1 Stock

- El stock debe modelarse por SKU o variante.
- Agregar un producto al carrito no garantiza reserva de stock.
- La validación fuerte de disponibilidad debe realizarse al confirmar el checkout.
- El descuento de stock debe ocurrir de forma atómica al completar exitosamente la operación de pedido/pago definida por el flujo.
- Debe existir protección contra reintentos duplicados.
- Debe definirse por cliente si el stock es administrado por el e-commerce o sincronizado desde un ERP.

### 9.2 Precio

- El precio aplicado al pedido debe persistirse como snapshot.
- Cambios posteriores en catálogo no deben alterar pedidos ya creados.
- El carrito puede recalcularse si cambian precios antes de la confirmación.
- Debe definirse si el precio maestro vive en el e-commerce, en el ERP o en un modelo mixto.

### 9.3 Pedido

Estados mínimos recomendados:

- `draft`
- `pending_payment`
- `paid`
- `payment_failed`
- `cancelled`
- `ready_to_ship`
- `shipped`
- `delivered`
- `refunded`

### 9.4 Pagos

- Todo webhook debe procesarse de forma idempotente.
- Un pago confirmado no debe generar más de un pedido final válido para la misma intención de pago.
- Los errores transitorios deben permitir reintentos controlados.

### 9.6 Integraciones

- Cada endpoint expuesto para sistemas externos debe ser versionable.
- Toda operación de integración debe ser autenticada, trazable e idempotente.
- Debe quedar definido el sistema maestro por dominio de datos.
- Las sincronizaciones deben contemplar manejo de conflicto, reintentos y auditoría.

### 9.5 Seguridad

- Contraseñas almacenadas con hashing seguro.
- Endpoints protegidos por autorización basada en roles.
- Secrets y credenciales fuera del código fuente.
- Auditoría mínima de operaciones sensibles de administración.

---

## 10. Consideraciones no funcionales

### 10.1 Rendimiento

- La búsqueda de catálogo debe responder de forma ágil con paginación.
- El sistema debe soportar crecimiento de catálogo sin degradación severa.
- Para el MVP se recomienda iniciar con PostgreSQL y evaluar `pg_trgm` antes de incorporar Elasticsearch.

### 10.2 Escalabilidad

- El diseño modular debe permitir separación futura por dominios.
- Se debe evitar acoplar reglas de negocio críticas al frontend.
- La plataforma debe soportar múltiples despliegues independientes del mismo producto para distintos clientes.

### 10.3 Observabilidad

- Logging estructurado en backend.
- Correlation ID por request.
- Trazabilidad de checkout, pago y actualización de stock.
- Trazabilidad específica para integraciones ERP y procesos de sincronización.
- Visibilidad operativa suficiente para alimentar dashboards de backoffice.

### 10.4 Resiliencia

- Reintentos seguros para integraciones externas.
- Registro de fallos de pasarela y webhooks.
- Manejo explícito de timeout y respuestas inconsistentes de terceros.

### 10.5 Mantenibilidad del producto

- El core funcional no debe depender de personalizaciones visuales específicas.
- El theming debe resolverse por configuración y assets, no por forks del código.
- Las integraciones con ERP deben abstraerse para evitar acoplamiento a un único cliente o proveedor.

---

## 11. Infraestructura y despliegue

- Todo el entorno de desarrollo debe correr en Docker.
- Debe existir un `docker-compose.yml` para levantar backend, frontend y PostgreSQL.
- La configuración debe basarse en variables de entorno.
- Las credenciales sensibles no deben guardarse en el repositorio.
- Deben contemplarse ambientes separados como mínimo para desarrollo y producción.
- Cada cliente debe poder desplegarse como stack independiente.
- Cada despliegue debe poder vincularse a su propio dominio.

Recomendaciones adicionales:

- archivo `.env` para desarrollo local
- `appsettings.*.json` por ambiente
- migraciones versionadas de base de datos
- reverse proxy para enrutar múltiples dominios a distintos stacks desplegados
- estrategia de naming y versionado de contenedores por cliente

### 11.1 Estrategia de despliegue por cliente

Modelo recomendado:

- un conjunto de contenedores por cliente
- una base de datos por cliente
- un dominio o subdominio propio por cliente
- configuración aislada por variables de entorno

Modelo operativo esperado:

- varios clientes pueden convivir en un mismo servidor Ubuntu
- cada cliente corre en su propio stack Docker
- si la capacidad se agota, se agregan nuevos servidores o se redistribuyen clientes
- algunos clientes pueden agruparse por máquina virtual y otros separarse según criticidad, tráfico o necesidades operativas

---

## 12. Integraciones externas previstas

- pasarela de pagos
- proveedor de correo electrónico
- almacenamiento de imágenes
- ERP web

Estas integraciones deben abstraerse detrás de interfaces de aplicación para no acoplar la lógica del dominio a proveedores específicos.

---

## 13. Modelo de datos inicial a definir

Entidades base recomendadas:

- `User`
- `Role`
- `Address`
- `Category`
- `Product`
- `ProductVariant`
- `ProductImage`
- `InventoryItem`
- `InventoryMovement`
- `Cart`
- `CartItem`
- `Order`
- `OrderItem`
- `Payment`
- `PaymentEvent`
- `IntegrationEndpoint`
- `IntegrationLog`
- `ClientConfiguration`
- `ThemeAsset`
- `AdminUser`
- `AdminRole`
- `AdminAuditLog`

Puntos de modelado que deben resolverse antes de implementar:

- relación entre producto y variantes
- estrategia de SKU
- estructura de atributos
- snapshot de precios y direcciones en pedido
- historial de estados del pedido
- modelo de configuración por cliente
- ownership de datos entre e-commerce y ERP
- nivel de permisos y auditoría del backoffice

---

## 14. Riesgos tempranos a controlar

- arrancar con microservicios antes de validar el negocio
- modelar stock a nivel producto y no a nivel variante
- acoplar demasiado el checkout al proveedor de pagos
- no definir idempotencia en webhooks desde el inicio
- no persistir snapshot de precio y datos del pedido
- subestimar la complejidad real del flujo de inventario
- convertir el theming en forks por cliente en vez de configuración reusable
- acoplar la integración a un ERP específico desde la primera versión
- mezclar clientes en una misma base de datos sin necesidad real

---

## 15. Hoja de ruta sugerida

### Fase 1: Base del sistema

- estructura de solución backend y frontend
- autenticación y autorización
- modelo de datos inicial
- migraciones
- docker compose
- base de configuración por cliente
- estructura base de backoffice

### Fase 2: Catálogo e inventario

- categorías
- productos
- variantes
- imágenes
- stock y movimientos
- búsqueda y filtros básicos
- definición de ownership con ERP

### Fase 3: Experiencia de compra

- carrito persistente
- checkout
- direcciones
- validaciones finales
- primera capa de theming configurable

### Fase 4: Pagos y órdenes

- integración con pasarela
- webhooks
- creación de pedidos
- historial de estados
- endpoints de integración externa

### Fase 5: Endurecimiento operativo

- logs
- auditoría
- manejo de errores
- ajustes de performance
- despliegue multi-cliente por stacks independientes

---

## 16. Definición de MVP recomendada

Se considera que el MVP está listo cuando permita:

1. administrar productos, categorías, variantes e inventario desde backoffice
2. publicar un catálogo navegable con búsqueda y filtros básicos
3. permitir a un cliente armar carrito sin login obligatorio
4. realizar checkout con dirección y validación final
5. pagar con una pasarela integrada
6. generar pedido persistido con estado y detalle
7. consultar pedidos desde cliente y administración
8. exponer endpoints base para integración con ERP
9. permitir branding y theme básico por cliente
10. operar el e-commerce desde un backoffice interno básico

---

## 17. Próximos pasos inmediatos

1. Validar con negocio la visión comercial y operativa del proyecto.
2. Confirmar qué entidades serán maestras en el e-commerce y cuáles en el ERP.
3. Confirmar si el rol vendedor es realmente necesario en el MVP.
4. Definir flujo exacto de stock y momento de descuento.
5. Definir estados finales del pedido y reglas de transición.
6. Elegir la primera pasarela de pago a integrar.
7. Definir el alcance exacto del sistema de theming por cliente.
8. Definir el alcance funcional del backoffice en modo autónomo e integrado.
9. Diseñar modelo de datos inicial.
10. Crear sitemap y flujos de usuario.
11. Comenzar implementación por catálogo e inventario.

---

## 18. Decisiones tomadas en esta versión

- Se adopta **modular monolith** como arquitectura recomendada para el MVP.
- El stock se modela por **SKU/variante**.
- El documento pasa de lluvia de ideas a base de arranque funcional y técnico.
- Se incorpora definición de alcance, reglas críticas, riesgos y roadmap.
- Se define una estrategia **white-label** con despliegue aislado por cliente.
- Se define soporte para operación autónoma o integrada con ERP.
- Se descarta multi-tenant compartido como estrategia principal del producto.

---

## 19. Temas pendientes de definición con negocio

- tipo de operación comercial exacta
- modelo de sincronización con ERP
- ownership por entidad entre ERP y e-commerce
- alcance real del rol vendedor
- alcance exacto del backoffice y sus permisos
- reglas de envío
- reglas fiscales e impuestos
- cupones y promociones
- devoluciones y reembolsos
- cobertura geográfica
- moneda o monedas operadas
- alcance exacto del branding configurable

---

## 20. Matriz inicial de ownership ERP vs E-commerce

La siguiente matriz propone una base operativa inicial para definir qué sistema es maestro por entidad en escenarios integrados.

Principio general recomendado:

- **ERP como maestro** de datos administrativos y operativos estructurales
- **E-commerce como maestro** de experiencia digital, canal online y presentación
- **Modelo mixto controlado** para pedidos, donde el pedido nace en e-commerce y continúa su ciclo operativo en ERP

| Entidad / Dominio | Sistema maestro | Réplica sugerida | API / flujo sugerido | Frecuencia | Notas |
|---|---|---|---|---|---|
| Categorías | ERP | E-commerce | ERP -> E-commerce | Programada o por evento | Conviene centralizar estructura comercial. |
| Productos base | ERP | E-commerce | ERP -> E-commerce | Programada o por evento | Nombre, código, estado, descripción base. |
| Variantes / SKU | ERP | E-commerce | ERP -> E-commerce | Programada o por evento | Debe evitarse doble alta entre sistemas. |
| Stock | ERP | E-commerce | ERP -> E-commerce | Alta frecuencia | Requiere idempotencia y control de consistencia. |
| Precios base | ERP | E-commerce | ERP -> E-commerce | Programada o por evento | En modo integrado no se recomienda edición libre en e-commerce. |
| Imágenes de producto | E-commerce | ERP opcional | E-commerce -> ERP opcional | Bajo demanda | Mejor tratarlas como activos digitales del canal web. |
| SEO, slugs y metadatos web | E-commerce | No obligatoria | Interno | Interno | Deben pertenecer al canal digital. |
| Atributos de presentación | E-commerce | No obligatoria | Interno | Interno | Ejemplo: destacados, badges, orden visual. |
| Clientes web | E-commerce | ERP | E-commerce -> ERP | Al registrarse o comprar | Debe contemplar deduplicación. |
| Direcciones de clientes | E-commerce | ERP opcional | E-commerce -> ERP | Al registrarse o comprar | Relacionadas al checkout y cuenta online. |
| Carrito | E-commerce | No aplica | Interno | Tiempo real | Debe vivir 100% en e-commerce. |
| Checkout | E-commerce | No aplica | Interno | Tiempo real | Flujo propio del canal digital. |
| Pedido al crearse | E-commerce | ERP | E-commerce -> ERP | Inmediata | El pedido nace en el canal online. |
| Estado logístico del pedido | ERP | E-commerce | ERP -> E-commerce | Por evento o programada | Preparación, despacho, entrega. |
| Pagos / intents / webhooks | E-commerce | ERP resumen | E-commerce -> ERP | Inmediata o por cierre | El canal de pago debe quedar del lado web. |
| Facturación fiscal | ERP | E-commerce | ERP -> E-commerce | Según emisión | Factura, documento fiscal y estado tributario. |
| Promociones web simples | E-commerce | No obligatoria | Interno | Interno | Recomendado para el MVP si no hay motor centralizado. |
| Cupones comerciales globales | A definir | Mixta | Mixto | A definir | Depende del modelo comercial del cliente. |
| Métodos de envío y reglas web | E-commerce o mixto | ERP opcional | Interno o mixto | A definir | Depende de si logística vive o no en ERP. |
| Usuarios administrativos | E-commerce | No obligatoria | Interno | Interno | Propio del producto. |
| Theme, branding y assets | E-commerce | No aplica | Interno | Bajo demanda | 100% parte del producto white-label. |

### 20.1 Recomendación práctica para el MVP

Para una primera implementación integrada, se recomienda:

- **ERP maestro:** productos, variantes, stock, precios, facturación y estados logísticos
- **E-commerce maestro:** clientes web, direcciones, carrito, checkout, pagos, branding y contenido visual
- **Ownership secuencial o mixto:** pedidos

### 20.2 Regla de pedidos recomendada

Flujo recomendado:

1. el cliente confirma la compra en el e-commerce
2. el e-commerce registra el pedido y el resultado del pago
3. el e-commerce envía el pedido al ERP
4. el ERP confirma recepción y asigna su identificador interno si aplica
5. el ERP pasa a ser maestro de los estados operativos posteriores
6. el e-commerce refleja al cliente los estados sincronizados desde ERP

### 20.3 Reglas de cuidado especial

- Si el ERP es maestro de stock, el e-commerce no debe comportarse como fuente primaria de stock persistente en modo integrado.
- Si el ERP es maestro de precios, el e-commerce no debe permitir cambios manuales que rompan consistencia.
- Toda sincronización crítica debe ser autenticada, trazable e idempotente.
- Debe definirse manejo de conflictos para casos de edición concurrente o datos desfasados.
- La integración debe abstraerse para no quedar acoplada a un ERP específico desde la primera versión.

---

## 21. Conclusión

El proyecto está bien planteado como iniciativa y tiene una base tecnológica razonable. La mejor forma de maximizar velocidad y reducir riesgo es iniciar con un MVP sólido, modular, integrable y transaccionalmente consistente, reservando complejidades mayores para una segunda etapa.

La visión de producto como e-commerce reusable, integrable con ERP y desplegable por cliente de forma aislada encaja bien con este enfoque y le da una dirección comercial y técnica mucho más clara.

Este documento deja establecido un punto de partida más claro para conversar visión, priorización y ejecución.
