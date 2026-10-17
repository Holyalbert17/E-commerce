# QA — Panel Admin E-commerce

**Ámbito:** solo panel admin (`apps/admin`, típ. `:4201`)  
**Tenant de referencia:** `casa-imperial` (empresa 41)  
**API:** `http://localhost:5082`  
**Fecha de generación:** 2026-08-11  

### Ejecución smoke automatizada (2026-08-11)

| Campo | Valor |
|-------|-------|
| Script | `tools/qa-admin-smoke.ps1` |
| Resultado | **PASS 67 · FAIL 0 · SKIP 1** (~16s) |
| Detalle | [`QA-ADMIN-SMOKE-RESULTS.md`](./QA-ADMIN-SMOKE-RESULTS.md) · CSV hermano |
| Cobertura | Login +/- · auth 401 · GETs de todos los módulos · detalle producto/pedido/carrito · SPA 21 rutas |
| Continuación | `tools/qa-continue-smoke.ps1` → mutaciones cupón/chofer + storefront → [`QA-CONTINUE-SMOKE-RESULTS.md`](./QA-CONTINUE-SMOKE-RESULTS.md) **47 PASS / 0 FAIL** |
| Continue-2 | `tools/qa-continue-2.ps1` → pedido admin + cupón carrito + devolución → [`QA-CONTINUE-2-RESULTS.md`](./QA-CONTINUE-2-RESULTS.md) **20 PASS / 0 FAIL** |
| Fix durante QA | `dashboard-page` `@else if (; as)`; `GetMarcasAsync` EF 500; API republicada `_run-api-v26` |

**Storefront:** fuera de alcance en esta pasada (salvo smoke “Abrir tienda”)

---

## 0. Preparación

| # | Check | OK |
|---|--------|----|
| 0.1 | API levantada y responde `/health` o login | ☐ |
| 0.2 | Admin build/serve sin error de consola al cargar login | ☐ |
| 0.3 | Usuario admin con permisos amplios (`manager:all` o set completo) | ☐ |
| 0.4 | Usuario limitado (solo Pedidos o solo Catálogo) disponible para negativos | ☐ |
| 0.5 | Anotar slug tenant, idEmpresa, modo Standalone vs Integrated | ☐ |

**Resultado bloque 0:** Pass ☐ / Fail ☐ / Blocked ☐  
**Notas:**

---

## 1. Acceso, sesión y shell

### 1.1 Login `/login`

| ID | Caso | Pasos | Esperado | OK |
|----|------|-------|----------|----|
| A-01 | Login OK | Slug + user + pass → Entrar | Entra al dashboard o 1ª ruta permitida; se ve tenant en header | ☐ |
| A-02 | Credenciales inválidas | Pass incorrecto | Alert danger; no navega | ☐ |
| A-03 | Tenant inexistente | Slug inventado | Error claro; sin sesión | ☐ |
| A-04 | Mostrar/ocultar password | Toggle ojo | Input type cambia | ☐ |
| A-05 | Sin permisos e-com | User sin ops | Mensaje / no entra a rutas protegidas | ☐ |

### 1.2 Shell (navbar / sidebar / jump)

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| A-06 | Sidebar muestra ítems según permisos | Usuario limitado no ve Marketplace/CRM/etc. | ☐ |
| A-07 | Jump “Ir a…” (Ctrl+/) | Filtra y navega a Catálogo, Sync, Branding | ☐ |
| A-08 | Abrir Tienda | Abre storefront del tenant en nueva pestaña | ☐ |
| A-09 | Badge CRM / Contacto | Si hay no leídos, badge visible y actualiza | ☐ |
| A-10 | Logout | Vuelve a login; rutas auth redirigen | ☐ |
| A-11 | Responsive | Sidebar toggle en <992px usable | ☐ |

**Resultado bloque 1:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 2. Dashboard `/`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| D-01 | Carga KPIs | Vitrina, agotados, pedidos hoy, pendientes ETL (o placeholders sin error) | ☐ |
| D-02 | Refrescar | Recarga números sin romper UI | ☐ |
| D-03 | KPI Agotados | Navega a catálogo filtrado | ☐ |
| D-04 | Checklist “Ir” | Links a sync / activar / listas / branding / config | ☐ |
| D-05 | Strip ERP | Traer stock / links Config / Sync (si Integrated) | ☐ |
| D-06 | Abrir tienda desde dashboard | Storefront OK | ☐ |

**Resultado bloque 2:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 3. Sync ERP `/sync`

