# Prompts y Plan de Funcionalidades — Datamex E-Commerce

**Objetivo:** cerrar el circuito Integrated y pulir Standalone para que la plataforma quede sumamente funcional.  
**Fecha:** 4 de agosto de 2026  
**Base:** [`INFORME-FUNCIONAL-COMPLETO.md`](./INFORME-FUNCIONAL-COMPLETO.md) + [`INFORME-ESTADO.md`](./INFORME-ESTADO.md)  
**Regla permanente:** **siempre pensar en el ERP Datamex** — mismos IDs, schema `dbo` lowercase, permisos `Site_App_Ecommerce_*`, no mutar ERP salvo `AllowWriteBack=true` explícito, sync = solo lectura.

---

## 0. Cómo usar este documento

Cada ítem tiene:
- **Estado real (ago 2026)** — qué ya hay en el repo (no asumir el informe viejo al pie de la letra)
- **Contexto** — qué falta y por qué
- **Criterios de aceptación**
- **Prompt listo** para pegar en el agente

Orden: **P0 → P1 → P2**. Antes de tocar código: leer `GAPS-ERP-PARITY.md`, `ERP-SCHEMA-ALIGNMENT.md`, `ERP-LIVE-PANORAMA.md` y el servicio real.

### Principios ERP (obligatorios en todo P0/P1)

1. **Espejo ID-preserving** — PK/FK del ERP se conservan en EcomDb.
2. **Hot-path sin ERP** — storefront/admin no hacen SQL al Postgres ERP en request.
3. **Pull ≠ push** — inbound = SELECT; write-back = INSERT solo con `Erp:AllowWriteBack=true`.
4. **Schema vivo** — `dbo.*`, columnas lowercase (`id`, `activoecommerce`, `idempresa`).
5. **Multi-tenant** — config (Bancard, SMTP, IntegrationMode, lista B2B) **por empresa**, no global.
6. **Tenant UAT de referencia** — empresa **41 · CASA IMPERIAL · slug `casa-imperial`**.

---

## Estado real vs plan original (corrección crítica)

| Ítem | Plan original decía | Realidad en código (ago 2026) |
|---|---|---|
| **P0.1 Sync inbound** | Endurecer métricas | **Hecho + híbrido (ago-4):** métricas UI; job periódico; **lazy-pull** PDP/`POST …/sync/ensure-articulo` si falta en local. |
| **P0.2 Write-back** | UAT + reintento | **Hecho circuito sin ETL (ago-4):** auto post-checkout/pago + retry job; idempotencia; `AllowWriteBack` gate INSERT real. UAT vivo pendiente del flag. |
| **P0.3 Bancard** | Stub default | Stub/Live por `Bancard:Mode`. **Hecho (ago-4):** confirm idempotente + log rejected/mismatch; rollback restaura stock Standalone solo si hubo pago; txn relacional; tests OK/fail/dup/rollback. |
| **P0.4 Stock mode** | No respeta Integrated | **Hecho (ago-4):** `ModoIntegracion` por empresa; checkout/pago/cancel usan `TenantIntegrationMode`; sync ERP marca Integrated; admin selector en `/configuracion`. |
| **P0.5 SMTP** | Logging | `Smtp:Enabled` + servicio; falta prod + reenviar + branding. |

**Implicación:** P0.1 no se reescribe desde cero — se **endurece** (métricas insert/update, watermark incremental real, tests, stock depósito, UI admin). P0.2 es el siguiente gran salto Integrated.

---

## P0 — Paridad Integrated

### P0.1 — Endurecer sync inbound (no reimplementar)

**Estado real:** pull lectura OK (~272 filas verificadas en Casa Imperial). Cursors por entidad. Skip sin CS.

**Qué falta**
- Desglose **leídas / insertadas / actualizadas / errores / duración** por tabla (hoy: `RowsProcessed` agregado).
- Watermark incremental fiable por `ultmodificacion` (evitar full pull innecesario).
- Stock depósito e-com con filas reales (hoy a menudo 0 en depósito 1).
- Tests de idempotencia.
- UI `/sync` más rica.

**Criterios de aceptación**
- Con `ConnectionStrings:Erp`, upsert ID-preserving sin duplicar en 2 corridas.
- Resultado de `/sync` con métricas por entidad.
- Cero INSERT/UPDATE/DELETE al ERP.
- Sin CS → skip como hoy.
- Logging estructurado por corrida.
- `GAPS-ERP-PARITY.md` refleja “implementado + métricas”.

