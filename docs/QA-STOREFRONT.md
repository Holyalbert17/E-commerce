# QA — Storefront E-commerce (smoke ejecutado)

**Tenant:** `casa-imperial` · **API:** `:5082` (`_run-api-v26`) · **Store:** `:4200`  
**Script:** `tools/qa-continue-smoke.ps1`  
**Resultado:** [`QA-CONTINUE-SMOKE-RESULTS.md`](./QA-CONTINUE-SMOKE-RESULTS.md)

## Score (2026-08-11)

| | |
|--|--|
| PASS | **47** |
| FAIL | **0** |
| SKIP | **3** (registro/login cliente — validación 400 / sin sesión) |
| Tiempo | ~11s |

## Cubierto

- Home APIs: branding, CMS home, personalización, collections, marketing, recomendados, más vendidos, ofertas
- Catálogo: products, detail, marcas, líneas, tipos, search
- Geography: ciudades, agencias, map defaults
- Cart guest + add item (con `X-Cart-Session` + producto con stock)
- Feeds “Ver todo”: mas-vendidos / ofertas
- SPA 19 rutas incl. `?feed=recomendados|masVendidos|ofertas`

## Admin mutations (mismo run)

- Crear/desactivar cupón QA*
- Crear/desactivar chofer QA*
- Promotions GET post-mutation OK

## Bugs encontrados y corregidos

| Bug | Fix |
|-----|-----|
| `GET /api/catalog/marcas` → 500 (EF GroupBy + DTO) | `CatalogService.GetMarcasAsync` → Distinct + materialize; mismo patrón en fallbacks líneas/tipos |
| Admin SPA dist vacío / dashboard `@else if (; as)` | Rebuild admin + fix template |

## Pendiente manual / auth

- Registro completo con tipos documento válidos del tenant
- Activación email + login + pedidos/devoluciones/chat autenticados
- Checkout end-to-end + cupón en checkout
- UI visual: carruseles flechas, Ver todo en browser

## Go / No-go smoke storefront

**Go** para APIs públicas + SPA shell.  

### E2E autenticado (2026-08-11)

Script: `tools/qa-e2e-checkout.ps1` → [`QA-E2E-CHECKOUT-RESULTS.md`](./QA-E2E-CHECKOUT-RESULTS.md)  
**PASS 19 / FAIL 0** — registro → activar → login → carrito → pickup → checkout COD → pedidos/devoluciones/chat.  
Nota: en horario de almuerzo el script amplía temporalmente el horario de CASA MATRIZ vía admin y lo restaura.

### Continue-2 (2026-08-11)

Script: `tools/qa-continue-2.ps1` → [`QA-CONTINUE-2-RESULTS.md`](./QA-CONTINUE-2-RESULTS.md)  
**PASS 20 / FAIL 0 / SKIP 1** — admin pedido+tracking · cupón 10% en carrito · checkout · devolución solicitada/rechazada · marcas OK.

### Continue-3 / Continue-4 (2026-08-11)

- [`QA-CONTINUE-3-RESULTS.md`](./QA-CONTINUE-3-RESULTS.md) — delivery + devolución ERP NC · **PASS 16 / SKIP 1** (Bancard Off).
- [`QA-CONTINUE-4-RESULTS.md`](./QA-CONTINUE-4-RESULTS.md) — **Bancard Stub simulate** + favoritos + chat CRM + feeds · **PASS 16 / FAIL 0**. API `_run-api-v31` Mode=Stub.