> **Cuidado:** acciones masivas. Preferir ambiente de prueba.

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| S-01 | Estado / salud | Tabla cursors con ok/error/omitido | ☐ |
| S-02 | Traer stock | Feedback filas; stock visible luego en catálogo | ☐ |
| S-03 | Traer todo del ERP | Completa sin crash; errores en lastError | ☐ |
| S-04 | Reintentar pendientes | Confirm → reintenta | ☐ |
| S-05 | Publicar todo fuera de vitrina | Doble confirm; solo si se quiere | ☐ |
| S-06 | Busy lock | Mientras corre, botones bloqueados | ☐ |
| S-07 | Quick links | Config, Catálogo, Listas, Clientes, etc. | ☐ |

**Resultado bloque 3:** Pass ☐ / Fail ☐ / Blocked ☐ / N/A Standalone ☐

---

## 4. Catálogo y producto

### 4.1 Listado `/catalogo`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-01 | Listado pagina | Items + Anterior/Siguiente | ☐ |
| C-02 | Buscar | Filtra por nombre/SKU | ☐ |
| C-03 | Filtro vitrina on/off | Solo los correspondientes | ☐ |
| C-04 | Filtro estado (activo/agotado/borrador/oculto) | Correcto | ☐ |
| C-05 | Calidad sin_precio / sin_stock | Lista candidatos | ☐ |
| C-06 | Toggle vitrina inline | Cambia y persiste al refrescar | ☐ |
| C-07 | Stock editable | Solo Standalone; Integrated locked | ☐ |
| C-08 | Export CSV | Descarga archivo válido | ☐ |
| C-09 | Import CSV (muestra chica) | Actualiza sin corromper | ☐ |
| C-10 | Links editar / imágenes / embalajes / movimientos | Navegan OK | ☐ |

### 4.2 Alta/edición `/catalogo/nuevo` · `/catalogo/:id/editar`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-11 | Crear artículo mínimo | Guarda; no publicado por defecto (si aplica) | ☐ |
| C-12 | Editar campos + SEO | Persistencia | ☐ |
| C-13 | Switches Activo / Vitrina / estado ecom | Persistencia | ☐ |
| C-14 | Eliminar / inactivar | Confirm; soft-delete | ☐ |
| C-15 | Validación required | No guarda sin SKU/nombre/etc. | ☐ |

### 4.3 Activar vitrina `/catalogo/activar`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-16 | Filtro fuera de vitrina | Default correcto | ☐ |
| C-17 | Seleccionar página → Publicar (N) | Pasan a vitrina | ☐ |
| C-18 | Ocultar seleccionados | OFF + estado oculto | ☐ |
| C-19 | Selección no cruza páginas | Al cambiar página se pierde o se documenta | ☐ |

### 4.4 Imágenes `/catalogo/:id/imagenes`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-20 | Upload multi (JPEG/PNG/WebP) | Aparecen en galería | ☐ |
| C-21 | Rechazo >20MB / tipo inválido | Mensaje error | ☐ |
| C-22 | Agregar por URL | OK o error URL | ☐ |
| C-23 | Principal + reordenar ↑↓ | Persistencia | ☐ |
| C-24 | Eliminar imagen | Confirm / desaparece | ☐ |

### 4.5 Unidades / embalajes `/unidades` · `/catalogo/:id/embalajes`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-25 | CRUD UOM | Alta/edición OK | ☐ |
| C-26 | Embalaje por artículo | Fracción, paso, mín. | ☐ |
| C-27 | Códigos de barra | Agregar / quitar | ☐ |

### 4.6 Parámetros `/parametros-producto`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-28 | Tabs Marcas/Líneas/Tipos/… | CRUD + incluir inactivos | ☐ |
| C-29 | Inactivar usado en producto | Soft; producto sigue referenciando | ☐ |

### 4.7 Movimientos `/catalogo/:id/movimientos`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| C-30 | Historial lectura | Tipos, cantidades, refs | ☐ |
| C-31 | Id inválido | Error o vacío controlado | ☐ |

**Resultado bloque 4:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 5. Pedidos `/pedidos` · `/pedidos/:id`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| P-01 | Listado + filtros N°/cliente | Resultados coherentes | ☐ |
| P-02 | Filtro ERP pendientes / ya | Correcto | ☐ |
| P-03 | Filtro fulfillment | Correcto | ☐ |
| P-04 | Bulk Aplicar estado | Cambia estados seleccionados | ☐ |
| P-05 | Facturar sel. | Solo cuando corresponde; sin inventar factura local fiscal | ☐ |
| P-06 | Enviar a ERP vs Solo marcar local | Distintos efectos; no confundir | ☐ |
| P-07 | CSV export | Archivo OK | ☐ |
| P-08 | Logs ERP → Reintentar | Reintenta error | ☐ |
| P-09 | Detalle: Ver/Imprimir factura ERP | Solo si `puedeDescargarFactura` / N° fiscal ERP | ☐ |
| P-10 | Verificar factura ERP | Actualiza espejo local | ☐ |
| P-11 | Reintentar migración | Si no migrado | ☐ |
| P-12 | Asignar chofer + Guardar | Persiste; Sin chofer limpia | ☐ |
| P-13 | Agregar hito tracking | Estados + mensaje | ☐ |

