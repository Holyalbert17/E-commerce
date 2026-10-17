import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AccountProfile,
  ApiService,
  ChatMensaje,
  ProductoElegibleResena,
  isLoggedIn,
} from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from '../shared/dx-icon.component';

const QUICK_EMOJIS = ['😊', '👍', '🙏', '✅', '📦', '🚚', '📷', '⚠️'];

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe, RouterLink, DxIconComponent],
  template: `
    <section class="cuenta" aria-labelledby="cuenta-title">
      <header class="toolbar">
        <div>
          <h1 id="cuenta-title"><dx-icon name="user" [size]="26" /> Mi cuenta</h1>
          <p class="sub">Perfil, canal de precios, reseñas, devoluciones y soporte.</p>
        </div>
        <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
      </header>

      @if (!logged) {
        <div class="empty-box">
          <dx-icon name="login" [size]="36" />
          <p>Necesitás iniciar sesión para administrar tu cuenta.</p>
          <button type="button" class="dx-btn" (click)="openLogin()">
            <dx-icon name="login" [size]="16" /> Iniciar sesión
          </button>
        </div>
      } @else {
        @if (error()) {
          <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
        }
        @if (ok()) {
          <p class="dx-alert dx-alert--ok" role="status">{{ ok() }}</p>
        }

        @if (profile(); as p) {
          <div class="meta-row" aria-label="Cuenta ERP">
            <span>
              <dx-icon name="store" [size]="14" />
              Cuenta ERP
              <strong>{{ p.idCuentaPrincipal ?? '—' }}</strong>
            </span>
            <span>
              <dx-icon name="tag" [size]="14" />
              Lista
              <strong>{{ p.listaPrecioDescripcion || 'Sin lista' }}</strong>
            </span>
            <span>
              <dx-icon name="user" [size]="14" />
              Email
              <strong>{{ p.email || p.usuario || '—' }}</strong>
            </span>
          </div>
        }

        <div class="hub-grid">
          <div class="hub-col">
            <aside class="panel">
              <div class="panel-head">
                <h2><dx-icon name="user" [size]="18" /> Datos personales</h2>
              </div>
              <form class="profile-form" (ngSubmit)="guardarPerfil()">
                <div class="row">
                  <label>
                    Nombre
                    <input class="dx-input" [(ngModel)]="perfilForm.nombre" name="nombre" required />
                  </label>
                  <label>
                    Apellido
                    <input class="dx-input" [(ngModel)]="perfilForm.apellido" name="apellido" required />
                  </label>
                </div>
                <div class="row">
                  <label>
                    Teléfono
                    <input class="dx-input" [(ngModel)]="perfilForm.telefono" name="telefono" />
                  </label>
                  <label>
                    Documento
                    <input class="dx-input" [(ngModel)]="perfilForm.numeroDocumento" name="doc" />
                  </label>
                </div>
                <button type="submit" class="dx-btn" [disabled]="savingProfile()">
                  {{ savingProfile() ? 'Guardando…' : 'Guardar perfil' }}
                </button>
              </form>
            </aside>

            <aside class="panel">
              <div class="panel-head">
                <h2><dx-icon name="shield" [size]="18" /> Cambiar contraseña</h2>
              </div>
              <form class="profile-form" (ngSubmit)="cambiarPassword()">
                <label>
                  Contraseña actual
                  <input
                    class="dx-input"
                    type="password"
                    [(ngModel)]="pwdForm.actual"
                    name="pwdActual"
                    required
                    autocomplete="current-password"
                  />
                </label>
                <div class="row">
                  <label>
                    Nueva contraseña
                    <input
                      class="dx-input"
                      type="password"
                      [(ngModel)]="pwdForm.nueva"
                      name="pwdNueva"
                      required
                      autocomplete="new-password"
                    />
                  </label>
                  <label>
                    Confirmar
                    <input
                      class="dx-input"
                      type="password"
                      [(ngModel)]="pwdForm.confirmacion"
                      name="pwdConfirm"
                      required
                      autocomplete="new-password"
                    />
                  </label>
                </div>
                <button type="submit" class="dx-btn" [disabled]="savingPwd()">
                  {{ savingPwd() ? 'Actualizando…' : 'Actualizar contraseña' }}
                </button>
              </form>
            </aside>
          </div>

          <div class="hub-col">
            <aside class="panel">
              <div class="panel-head">
                <h2><dx-icon name="percent" [size]="18" /> Canal de precios</h2>
              </div>
              @if (profile(); as p) {
                <p class="estado-line">
                  Estado
                  <span [class]="mayoristaBadgeClass(p.estadoMayorista)">
                    {{ mayoristaLabel(p.estadoMayorista) }}
                  </span>
                </p>
                <p class="muted">
                  Compras acumuladas: {{ p.montoComprasAcumulado | number: '1.0-0' }} Gs
                  @if (p.montoUmbralMayorista > 0) {
                    · Umbral auto: {{ p.montoUmbralMayorista | number: '1.0-0' }} Gs
                  }
                </p>
                @if (p.listaPrecioDescripcion) {
                  <p class="muted">Lista ERP: {{ p.listaPrecioDescripcion }}</p>
                }
                @if (p.estadoMayorista !== 'Aprobado' && p.estadoMayorista !== 'Solicitado') {
                  <button type="button" class="dx-btn" (click)="pedirMayorista()">
                    {{ p.estadoMayorista === 'Rechazado' ? 'Volver a solicitar' : 'Solicitar precio mayorista' }}
                  </button>
                }
              }
            </aside>

            <aside class="panel quick-panel">
              <div class="panel-head">
                <h2><dx-icon name="grid" [size]="18" /> Accesos rápidos</h2>
              </div>
              <nav class="quick-links" aria-label="Accesos de cuenta">
                <a routerLink="/mis-pedidos">
                  <dx-icon name="package" [size]="18" />
                  <span>Mis pedidos</span>
                  <dx-icon name="chevron-right" [size]="16" class="chev" />
                </a>
                <a routerLink="/devoluciones">
                  <dx-icon name="truck" [size]="18" />
                  <span>Devoluciones</span>
                  <dx-icon name="chevron-right" [size]="16" class="chev" />
                </a>
                <a routerLink="/favoritos">
                  <dx-icon name="heart" [size]="18" />
                  <span>Favoritos</span>
                  <dx-icon name="chevron-right" [size]="16" class="chev" />
                </a>
                <a routerLink="/seguidos">
                  <dx-icon name="user-plus" [size]="18" />
                  <span>Seguidos</span>
                  <dx-icon name="chevron-right" [size]="16" class="chev" />
                </a>
              </nav>
            </aside>
          </div>
        </div>

        <nav class="tabs" role="tablist" aria-label="Secciones de mi cuenta">
          <button
            type="button"
            role="tab"
            id="tab-resenas"
            [attr.aria-selected]="tab() === 'resenas'"
            [attr.aria-controls]="'panel-resenas'"
            [class.on]="tab() === 'resenas'"
            (click)="tab.set('resenas')"
          >
            <dx-icon name="star" [size]="14" /> Reseñas
          </button>
          <button
            type="button"
            role="tab"
            id="tab-dev"
            [attr.aria-selected]="tab() === 'dev'"
            [attr.aria-controls]="'panel-dev'"
            [class.on]="tab() === 'dev'"
            (click)="goDevoluciones()"
          >
            <dx-icon name="truck" [size]="14" /> Devoluciones
          </button>
          <button
            type="button"
            role="tab"
            id="tab-chat"
            [attr.aria-selected]="tab() === 'chat'"
            [attr.aria-controls]="'panel-chat'"
            [class.on]="tab() === 'chat'"
            (click)="openChatTab()"
          >
            <dx-icon name="store" [size]="14" /> Soporte
          </button>
        </nav>

        @if (tab() === 'resenas') {
          <div id="panel-resenas" role="tabpanel" aria-labelledby="tab-resenas" class="tab-panel">
            <h2>Calificá lo que compraste</h2>
            @for (p of elegibles(); track p.idArticulo) {
              <article class="card">
                <div
                  class="img"
                  [style.backgroundImage]="p.imagenUrl ? 'url(' + mediaUrl(p.imagenUrl) + ')' : 'none'"
                ></div>
                <div class="body">
                  <strong>{{ p.nombre }}</strong>
                  <small>Pedido #{{ p.nroPedido }}</small>
                  @if (p.yaReseno) {
                    <span class="soft-badge soft-badge--ok">Ya reseñado</span>
                  } @else {
                    <div class="stars">
                      @for (s of [1, 2, 3, 4, 5]; track s) {
                        <button type="button" (click)="score.set(s)" [attr.aria-label]="'Puntuar ' + s">
                          <dx-icon name="star" [size]="18" [filled]="score() >= s" />
                        </button>
                      }
                    </div>
                    <input class="dx-input" [(ngModel)]="titulo" placeholder="Título (opcional)" />
                    <textarea class="dx-input" [(ngModel)]="comentario" rows="2" placeholder="Tu comentario"></textarea>
                    <button type="button" class="dx-btn" (click)="enviarResena(p)">Publicar reseña</button>
                  }
                </div>
              </article>
            } @empty {
              <div class="empty-box">
                <dx-icon name="star" [size]="32" />
                <p class="empty">Todavía no hay compras para reseñar.</p>
              </div>
            }
          </div>
        }

        @if (tab() === 'dev') {
          <div id="panel-dev" role="tabpanel" aria-labelledby="tab-dev" class="tab-panel">
            <div class="empty-box">
              <dx-icon name="truck" [size]="36" />
              <p>Las devoluciones tienen su propio apartado, con foto del producto y seguimiento del estado.</p>
              <a class="dx-btn" routerLink="/devoluciones">Ir a Devoluciones</a>
            </div>
          </div>
        }

        @if (tab() === 'chat') {
          <div id="panel-chat" role="tabpanel" aria-labelledby="tab-chat" class="wa">
            <header class="wa-head">
              <div class="wa-title">
                <span class="wa-dot" aria-hidden="true"></span>
                <div>
                  <h2>Chat con la tienda</h2>
                  <small>Consultas y reclamos · respondemos acá</small>
                </div>
              </div>
              <select class="dx-select" [(ngModel)]="chatAsunto" name="asunto">
                <option value="consulta">Consulta</option>
                <option value="reclamo">Reclamo</option>
                <option value="pedido">Pedido</option>
                <option value="general">General</option>
              </select>
            </header>
            <div class="wa-bubbles" #chatScroll>
              @for (m of chats(); track m.id) {
                <div class="wa-bubble" [class.me]="m.origen === 'cliente'" [class.them]="m.origen !== 'cliente'">
                  @if (m.origen !== 'cliente' && m.nombreAutor) {
                    <span class="wa-author">{{ m.nombreAutor }}</span>
                  }
                  @if (m.adjunto?.url) {
                    <a [href]="mediaUrl(m.adjunto!.url)" target="_blank" rel="noopener">
                      <img [src]="mediaUrl(m.adjunto!.url)" alt="adjunto" />
                    </a>
                  }
                  @if (m.mensaje) {
                    <p>{{ m.mensaje }}</p>
                  }
                  <small>{{ m.fechaMensaje | date: 'short' }}</small>
                </div>
              } @empty {
                <div class="wa-empty">
                  <p>Escribí o mandá una foto para empezar.</p>
                </div>
              }
            </div>
            <footer class="wa-composer">
              <div class="wa-emojis" aria-label="Emojis rápidos">
                @for (emoji of quickEmojis; track emoji) {
                  <button type="button" class="wa-emoji" (click)="insertChatEmoji(emoji)">{{ emoji }}</button>
                }
              </div>
              <div class="wa-row">
                <label class="wa-attach" title="Subir foto">
                  <dx-icon name="tag" [size]="18" />
                  <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden (change)="onChatFile($event)" />
                </label>
                <textarea
                  #chatDraft
                  class="wa-input"
                  rows="1"
                  [(ngModel)]="chatMsg"
                  placeholder="Escribí un mensaje"
                  (input)="autoGrowChat()"
                  (keydown.enter)="sendChatOnEnter($event)"
                ></textarea>
                <button
                  type="button"
                  class="wa-send"
                  (click)="enviarChat()"
                  [disabled]="!chatMsg.trim()"
                  title="Enviar"
                  aria-label="Enviar"
                >
                  ➤
                </button>
              </div>
            </footer>
          </div>
        }
      }
    </section>
  `,
  styles: [
    `
      .cuenta {
        max-width: 1080px;
        margin: 0 auto;
        padding: 1.25rem 1rem 2.5rem;
        color: var(--dx-ink);
      }
      .toolbar {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 1.15rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.55rem, 3vw, 2rem);
        color: var(--dx-ink);
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
      }
      h2 {
        margin: 0 0 0.75rem;
        font-size: 1.1rem;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
      .sub {
        color: var(--dx-mute);
        margin: 0.35rem 0 0;
      }
      .toolbar > a {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        font-weight: 700;
        text-decoration: none;
        color: var(--dx-red);
      }
      .meta-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem 1.25rem;
        padding: 0.75rem 1rem;
        margin-bottom: 1.1rem;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        font-size: 0.9rem;
        color: var(--dx-ink-soft);
      }
      .meta-row span {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
      .meta-row strong {
        color: var(--dx-ink);
        font-weight: 700;
      }
      .hub-grid {
        display: grid;
        grid-template-columns: 1.35fr 1fr;
        gap: 1rem;
        margin-bottom: 1.25rem;
      }
      @media (max-width: 860px) {
        .hub-grid {
          grid-template-columns: 1fr;
        }
      }
      .hub-col {
        display: grid;
        gap: 1rem;
        align-content: start;
      }
      .panel {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1rem 1.1rem;
        box-shadow: var(--dx-shadow);
      }
      .panel-head {
        margin-bottom: 0.65rem;
      }
      .panel-head h2 {
        margin: 0;
        font-size: 1.05rem;
      }
      .estado-line {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin: 0 0 0.5rem;
      }
      .soft-badge {
        display: inline-flex;
        align-items: center;
        gap: 0.2rem;
        padding: 0.22rem 0.55rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 700;
        line-height: 1.2;
      }
      .soft-badge--ok {
        background: #e8f5e9;
        color: var(--dx-ok);
      }
      .soft-badge--warn {
        background: #fff8e1;
        color: #b45309;
      }
      .soft-badge--danger {
        background: #fdecea;
        color: var(--dx-warn);
      }
      .soft-badge--info {
        background: #e3f2fd;
        color: var(--dx-focus);
      }
      .quick-links {
        display: grid;
        gap: 0.45rem;
      }
      .quick-links a {
        display: grid;
        grid-template-columns: auto 1fr auto;
        align-items: center;
        gap: 0.65rem;
        padding: 0.7rem 0.8rem;
        border: 1px solid var(--dx-line);
        border-radius: 10px;
        text-decoration: none;
        color: var(--dx-ink);
        font-weight: 600;
        background: #fafafa;
        transition: background 0.15s ease, border-color 0.15s ease;
      }
      .quick-links a:hover {
        background: var(--dx-red-soft);
        border-color: #ef9a9a;
      }
      .quick-links .chev {
        color: var(--dx-mute);
      }
      .tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-bottom: 1.1rem;
      }
      .tabs button {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.5rem 1rem;
        border-radius: 999px;
        border: 1px solid var(--dx-line);
        background: #fff;
        color: var(--dx-ink);
        cursor: pointer;
        font-weight: 600;
      }
      .tabs button.on {
        background: var(--dx-red);
        color: #fff;
        border-color: transparent;
      }
      .tab-panel {
        margin-bottom: 1rem;
      }
      .card {
        display: grid;
        grid-template-columns: 72px 1fr;
        gap: 0.85rem;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 0.85rem;
        margin-bottom: 0.75rem;
        box-shadow: var(--dx-shadow);
      }
      .img {
        width: 72px;
        height: 72px;
        border-radius: 10px;
        background: #f1f5f9 center/cover no-repeat;
      }
      .body {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
      }
      .body small {
        color: var(--dx-mute);
      }
      .stars {
        display: flex;
        gap: 0.15rem;
      }
      .stars button {
        background: none;
        border: none;
        color: #f59e0b;
        cursor: pointer;
        padding: 0;
        line-height: 0;
      }
      .profile-form {
        display: grid;
        gap: 0.65rem;
      }
      .profile-form .row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.65rem;
      }
      @media (max-width: 560px) {
        .profile-form .row {
          grid-template-columns: 1fr;
        }
      }
      .profile-form label {
        display: grid;
        gap: 0.3rem;
        font-size: 0.88rem;
        font-weight: 600;
      }
      .form {
        display: grid;
        gap: 0.5rem;
        max-width: 480px;
        margin-bottom: 1rem;
      }
      .form-dev .lbl {
        font-size: 0.85rem;
        font-weight: 600;
        color: var(--dx-ink);
        margin-top: 0.25rem;
      }
      .hint {
        color: var(--dx-mute);
        font-size: 0.92rem;
        margin: -0.35rem 0 0.85rem;
        max-width: 42rem;
      }
      .pedido-meta {
        margin: -0.15rem 0 0.35rem;
        font-size: 0.86rem;
        color: var(--dx-mute);
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem 0.65rem;
        align-items: center;
      }
      .pedido-meta a {
        color: var(--dx-red);
        font-weight: 600;
      }
      .subh {
        font-size: 1rem;
        margin: 1.25rem 0 0.65rem;
      }
      .dev-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: grid;
        gap: 0.55rem;
      }
      .dev-list li {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        padding: 0.75rem 0.9rem;
        display: grid;
        gap: 0.35rem;
      }
      .dev-top {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: 0.45rem;
      }
      .dev-body {
        display: grid;
        gap: 0.2rem;
        font-size: 0.92rem;
      }
      .dev-body small {
        color: var(--dx-mute);
      }
      .wa {
        display: flex;
        flex-direction: column;
        height: min(70dvh, 640px);
        min-height: 420px;
        background: #ece5dd;
        border-radius: 16px;
        overflow: hidden;
        border: 1px solid var(--dx-line);
        box-shadow: var(--dx-shadow);
      }
      .wa-head {
        flex: 0 0 auto;
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        padding: 0.75rem 1rem;
        background: linear-gradient(110deg, #075e54, #0f766e);
        color: #fff;
      }
      .wa-title {
        display: inline-flex;
        align-items: center;
        gap: 0.55rem;
        min-width: 0;
      }
      .wa-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: #25d366;
        box-shadow: 0 0 0 3px rgba(37, 211, 102, 0.35);
        flex: 0 0 auto;
      }
      .wa-head h2 {
        margin: 0;
        font-size: 1.05rem;
        color: #fff;
      }
      .wa-head small {
        display: block;
        opacity: 0.85;
        font-size: 0.72rem;
      }
      .wa-head select {
        max-width: 140px;
        background: rgba(255, 255, 255, 0.95);
      }
      .wa-bubbles {
        flex: 1 1 auto;
        min-height: 0;
        overflow-y: auto;
        overflow-x: hidden;
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.55rem;
        scroll-behavior: smooth;
        -webkit-overflow-scrolling: touch;
        background:
          radial-gradient(circle at 1px 1px, rgba(15, 118, 110, 0.045) 1px, transparent 0) 0 0 / 24px 24px;
      }
      .wa-bubble {
        max-width: min(82%, 420px);
        padding: 0.55rem 0.75rem;
        border-radius: 12px;
        background: #fff;
        box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06);
      }
      .wa-bubble.me {
        align-self: flex-end;
        background: #dcf8c6;
        border-bottom-right-radius: 4px;
      }
      .wa-bubble.them {
        align-self: flex-start;
        border-bottom-left-radius: 4px;
      }
      .wa-author {
        display: block;
        font-size: 0.72rem;
        font-weight: 800;
        color: #075e54;
        margin-bottom: 0.12rem;
      }
      .wa-bubble img {
        max-width: 200px;
        border-radius: 8px;
        display: block;
        margin-bottom: 0.3rem;
      }
      .wa-bubble p {
        margin: 0;
        white-space: pre-wrap;
        word-break: break-word;
      }
      .wa-bubble small {
        display: block;
        margin-top: 0.2rem;
        color: #64748b;
        font-size: 0.72rem;
        text-align: right;
      }
      .wa-composer {
        flex: 0 0 auto;
        display: grid;
        gap: 0.3rem;
        padding: 0.45rem 0.55rem 0.6rem;
        background: #f0f2f5;
        border-top: 1px solid #d1d5db;
      }
      .wa-emojis {
        display: flex;
        gap: 0.15rem;
        overflow-x: auto;
        scrollbar-width: none;
      }
      .wa-emojis::-webkit-scrollbar {
        display: none;
      }
      .wa-emoji {
        width: 28px;
        height: 28px;
        border: 0;
        border-radius: 8px;
        background: transparent;
        font-size: 1rem;
        cursor: pointer;
        flex: 0 0 auto;
      }
      .wa-emoji:hover {
        background: #fff;
        transform: scale(1.1);
      }
      .wa-row {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 0.4rem;
        align-items: end;
      }
      .wa-attach {
        width: 42px;
        height: 42px;
        border-radius: 50%;
        background: #fff;
        display: grid;
        place-items: center;
        cursor: pointer;
        color: #54656f;
        box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06);
      }
      .wa-attach:hover {
        color: #075e54;
      }
      .wa-input {
        min-height: 42px;
        max-height: 120px;
        height: 42px;
        resize: none;
        border: 0;
        border-radius: 21px;
        padding: 0.6rem 0.9rem;
        font: inherit;
        line-height: 1.35;
        overflow-y: auto;
        outline: none;
        box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06);
      }
      .wa-send {
        width: 42px;
        height: 42px;
        border: 0;
        border-radius: 50%;
        background: #25d366;
        color: #fff;
        cursor: pointer;
        display: grid;
        place-items: center;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.18);
      }
      .wa-send:disabled {
        opacity: 0.45;
        cursor: default;
      }
      .wa-empty {
        margin: auto;
        text-align: center;
        color: #64748b;
        padding: 2rem 1rem;
      }
      .wa-empty p {
        margin: 0;
      }
      @media (max-width: 640px) {
        .wa {
          height: min(75dvh, 580px);
          min-height: 360px;
          border-radius: 12px;
        }
      }
      .muted {
        color: var(--dx-mute);
        margin: 0.25rem 0;
      }
      .empty {
        color: var(--dx-mute);
        padding: 0;
        margin: 0;
      }
      .empty-box {
        display: grid;
        justify-items: center;
        text-align: center;
        gap: 0.65rem;
        padding: 1.75rem 1rem;
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        color: var(--dx-mute);
        margin-bottom: 1rem;
      }
      .empty-box dx-icon {
        color: var(--dx-mute);
      }
    `,
  ],
})
export class AccountHubPageComponent implements OnInit {
  @ViewChild('chatScroll') chatScroll?: ElementRef<HTMLDivElement>;
  @ViewChild('chatDraft') chatDraft?: ElementRef<HTMLTextAreaElement>;

  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthModalService);
  private readonly session = inject(SessionService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly tab = signal<'resenas' | 'dev' | 'chat'>('resenas');
  readonly elegibles = signal<ProductoElegibleResena[]>([]);
  readonly chats = signal<ChatMensaje[]>([]);
  readonly profile = signal<AccountProfile | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly score = signal(5);
  readonly savingProfile = signal(false);
  readonly savingPwd = signal(false);
  logged = isLoggedIn();
  titulo = '';
  comentario = '';
  chatMsg = '';
  chatAsunto = 'consulta';
  chatConvId: number | null = null;
  readonly quickEmojis = QUICK_EMOJIS;
  perfilForm = { nombre: '', apellido: '', telefono: '', numeroDocumento: '' };
  pwdForm = { actual: '', nueva: '', confirmacion: '' };

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  insertChatEmoji(emoji: string): void {
    this.chatMsg = `${this.chatMsg}${this.chatMsg && !this.chatMsg.endsWith(' ') ? ' ' : ''}${emoji} `;
    setTimeout(() => this.autoGrowChat(), 0);
  }

  autoGrowChat(): void {
    const el = this.chatDraft?.nativeElement;
    if (!el) return;
    el.style.height = '42px';
    el.style.height = `${Math.min(el.scrollHeight, 120)}px`;
  }

  sendChatOnEnter(event: Event): void {
    const keyboard = event as KeyboardEvent;
    if (keyboard.shiftKey) return;
    keyboard.preventDefault();
    this.enviarChat();
  }

  private resetChatDraft(): void {
    this.chatMsg = '';
    setTimeout(() => {
      const el = this.chatDraft?.nativeElement;
      if (el) el.style.height = '42px';
    }, 0);
  }

  private scrollChatBottom(smooth = false): void {
    const el = this.chatScroll?.nativeElement;
    if (!el) return;
    if (smooth) el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    else el.scrollTop = el.scrollHeight;
  }

  ngOnInit(): void {
    if (!this.logged) return;
    const qp = this.route.snapshot.queryParamMap;
    const tabQp = qp.get('tab');
    if (tabQp === 'dev' || tabQp === 'devoluciones') {
      const pedido = qp.get('pedido') || qp.get('idPedidoMovil');
      const nro = qp.get('nro');
      void this.router.navigate(['/devoluciones'], {
        queryParams: {
          ...(pedido ? { pedido } : {}),
          ...(nro ? { nro } : {}),
        },
        replaceUrl: true,
      });
      return;
    }
    if (tabQp === 'chat' || tabQp === 'soporte') this.tab.set('chat');
    else if (tabQp === 'resenas') this.tab.set('resenas');

    this.api.me().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.fillPerfil(p);
        this.session.applyProfileCanal(p.estadoMayorista, p.canalPreferido);
      },
    });
    this.api.getElegiblesResena().subscribe({
      next: (x) => this.elegibles.set(x),
      error: (e) => {
        if (e?.status === 401) {
          this.logged = false;
          this.flashError('Sesión vencida. Volvé a iniciar sesión.');
          return;
        }
        this.flashError(e?.error?.error || e.message);
      },
    });
    this.reloadChat();
  }

  goDevoluciones(): void {
    void this.router.navigate(['/devoluciones']);
  }

  openChatTab(): void {
    this.tab.set('chat');
    this.reloadChat();
    setTimeout(() => this.scrollChatBottom(false), 80);
  }

  private reloadChat(): void {
    if (!this.logged && !this.session.loggedIn()) return;
    this.api.getChat(undefined, this.chatConvId ?? undefined).subscribe({
      next: (x) => {
        this.chats.set(x);
        const last = x[x.length - 1];
        if (last?.idConversacion) this.chatConvId = last.idConversacion;
        if (this.chatConvId) this.api.marcarChatLeidos(this.chatConvId).subscribe({ error: () => undefined });
        if (this.tab() === 'chat') setTimeout(() => this.scrollChatBottom(false), 50);
      },
      error: (e) => {
        if (e?.status === 401) {
          this.logged = false;
          this.flashError('Sesión vencida. Volvé a iniciar sesión.');
        }
      },
    });
  }

  mayoristaLabel(estado: string): string {
    switch (estado) {
      case 'Aprobado':
        return 'Mayorista aprobado';
      case 'Solicitado':
        return 'Solicitud pendiente';
      case 'Rechazado':
        return 'Rechazado';
      default:
        return 'Minorista';
    }
  }

  mayoristaBadgeClass(estado: string): string {
    switch (estado) {
      case 'Aprobado':
        return 'soft-badge soft-badge--ok';
      case 'Solicitado':
        return 'soft-badge soft-badge--warn';
      case 'Rechazado':
        return 'soft-badge soft-badge--danger';
      default:
        return 'soft-badge soft-badge--info';
    }
  }

  private fillPerfil(p: AccountProfile): void {
    this.perfilForm = {
      nombre: p.nombre || '',
      apellido: p.apellido || '',
      telefono: p.telefono || '',
      numeroDocumento: p.numeroDocumento || '',
    };
  }

  private flashOk(message: string): void {
    this.error.set(null);
    this.ok.set(message);
    this.toast.success(message);
  }

  private flashError(message: string): void {
    this.ok.set(null);
    this.error.set(message);
    this.toast.error(message);
  }

  openLogin(): void {
    this.auth.showLogin();
  }

  guardarPerfil(): void {
    if (!this.perfilForm.nombre.trim() || !this.perfilForm.apellido.trim()) {
      this.flashError('Nombre y apellido son obligatorios.');
      return;
    }
    this.savingProfile.set(true);
    this.error.set(null);
    this.api
      .updateProfile({
        nombre: this.perfilForm.nombre.trim(),
        apellido: this.perfilForm.apellido.trim(),
        telefono: this.perfilForm.telefono.trim() || undefined,
        numeroDocumento: this.perfilForm.numeroDocumento.trim() || undefined,
      })
      .subscribe({
        next: (p) => {
          this.savingProfile.set(false);
          this.profile.set(p);
          this.fillPerfil(p);
          const name = `${p.nombre} ${p.apellido}`.trim() || p.nombre;
          localStorage.setItem('ecom_user_nombre', name);
          this.session.userName.set(name);
          this.flashOk('Perfil actualizado.');
        },
        error: (e) => {
          this.savingProfile.set(false);
          this.flashError(e?.error?.error || e.message || 'No se pudo guardar el perfil');
        },
      });
  }

  cambiarPassword(): void {
    if (!this.pwdForm.actual || !this.pwdForm.nueva) {
      this.flashError('Completá la contraseña actual y la nueva.');
      return;
    }
    if (this.pwdForm.nueva !== this.pwdForm.confirmacion) {
      this.flashError('Las contraseñas nuevas no coinciden.');
      return;
    }
    this.savingPwd.set(true);
    this.error.set(null);
    this.api.changePassword(this.pwdForm.actual, this.pwdForm.nueva, this.pwdForm.confirmacion).subscribe({
      next: (r) => {
        this.savingPwd.set(false);
        this.pwdForm = { actual: '', nueva: '', confirmacion: '' };
        this.flashOk(r.mensaje || 'Contraseña actualizada.');
      },
      error: (e) => {
        this.savingPwd.set(false);
        this.flashError(e?.error?.error || e.message || 'No se pudo cambiar la contraseña');
      },
    });
  }

  pedirMayorista(): void {
    this.api.solicitarMayorista('Solicitud desde Mi cuenta').subscribe({
      next: (p) => {
        this.profile.set(p);
        this.flashOk('Solicitud de mayorista enviada. Un administrador la revisará.');
      },
      error: (e) => this.flashError(e?.error?.error || e.message),
    });
  }

  enviarResena(p: ProductoElegibleResena): void {
    this.api
      .crearResena({
        idArticulo: p.idArticulo,
        puntuacion: this.score(),
        titulo: this.titulo || undefined,
        comentario: this.comentario || undefined,
        idPedidoMovil: p.idPedidoMovil,
        idCarritoCompra: p.idCarritoCompra,
      })
      .subscribe({
        next: () => {
          this.flashOk('Reseña publicada');
          this.titulo = '';
          this.comentario = '';
          this.ngOnInit();
        },
        error: (e) => this.flashError(e?.error?.error || e.message),
      });
  }

  enviarChat(): void {
    if (!this.chatMsg.trim()) return;
    this.api.enviarChat(this.chatMsg, undefined, this.chatAsunto, this.chatConvId ?? undefined).subscribe({
      next: (m) => {
        this.chats.update((c) => [...c, m]);
        if (m.idConversacion) this.chatConvId = m.idConversacion;
        this.resetChatDraft();
        setTimeout(() => this.scrollChatBottom(true), 40);
      },
      error: (e) => this.flashError(e?.error?.error || e.message),
    });
  }

  onChatFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.api.enviarChatFoto(file, this.chatMsg || undefined, this.chatAsunto, this.chatConvId ?? undefined).subscribe({
      next: (m) => {
        this.chats.update((c) => [...c, m]);
        if (m.idConversacion) this.chatConvId = m.idConversacion;
        this.resetChatDraft();
        input.value = '';
        setTimeout(() => this.scrollChatBottom(true), 40);
      },
      error: (e) => this.flashError(e?.error?.error || e.message),
    });
  }
}
