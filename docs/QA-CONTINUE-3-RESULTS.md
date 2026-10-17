# QA Continue-3 - Delivery + Pago + Devolucion aprobar

**Ejecutado:** 2026-08-11 15:47:30  
**API:** http://localhost:5082 · Tenant: casa-imperial · publish `_run-api-v30`  
**Score:** PASS **16** · FAIL **0** · SKIP **1** · 18,8s

| Id | Caso | Status | Detalle |
|----|------|--------|---------|
| D0 | Admin login | **PASS** | ok |
| D1 | List tarifas | **PASS** | n=126 |
| D1b | Tarifas por estado | **PASS** | 1:30, 0:93, 2:3 |
| D2 | Vigencia tarifas pendientes | **PASS** | fixed=8/8 |
| D3 | Autorizar tarifas sample | **PASS** | updated=8 |
| D4 | Storefront ciudades-delivery | **PASS** | n=1 |
| D5 | Storefront barrios-delivery | **PASS** | ciudad=25 barrio=465 importe=15000 |
| D6 | Register | **PASS** | qa3.*@example.com |
| D7 | Activate | **PASS** | ok |
| D8 | Add to cart | **PASS** | total=490000 |
| D9 | Apply shipping barrio | **PASS** | ok |
| D10 | Checkout online | **PASS** | nro=76 requierePagoOnline=False mode=Off |
| D11 | Bancard session | **PASS** | shopProcessId=0 mode=Off completed=True |
| D12 | Simulate bancard success | **SKIP** | Bancard Mode=Off (pedido ya cerrado sin VPOS) |
| D13 | Crear devolucion | **PASS** | id=9 |
| D14 | Admin aprueba devolucion | **PASS** | id=9 (ERP NC → completada) |
| D15 | Admin completa devolucion | **PASS** | estado=completada |

## Fallos

_Ninguno._

## Notas

- **Delivery:** tarifas autorizadas tenían vigencia vencida (2024). El smoke actualiza 8 pendientes (estado=0) con vigencia UTC y las autoriza → ciudades/barrios storefront OK → checkout delivery OK.
- **Bancard:** `Bancard:Mode=Off` en appsettings → no hay `shopProcessId` ni simulate; pago se considera completado. Para cubrir D12: `Mode=Stub` + restart.
- **Bugs corregidos en esta pasada:**
  1. `UpdateTarifaAsync`: DateTime Unspecified → UTC (`timestamp with time zone`).
  2. `ErpPedidoLifecycleWriter` devolución ERP: `detallefactura.idarticulo` → join `articuloembalaje`; `empresatransaccion` (singular); INSERT `detallenotacredito` alineado a EntidadDetalleVenta.
  3. Script QA: no usar `.items` sobre arrays JSON (PS enumera nulos); `ConvertTo-Json -InputObject`.