**Resultado bloque 5:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 6. Carritos `/carritos` · `/carritos/:id`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| K-01 | Filtros search / migración / fechas | OK | ☐ |
| K-02 | Abandonados 24h+ | Lista coherente | ☐ |
| K-03 | Marcar migrado | Solo Cerrado no migrado | ☐ |
| K-04 | Exportar | CSV; flags documentados | ☐ |
| K-05 | Detalle solo lectura | Cabecera, ítems, timeline, link pedido | ☐ |

**Resultado bloque 6:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 7. Clientes `/clientes`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| L-01 | Nuevo / editar / Guardar | Persistencia | ☐ |
| L-02 | Aprobar mayorista + lista | Estado + lista asignada | ☐ |
| L-03 | Rechazar mayorista | Estado rechazo | ☐ |
| L-04 | Asignar lista + historial | OK | ☐ |
| L-05 | Desactivar (trash) | Soft-delete | ☐ |
| L-06 | Export CSV/Excel · Import CSV | Smoke | ☐ |
| L-07 | Links a pedidos/carritos | Query correcta | ☐ |

**Resultado bloque 7:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 8. Choferes `/choferes`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| H-01 | Nuevo chofer (nro*, nombre*) | Guarda activo | ☐ |
| H-02 | Filtro activos/inactivos + search | OK | ☐ |
| H-03 | Desactivar | Confirm; no aparece en select pedidos (activos) | ☐ |
| H-04 | Nro duplicado | Error controlado | ☐ |

**Resultado bloque 8:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 9. Listas de precios `/listas-precios`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| LP-01 | Nueva lista (canal B2C/B2B/BOTH) | Pendiente | ☐ |
| LP-02 | Autorizar / Anular / Cancelar | Transiciones válidas | ☐ |
| LP-03 | Detalle: alta/edición/baja línea | OK | ☐ |
| LP-04 | Import/Export CSV detalle | Smoke | ☐ |
| LP-05 | Lista no autorizada | No afecta precios vitrina | ☐ |

**Resultado bloque 9:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 10. Tarifas delivery `/tarifas`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| T-01 | Crear tarifa | Estado Pendiente | ☐ |
| T-02 | Editar importe solo Pendiente | Autorizada no edita importe | ☐ |
| T-03 | Bulk Autorizar / Anular / Cancelar | Según reglas | ☐ |
| T-04 | Filtro ciudad/barrio/estado | OK | ☐ |

**Resultado bloque 10:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 11. Marketing y engagement

### 11.1 Promociones `/promociones`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| M-01 | Nuevo TipoGrupo 2 (Publicidad) + imagen | Aparece en home si layout ON | ☐ |
| M-02 | Nuevo TipoGrupo 3 (Oferta) | Flyers / productos oferta | ☐ |
| M-03 | Destacados / Combos (0–1) | Guardan y listan | ☐ |
| M-04 | Vigencia / orden / artículos | Persistencia | ☐ |
| M-05 | Desactivar | Confirm; deja de verse | ☐ |
| M-06 | Tip cupones | Copy indica Marketplace + banner | ☐ |

### 11.2 Marketplace `/marketplace`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| M-07 | Tab Reseñas: aprobar / ocultar | Persistencia | ☐ |
| M-08 | Tab Cupones: alta código + vigencia | Activo; tip marketing visible | ☐ |
| M-09 | Activar/Desactivar cupón | OK | ☐ |
| M-10 | Devoluciones: filtro pendientes | Orden/prioridad | ☐ |
| M-11 | Aprobar / rechazar / completar | Confirm; estados correctos | ☐ |
| M-12 | Tabs ocultos sin permiso | Usuario sin Cupones no ve tab | ☐ |

### 11.3 Contenido CMS `/contenido`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| M-13 | Bloques home: editar + Guardar | Draft vs published | ☐ |
| M-14 | Completar bloques / Actualizar | Smoke | ☐ |
| M-15 | Menú tienda: ítems + Guardar | Visible en storefront si published | ☐ |