**Prompt:**
```
Estoy en Datamex.ECommerce.Infrastructure. ErpSyncService YA hace pull real (no stub).
NO reescribas el sync desde cero.

1. Lee ERP-SCHEMA-ALIGNMENT.md, ERP-LIVE-PANORAMA.md, ErpSyncService, SyncCursor DTOs
   y sync-ops-page del admin.
2. Extiende el resultado de RunDeltaAsync para reportar por entidad: leídas, insertadas,
   actualizadas, errores, duraciónMs (además de RowsProcessed).
3. Mejora el watermark/cursor incremental por UltModificacion (o equivalente ERP) donde
   hoy se haga full-scan innecesario — sin romper idempotencia ni IDs.
4. Revisa SyncStock: si el depósito e-com (IDDeposito de empresaconfiguracionecommerce)
   no trae filas, documentá/diagnóstica y preferí el depósito configurado del tenant ERP.
5. Admin /sync: mostrar el desglose nuevo (no solo “N filas”).
6. Tests de integración o unitarios con mock Npgsql/fixture: 2 corridas no duplican.
7. Cero escritura al ERP. Actualiza GAPS-ERP-PARITY.md.
Regla: schema dbo lowercase, empresa 41 como referencia UAT.
```

---

### P0.2 — UAT write-back `PedidoMovil` (ERP vivo)

**Estado real:** INSERT implementado; default `AllowWriteBack=false` (simula).

**Criterios de aceptación**
- Con `AllowWriteBack=true` + ERP UAT: checkout → fila real en `dbo.pedidomovil` + detalle, FKs correctas.
- Fallo → pedido local “pendiente migración” + error en `MigracionPedidoLog`; no se pierde ni duplica.
- Reintento desde `/pedidos/:id` idempotente (por nro pedido / id e-com).
- Log de cada intento.

**Prompt:**
```
PedidoErpMigrationService ya tiene INSERT real gateado por Erp:AllowWriteBack.
Necesito UAT confiable contra Postgres ERP (nunca tocar prod sin flag).

1. Revisa PedidoErpMigrationService, MigracionPedidoLog, ERP-SCHEMA-ALIGNMENT.md
   (dbo.pedidomovil lowercase) y la UI /pedidos/:id.
2. Verifica/corrige el INSERT (cabecera+detalle) con FKs: idempresa, cuenta, idchofer,
   montos, articulos/embalajes que existan en ERP.
3. Idempotencia: si ya migró, no re-INSERT; linkea ID ERP.
4. Fallo → log + estado pendiente; endpoint/acción admin “Reintentar migración”.
5. Tests: éxito, FK fail, reintento, doble intento.
6. GAPS-ERP-PARITY.md → write-back “validado UAT” solo si pasa.
Nunca pongas AllowWriteBack=true por defecto en Development.
```

---

### P0.3 — Bancard Live

**Estado real (ago-4):** Stub/Live por `Bancard:Mode` + HttpBancardClient. Confirm webhook **idempotente** (estado Cerrado / log success); rechazo y mismatch logueados sin mutar stock; **rollback** anula y restaura stock Standalone solo si el pago había cerrado el carrito; tests InMemory verdes. Pendiente: UAT con claves Live reales por tenant.

**Criterios:** Live con claves por tenant; confirm/rollback con `RestorePedidoStockAsync`; idempotencia webhook; Stub intacto.

**Prompt:**
```
Implementá Bancard Live sin romper Stub (Bancard:Mode).

1. Lee PaymentService /api/payments/bancard y config por empresa.
2. Live: URL real + confirm single/multi-buy; claves por tenant.
3. Éxito: marca pagado sin re-reservar stock.
4. Fallo/cancel: pendiente de pago + RestorePedidoStockAsync.
5. Confirm idempotente (txn/pedido).
6. Tests: OK, fail+restore, webhook duplicado.
```

---

### P0.4 — Stock según IntegrationMode (por tenant)

**Estado real:** **Hecho (ago-4)** — `ModoIntegracion` por empresa; checkout/pago/cancel usan `TenantIntegrationMode`.

**Criterios:** Standalone muta; Integrated solo refleja sync; gate stock igual; modo **por empresa**.

