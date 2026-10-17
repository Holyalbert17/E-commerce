# QA Continue-2 - Admin order + cupon + devolucion

**Ejecutado:** 2026-08-11 14:18:10
**API:** http://localhost:5082 Â· Tenant: casa-imperial
**Score:** PASS **20** Â· FAIL **0** Â· SKIP **1** Â· 9,9s

| Id | Caso | Status | Detalle |
|----|------|--------|---------|
| C0 | Admin login | **PASS** | ok |
| C1 | Admin orders list | **PASS** | count=5 total=5 |
| C2 | Pick order | **PASS** | idPedidoMovil=5 nro=67 migrado=True |
| C3 | Admin order detail | **PASS** | estado=1 items=1 |
| C4 | Add tracking hito | **PASS** | preparando |
| C5 | List tracking | **PASS** | events=3 |
| C6 | Migracion logs | **PASS** | rows=67 |
| C7 | Marcar migrado local | **SKIP** | ya migrado |
| C8 | Crear cupon QA2141800 | **PASS** | id=4 |
| C9 | Register customer | **PASS** | qa2.20260811141800@example.com |
| C10 | Activate customer | **PASS** | ok |
| C11 | Cart with stock item | **PASS** | total=490000,00 |
| C12 | Aplicar cupon en carrito | **PASS** | descuento=49000 msg=Cupón QA2141800 aplicado. |
| C13 | Checkout for devolucion | **PASS** | nro=68 idPm=6 |
| C14 | Crear devolucion | **PASS** | id=1 estado=solicitada |
| C15 | Admin ve devolucion | **PASS** | id=1 |
| C16 | Admin rechaza devolucion | **PASS** | rechazada |
| C17 | Desactivar cupon QA | **PASS** | id=4 |
| C18 | Home mas-vendidos | **PASS** | n=3 |
| C19 | Home recomendados | **PASS** | n=8 |
| C20 | Catalog marcas (fix) | **PASS** | n=148 |

## Fallos
_Ninguno._
