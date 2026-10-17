# Backoffice Funcional del Ecommerce

**Proyecto:** Plataforma de E-commerce Datamex  
**Fecha:** 2026-06-13  
**Objetivo:** definir el alcance funcional del backoffice del ecommerce, sus módulos, navegación interna, permisos y comportamiento en modo autónomo e integrado con ERP.

---

## 1. Propósito

El backoffice no debe verse como una utilidad secundaria del proyecto, sino como una aplicación interna del producto.

Debe permitir:

- operar el canal digital
- administrar contenido y catálogo
- gestionar pedidos y clientes
- monitorear integraciones
- brindar trazabilidad y control operativo

Esto aplica tanto para:

- clientes que usen el ecommerce en modo autónomo
- clientes que lo usen integrado con ERP

---

## 2. Objetivo del backoffice

El panel administrativo debe servir como centro operativo del ecommerce.

En modo autónomo, será el sistema principal de operación comercial digital.

En modo integrado, será el sistema principal para administrar:

- la capa digital del canal
- el contenido visible
- la navegación
- la observabilidad de integraciones
- la revisión de pedidos generados desde la web

---

## 3. Principios funcionales

- Debe ser usable por usuarios no técnicos.
- Debe exponer claramente qué información es editable y cuál proviene de un sistema externo.
- Debe respetar permisos por rol.
- Debe dejar trazabilidad de acciones sensibles.
- Debe ser consistente entre clientes, aunque el branding cambie.
- Debe poder restringir funciones según modo de operación y ownership de datos.

---

## 4. Módulos funcionales del backoffice

## 4.1 Dashboard

### Objetivo

Dar una vista rápida del estado del ecommerce.

### Contenido mínimo

- pedidos recientes
- pedidos pendientes o con error
- productos destacados o sin stock
- alertas de integración
- métricas básicas
- accesos rápidos

### Métricas sugeridas

- ventas del día
- pedidos del día
- productos sin stock
- productos despublicados
- errores de sincronización recientes

---

## 4.2 Catálogo

### Objetivo

Administrar productos, categorías, atributos, variantes e imágenes.

### Pantallas sugeridas

- listado de productos
- creación/edición de producto
- listado de categorías
- creación/edición de categoría
- atributos y variantes
- galería o gestión de imágenes

### Funciones clave

- alta y edición
- activación/desactivación
- filtros
- búsqueda
- ordenación
- publicación

### Diferencia por modo

#### Autónomo

- edición completa

#### Integrado con ERP

- edición total o parcial según ownership
- algunos campos pueden ser solo lectura
- debe mostrarse claramente si el dato proviene del ERP

---

## 4.3 Inventario

### Objetivo

Consultar y gestionar stock e historial de movimientos.

### Pantallas sugeridas

- listado de inventario
- detalle por SKU
- historial de movimientos
- alertas de bajo stock

### Funciones clave

- ver stock actual
- ver movimientos
- ajustar stock si aplica
- detectar inconsistencias

### Diferencia por modo

#### Autónomo

- ajustes de stock permitidos

#### Integrado con ERP

- preferentemente modo informativo
- edición restringida o inexistente si ERP es maestro

---

## 4.4 Pedidos

### Objetivo

Consultar, filtrar y operar pedidos del canal digital.

### Pantallas sugeridas

- listado de pedidos
- detalle de pedido
- timeline de estado
- eventos de pago
- observaciones internas

### Funciones clave

- buscar pedidos
- ver estado
- ver detalle de pago
- revisar cliente y dirección
- agregar notas internas

### Diferencia por modo

#### Autónomo

- mayor capacidad de gestión directa

#### Integrado con ERP

- operación centrada en consulta, monitoreo y trazabilidad
- estados operativos pueden venir del ERP

---

## 4.5 Clientes

### Objetivo

Consultar y administrar clientes del canal web.

### Pantallas sugeridas

- listado de clientes
- detalle de cliente
- direcciones
- historial de pedidos

### Funciones clave

- búsqueda
- consulta de perfil
- revisión de direcciones
- revisión de historial

### Diferencia por modo

#### Autónomo

- mayor control sobre el dato del cliente

#### Integrado con ERP

- puede requerir sincronización y deduplicación

---

## 4.6 Contenido y navegación

### Objetivo

Administrar la capa visible del storefront.

### Pantallas sugeridas

- bloques del home
- banners
- sliders
- navegación principal
- navegación lateral
- footer

### Funciones clave

- activar/desactivar bloques
- cambiar orden
- editar textos
- reemplazar imágenes
- asociar productos o categorías
- editar menús

### Importancia

Este módulo es especialmente crítico en modo integrado, porque aunque el ERP gobierne parte del catálogo, el storefront digital sigue necesitando administración propia.

---

## 4.7 Integraciones