**Prompt:**
```
Hacé que ApplyPedidoStockAsync / RestorePedidoStockAsync respeten IntegrationMode POR TENANT.

1. Lee IntegrationMode, CheckoutService, PaymentService, InventoryService.
2. Standalone + ControlaStockEnCarrito: comportamiento actual.
3. Integrated: confirmar/cancelar NO muta stock local (solo sync P0.1).
4. Gate “stock insuficiente” lee siempre el valor local vigente.
5. Config por empresa (no solo ECommerceOptions global).
6. Tests Standalone muta / Integrated no muta.
Alineado a Pedido ERP: en Integrated el stock lo mueve el ERP al facturar.
```

---

### P0.5 — SMTP producción

**Prompt:**
```
Activá SMTP real (Smtp:Enabled=true) por tenant: activación, recuperar password,
confirmación pedido. Fallo de mail no rompe el flujo; opción reenviar.
Modo logging intacto si Enabled=false.
```

---

## P1 — Producto / operaciones

### P1.1 — SignalR primario + typing (CRM)
```
SignalR fuente primaria (mensajes, leídos, typing). Polling = fallback con backoff.
Sin cambiar modelo de datos. Sin duplicar al reconectar.
```

### P1.2 — Route guards por permiso
```
Mapa ruta→Site_App_Ecommerce_* en un solo config. Guard Angular + pantalla sin acceso.
manager:all bypass. Paridad con [RequirePermission] del API.
```

### P1.3 — Reportes ricos
```
Además de ventas/día: por producto/categoría, canal B2C/B2B, top clientes,
embudo carrito→pago, por zona delivery. Filtro IDEmpresa. CSV. No romper reporte actual.
```

### P1.4 — Fulfillment / bulk pedidos
```
Estados internos (recibido→preparando→listo→en ruta→entregado/cancelado) distintos
del timeline público. Transiciones validadas + bulk. Timeline refleja, no duplica.
Chofer compatible con ERP.
```

### P1.5 — Lista precio por cuenta B2B

**Estado real (ago-5):** espejo `listapreciocuenta` + sync en `customers` + admin historial en `/clientes` + checkout escribe FK real (no `listaprecio.id`). Pricing usa historial activo si `UsarListaPrecioCuentaErp`.

**Prompt (cerrado):**
```
Login B2B → lista de la cuenta si existe (espejo listapreciocuenta ERP vía sync);
si no, lista B2B general. Carrito/checkout recalculan.
```

### P1.6 — Alertas seguidos (depende P0.5)
```
Sin-stock→con-stock → email con link. Anti-spam 24h. Marca notificado hasta nuevo ciclo.
```

---

## P2 — Polish

| ID | Prompt corto |
|---|---|
| **P2.1** | Dashboard atajos ops + consistencia dx-* en catálogo/pedidos/CRM; marketplace bulk/filtros |
| **P2.2** | Web Notifications CRM desde SignalR; click → conversación |
| **P2.3** | PWA (manifest + SW assets); meta SEO por tenant; no cachear precios |
| **P2.4** | Mi cuenta en tabs lazy: Perfil, Direcciones, Pedidos, Mayorista, Seguridad |

---

## Orden de ejecución sugerido

1. **P0.1 endurecer** (métricas, stock sync, tests) — base de datos ERP fresca  
2. **P0.2 write-back UAT** — cierra Integrated de pedidos  
3. **P0.4 IntegrationMode por tenant** — coherencia stock  
4. **P0.3 Bancard Live** (paralelo, no bloquea ERP)  
5. **P0.5 SMTP** → desbloquea P1.6  
6. **P1:** 1.2 → 1.1 → 1.4 → 1.3 → 1.5 → 1.6  
7. **P2** al final  

## Credenciales / puertos de trabajo

| Pieza | Valor |
|---|---|
| API / Admin / Store | `:5082` / `:4201` / `:4200` |
| Demo | slug `demo` · admin `admin`/`Admin12!` · comprador `demo`/`demo123` |
| ERP UAT | slug `casa-imperial` · empresa 41 · admin igual |
| Write-back | **OFF** salvo UAT explícito |

## Docs relacionados

- [`INFORME-FUNCIONAL-COMPLETO.md`](./INFORME-FUNCIONAL-COMPLETO.md)  
- [`INFORME-ESTADO.md`](./INFORME-ESTADO.md)  
- [`GAPS-ERP-PARITY.md`](./GAPS-ERP-PARITY.md)  
- [`ERP-SCHEMA-ALIGNMENT.md`](./ERP-SCHEMA-ALIGNMENT.md)  
- [`ERP-LIVE-PANORAMA.md`](./ERP-LIVE-PANORAMA.md)  
- [`ROADMAP-FUNCIONALIDADES.md`](./ROADMAP-FUNCIONALIDADES.md)  
