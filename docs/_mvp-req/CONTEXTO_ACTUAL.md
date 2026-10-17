# Contexto Actual del Proyecto

**Proyecto:** Plataforma de E-commerce Datamex  
**Fecha de actualización:** 2026-06-10  
**Objetivo de este documento:** dejar una referencia breve y práctica para retomar el trabajo en otra sesión sin perder contexto.

---

## 1. Estado actual

Se trabajó sobre el documento principal [DEFI.md](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/DEFI.md), que quedó evolucionado hasta la versión `0.3`.

El documento ya no es solo una lluvia de ideas. Ahora funciona como base de arranque funcional y técnico del producto.

Temas ya incorporados:

- visión del producto como e-commerce reusable para múltiples clientes
- operación en modo autónomo o integrado con ERP web
- enfoque `white-label`
- despliegue aislado por cliente
- descarte explícito de multi-tenant compartido como estrategia principal
- arquitectura recomendada `modular monolith`
- necesidad explícita de backoffice propio del ecommerce
- módulos principales del sistema
- alcance del MVP
- reglas críticas de negocio
- riesgos tempranos
- hoja de ruta sugerida
- matriz inicial de ownership entre ERP y e-commerce

---

## 2. Visión del producto acordada hasta ahora

La plataforma debe:

- poder funcionar por sí sola como e-commerce completo
- también poder integrarse con un ERP web externo
- exponer endpoints para sincronización o alimentación desde sistemas externos
- permitir personalización visual por cliente
- reutilizar un único código base para múltiples clientes
- desplegarse por cliente en instancias independientes
- incluir un panel de backoffice para operación administrativa del canal digital

Modelo de despliegue acordado:

- cada cliente tendrá su propio dominio
- cada cliente tendrá su propio stack Docker
- cada cliente tendrá su propia base de datos
- varios clientes pueden convivir en un mismo servidor Ubuntu
- si la carga crece, se pueden distribuir clientes entre más máquinas o máquinas virtuales

Conclusión de arquitectura de producto:

- **sí a single-tenant por despliegue**
- **no a multi-tenant compartido como base del producto**

---

## 3. Decisiones importantes ya tomadas

- Se recomienda iniciar con **modular monolith**.
- El stock debe modelarse por **SKU/variante**.
- El producto debe ser **ERP-ready**.
- El producto debe ser **white-label**.
- El despliegue debe ser **aislado por cliente**.
- El e-commerce debe soportar **modo autónomo** y **modo integrado**.
- El producto debe incluir **backoffice interno** tanto para modo autónomo como integrado.

### Alcance actual del backoffice

Se dejó establecido que el backoffice no debe ser tratado como una utilidad secundaria, sino como una aplicación interna del producto.

Debe cubrir al menos:

- catálogo
- categorías
- variantes
- contenido del home y navegación
- pedidos
- clientes
- monitoreo de integraciones
- auditoría básica

En modo integrado con ERP:

- algunas operaciones podrán ser informativas o restringidas
- el backoffice seguirá siendo necesario para monitorear sincronizaciones y administrar la capa digital del ecommerce

---

## 4. Ownership propuesto entre ERP y E-commerce

Línea general acordada:

- **ERP maestro** para datos operativos y estructurales
- **E-commerce maestro** para experiencia digital y canal web
- **Pedidos** con ownership secuencial o mixto

Resumen práctico:

- **ERP maestro:** categorías, productos base, variantes/SKUs, stock, precios base, facturación, estados logísticos
- **E-commerce maestro:** imágenes, SEO, slugs, atributos de presentación, clientes web, direcciones, carrito, checkout, pagos, theme, branding
- **Mixto:** pedidos

Flujo recomendado de pedidos:

1. el cliente compra en el e-commerce
2. el e-commerce registra pedido y pago
3. el e-commerce envía el pedido al ERP
4. el ERP confirma recepción y continúa el ciclo operativo
5. el e-commerce refleja los estados posteriores desde ERP

---

## 5. Riesgos que quedaron identificados

- arrancar con microservicios demasiado pronto
- modelar stock a nivel producto en vez de variante
- acoplar la solución a un ERP específico
- no definir idempotencia desde el inicio
- permitir forks del frontend por cliente en vez de un sistema de theming
- mezclar clientes en una misma base sin necesidad real
- no definir claramente qué sistema es maestro por entidad

---

## 6. Qué archivo mirar primero al retomar

Archivo principal:

