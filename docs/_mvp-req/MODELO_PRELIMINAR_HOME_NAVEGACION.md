# Modelo Preliminar de Home y Navegación

**Proyecto:** Plataforma de E-commerce Datamex  
**Fecha:** 2026-06-13  
**Objetivo:** proponer un pre-modelado técnico de base de datos para soportar navegación configurable, categorías, bloques dinámicos de contenido y administración editorial del storefront.

---

## 1. Alcance de este documento

Este documento cubre una propuesta preliminar para las entidades relacionadas con:

- categorías
- menús de navegación
- ítems de navegación
- bloques de contenido
- ítems de bloques
- relaciones de bloques con productos y categorías
- reglas de selección dinámica de contenido
- zonas de página
- trazabilidad editorial básica

No pretende cerrar el modelo final, pero sí dejar una base suficientemente concreta como para:

- discutir estructura técnica
- validar si soporta el MVP
- preparar migraciones iniciales sin rediseñar luego

---

## 2. Convenciones sugeridas

### 2.1 Convenciones generales

- claves primarias tipo `uuid`
- nombres de tablas en singular o plural según estándar del proyecto, pero consistentes
- timestamps en UTC
- soft delete opcional solo donde tenga valor real
- campos `jsonb` solo para configuración secundaria, no para estructura central

### 2.2 Tipos sugeridos

- `uuid`
- `varchar(n)`
- `text`
- `boolean`
- `integer`
- `timestamp with time zone`
- `jsonb`

### 2.3 Estado editorial sugerido

Para algunos objetos visibles del storefront conviene contemplar estados como:

- `draft`
- `published`
- `archived`

---

## 3. Entidades base

## 3.0 Consideración general sobre backoffice

Todas estas entidades deben pensarse como administrables desde el backoffice del ecommerce.

Eso implica que, aunque el storefront consuma esta estructura, el modelo también debe servir para:

- altas y ediciones internas
- publicación y despublicación
- ordenamiento visual
- auditoría mínima
- restricciones según modo autónomo o integrado con ERP

## 3.1 `Category`

Representa el árbol real de categorías del catálogo.

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `id` | `uuid` | No | PK |
| `client_id` | `uuid` | Sí | Opcional si la configuración por cliente se separa explícitamente |
| `parent_category_id` | `uuid` | Sí | FK a `category.id` para jerarquía |
| `name` | `varchar(150)` | No | Nombre visible |
| `slug` | `varchar(180)` | No | Slug SEO |
| `description` | `text` | Sí | Descripción opcional |
| `menu_label` | `varchar(150)` | Sí | Label alternativo para navegación |
| `image_url` | `text` | Sí | Imagen principal de categoría |
| `icon_url` | `text` | Sí | Ícono visual para menú/sidebar |
| `display_order` | `integer` | No | Orden general |
| `sidebar_order` | `integer` | Sí | Orden específico en sidebar |
| `show_in_main_menu` | `boolean` | No | Visibilidad en menú superior |
| `show_in_sidebar_menu` | `boolean` | No | Visibilidad en sidebar |
| `show_in_home` | `boolean` | No | Visibilidad en bloques home |
| `badge_text` | `varchar(50)` | Sí | Badge tipo `New` |
| `badge_type` | `varchar(30)` | Sí | Estilo lógico del badge |
| `is_active` | `boolean` | No | Estado activo |
| `content_status` | `varchar(20)` | No | Estado editorial sugerido |
| `updated_by_admin_user_id` | `uuid` | Sí | Usuario administrativo que hizo la última edición |
| `created_at` | `timestamptz` | No | Fecha creación |
| `updated_at` | `timestamptz` | No | Fecha actualización |

### Índices sugeridos

- PK en `id`
- índice único en `slug` por cliente si aplica
- índice en `parent_category_id`
- índice en `show_in_main_menu`
- índice en `show_in_sidebar_menu`
- índice en `display_order`

---

## 3.2 `NavigationMenu`

Representa un menú configurable del sitio.

Ejemplos:

- menú principal superior
- menú lateral de categorías
- menú del footer
- barra superior de links rápidos

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `id` | `uuid` | No | PK |
| `client_id` | `uuid` | Sí | Referencia a configuración por cliente |
| `code` | `varchar(100)` | No | Código técnico único por cliente |
| `name` | `varchar(150)` | No | Nombre interno |
| `menu_type` | `varchar(50)` | No | Ej.: `header`, `sidebar`, `footer`, `topbar` |
| `page_code` | `varchar(80)` | Sí | Ej.: `home`, `global`, `catalog` |
| `page_zone` | `varchar(50)` | Sí | Ej.: `topbar`, `header`, `sidebar`, `footer`, `bottom_footer` |
| `layout_variant` | `varchar(50)` | Sí | Ajuste visual |
| `display_order` | `integer` | No | Orden si hay varios menús por zona |
| `is_active` | `boolean` | No | Estado activo |
| `content_status` | `varchar(20)` | No | Estado editorial sugerido |
| `settings_json` | `jsonb` | Sí | Configuración secundaria |
| `updated_by_admin_user_id` | `uuid` | Sí | Último usuario que editó |
| `created_at` | `timestamptz` | No | Fecha creación |
| `updated_at` | `timestamptz` | No | Fecha actualización |

### Índices sugeridos

- PK en `id`
- índice único en `client_id + code`
- índice en `menu_type`
- índice en `page_code`
- índice en `page_zone`
- índice en `is_active`

---

## 3.3 `NavigationMenuItem`

Representa cada ítem visible dentro de un menú.

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `id` | `uuid` | No | PK |
| `navigation_menu_id` | `uuid` | No | FK a `navigation_menu.id` |
| `parent_item_id` | `uuid` | Sí | FK a `navigation_menu_item.id` para subniveles |
| `category_id` | `uuid` | Sí | FK opcional a `category.id` |
| `page_id` | `uuid` | Sí | FK opcional a tabla de páginas si existe |
| `item_type` | `varchar(50)` | No | `category_link`, `page_link`, `external_link`, `promo_link`, `group_label` |
| `label` | `varchar(150)` | No | Texto visible |
| `url` | `text` | Sí | URL cuando no depende de entidad relacionada |
| `target` | `varchar(20)` | Sí | `_self`, `_blank` |
| `icon_url` | `text` | Sí | Icono opcional |
| `badge_text` | `varchar(50)` | Sí | Badge visible |
| `badge_type` | `varchar(30)` | Sí | Tipo lógico de badge |
| `display_order` | `integer` | No | Orden dentro del menú |
| `is_active` | `boolean` | No | Estado activo |
| `content_status` | `varchar(20)` | No | Estado editorial sugerido |
| `settings_json` | `jsonb` | Sí | Configuración secundaria |
| `updated_by_admin_user_id` | `uuid` | Sí | Último usuario que editó |
| `created_at` | `timestamptz` | No | Fecha creación |
| `updated_at` | `timestamptz` | No | Fecha actualización |

### Reglas sugeridas

- si `item_type = category_link`, debe existir `category_id`
- si `item_type = page_link`, debe existir `page_id` o `url`
- si `item_type = external_link`, debe existir `url`
- si `parent_item_id` existe, el ítem es hijo de otro ítem del mismo menú

### Índices sugeridos

- PK en `id`
- índice en `navigation_menu_id`
- índice en `parent_item_id`
- índice en `category_id`
- índice en `display_order`
- índice en `is_active`

---

## 3.4 `ContentBlock`

Representa un bloque visible en una página, por ejemplo una sección del home.

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `id` | `uuid` | No | PK |
| `client_id` | `uuid` | Sí | Configuración por cliente |
| `page_code` | `varchar(80)` | No | Ej.: `home` |
| `page_zone` | `varchar(50)` | Sí | Ej.: `topbar`, `header`, `home_main`, `pre_footer`, `footer`, `bottom_footer` |
| `block_type` | `varchar(60)` | No | Ej.: `hero_slider`, `product_carousel` |
| `internal_name` | `varchar(150)` | No | Nombre interno |
| `title` | `varchar(250)` | Sí | Título principal |
| `subtitle` | `varchar(250)` | Sí | Subtítulo |
| `description` | `text` | Sí | Descripción opcional |
| `layout_variant` | `varchar(50)` | Sí | Variante de layout |
| `display_order` | `integer` | No | Orden en la página |
| `is_active` | `boolean` | No | Estado activo |
| `content_status` | `varchar(20)` | No | Estado editorial sugerido |
| `is_read_only_when_integrated` | `boolean` | No | Indica si el bloque debe quedar solo lectura en ciertos modos |
| `start_date` | `timestamptz` | Sí | Inicio de vigencia |
| `end_date` | `timestamptz` | Sí | Fin de vigencia |
| `settings_json` | `jsonb` | Sí | Configuración secundaria del bloque |
| `published_at` | `timestamptz` | Sí | Momento de publicación si aplica |
| `updated_by_admin_user_id` | `uuid` | Sí | Último usuario que editó |
| `created_at` | `timestamptz` | No | Fecha creación |
| `updated_at` | `timestamptz` | No | Fecha actualización |

