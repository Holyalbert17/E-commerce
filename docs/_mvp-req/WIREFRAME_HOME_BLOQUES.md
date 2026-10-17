# Wireframe del Home por Bloques

**Proyecto:** Plataforma de E-commerce Datamex  
**Fecha:** 2026-06-13  
**Objetivo:** definir la estructura del home por bloques funcionales, separando lo necesario para el MVP de lo que puede pasar a una segunda fase.

---

## 1. Propósito

Este documento toma como base:

- la referencia visual `Index Two`
- la maqueta actual `home-mockup-bootstrap.html`
- el documento `UX_HOME_Y_CONTENIDO.md`
- el modelo preliminar `MODELO_PRELIMINAR_HOME_NAVEGACION.md`

La meta es ordenar el home como una secuencia de bloques funcionales para:

- facilitar el wireframe
- ayudar a priorizar el MVP
- alinear frontend, UX y modelo de datos

---

## 2. Criterio de priorización

### 2.1 Bloques MVP

Son los bloques necesarios para que el home:

- permita navegar
- exprese la identidad visual del cliente
- muestre promociones principales
- exponga productos relevantes
- convierta tráfico en compra

### 2.2 Bloques Fase 2

Son bloques valiosos, pero no imprescindibles para el primer release funcional.

Se agregan después para:

- enriquecer marketing
- reforzar confianza
- completar experiencia institucional
- mejorar captación

---

## 3. Estructura general recomendada del home

Orden sugerido de arriba hacia abajo:

1. top bar informativa
2. header principal
3. navegación principal
4. home hero con sidebar opcional
5. promo cards superiores
6. oferta destacada de la semana
7. productos más vendidos
8. productos en tendencia
9. ofertas calientes
10. categorías populares
11. newsletter
12. beneficios del ecommerce
13. footer principal
14. footer inferior

---

## 4. Bloques detallados

## 4.1 Top bar informativa

**Prioridad:** MVP

### Propósito

- mostrar mensajes rápidos de campaña
- exponer links secundarios útiles
- mostrar idioma y moneda si aplica

### Contenido esperado

- texto promocional
- contador opcional
- links rápidos
- selector de idioma
- selector de moneda

### Datos mínimos

- lista de mensajes
- links
- estado activo
- orden

---

## 4.2 Header principal

**Prioridad:** MVP

### Propósito

- reforzar branding
- permitir búsqueda
- dar acceso a cuenta y carrito

### Contenido esperado

- logo
- nombre de marca
- buscador
- selector de categoría dentro de búsqueda
- acceso a perfil
- acceso a favoritos
- acceso a carrito

### Datos mínimos

- branding por cliente
- categorías visibles en búsqueda
- links funcionales

---

## 4.3 Navegación principal

**Prioridad:** MVP

### Propósito

- navegación superior del sitio
- acceso a páginas y áreas principales

### Contenido esperado

- menú principal
- menú lateral opcional
- CTA o teléfono de soporte

### Datos mínimos

- `NavigationMenu`
- `NavigationMenuItem`

---

## 4.4 Home hero con sidebar opcional

**Prioridad:** MVP

### Propósito

- presentar la oferta principal
- generar impacto visual
- dirigir al usuario a la primera acción comercial

### Variante A

- hero sin sidebar

### Variante B

- sidebar de categorías + hero principal

### Contenido esperado

- título fuerte
- subtítulo
- precio o claim
- CTA
- imagen principal
- slider o banner
- dots o navegación del slider
- sidebar de categorías si aplica

### Datos mínimos

- `ContentBlock` tipo `hero_slider` o `hero_banner`
- `ContentBlockItem`
- categorías visibles en sidebar

---

## 4.5 Promo cards superiores

**Prioridad:** MVP

### Propósito

- mostrar promociones rápidas
- presentar campañas secundarias
- diversificar accesos desde el home

### Contenido esperado

- badge
- título
- descripción corta
- CTA
- imagen o elemento visual

### Datos mínimos

- `ContentBlock` tipo `promo_banner_grid`
- `ContentBlockItem`

---

## 4.6 Oferta destacada de la semana

**Prioridad:** MVP

### Propósito

- empujar una promoción principal con foco comercial claro

### Contenido esperado

- título de sección
- producto o colección destacada
- countdown opcional
- productos secundarios relacionados
- CTA

### Datos mínimos

- `ContentBlock`
- productos manuales o por regla
- countdown