### Objetivo

Monitorear y controlar integraciones externas.

### Pantallas sugeridas

- dashboard de integraciones
- historial de sincronizaciones
- errores de importación/exportación
- logs de webhooks
- detalle por endpoint

### Funciones clave

- ver estado de sincronizaciones
- detectar errores
- reintentar procesos si aplica
- consultar payloads o eventos
- auditar operaciones externas

---

## 4.8 Configuración del cliente

### Objetivo

Administrar parámetros del ecommerce por cliente.

### Pantallas sugeridas

- branding
- textos globales
- configuración general
- tema básico
- moneda e idioma si aplica

### Funciones clave

- logo
- colores
- activos visuales
- links institucionales
- parámetros operativos visibles

---

## 4.9 Usuarios y permisos

### Objetivo

Administrar acceso al panel.

### Pantallas sugeridas

- usuarios administrativos
- roles
- permisos

### Funciones clave

- alta/baja de usuario
- asignación de roles
- bloqueo o reactivación
- control de acceso por módulo

---

## 4.10 Auditoría

### Objetivo

Mantener trazabilidad de acciones sensibles.

### Eventos sugeridos

- edición de productos
- cambios en navegación o contenido
- cambios en configuración
- reintentos de integración
- cambios de permisos

---

## 5. Menú lateral sugerido del backoffice

Orden recomendado:

1. Dashboard
2. Catálogo
3. Categorías
4. Inventario
5. Pedidos
6. Clientes
7. Contenido del sitio
8. Navegación
9. Integraciones
10. Configuración
11. Usuarios y permisos
12. Auditoría

Dependiendo del cliente o del rol, algunos módulos pueden ocultarse o simplificarse.

---

## 6. Roles sugeridos

## 6.1 Super Admin

Puede:

- administrar toda la plataforma del cliente
- gestionar configuración
- gestionar permisos
- ver integraciones
- editar contenido
- operar catálogo y pedidos

## 6.2 Operador Ecommerce

Puede:

- gestionar catálogo
- revisar pedidos
- gestionar contenido del sitio
- ver clientes

No debería:

- cambiar configuración crítica
- cambiar permisos

## 6.3 Operador de Contenido

Puede:

- editar home
- editar banners
- editar navegación
- editar bloques visuales

## 6.4 Operador de Integraciones

Puede:

- monitorear sincronizaciones
- revisar errores
- consultar logs
- ejecutar reintentos si aplica

## 6.5 Vendedor

Rol opcional.

Puede:

- ver catálogo asignado
- ver pedidos relacionados
- ver stock relacionado

---

## 7. Diferencias funcionales por modo

## 7.1 Modo autónomo

El backoffice es el centro principal de operación.

Debe permitir:

- editar catálogo
- gestionar stock
- operar pedidos
- administrar contenido
- administrar configuración

## 7.2 Modo integrado con ERP

El backoffice sigue siendo obligatorio, pero con foco distinto.

Debe permitir:

- monitorear catálogo sincronizado
- administrar contenido del sitio
- revisar pedidos del canal digital
- ver stock y precios sincronizados
- auditar sincronizaciones
- identificar errores

Debe poder marcar visualmente:

- editable localmente
- sincronizado desde ERP
- bloqueado por ownership externo

---

## 8. Requisitos UX del backoffice

- navegación lateral clara
- filtros potentes en listados
- tablas operativas
- formularios consistentes
- indicadores visuales de estado
- badges o etiquetas de ownership
- alertas visibles para errores de integración

---

## 9. Prioridad para MVP

## 9.1 Módulos mínimos del MVP

- Dashboard
- Catálogo
- Categorías
- Pedidos
- Contenido del sitio
- Navegación
- Integraciones básicas
- Configuración básica
- Usuarios administrativos básicos

## 9.2 Módulos que pueden simplificarse en MVP

- Inventario avanzado
- Clientes avanzado
- Auditoría detallada
- permisos muy granulares

---

## 10. Relación con otros documentos

Este documento complementa:

- `DEFI.md`
- `UX_HOME_Y_CONTENIDO.md`
- `MODELO_PRELIMINAR_HOME_NAVEGACION.md`
- `WIREFRAME_HOME_BLOQUES.md`

---

## 11. Próximo paso sugerido

El siguiente paso lógico sería crear un documento como:

- `WIREFRAME_BACKOFFICE_BLOQUES.md`

con:

- menú lateral del admin
- pantallas clave
- jerarquía interna del panel
- estados por modo autónomo / integrado

---

## 12. Conclusión

El backoffice debe tratarse como una parte central del producto. No solo habilita operación interna, sino que también sostiene el valor white-label y ERP-ready del ecommerce.

En modo autónomo, administra la operación.

En modo integrado, administra el canal digital y la observabilidad de integración.
