# QA Continue-4 - Bancard Stub + Favoritos + Chat + Feeds

**Ejecutado:** 2026-08-11 16:30:52  
**API:** http://localhost:5082 · Tenant: casa-imperial · publish `_run-api-v31` · **Bancard Mode=Stub**  
**Score:** PASS **16** · FAIL **0** · SKIP **0** · 14s

| Id | Caso | Status | Detalle |
|----|------|--------|---------|
| F1 | Feed mas-vendidos | **PASS** | n=4 |
| F2 | Feed ofertas | **PASS** | n=0 |
| F3 | Feed recomendaciones | **PASS** | n=8 |
| B0 | Register | **PASS** | qa4.*@example.com |
| B1 | Activate | **PASS** | ok |
| B2 | Admin login | **PASS** | ok |
| V1 | Favoritos crear seccion | **PASS** | id=2 |
| V2 | Favoritos agregar item | **PASS** | art=477136 |
| V3 | Favoritos listar | **PASS** | secciones=2 |
| C1 | Cliente envia chat | **PASS** | conv=2 |
| C2 | Admin lista chats | **PASS** | ok |
| C3 | Admin responde chat | **PASS** | conv=2 |
| B3 | Add to cart | **PASS** | total=490000 |
| B4 | Checkout online (Stub) | **PASS** | nro=77 requierePagoOnline=True mode=Stub |
| B5 | Bancard session | **PASS** | shopProcessId=159912 completed=False |
| B6 | Simulate bancard success | **PASS** | nro=77 pago confirmado |

## Fallos

_Ninguno._

## Notas

- Se reactivó el flujo online: checkout ya no fuerza `PagarEnLaEntrega`; `TipoPago` respeta el request; en **Stub** `IsBancardConfigured` = true.
- API quedó en `_run-api-v31` con `Bancard:Mode=Stub`. Contra entrega sigue con `pagarEnLaEntrega=true`.
- Ofertas feed vacío (n=0) en este tenant: no es fallo de API.
