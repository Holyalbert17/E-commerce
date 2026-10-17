# Cumplimiento DEFI / MVP — plan realista

**Fecha:** 5 ago 2026 (actualizado 29 jul 2026 sprint)  
**Fuentes:** `docs/_mvp-req/` (DEFI 0.3, BACKOFFICE_FUNCIONAL, UX/WIREFRAME home)  
**Producto:** Datamex E-Commerce (código actual)

---

## 1. Qué significa “100%” aquí

El DEFI de junio 2026 describe un e-commerce **genérico white-label single-tenant**.  
El producto construido es **ERP-ready Datamex** (multi-tenant por `IDEmpresa`, embalajes, permisos `Site_App_Ecommerce_*`).

Por eso **no se puede cumplir literalmente el 100% del DEFI sin romper la paridad ERP**.  
La meta operativa es:

| Capa | Meta |
|---|---|
| Funcional MVP (DEFI §16 + backoffice mínimo + home MVP) | **→ 100% alcanzable** |
| Arquitectura DEFI literal (single-tenant, UUID, ProductVariant, .NET 10) | **Excepciones aceptadas** (abajo) |

---

## 2. Excepciones aceptadas (no se “arreglan”)

| DEFI | Realidad Datamex | Motivo |
|---|---|---|
| Single-tenant por Docker/DB | Multi-tenant compartido `IDEmpresa` + slug | Misma lógica que ERP Site |
| Stock/variantes genéricas | `Articulo` + `ArticuloEmbalaje` | Paridad ERP |
| .NET 10 | .NET 8 LTS | Estabilidad / tooling actual |
| PK UUID en CMS | `long` ID como resto del dominio | Consistencia EF/ERP |
| Sin multi-tenant compartido | Tenancy por header | Producto multi-cliente en un API |

Estas excepciones se consideran **cumplimiento por equivalencia funcional**, no defecto.

---

## 3. Checklist de cierre funcional (implementación)

### Hecho antes de esta ola
- [x] Catálogo, carrito, checkout, Bancard, pedidos, tracking
- [x] Branding / personalización JSON
- [x] Admin amplio + sync ERP + factura Pentaho
- [x] Autónomo / Integrated (`ModoIntegracion`)

### Esta ola (DEFI compliance sprint)
- [x] CMS `ContenidoBloque` + ítems (topbar, hero, promos, newsletter, beneficios)
- [x] Menú navegación configurable (header/topbar/footer)
- [x] Admin: Contenido del sitio
- [x] Storefront renderiza bloques publicados
- [x] `AdminAuditoriaLog` + registro en acciones CMS (+ listado en admin Contenido)
- [x] Reenviar activación + branding mails con `NombreTiendaDisplay`
- [x] `SlugSeo` en artículo + ruta `/producto/:id|slug` + API `products/slug/{slug}`
- [x] Guard Angular ruta→permiso (`adminPermissionGuard` en rutas admin)
- [x] `InventarioMovimiento` al aplicar/restaurar stock + ajustes manuales + UI `/catalogo/:id/movimientos`
- [x] Badge ownership ERP/Local Integrated en catálogo admin

### Fuera de esta ola / fase 2 DEFI
- [ ] Refresh token JWT completo
- [ ] Árbol `Category` CMS independiente del ERP (grupos siguen siendo puente)
- [ ] API M2M versionada genérica (`/v1/integrations/...`)
- [ ] Auditoría exhaustiva de todos los endpoints
- [ ] Roles DEFI con nombres exactos (Operador Contenido, etc.) — se mapean a `Site_App_*`

---

## 4. Criterio de “listo”

El producto se declara **MVP DEFI-compliant (con excepciones §2)** cuando:

1. Los 10 criterios DEFI §16 están en Cumple o Parcial documentado con path de cierre.
2. Home tenga bloques CMS MVP del wireframe (topbar, hero, promos, newsletter, feature_list, footer).
3. Backoffice tenga módulo Contenido + Auditoría básica + guards por permiso.
4. Auth tenga reenvío de activación y mails con nombre de tienda.
5. Stock Standalone deje rastro en `InventarioMovimiento`.

**Estado:** criterios 2–5 cumplidos en código; criterio 1 = cumplido por equivalencia (§2).  
Aplicar migración `DefiComplianceCms` en cada entorno (`dotnet ef database update`).
