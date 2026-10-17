# QA Admin Smoke - Resultados

**Ejecutado:** 2026-08-18 14:04:56
**API:** http://localhost:5082 Â· **Admin:** http://localhost:4201 Â· **Tenant:** casa-imperial
**Score:** PASS **67** Â· FAIL **0** Â· SKIP **1** Â· 6,3s

| Id | Bloque | Caso | Status | Detalle |
|----|--------|------|--------|---------|
| 0.1 | prep | API health | **PASS** | HTTP 200 |
| 0.2 | prep | Admin SPA /login | **PASS** | HTTP 200 len=1521 |
| A-01 | auth | Login OK | **PASS** | idEmpresa=41 perms=1 |
| A-02 | auth | Login bad password | **PASS** | rejected as expected |
| A-03 | auth | Login bad tenant | **PASS** | rejected as expected |
| N-04 | neg | Dashboard sin auth | **PASS** | HTTP 401 |
| D-01 | dashboard | GET dashboard | **PASS** | HTTP 200 |
| S-01 | sync | GET sync/status | **PASS** | HTTP 200 |
| C-01 | catalog | GET products | **PASS** | HTTP 200 items=10 total=15564 |
| C-28 | catalog | GET parametros-producto | **PASS** | HTTP 200 items=193 total=System.Object[] |
| C-25 | catalog | GET unidades-medida | **PASS** | HTTP 200 count=0 |
| P-01 | orders | GET orders | **PASS** | HTTP 200 items=10 total=17 |
| P-08 | orders | GET migracion-logs | **PASS** | HTTP 200 items=79 total=System.Object[] |
| K-01 | carts | GET carritos | **PASS** | HTTP 200 items=10 total=36 |
| L-01 | clients | GET clientes | **PASS** | HTTP 200 items=10 total=21936 |
| L-02 | clients | GET solicitudes-mayorista | **PASS** | HTTP 200 count=0 |
| H-01 | drivers | GET choferes | **PASS** | HTTP 200 items=3 total=System.Object[] |
| LP-01 | prices | GET listas-precios | **PASS** | HTTP 200 items=9 total=System.Object[] |
| T-01 | shipping | GET tarifas | **PASS** | HTTP 200 items=126 total=System.Object[] |
| M-01 | marketing | GET promotions | **PASS** | HTTP 200 items=5 total=System.Object[] |
| M-07 | marketing | GET resenas | **PASS** | HTTP 200 count=0 |
| M-08 | marketing | GET cupones | **PASS** | HTTP 200 items=4 total=System.Object[] |
| M-10 | marketing | GET devoluciones | **PASS** | HTTP 200 items=10 total=System.Object[] |
| M-13 | cms | GET content/blocks | **PASS** | HTTP 200 items=7 total=System.Object[] |
| M-15 | cms | GET content/menus | **PASS** | HTTP 200 items=3 total= |
| M-16 | cms | GET personalizacion/schemas | **PASS** | HTTP 200 |
| M-21 | branding | GET branding | **PASS** | HTTP 200 |
| CFG-01 | config | GET empresa/config | **PASS** | HTTP 200 modo=Integrated slug= |
| CFG-06 | config | GET agencias | **PASS** | HTTP 200 items=5 total=System.Object[] |
| CFG-06b | config | GET depositos | **PASS** | HTTP 200 items=5 total=System.Object[] |
| CFG-06c | config | GET horarios | **PASS** | HTTP 200 items=18 total=System.Object[] |
| CFG-05 | config | GET erp-health | **PASS** | HTTP 200 |
| R-01 | crm | GET chat conversaciones | **PASS** | HTTP 200 items=2 total=System.Object[] |
| R-08 | contacto | GET contacto | **PASS** | HTTP 200 count=0 |
| REP-01 | reports | GET reportes ventas | **PASS** | HTTP 200 |
| SEC-01 | security | GET manager roles | **PASS** | HTTP 200 items=2 total=System.Object[] |
| SEC-02 | security | GET manager usuarios | **PASS** | HTTP 200 items=2 total=System.Object[] |
| SEC-04 | security | GET functionalidades | **PASS** | HTTP 200 items=48 total=System.Object[] |
| SEC-04b | security | GET operaciones-tree | **PASS** | HTTP 200 items=6 total=System.Object[] |
| TEN-01 | tenants | GET tenants | **PASS** | HTTP 200 count=1 |
| C-11 | catalog | GET product detail #477013 | **PASS** | HTTP 200 |
| C-20 | catalog | GET product images #477013 | **PASS** | HTTP 200 |
| C-26 | catalog | GET embalajes #477013 | **PASS** | HTTP 200 |
| C-30 | catalog | GET movimientos #477013 | **PASS** | HTTP 200 |
| P-09 | orders | GET order detail #17 | **PASS** | HTTP 200 |
| K-05 | carts | GET carrito detail #36 | **PASS** | HTTP 200 |
| SPA-/ | spa | Admin route / | **PASS** | HTTP 200 |
| SPA-/catalogo | spa | Admin route /catalogo | **PASS** | HTTP 200 |
| SPA-/pedidos | spa | Admin route /pedidos | **PASS** | HTTP 200 |
| SPA-/carritos | spa | Admin route /carritos | **PASS** | HTTP 200 |
| SPA-/clientes | spa | Admin route /clientes | **PASS** | HTTP 200 |
| SPA-/choferes | spa | Admin route /choferes | **PASS** | HTTP 200 |
| SPA-/listas-precios | spa | Admin route /listas-precios | **PASS** | HTTP 200 |
| SPA-/tarifas | spa | Admin route /tarifas | **PASS** | HTTP 200 |
| SPA-/promociones | spa | Admin route /promociones | **PASS** | HTTP 200 |
| SPA-/marketplace | spa | Admin route /marketplace | **PASS** | HTTP 200 |
| SPA-/crm | spa | Admin route /crm | **PASS** | HTTP 200 |
| SPA-/contacto | spa | Admin route /contacto | **PASS** | HTTP 200 |
| SPA-/contenido | spa | Admin route /contenido | **PASS** | HTTP 200 |
| SPA-/personalizacion | spa | Admin route /personalizacion | **PASS** | HTTP 200 |
| SPA-/branding | spa | Admin route /branding | **PASS** | HTTP 200 |
| SPA-/configuracion | spa | Admin route /configuracion | **PASS** | HTTP 200 |
| SPA-/sync | spa | Admin route /sync | **PASS** | HTTP 200 |
| SPA-/reportes | spa | Admin route /reportes | **PASS** | HTTP 200 |
| SPA-/manager | spa | Admin route /manager | **PASS** | HTTP 200 |
| SPA-/empresas | spa | Admin route /empresas | **PASS** | HTTP 200 |
| SPA-/catalogo/activar | spa | Admin route /catalogo/activar | **PASS** | HTTP 200 |
| M-17 | cms | GET personalizacion | **SKIP** | schemas already covered |

## Fallos
_Ninguno._

## Nota
Smoke **read-only** via API + carga SPA. No cubre mutaciones UI (bulk ERP, publicar vitrina, chat live, uploads). Esos quedan manuales en `QA-ADMIN-PANEL.md`.