### 11.4 Personalización `/personalizacion`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| M-16 | Layout home: toggles + orden ↑↓ | Persistencia | ☐ |
| M-17 | mostrarCupon OFF por defecto / tip banners | UX marketing | ☐ |
| M-18 | Tema / textos carrito / checkout / widgets | Guardan | ☐ |
| M-19 | Publicar ahora / Programar / Restaurar | Versiones OK | ☐ |
| M-20 | Defaults | Confirma y restaura defaults | ☐ |

### 11.5 Branding `/branding`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| M-21 | Guardar identidad + colores | Storefront refleja | ☐ |
| M-22 | Upload/Quitar logo, favicon, banner | OK | ☐ |
| M-23 | Footer / redes / mapa coords | Persistencia | ☐ |

**Resultado bloque 11:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 12. Atención al cliente

### 12.1 CRM `/crm`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| R-01 | Inbox carga; filtros cola | Todas/Abiertas/Sin asignar/Mis/Cerradas | ☐ |
| R-02 | Badge “sin asignar” / abiertas | Números coherentes | ☐ |
| R-03 | Abrir hilo + Asignarme | Agente asignado | ☐ |
| R-04 | Responder texto + quick reply | Mensaje llega (SignalR o poll) | ☐ |
| R-05 | Adjuntar imagen | Preview / envío | ☐ |
| R-06 | Cerrar chat / Reabrir | Estados OK | ☐ |
| R-07 | Menú sidebar “Atención al cliente” | Label correcto | ☐ |

### 12.2 Contacto `/contacto`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| R-08 | Filtro Todos / No leídos / Leídos | OK | ☐ |
| R-09 | Marcar leído / no leído | Badge actualiza | ☐ |
| R-10 | Responder email (mailto) | Abre cliente mail | ☐ |

**Resultado bloque 12:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 13. Configuración empresa `/configuracion`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| CFG-01 | Guardar datos empresa | Persistencia | ☐ |
| CFG-02 | Modo Standalone vs Integrated | UI acorde | ☐ |
| CFG-03 | Activar integración completa | Confirm; flags cambian | ☐ |
| CFG-04 | Flags sync stock / write-back / migrar | Persistencia | ☐ |
| CFG-05 | Traer stock / Salud / Reintentar | Smoke | ☐ |
| CFG-06 | Agencias + horarios CRUD | Alta/Guardar/Desactivar | ☐ |
| CFG-07 | Limpiar CS tenant (si aplica) | Confirm danger; solo en prueba | ☐ |

**Resultado bloque 13:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 14. Reportes `/reportes`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| REP-01 | Rango fechas → Generar | Totales + top SKUs | ☐ |
| REP-02 | Rango vacío/invertido | Validación | ☐ |
| REP-03 | Export CSV | Archivo OK | ☐ |
| REP-04 | Errores write-back → Ver pedido | Navega al detalle | ☐ |
| REP-05 | Link pedidos pendientes ERP | Filtro correcto | ☐ |

**Resultado bloque 14:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 15. Multi-tenant y seguridad (super)

### 15.1 Empresas `/empresas`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| TEN-01 | Listado tiendas | Activo, origen, ERP | ☐ |
| TEN-02 | Crear Standalone (solo si ambiente lo permite) | Slug único | ☐ |
| TEN-03 | Provisionar desde ERP | IDEmpresa + depósito | ☐ |
| TEN-04 | Sync ERP ahora / Estado | Smoke | ☐ |
| TEN-05 | Usuario no-super | Ruta bloqueada | ☐ |

### 15.2 Manager `/manager`

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| SEC-01 | Roles: nuevo + árbol permisos + Guardar | Persistencia | ☐ |
| SEC-02 | Usuarios: alta + asignar roles | Login con nuevo user | ☐ |
| SEC-03 | Desactivar usuario/rol | Soft; no puede entrar | ☐ |
| SEC-04 | Catálogo operaciones | Consulta PermissionCodes | ☐ |
| SEC-05 | Auto-lock | No quitarse a sí mismo todo acceso sin aviso | ☐ |

**Resultado bloque 15:** Pass ☐ / Fail ☐ / Blocked ☐ / N/A ☐

---

## 16. Permisos negativos (smoke)

| ID | Usuario | Acción | Esperado | OK |
|----|---------|--------|----------|----|
| N-01 | Solo Catálogo | Abrir `/pedidos` | Redirect / vacío / 403 UI | ☐ |
| N-02 | Solo Pedidos | Abrir `/marketplace` cupones | Tab/ruta bloqueada | ☐ |
| N-03 | Sin Chat | `/crm` | No entra; sin ítem menú | ☐ |
| N-04 | URL directa sin auth | `/catalogo` | → login | ☐ |

