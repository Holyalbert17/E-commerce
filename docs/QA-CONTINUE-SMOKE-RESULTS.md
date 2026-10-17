# QA Continue - Admin mutations + Storefront

**Ejecutado:** 2026-08-11 12:09:37
**API:** http://localhost:5082 Â· Admin http://localhost:4201 Â· Store http://localhost:4200 Â· Tenant casa-imperial
**Score:** PASS **47** Â· FAIL **0** Â· SKIP **3** Â· 10,8s

| Id | Bloque | Caso | Status | Detalle |
|----|--------|------|--------|---------|
| AM-00 | admin-auth | Admin login | **PASS** | idEmpresa=41 |
| AM-01 | marketplace | Crear cupon QA20260811120926 | **PASS** | id=3 |
| AM-02 | marketplace | Desactivar cupon | **PASS** | id=3 |
| AM-03 | choferes | Crear chofer 9120926 | **PASS** | id=3 |
| AM-04 | choferes | Desactivar chofer | **PASS** | id=3 |
| AM-05 | promos | GET promotions post-mutation | **PASS** | HTTP 200 |
| SF-01 | storefront | branding | **PASS** | HTTP 200 |
| SF-02 | storefront | home content | **PASS** | HTTP 200 |
| SF-03 | storefront | personalizacion | **PASS** | HTTP 200 |
| SF-04 | storefront | collections | **PASS** | HTTP 200 |
| SF-05 | storefront | marketing slots | **PASS** | HTTP 200 |
| SF-06 | storefront | products B2C | **PASS** | HTTP 200 items=48 total=12545 pickStock=477136 |
| SF-07 | storefront | mas-vendidos | **PASS** | HTTP 200 count=3 |
| SF-08 | storefront | ofertas | **PASS** | HTTP 200 |
| SF-09 | storefront | recomendaciones | **PASS** | HTTP 200 |
| SF-10 | storefront | marcas | **PASS** | HTTP 200 |
| SF-11 | storefront | lineas | **PASS** | HTTP 200 |
| SF-12 | storefront | tipos | **PASS** | HTTP 200 |
| SF-13 | storefront | search suggest | **PASS** | HTTP 200 |
| SF-14 | storefront | ciudades delivery | **PASS** | HTTP 200 |
| SF-15 | storefront | agencias | **PASS** | HTTP 200 |
| SF-16 | storefront | map defaults | **PASS** | HTTP 200 |
| SF-17 | storefront | cart guest | **PASS** | HTTP 200 |
| SF-18 | storefront | browse feed recomendados | **PASS** | HTTP 200 |
| SF-19 | storefront | product detail #477136 | **PASS** | HTTP 200 |
| SF-20 | cart | Add item to cart | **PASS** | items=1 |
| SF-21 | home | Ver todo mas-vendidos (48) | **PASS** | count=3 |
| SF-22 | home | Ver todo ofertas (48) | **PASS** | count=0 |
| SF-23 | auth | Register storefront user | **SKIP** | HTTP 400 The remote server returned an error: (400) Bad Request. |
| SF-24 | auth | Login storefront | **SKIP** | HTTP 401 - may need activation |
| SF-25 | account | account endpoints | **SKIP** | sin sesion cliente |
| SPA-/catalogo | spa-sf | Store route /catalogo | **PASS** | HTTP 200 |
| SPA-/catalogo?browse=1 | spa-sf | Store route /catalogo?browse=1 | **PASS** | HTTP 200 |
| SPA-/catalogo?browse=1&feed=recomendados | spa-sf | Store route /catalogo?browse=1&feed=recomendados | **PASS** | HTTP 200 |
| SPA-/catalogo?browse=1&feed=masVendidos | spa-sf | Store route /catalogo?browse=1&feed=masVendidos | **PASS** | HTTP 200 |
| SPA-/catalogo?browse=1&feed=ofertas | spa-sf | Store route /catalogo?browse=1&feed=ofertas | **PASS** | HTTP 200 |
| SPA-/carrito | spa-sf | Store route /carrito | **PASS** | HTTP 200 |
| SPA-/checkout | spa-sf | Store route /checkout | **PASS** | HTTP 200 |
| SPA-/login | spa-sf | Store route /login | **PASS** | HTTP 200 |
| SPA-/registro | spa-sf | Store route /registro | **PASS** | HTTP 200 |
| SPA-/contacto | spa-sf | Store route /contacto | **PASS** | HTTP 200 |
| SPA-/faq | spa-sf | Store route /faq | **PASS** | HTTP 200 |
| SPA-/mis-pedidos | spa-sf | Store route /mis-pedidos | **PASS** | HTTP 200 |
| SPA-/devoluciones | spa-sf | Store route /devoluciones | **PASS** | HTTP 200 |
| SPA-/favoritos | spa-sf | Store route /favoritos | **PASS** | HTTP 200 |
| SPA-/mi-cuenta | spa-sf | Store route /mi-cuenta | **PASS** | HTTP 200 |
| SPA-/sucursales | spa-sf | Store route /sucursales | **PASS** | HTTP 200 |
| SPA-/como-comprar | spa-sf | Store route /como-comprar | **PASS** | HTTP 200 |
| SPA-/terminos | spa-sf | Store route /terminos | **PASS** | HTTP 200 |
| SPA-/producto/477136 | spa-sf | Store route /producto/477136 | **PASS** | HTTP 200 |

## Fallos
_Ninguno._