---

## 4.7 Productos más vendidos

**Prioridad:** MVP

### Propósito

- mostrar prueba social implícita
- destacar productos con alta rotación

### Contenido esperado

- título de sección
- card promocional lateral opcional
- grid o slider de productos

### Datos mínimos

- `ContentBlock` tipo `product_carousel`
- `ContentBlockRule` tipo `best_sellers`

---

## 4.8 Productos en tendencia

**Prioridad:** MVP

### Propósito

- mostrar productos agrupados por interés o categoría
- aumentar exploración

### Contenido esperado

- título
- tabs por categoría o grupo
- listado de productos

### Datos mínimos

- `ContentBlock` tipo `tabbed_product_collection`
- `ContentBlockItem` para tabs
- reglas o relaciones por tab

---

## 4.9 Ofertas calientes

**Prioridad:** MVP

### Propósito

- crear un bloque de conversión fuerte
- combinar banner comercial con productos

### Contenido esperado

- banner promocional destacado
- countdown
- grid lateral de productos
- CTA

### Datos mínimos

- `ContentBlock`
- `ContentBlockItem`
- productos manuales o por regla

---

## 4.10 Categorías populares

**Prioridad:** Fase 2

### Propósito

- reforzar exploración de catálogo
- dar acceso rápido a familias importantes

### Contenido esperado

- cards por categoría
- imagen o ícono
- nombre
- mini listado interno o subcategorías
- link a ver más

### Datos mínimos

- `Category`
- `ContentBlockCategory`

---

## 4.11 Newsletter

**Prioridad:** Fase 2

### Propósito

- captación de leads
- suscripción a novedades o promociones

### Contenido esperado

- título
- incentivo de descuento
- input email
- botón de suscripción

### Datos mínimos

- textos configurables
- integración futura con email marketing

---

## 4.12 Beneficios del ecommerce

**Prioridad:** Fase 2

### Propósito

- aumentar confianza
- comunicar diferenciales del servicio

### Contenido esperado

- envío gratis
- pagos seguros
- soporte
- satisfacción garantizada

### Datos mínimos

- lista de beneficios
- ícono
- título
- descripción

---

## 4.13 Footer principal

**Prioridad:** Fase 2

### Propósito

- navegación institucional
- soporte
- links de cuenta
- links corporativos

### Contenido esperado

- bloque de marca
- contacto
- menús de información
- soporte al cliente
- mi cuenta
- descargas de app o pagos si aplica

### Datos mínimos

- `NavigationMenu`
- `NavigationMenuItem`
- `ClientConfiguration`

---

## 4.14 Footer inferior

**Prioridad:** Fase 2

### Propósito

- cierre legal y social

### Contenido esperado

- copyright
- redes sociales
- links secundarios

### Datos mínimos

- links sociales
- texto legal

---

## 5. Resumen de bloques por fase

## 5.1 MVP

- top bar informativa
- header principal
- navegación principal
- hero con sidebar opcional
- promo cards superiores
- oferta destacada de la semana
- productos más vendidos
- productos en tendencia
- ofertas calientes

## 5.2 Fase 2

- categorías populares
- newsletter
- beneficios del ecommerce
- footer principal ampliado
- footer inferior completo

---

## 6. Orden sugerido para construir

### Etapa 1

- top bar
- header
- navegación
- sidebar de categorías
- hero

### Etapa 2

- promo cards
- oferta destacada
- productos más vendidos
- productos en tendencia

### Etapa 3

- ofertas calientes
- categorías populares
- newsletter
- beneficios
- footer

---

## 7. Recomendación práctica para wireframe

Antes de pasar a diseño visual más detallado, conviene hacer:

1. wireframe low fidelity del home completo
2. wireframe del hero con y sin sidebar
3. wireframe de card de producto
4. wireframe de card promocional
5. wireframe de footer

Esto va a permitir validar:

- jerarquía de contenido
- densidad visual
- prioridades comerciales
- navegación efectiva

---

## 8. Conclusión

La referencia `Index Two` confirma que el home no debe pensarse como una sola pieza, sino como una secuencia de bloques reutilizables con prioridad distinta.

Para el MVP, la mejor estrategia es construir primero los bloques que:

- definen navegación
- muestran promoción principal
- exhiben producto
- permiten conversión

Y dejar para una segunda etapa los bloques institucionales o de enriquecimiento comercial que no bloquean la salida inicial.
