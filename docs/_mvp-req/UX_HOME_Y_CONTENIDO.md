# Estructura del Home y Modelo de Contenido

**Proyecto:** Plataforma de E-commerce Datamex  
**Fecha:** 2026-06-11  
**Objetivo:** definir la estructura funcional del home, la navegación visible y un modelo de datos base para contenido dinámico reutilizable en el MVP y fases posteriores.

---

## 1. Propósito

Este documento busca resolver una necesidad clave del producto:

- definir qué elementos componen el home del e-commerce
- decidir cuáles de esos elementos deben ser administrables
- proponer una estructura de datos que permita reutilizar bloques de contenido
- dejar preparada la base de datos para soportar evolución futura sin rediseños grandes

La idea principal es que muchos bloques del home comparten la misma lógica base:

- título
- subtítulo
- imagen o imágenes
- link o acción
- orden de aparición
- vigencia
- estado
- configuración visual

Lo que cambia entre ellos no es tanto la naturaleza de los datos, sino la forma en que se presentan en frontend.

---

## 2. Principio de diseño recomendado

Se recomienda un enfoque de **bloques de contenido genéricos con presentación por tipo**.

Esto significa:

- el home se compone por una lista ordenada de bloques
- cada bloque tiene un tipo
- cada tipo define cómo se renderiza visualmente
- el contenido del bloque vive en base de datos
- el frontend interpreta el tipo y presenta el layout correspondiente

Ejemplos:

- un bloque tipo `hero_slider`
- un bloque tipo `featured_categories`
- un bloque tipo `product_carousel`
- un bloque tipo `promo_banner_grid`

Con este enfoque, el MVP no necesita implementar todos los tipos desde el inicio, pero sí puede dejar la estructura preparada para soportarlos.

---

## 3. Qué debe vivir en base de datos y qué en frontend

### 3.1 Base de datos

Debe vivir en base de datos todo aquello que pueda cambiar por:

- cliente
- campaña
- temporada
- contenido comercial
- activación o desactivación
- orden de aparición

Ejemplos:

- árbol de categorías
- visibilidad en menú
- banners
- sliders
- colecciones destacadas
- bloques del home
- textos promocionales
- productos destacados

### 3.2 Frontend / theme

Debe vivir en frontend todo lo que define la presentación reusable:

- layout de cada tipo de bloque
- comportamiento de slider o carrusel
- estilo visual
- spacing
- responsive behavior
- animaciones

### 3.3 Storage de assets

Las imágenes y archivos no deberían quedar embebidos dentro del código del frontend como solución final.

Se recomienda:

- archivo físico en storage
- metadata en base de datos

---

## 4. Estructura funcional recomendada del home para el MVP

El home del MVP debería poder soportar al menos estos bloques:

1. header con navegación principal
2. hero principal o slider
3. categorías destacadas
4. carrusel de productos destacados
5. banners promocionales
6. productos por colección o criterio comercial
7. footer

Además, conviene contemplar desde el modelo dos variantes estructurales del home:

- `home_standard`
- `home_with_category_sidebar`

### 4.1 Header y navegación

Debe poder mostrar:

- logo
- menú principal
- acceso a búsqueda
- acceso a login o cuenta
- acceso a carrito
- opcionalmente teléfono, redes o CTA comercial

Los menús deberían poder alimentarse desde categorías o desde estructura manual configurable.

### 4.1.1 Variante con sidebar de categorías

Algunos clientes pueden requerir una estructura de home donde la navegación de categorías tenga protagonismo permanente en el primer pantallazo.

En ese escenario, el home se compone por:

- header superior
- navegación principal horizontal
- sidebar lateral de categorías
- hero principal ocupando el espacio central o derecho

Este patrón es especialmente útil cuando:

- el catálogo tiene muchas categorías
- el cliente quiere una experiencia más tipo retail o marketplace
- la navegación por categoría es más importante que el storytelling visual

Por eso, el producto debería soportar al menos estas dos estructuras:

- home centrado en hero y bloques promocionales
- home con navegación lateral de categorías + hero

### 4.2 Hero principal

Puede ser:

- imagen única
- slider de varias piezas

Debe soportar:

- imagen desktop
- imagen mobile
- título
- subtítulo
- CTA
- orden
- vigencia
- activación

También debería poder renderizarse en más de un layout:

- hero full width
- hero compartiendo espacio con sidebar de categorías

### 4.3 Categorías destacadas

Bloque que toma categorías seleccionadas y las presenta en formato visual.

Debe soportar:

- selección manual de categorías
- imagen representativa
- texto opcional
- orden visual

No debe confundirse este bloque con la navegación estructural de categorías.

Una cosa es:

- navegación de categorías

y otra distinta:

- bloque de categorías destacadas dentro del home

### 4.4 Carrusel de productos

Bloque reusable para mostrar productos bajo distintos criterios:

- más vendidos
- nuevos ingresos
- destacados
- temporada
- promociones

El tipo visual puede ser el mismo, y lo que cambia es la regla de selección.

### 4.5 Banners promocionales

Bloque para promociones o campañas.

Debe soportar:

- una o varias piezas
- imagen
- texto
- link
- posición
- vigencia

### 4.6 Footer

Debe soportar contenido administrable al menos en parte:

- texto de marca
- links útiles
- contacto
- redes sociales

---

## 5. Enfoque de modelo genérico de bloques

La idea recomendada es separar:

- definición del bloque
- ítems del bloque
- relaciones opcionales a productos, categorías o promociones

Esto evita crear una tabla nueva para cada widget o sección del home.

---

## 6. Propuesta de entidades base

### 6.1 `ContentBlock`

Representa un bloque visible en una página.

Campos sugeridos:

- `Id`
- `ClientId` o referencia de configuración por cliente
- `PageCode`
- `BlockType`
- `InternalName`
- `Title`
- `Subtitle`
- `Description`
- `DisplayOrder`
- `IsActive`
- `StartDate`
- `EndDate`
- `LayoutVariant`
- `SettingsJson`
- `CreatedAt`
- `UpdatedAt`

Notas:

- `PageCode` permite reutilizar la misma estructura para `home`, `category_landing`, `footer`, etc.
- `BlockType` define el tipo lógico del bloque.
- `LayoutVariant` permite ajustar estilos del mismo tipo.
- `SettingsJson` permite guardar pequeñas configuraciones sin romper el modelo.
- `LayoutVariant` también puede servir para distinguir bloques que cambian su render si existe sidebar o estructura especial en la página.

### 6.2 `ContentBlockItem`

Representa cada elemento interno de un bloque.

Ejemplos:

- una diapositiva del slider
- un banner dentro de un grid
- una categoría destacada
- una tarjeta promocional

Campos sugeridos:

- `Id`
- `ContentBlockId`
- `ItemType`
- `Title`
- `Subtitle`
- `Description`
- `ImageUrl`
- `MobileImageUrl`
- `IconUrl`
- `LinkUrl`
- `LinkTarget`
- `BadgeText`
- `DisplayOrder`
- `IsActive`
- `StartDate`
- `EndDate`
- `SettingsJson`

### 6.3 `ContentBlockProduct`

Tabla de relación para cuando un bloque usa selección manual de productos.

Campos sugeridos:

- `ContentBlockId`
- `ProductId`
- `DisplayOrder`

### 6.4 `ContentBlockCategory`

Tabla de relación para bloques que muestran categorías.

Campos sugeridos:

- `ContentBlockId`
- `CategoryId`
- `DisplayOrder`

### 6.5 `ContentBlockRule`

Opcional para bloques que no toman selección manual, sino reglas.

Campos sugeridos:

- `Id`
- `ContentBlockId`
- `RuleType`
- `RuleValue`
- `DisplayOrder`

Ejemplos de `RuleType`:

- `best_sellers`
- `new_arrivals`
- `featured_products`
- `seasonal_products`
- `discounted_products`
- `products_by_category`
- `products_by_tag`

---

## 7. Tipos de bloques recomendados

### 7.1 Tipos que el modelo debería soportar desde el inicio

Aunque no todos se implementen en el MVP, conviene dejar el modelo preparado para:

- `top_info_bar`
- `main_navigation`
- `category_sidebar_navigation`
- `hero_slider`
- `hero_banner`
- `featured_categories`
- `product_carousel`
- `promo_banner_grid`
- `text_banner`
- `brand_strip`
- `icon_feature_list`
- `custom_html_block` solo si se controla muy bien

### 7.2 Tipos mínimos a implementar en el MVP

Recomendación:

- `hero_slider` o `hero_banner`
- `featured_categories`
- `product_carousel`
- `promo_banner_grid`

Y, si el cliente lo necesita desde el inicio:

- `category_sidebar_navigation`

Con esos tipos ya se puede construir un home comercialmente usable.

---

## 8. Cómo modelar los productos mostrados en el home

Para bloques de productos, no siempre conviene cargar productos uno por uno.

Se recomienda soportar dos estrategias:

### 8.1 Selección manual

Útil para:

- destacados puntuales
- promociones específicas
- campañas curadas

Se implementa con:

- `ContentBlockProduct`

### 8.2 Selección por regla

Útil para:

- más vendidos
- nuevos ingresos
- productos de temporada
- productos de una categoría

Se implementa con:

- `ContentBlockRule`

Esto permite que varios bloques compartan la misma estructura base pero obtengan contenido distinto.

---

## 9. Menús y categorías

### 9.1 Recomendación general

El menú principal no debería vivir hardcodeado en frontend.

Debería poder nacer de:

- estructura de categorías
- o una combinación entre categorías y configuración manual

La misma lógica aplica para el sidebar lateral de categorías en las variantes de home que lo usen.

### 9.2 Entidad de categorías

La tabla `Category` debería soportar al menos:

- `Id`
- `ParentCategoryId`
- `Name`
- `Slug`
- `Description`
- `ImageUrl`
- `DisplayOrder`
- `IsActive`
- `ShowInMainMenu`
- `ShowInSidebarMenu`
- `ShowInHome`
- `MenuLabel`
- `IconUrl`
- `BadgeText`
- `BadgeType`
- `SidebarOrder`

### 9.3 Cuándo usar solo categorías y cuándo usar configuración adicional

Si el menú será simple, puede derivarse solo de `Category`.

Si el menú necesita:

- destacados manuales
- enlaces externos
- promos dentro del menú
- mezcla de categorías y páginas

convendrá una estructura adicional como `NavigationMenu` y `NavigationMenuItem`.

Para el MVP, puede bastar con:

- categorías jerárquicas
- flags de visibilidad

Pero si el producto debe soportar con flexibilidad:

- menú superior
- menú lateral
- links que no son categorías
- badges
- CTAs
- páginas informativas
- enlaces externos

entonces conviene agregar una capa explícita de navegación.

### 9.4 Propuesta de navegación flexible

#### `NavigationMenu`

Representa un menú configurable.

Ejemplos:

- menú principal superior
- menú lateral de categorías
- menú de footer
- menú secundario corporativo

Campos sugeridos:

- `Id`
- `ClientId`
- `Code`
- `Name`
- `MenuType`
- `PageCode`
- `LayoutVariant`
- `IsActive`
- `DisplayOrder`
- `SettingsJson`

Ejemplos de `Code`:

- `main_header_menu`
- `home_category_sidebar`
- `footer_links`
- `top_info_links`

#### `NavigationMenuItem`

Representa cada ítem del menú.

Campos sugeridos:

- `Id`
- `NavigationMenuId`
- `ParentItemId`
- `CategoryId` opcional
- `PageId` opcional
- `Label`
- `Url`
- `Target`
- `IconUrl`
- `BadgeText`
- `BadgeType`
- `DisplayOrder`
- `IsActive`
- `ItemType`
- `SettingsJson`

Ejemplos de `ItemType`:

- `category_link`
- `page_link`
- `external_link`
- `promo_link`
- `group_label`

### 9.5 Cuándo usar `Category` y cuándo `NavigationMenu`

#### Usar solo `Category`

Sirve si:

- el menú refleja exactamente el árbol de categorías
- no hay necesidad de mezclar otros tipos de links
- no hace falta mucha personalización visual

#### Usar `NavigationMenu` + `NavigationMenuItem`

Conviene si:

- el menú superior no coincide exactamente con las categorías
- el sidebar necesita destacados, promos o enlaces especiales
- hay páginas institucionales mezcladas con categorías
- se quieren badges, iconos o comportamiento especial por ítem
- cada cliente puede querer una navegación distinta con el mismo catálogo base

---

## 10. Propuesta mínima de estructura de base preparada para el MVP

Si se busca una base simple pero extensible, mi recomendación es preparar al menos estas entidades:

- `Category`
- `NavigationMenu`
- `NavigationMenuItem`
- `Product`
- `ProductVariant`
- `ProductImage`
- `ContentBlock`
- `ContentBlockItem`
- `ContentBlockProduct`
- `ContentBlockCategory`
- `ContentBlockRule`
- `ClientConfiguration`
- `ThemeAsset`

Con eso ya queda soportado:

- menú por categorías
- menú configurable superior o lateral
- home administrable
- slider
- banners
- categorías destacadas
- carruseles de productos
- variación de contenido por cliente

---

## 11. Qué no haría en el MVP

- crear una tabla específica por cada widget visual
- hardcodear banners y sliders en el frontend
- acoplar el home a un layout fijo no reutilizable
- meter demasiada lógica comercial en `SettingsJson`
- permitir HTML libre sin control, salvo necesidad muy justificada

---

## 12. Recomendación final

Para el MVP, el mejor equilibrio es:

- modelo de bloques genérico
- pocos tipos de bloque implementados
- soporte tanto para selección manual como por reglas
- menús derivados principalmente de categorías
- contenido del home guardado en base de datos
- assets guardados en storage y referenciados desde la base

De esta forma, la base de datos ya nace lista para crecer, sin obligar a implementar toda la complejidad desde el día uno.

---

## 13. Próximo paso sugerido

El siguiente paso natural sería definir una **propuesta de tablas más formal**, por ejemplo:

- nombre de tabla
- columnas
- tipo de dato
- nulabilidad
- claves foráneas
- índices sugeridos

Eso permitiría pasar de definición conceptual a pre-modelado técnico del esquema.
