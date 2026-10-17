# Roadmap de funcionalidades e-commerce

Fecha: 2026-08-03  
Criterio: impacto en comprador / operador, alineado a ERP, esfuerzo relativo.

---

## Ya útil hoy (verificar en local)

| Flujo | Cómo probar |
|---|---|
| Admin ops | `:4201` login `casa-imperial` / admin · Dashboard · Sync · Activar · Abrir tienda |
| Vitrina | `:4200/?tenant=casa-imperial` (chip de tenant en header) |
| Demo intacto | tenant `demo` sigue en seed |

**Verificado 2026-08-03:** sync lectura ERP → `datamex_ecom.migrate.db` = **272 filas** (empresa 41 · slug `casa-imperial` · 20 arts · 5 ya ON vitrina). Admin seed: `tools/SeedAdminOnce` → `admin` / `Admin12!`. Builds storefront + admin OK. `AllowWriteBack=false`.

**Nota:** si falta admin tras sync, correr `tools/SeedAdminOnce`. Si `geo-reg.db` quedó en 0 bytes, usar `migrate.db` (Development ya apunta ahí).

---

## Prioridad A — alto impacto, prácticos

1. **Publicar con un clic desde Sync** — tras pull, botón “Activar todos los de lista ecom / con precio”.
2. **Stock real en vitrina Casa Imperial** — sync stock depósito 1; badge “Consultar” vs “Disponible” correcto.
3. **Imágenes ERP** — exportar/servir `articuloimagen` (hoy solo meta/URL placeholder).
4. **Checkout smoke UAT** carrito → pedido local (sin write-back ERP mientras `AllowWriteBack=false`).
5. **Filtro “sin precio” / “sin imagen”** en admin catálogo (calidad de vitrina).
6. **Notificación toast** en admin al guardar producto/branding: “Cambios visibles en tienda” + link Abrir.

## Prioridad B — paridad ERP / operaciones

7. **Write-back PedidoMovil** UAT con `AllowWriteBack=true` (solo cuando lo autoricen).
8. **Historial lista precio ↔ cuenta** (admin clientes) — **hecho (ago-5)**
9. **DetalleListaPrecio** en carrito (misma regla ERP add-to-cart).
10. **Multi-depósito / agencia pickup** en checkout con stock por sucursal.
11. **Planillas Excel EPPlus** — **hecho (ago-5):** `?format=xlsx` (+ CSV intacto).
12. **Chofer + timeline** ya existe — pulir estados que el comprador entiende (“en camino”, ETA).

## Prioridad C — crecimiento comercial

13. **Cupones / combos** más visibles en home y checkout.
14. **Wishlist → aviso de reposición** (producto seguido).
15. **Búsqueda por código de barras / SKU exacto** en storefront.
16. **PWA / mobile install** para compradores habituales.
17. **WhatsApp CTA** prearmado con SKU + link producto.
18. **SEO por tenant** (title/meta con nombre tienda).

## Prioridad D — plataforma

19. **Switcher de tenant** en admin sin re-login (para soporte).
20. **Auditoría** quién activó/desactivó vitrina.
21. **Health endpoint** sync lag / última corrida por empresa.
22. **Tests E2E** Playwright: sync → activar → ver en tienda → checkout.

---

## Recomendación inmediata (próximos 2–3 días)

1. Recrear DB local + Sync lectura + Activar productos con precio.  
2. A +1 (publicar tras sync) y A +5 (filtros calidad).  
3. Smoke checkout Standalone.  
4. Dejar write-back apagado hasta UAT explícito.
