# Checklist de avance — Datamex E-Commerce

**Fecha:** 1 de agosto de 2026 (tracking timeline + personalización marketing)  
**Método:** `Hecho` = 100% · `Parcial` = 50% · `Pendiente` = 0%.

---

## Resumen de avance

| Vista | Avance |
|---|---|
| **Global** (hacia Integrated completo) | **~83%** |
| Solo **Standalone** | **~96%** |
| Circuito **Integrated** | **~35%** |

```
Standalone ███████████████████░  96%
Global     ████████████████░░░░  83%
Integrated ███████░░░░░░░░░░░░░  35%
```

---

## Completado en esta pasada

| Ítem | Estado |
|---|---|
| `ConfiguracionPersonalizacion` + migración EF | Hecho |
| API admin/storefront personalización (layout/tema/textos) | Hecho |
| Admin `/personalizacion` | Hecho |
| Storefront aplica layout/tema/textos | Hecho |
| Timeline tracking Amazon-style + SignalR `trackingUpdated` | Hecho |
| Spec [`SPEC-TRACKING-PERSONALIZACION.md`](./SPEC-TRACKING-PERSONALIZACION.md) | Hecho |

---

## Bloques

| Bloque | % |
|---|---|
| Modelo / tablas ERP | 92% |
| Storefront | ~88% |
| Admin | ~93% |
| Backend API | ~94% |
| Integración ERP | ~35% (INSERT listo; sync inbound aún scaffold) |

---

## Sigue pendiente (P0 Integrated)

- [ ] Sync **inbound** real (catálogo/precios/stock desde ERP)
- [ ] Bancard Live UAT + webhook producción
- [ ] Stock continuo desde ERP

## Sigue pendiente (tracking Amazon / personalización avanzada)

- [ ] ETA dinámico + mapa live en reparto
- [ ] Link público compartible + preferencias de notificación
- [ ] Formularios dinámicos / preview borrador de personalización