### Tipos de bloque sugeridos

- `top_info_bar`
- `category_sidebar_navigation`
- `hero_slider`
- `hero_banner`
- `featured_categories`
- `promo_banner_grid`
- `product_carousel`
- `tabbed_product_collection`
- `coupon_strip`
- `countdown_promo_banner`
- `newsletter`
- `feature_list`
- `footer_links`
- `footer_brand_block`
- `footer_app_block`

### Índices sugeridos

- PK en `id`
- índice en `client_id + page_code`
- índice en `page_zone`
- índice en `block_type`
- índice en `display_order`
- índice en `is_active`
- índice compuesto en `start_date, end_date`

---

## 3.5 `ContentBlockItem`

Representa un elemento individual dentro de un bloque.

Ejemplos:

- slide individual dentro de slider
- banner dentro de un grid
- tab de colección
- pieza promocional dentro de un strip

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `id` | `uuid` | No | PK |
| `content_block_id` | `uuid` | No | FK a `content_block.id` |
| `parent_item_id` | `uuid` | Sí | FK opcional a sí misma para estructuras internas |
| `item_type` | `varchar(50)` | No | Tipo lógico del ítem |
| `title` | `varchar(250)` | Sí | Título |
| `subtitle` | `varchar(250)` | Sí | Subtítulo |
| `description` | `text` | Sí | Texto adicional |
| `image_url` | `text` | Sí | Imagen desktop |
| `mobile_image_url` | `text` | Sí | Imagen mobile |
| `icon_url` | `text` | Sí | Ícono opcional |
| `link_url` | `text` | Sí | CTA o link principal |
| `link_target` | `varchar(20)` | Sí | `_self`, `_blank` |
| `badge_text` | `varchar(50)` | Sí | Badge |
| `badge_type` | `varchar(30)` | Sí | Tipo de badge |
| `price_text` | `varchar(100)` | Sí | Texto de precio promocional |
| `countdown_end_at` | `timestamptz` | Sí | Countdown si aplica |
| `display_order` | `integer` | No | Orden interno |
| `is_active` | `boolean` | No | Estado activo |
| `content_status` | `varchar(20)` | No | Estado editorial sugerido |
| `start_date` | `timestamptz` | Sí | Inicio de vigencia |
| `end_date` | `timestamptz` | Sí | Fin de vigencia |
| `settings_json` | `jsonb` | Sí | Configuración secundaria |
| `published_at` | `timestamptz` | Sí | Momento de publicación si aplica |
| `updated_by_admin_user_id` | `uuid` | Sí | Último usuario que editó |
| `created_at` | `timestamptz` | No | Fecha creación |
| `updated_at` | `timestamptz` | No | Fecha actualización |

### Índices sugeridos

- PK en `id`
- índice en `content_block_id`
- índice en `parent_item_id`
- índice en `display_order`
- índice en `is_active`

---

## 3.6 `ContentBlockProduct`

Relaciona bloques con productos cuando la selección es manual.

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `content_block_id` | `uuid` | No | FK a `content_block.id` |
| `product_id` | `uuid` | No | FK a `product.id` |
| `display_order` | `integer` | No | Orden manual |
| `created_at` | `timestamptz` | No | Fecha creación |

### Claves e índices sugeridos

- PK compuesta en `content_block_id + product_id`
- índice en `display_order`

---

## 3.7 `ContentBlockCategory`

Relaciona bloques con categorías cuando la selección es manual.

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `content_block_id` | `uuid` | No | FK a `content_block.id` |
| `category_id` | `uuid` | No | FK a `category.id` |
| `display_order` | `integer` | No | Orden manual |
| `created_at` | `timestamptz` | No | Fecha creación |

### Claves e índices sugeridos

- PK compuesta en `content_block_id + category_id`
- índice en `display_order`

---

## 3.8 `ContentBlockRule`

Permite reglas dinámicas para poblar bloques sin selección manual.

Ejemplos:

- más vendidos
- productos nuevos
- productos por categoría
- promociones activas

| Columna | Tipo sugerido | Null | Descripción |
|---|---|---|---|
| `id` | `uuid` | No | PK |
| `content_block_id` | `uuid` | No | FK a `content_block.id` |
| `rule_type` | `varchar(50)` | No | Tipo de regla |
| `rule_value` | `varchar(200)` | Sí | Valor simple si aplica |
| `rule_json` | `jsonb` | Sí | Configuración avanzada |
| `display_order` | `integer` | No | Orden si hay múltiples reglas |
| `is_active` | `boolean` | No | Estado activo |
| `content_status` | `varchar(20)` | No | Estado editorial sugerido |
| `updated_by_admin_user_id` | `uuid` | Sí | Último usuario que editó |
| `created_at` | `timestamptz` | No | Fecha creación |
| `updated_at` | `timestamptz` | No | Fecha actualización |

### Ejemplos de `rule_type`

- `best_sellers`
- `new_arrivals`
- `featured_products`
- `products_by_category`
- `products_by_tag`
- `discounted_products`
- `seasonal_products`

### Índices sugeridos

- PK en `id`
- índice en `content_block_id`
- índice en `rule_type`
- índice en `is_active`

---

## 4. Relaciones principales

Relaciones recomendadas:

- `Category.parent_category_id -> Category.id`
- `NavigationMenuItem.navigation_menu_id -> NavigationMenu.id`
- `NavigationMenuItem.parent_item_id -> NavigationMenuItem.id`
- `NavigationMenuItem.category_id -> Category.id`
- `ContentBlockItem.content_block_id -> ContentBlock.id`
- `ContentBlockItem.parent_item_id -> ContentBlockItem.id`
- `ContentBlockProduct.content_block_id -> ContentBlock.id`
- `ContentBlockCategory.content_block_id -> ContentBlock.id`
- `ContentBlockCategory.category_id -> Category.id`
- `ContentBlockRule.content_block_id -> ContentBlock.id`

Si existe módulo de backoffice con usuarios administrativos, también conviene contemplar:

- `Category.updated_by_admin_user_id -> AdminUser.id`
- `NavigationMenu.updated_by_admin_user_id -> AdminUser.id`
- `NavigationMenuItem.updated_by_admin_user_id -> AdminUser.id`
- `ContentBlock.updated_by_admin_user_id -> AdminUser.id`
- `ContentBlockItem.updated_by_admin_user_id -> AdminUser.id`
- `ContentBlockRule.updated_by_admin_user_id -> AdminUser.id`

No es obligatorio definir `AdminUser` en este documento, pero sí dejar prevista la relación conceptual.

---

## 5. Recomendación de implementación por etapas

### Etapa 1

Implementar como mínimo:

- `Category`
- `NavigationMenu`
- `NavigationMenuItem`
- `ContentBlock`
- `ContentBlockItem`

Con:

- `page_zone`
- `content_status`
- timestamps base

### Etapa 2

Agregar:

- `ContentBlockProduct`
- `ContentBlockCategory`

### Etapa 3

Agregar:

- `ContentBlockRule`

Y, si el backoffice lo requiere:

- trazabilidad más explícita por usuario administrativo

Esto permite arrancar con contenido manual y luego evolucionar a bloques dinámicos.

---

## 6. Qué dejaría fuera del primer corte

- motor de páginas CMS completo
- HTML libre por bloque
- reglas demasiado abiertas en JSON
- sistema avanzado de personalización por dispositivo
- versionado editorial complejo
- workflow editorial complejo con aprobaciones múltiples

---

## 7. Recomendación final

La base más sana para el MVP es:

- usar `Category` como árbol real del catálogo
- usar `NavigationMenu` y `NavigationMenuItem` como capa de navegación configurable
- usar `ContentBlock` y `ContentBlockItem` como motor base del home
- agregar relaciones manuales a productos y categorías
- dejar `ContentBlockRule` listo para la segunda fase
- contemplar estados editoriales básicos y zonas de página desde el inicio

Con esto, el producto ya nace preparado para:

- home sin sidebar
- home con sidebar
- menú superior configurable
- menú lateral configurable
- bloques promocionales
- sliders
- carruseles
- evolución white-label por cliente
- newsletter y bloques de confianza
- footer configurable
- administración desde backoffice con una trazabilidad mínima