**Resultado bloque 16:** Pass ☐ / Fail ☐ / Blocked ☐

---

## 17. Hotspots cross-cutting (obligatorios)

| ID | Caso | Esperado | OK |
|----|------|----------|----|
| X-01 | Enviar a ERP ≠ Solo marcar local | Efectos distintos verificados | ☐ |
| X-02 | Factura imprimible solo si fiscal ERP | No PDF local como factura | ☐ |
| X-03 | Cupón Marketplace ≠ franja CMS home | Código aplica en checkout; banner publicita | ☐ |
| X-04 | Layout Personalización OFF | Sección no se ve aunque haya promo | ☐ |
| X-05 | Stock Integrated | Catálogo read-only; sync es fuente | ☐ |
| X-06 | Lista precios no autorizada | No cambia precio tienda | ☐ |
| X-07 | Consola browser | Sin errores rojos en flujos felices | ☐ |
| X-08 | Network 401/403 | Sesión expirada → re-login | ☐ |

**Resultado bloque 17:** Pass ☐ / Fail ☐ / Blocked ☐

---

## Matriz de cobertura (rutas)

| Ruta | Bloque | Smoke | Profundo |
|------|--------|-------|----------|
| `/login` | 1 | ☐ | ☐ |
| `/` | 2 | ☐ | ☐ |
| `/sync` | 3 | ☐ | ☐ |
| `/catalogo` (+ subrutas) | 4 | ☐ | ☐ |
| `/pedidos` (+ detalle) | 5 | ☐ | ☐ |
| `/carritos` (+ detalle) | 6 | ☐ | ☐ |
| `/clientes` | 7 | ☐ | ☐ |
| `/choferes` | 8 | ☐ | ☐ |
| `/listas-precios` | 9 | ☐ | ☐ |
| `/tarifas` | 10 | ☐ | ☐ |
| `/promociones` | 11 | ☐ | ☐ |
| `/marketplace` | 11 | ☐ | ☐ |
| `/contenido` | 11 | ☐ | ☐ |
| `/personalizacion` | 11 | ☐ | ☐ |
| `/branding` | 11 | ☐ | ☐ |
| `/crm` | 12 | ☐ | ☐ |
| `/contacto` | 12 | ☐ | ☐ |
| `/configuracion` | 13 | ☐ | ☐ |
| `/reportes` | 14 | ☐ | ☐ |
| `/empresas` | 15 | ☐ | ☐ |
| `/manager` | 15 | ☐ | ☐ |

---

## Orden sugerido de ejecución (1 día)

1. Prep + Login + Shell + permisos negativos (bloques 0, 1, 16) — **45 min**  
2. Dashboard + Config (lectura) + Sync stock (bloques 2, 13 parcial, 3) — **60 min**  
3. Catálogo completo (bloque 4) — **90 min**  
4. Pedidos + Carritos + Clientes + Choferes (5–8) — **90 min**  
5. Precios + Tarifas (9–10) — **45 min**  
6. Marketing + CRM + Contacto (11–12) — **75 min**  
7. Reportes + Seguridad/Tenants si aplica (14–15) + Hotspots (17) — **45 min**

**Total estimado smoke profundo:** ~8 h  
**Smoke mínimo (1 caso por ruta + hotspots):** ~2 h

---

## Registro de bugs

| Bug # | Módulo | ID caso | Severidad (S1–S4) | Resumen | Evidencia |
|-------|--------|---------|-------------------|---------|-----------|
| | | | | | |

**Severidad:**  
- **S1** Bloquea operación / pérdida datos / ERP incorrecto  
- **S2** Flujo principal roto con workaround  
- **S3** Secundario / UI / copy  
- **S4** Cosmético

---

## Cierre

| Campo | Valor |
|-------|-------|
| Ejecutado por | Auto (smoke API+SPA) |
| Ambiente | local · API `:5082` · admin `:4201` · tenant `casa-imperial` |
| Build / commit | admin rebuild development (fix dashboard) |
| Pass rate smoke | **67 / 68** (1 skip personalizacion items) |
| Bugs S1–S2 abiertos | 0 en smoke; unidades-medida count=0 (dato, no falla) |
| Go / No-go admin (smoke read-only) | **Go** ☑ · No-go ☐ |
| Firma / fecha | 2026-08-11 |

---

## Siguiente pasada

Cuando este plan de admin esté cerrado → **QA Storefront** (home, carruseles Ver todo, checkout, cupones, chat, devoluciones, factura).
