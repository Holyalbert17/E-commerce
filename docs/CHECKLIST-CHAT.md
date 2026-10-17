# Checklist — Chat / CRM (nuevo)

Fecha: 2026-07-30 (actualizado)

Leyenda: `[ ]` pendiente · `[~]` parcial · `[x]` hecho

---

## Hecho ahora
- [x] Header CRM compacto (menos alto, más espacio para la conversación)
- [x] Composer fijo abajo + área de mensajes con scroll propio
- [x] “Nueva conversación” sin poll/GET fantasma (fix status 0)
- [x] Forzar conversación nueva real (`forzarNueva`)
- [x] Cerrar chat (cliente + botón admin)
- [x] Media URL relativa / fix puerto 5081→5082 para ver fotos
- [x] Poll más calmado (evitar 429)
- [x] Build storefront: template usa `activeConvId()` (no `convId` private)
- [x] Verificado API: nueva conv · upload PNG · media 200 · cerrar · login admin

---

## Storefront (cliente)
- [ ] Typing indicator (“escribiendo…”) en tiempo real
- [ ] SignalR / WebSocket (dejar de depender de poll)
- [ ] Reabrir conversación cerrada con un tap
- [ ] Preview thumbnail en lista de conversaciones
- [ ] Entrega/lectura tipo doble check
- [ ] Adjuntar documento (PDF) además de foto
- [ ] Compresión de imagen antes de subir
- [ ] Offline queue si cae la red

## Admin CRM
- [ ] Notificación desktop / sonido al llegar mensaje nuevo
- [ ] Transferir conversación a otro agente
- [ ] Notas internas (no visibles al cliente)
- [ ] Adjuntar desde pedido / factura
- [ ] Filtro “cerradas” + reabrir
- [ ] SLA / tiempo de primera respuesta
- [ ] Plantillas editables por tenant (no hardcode)
- [ ] Vista mobile full-screen del hilo

## API / ops
- [ ] Rate limit por usuario JWT (no solo IP)
- [ ] Endpoint delta `?afterId=` para poll liviano
- [ ] Antivirus / validación magic-bytes en uploads
- [ ] CDN o storage S3 para media en prod
- [ ] Tests e2e: nueva conv · foto · cerrar · admin responde

## UX estética
- [~] CRM toolbar compacta (hecho baseline; pulir tipografía/tema tenant)
- [ ] Dark-friendly contraste en burbujas
- [ ] Animación suave al enviar / recibir
- [ ] Empty state con CTA “Escribile al cliente” si hay carrito abandonado

---

## Cómo probar ya
1. Storefront → FAB → `+ Nueva conversación` → mensaje (no debe salir error 0)
2. Adjuntar foto JPEG/PNG → debe verse en ambos lados
3. Botón **Cerrar** (cliente) / **Cerrar chat** (admin)
4. Admin `/crm` → header bajo, composer siempre visible

| Servicio | URL |
|----------|-----|
| Storefront | http://localhost:4200 |
| Admin | http://localhost:4201 |
| API | http://localhost:5082/swagger |

Demo: `demo` / `demo123` · admin `admin` / `Admin12!`
