# QA E2E Checkout - Resultados

**Ejecutado:** 2026-08-11 12:25:06
**API:** http://localhost:5082 Â· Tenant: casa-imperial
**User:** qa.e2e.20260811122455@example.com
**Score:** PASS **19** Â· FAIL **0** Â· SKIP **0** Â· 10s

| Id | Caso | Status | Detalle |
|----|------|--------|---------|
| E0 | GET tipos-documento | **PASS** | id=168 |
| E1 | Register | **PASS** | qa.e2e.20260811122455@example.com |
| E2 | Parse activation | **PASS** | ok |
| E3 | Activate | **PASS** | user=40 |
| E4 | Login | **PASS** | qa.e2e.20260811122455@example.com |
| E5 | GET account/me | **PASS** | HTTP 200 |
| E6 | Pick product | **PASS** | id=477136 |
| E7 | Embalaje | **PASS** | emb=73189 |
| E8 | Add to cart | **PASS** | total=490000,00 |
| E9 | Set pickup agencia 79 | **PASS** | CASA MATRIZ |
| E9b | Blocked by horario | **PASS** | widening via admin |
| E9c | Widen horario | **PASS** | id=2 |
| E9d | Restore horario | **PASS** | id=2 |
| E10 | Checkout pickup + COD | **PASS** | nroPedido=67 idPedidoMovil=5 total=490000,00 |
| E11 | GET account/orders | **PASS** | count=1 |
| E12 | GET checkout order | **PASS** | HTTP 200 |
| E13 | Reject bad cupon | **PASS** | HTTP 404 |
| E14 | GET devoluciones | **PASS** | count=0 |
| E15 | GET chat | **PASS** | HTTP 200 |

## Fallos
_Ninguno._