- [DEFI.md](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/DEFI.md)
- [UX_HOME_Y_CONTENIDO.md](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/UX_HOME_Y_CONTENIDO.md)
- [MODELO_PRELIMINAR_HOME_NAVEGACION.md](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/MODELO_PRELIMINAR_HOME_NAVEGACION.md)
- [home-mockup-bootstrap.html](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/home-mockup-bootstrap.html)
- [WIREFRAME_HOME_BLOQUES.md](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/WIREFRAME_HOME_BLOQUES.md)
- [BACKOFFICE_FUNCIONAL.md](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/BACKOFFICE_FUNCIONAL.md)

Secciones especialmente importantes dentro de `DEFI.md`:

- objetivo del proyecto
- enfoque arquitectónico recomendado
- estrategia de producto para múltiples clientes
- decisión sobre multi-tenant
- requerimientos funcionales
- próximos pasos inmediatos
- matriz de ownership ERP vs E-commerce

Documento complementario nuevo:

- `UX_HOME_Y_CONTENIDO.md`: define estructura funcional del home, bloques reutilizables, menús por categorías y modelo de contenido base para el MVP
- `MODELO_PRELIMINAR_HOME_NAVEGACION.md`: baja esa propuesta a tablas, columnas, tipos sugeridos, relaciones e índices iniciales
- `MODELO_PRELIMINAR_HOME_NAVEGACION.md`: actualizado para incluir zonas de página, bloques adicionales del storefront, trazabilidad editorial básica y alineación con backoffice
- `home-mockup-bootstrap.html`: maqueta estática del home en HTML + Bootstrap, inspirada en las capturas de referencia y útil como base visual antes de pasar a Angular
- `WIREFRAME_HOME_BLOQUES.md`: ordena el home en bloques funcionales y separa qué va al MVP y qué puede pasar a fase 2
- `BACKOFFICE_FUNCIONAL.md`: define el panel administrativo del ecommerce, sus módulos, roles, pantallas principales y diferencias entre modo autónomo e integrado con ERP

---

## 6.1 Estado actual de la maqueta visual

Se creó una maqueta estática del home:

- [home-mockup-bootstrap.html](D:/WORK/Definiciones/Ecommerce%20-%20Datamex/home-mockup-bootstrap.html)

Características actuales de la maqueta:

- HTML puro
- Bootstrap por CDN
- CSS custom dentro del mismo archivo
- variante de home con sidebar de categorías
- hero principal
- header con búsqueda y accesos rápidos
- navegación superior
- categorías destacadas
- banners promocionales
- secciones de productos
- cupón promocional

Objetivo de la maqueta:

- servir como referencia visual temprana
- validar estructura del home
- ayudar a decidir componentes futuros
- facilitar una posterior migración a Angular

---

## 7. Próximos pasos sugeridos para la próxima sesión

### Opción recomendada de continuación

Crear una **matriz técnica de integración ERP <-> E-commerce**.

La idea es transformar la matriz funcional actual en un documento más implementable, con columnas como:

- entidad
- sistema maestro
- sentido del flujo
- endpoint sugerido
- método HTTP
- payload base
- frecuencia
- mecanismo de autenticación
- idempotency key
- política de retry
- manejo de conflicto
- observaciones

### Ejemplos de flujos que convendría definir primero

- sincronización de productos
- sincronización de stock
- sincronización de precios
- alta o sincronización de clientes web hacia ERP
- creación de pedidos hacia ERP
- actualización de estados logísticos desde ERP hacia e-commerce

---

## 8. Decisiones de negocio que siguen pendientes

- qué entidades serán maestras en cada escenario real de cliente
- si el rol vendedor realmente formará parte del MVP
- cómo se manejarán envíos
- cómo se manejarán impuestos y facturación
- si promociones y cupones vivirán en e-commerce, ERP o ambos
- hasta qué nivel llegará el theming configurable por cliente
- si el catálogo integrado desde ERP será siempre total o parcialmente editable en e-commerce

---

## 9. Recomendación de continuidad

La próxima vez conviene no volver a discutir arquitectura general desde cero.

La base conceptual ya está suficientemente clara. El trabajo debería enfocarse en:

1. bajar las integraciones a contratos concretos
2. definir ownership real por entidad
3. diseñar el modelo de datos inicial
4. empezar a preparar la estructura técnica del MVP

---

## 10. Nota práctica

El archivo `DEFI.md` quedó guardado correctamente en UTF-8. Si en algún terminal se ven caracteres raros, el problema probablemente sea la codificación de salida de la consola y no el archivo.
