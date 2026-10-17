import { DatePipe } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import {
  ApiService,
  ChatConversacion,
  ChatMensaje,
  isLoggedIn,
} from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { MiniCartService } from '../core/mini-cart.service';
import { SessionService } from '../core/session.service';
import { SupportChatFacade } from '../core/support-chat.facade';
import { StorefrontChatHubService } from '../core/storefront-chat-hub.service';

const MAX_FILE_BYTES = 5 * 1024 * 1024;
const BG_POLL_MS = 30_000;
const OPEN_POLL_MS = 20_000;
const STICK_THRESHOLD_PX = 72;

@Component({
  selector: 'app-support-chat-widget',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  template: `
    <div class="scw" [class.away]="dockBlocked()" aria-live="polite">
      @if (open()) {
        <section
          id="scw-panel"
          class="panel"
          role="dialog"
          aria-label="Atención al cliente"
          [attr.aria-modal]="true"
        >
          <header class="head">
            <div class="brand">
              <span class="dot" aria-hidden="true"></span>
              <div>
                <strong>Atención al cliente</strong>
                @if (agenteAsignado()) {
                  <small>Te atiende <b>{{ agenteAsignado() }}</b></small>
                } @else {
                  <small>Consultas y reclamos · respondemos acá</small>
                }
              </div>
            </div>
            <button type="button" class="icon-btn" (click)="close()" aria-label="Cerrar chat">×</button>
          </header>

          @if (!logged()) {
            <div class="guest">
              <p class="guest-lead">Hola — ¿en qué te ayudamos?</p>
              <ul class="guest-help">
                <li>Estado de un pedido</li>
                <li>Consulta de producto o stock</li>
                <li>Reclamo o devolución</li>
              </ul>
              <button type="button" class="dx-btn" (click)="askLogin()">Iniciar sesión para chatear</button>
              <div class="guest-links">
                <a routerLink="/mis-pedidos" class="link" (click)="close()">Mis pedidos</a>
                <a routerLink="/devoluciones" class="link" (click)="close()">Devoluciones</a>
                <a routerLink="/contacto" class="link" (click)="close()">Contacto</a>
              </div>
            </div>
          } @else {
            <div class="conv-bar">
              <select class="conv-select" [(ngModel)]="convPicker" name="convPicker" (ngModelChange)="onConvPick($event)">
                <option [ngValue]="'nueva'">+ Nueva conversación</option>
                @for (c of conversaciones(); track c.id) {
                  <option [ngValue]="c.id">
                    {{ c.asunto }} · {{ c.previewUltimoMensaje || c.estado }}
                    @if (c.noLeidosCliente > 0) { ({{ c.noLeidosCliente }}) }
                  </option>
                }
              </select>
              @if (activeConvId()) {
                <button type="button" class="close-conv" (click)="cerrarConversacion()" title="Cerrar conversación">
                  Cerrar
                </button>
              }
            </div>

            <div class="toolbar">
              <label>
                Motivo
                <select [(ngModel)]="asunto" name="asuntoWa">
                  <option value="consulta">Consulta</option>
                  <option value="reclamo">Reclamo</option>
                  <option value="pedido">Pedido</option>
                  <option value="general">General</option>
                </select>
              </label>
              @if (idPedidoMovil) {
                <small class="pedido-tag">Pedido #{{ idPedidoMovil }}</small>
              }
            </div>
            <div class="quick-topics" role="group" aria-label="Consultas frecuentes">
              @for (t of quickTopics; track t) {
                <button type="button" class="topic-chip" (click)="insertTopic(t)">{{ t }}</button>
              }
            </div>

            <div class="bubbles-wrap">
              <div class="bubbles" #scrollBox (scroll)="onBubblesScroll()">
                @if (loading()) {
                  <p class="mute">Cargando mensajes…</p>
                } @else if (error()) {
                  <p class="err" role="alert">{{ error() }}</p>
                } @else if (messages().length === 0) {
                  <p class="mute">Escribí tu consulta o elegí un acceso rápido arriba. También podés adjuntar una foto.</p>
                }
                @for (m of messages(); track m.id) {
                  <div class="bubble" [class.me]="m.origen === 'cliente'" [class.them]="m.origen !== 'cliente'">
                    @if (m.origen !== 'cliente' && m.nombreAutor) {
                      <span class="author">{{ m.nombreAutor }}</span>
                    }
                    @if (m.adjunto?.url) {
                      <a [href]="mediaUrl(m.adjunto!.url)!" target="_blank" rel="noopener">
                        <img [src]="mediaUrl(m.adjunto!.url)!" alt="Foto adjunta" loading="lazy" />
                      </a>
                    }
                    @if (m.mensaje) {
                      <p>{{ m.mensaje }}</p>
                    }
                    <small>{{ m.fechaMensaje | date: 'shortTime' }}</small>
                  </div>
                }
              </div>
              @if (hasNewBelow()) {
                <button type="button" class="jump-latest" (click)="jumpToLatest()">
                  Nuevos mensajes ↓
                </button>
              }
            </div>

            @if (previewUrl()) {
              <div class="preview-box" role="region" aria-label="Vista previa de imagen">
                <img [src]="previewUrl()!" alt="Vista previa" />
                <div class="preview-actions">
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="cancelPreview()">Cancelar</button>
                  <button type="button" class="dx-btn" (click)="confirmSendPhoto()" [disabled]="busy()">Enviar foto</button>
                </div>
              </div>
            }

            <footer class="composer">
              <div class="emoji-bar" aria-label="Emojis rápidos">
                @for (emoji of quickEmojis; track emoji) {
                  <button type="button" class="emoji-btn" (click)="insertEmoji(emoji)">{{ emoji }}</button>
                }
              </div>
              <div class="composer-row">
                <label class="attach" title="Enviar foto">
                  <span aria-hidden="true">📎</span>
                  <span class="sr-only">Adjuntar foto</span>
                  <input type="file" accept="image/jpeg,image/png,image/webp" hidden (change)="onFile($event)" />
                </label>
                <textarea
                  #draftInput
                  class="reply-box"
                  [(ngModel)]="draft"
                  name="chatDraft"
                  rows="1"
                  placeholder="Escribí un mensaje…"
                  (input)="autoGrowDraft()"
                  (keydown.enter)="sendOnEnter($event)"
                  [disabled]="busy()"
                ></textarea>
                <button type="button" class="send" (click)="send()" [disabled]="busy() || !draft.trim()" aria-label="Enviar">
                  ➤
                </button>
              </div>
              <p class="file-hint">Fotos JPEG/PNG/WebP · máx. 5 MB · Enter envía · Shift+Enter nueva línea</p>
            </footer>
          }
        </section>
      }

      <button
        type="button"
        class="fab"
        [class.open]="open()"
        (click)="toggle()"
        [attr.aria-expanded]="open()"
        aria-controls="scw-panel"
        [attr.aria-label]="open() ? 'Cerrar atención al cliente' : 'Abrir atención al cliente'"
      >
        @if (open()) {
          <span aria-hidden="true">×</span>
        } @else {
          <svg viewBox="0 0 24 24" width="28" height="28" aria-hidden="true" fill="currentColor">
            <path
              d="M12 2C6.48 2 2 6.2 2 11.4c0 2.9 1.4 5.5 3.7 7.3L5 22l3.6-1.1c1.1.3 2.2.5 3.4.5 5.52 0 10-4.2 10-9.4S17.52 2 12 2zm0 16.8c-1 0-2-.2-2.9-.5l-.2-.1-2.1.6.6-2-.1-.2C6.1 15.3 5.2 13.4 5.2 11.4 5.2 7.7 8.2 4.8 12 4.8s6.8 2.9 6.8 6.6-3 6.6-6.8 6.6z"
            />
            <circle cx="8.5" cy="11.4" r="1.1" />
            <circle cx="12" cy="11.4" r="1.1" />
            <circle cx="15.5" cy="11.4" r="1.1" />
          </svg>
          @if (unread() > 0) {
            <span class="badge">{{ unread() > 9 ? '9+' : unread() }}</span>
          }
        }
      </button>
      <span class="fab-label" [class.hide]="open()">Atención al cliente</span>
    </div>
  `,
  styles: [
    `
      .scw {
        position: fixed;
        right: 1.1rem;
        bottom: 1.1rem;
        z-index: 1200;
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: 0.55rem;
        font-family: var(--dx-font, inherit);
      }
      .scw.away {
        opacity: 0;
        visibility: hidden;
        pointer-events: none;
      }
      .fab {
        width: 60px;
        height: 60px;
        border-radius: 50%;
        border: none;
        background: #128c7e;
        color: #fff;
        box-shadow: 0 8px 24px rgba(18, 140, 126, 0.45);
        cursor: pointer;
        display: grid;
        place-items: center;
        position: relative;
        transition: transform 0.2s ease, background 0.2s ease;
      }
      .fab:hover {
        transform: scale(1.06);
        background: #075e54;
      }
      .fab.open {
        background: #334155;
        box-shadow: 0 6px 18px rgba(15, 23, 42, 0.35);
      }
      .fab span {
        font-size: 1.75rem;
        line-height: 1;
      }
      .badge {
        position: absolute;
        top: -2px;
        right: -2px;
        min-width: 1.25rem;
        height: 1.25rem;
        padding: 0 0.25rem;
        border-radius: 999px;
        background: #c62828;
        color: #fff;
        font-size: 0.7rem;
        font-weight: 800;
        display: grid;
        place-items: center;
      }
      .fab-label {
        order: -1;
        background: #fff;
        color: #075e54;
        font-size: 0.8rem;
        font-weight: 700;
        padding: 0.35rem 0.7rem;
        border-radius: 999px;
        box-shadow: 0 4px 14px rgba(0, 0, 0, 0.12);
        border: 1px solid #d1fae5;
        pointer-events: none;
      }
      .fab-label.hide {
        display: none;
      }
      .panel {
        width: min(380px, calc(100vw - 1.5rem));
        height: min(560px, calc(100dvh - 6.5rem));
        max-height: calc(100dvh - 6.5rem);
        background: #ece5dd;
        border-radius: 18px;
        overflow: hidden;
        display: flex;
        flex-direction: column;
        box-shadow: 0 18px 50px rgba(15, 23, 42, 0.28);
        border: 1px solid rgba(0, 0, 0, 0.06);
        animation: pop 0.2s ease;
      }
      @keyframes pop {
        from {
          opacity: 0;
          transform: translateY(12px) scale(0.96);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }
      .head {
        flex: 0 0 auto;
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        padding: 0.75rem 1rem;
        background: linear-gradient(110deg, #075e54, #0f766e);
        color: #fff;
      }
      .brand {
        display: flex;
        gap: 0.65rem;
        align-items: center;
      }
      .dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: #25d366;
        box-shadow: 0 0 0 3px rgba(37, 211, 102, 0.35);
      }
      .brand strong {
        display: block;
        font-size: 0.98rem;
      }
      .brand small {
        display: block;
        opacity: 0.85;
        font-size: 0.75rem;
      }
      .icon-btn {
        border: none;
        background: rgba(255, 255, 255, 0.15);
        color: #fff;
        width: 36px;
        height: 36px;
        border-radius: 10px;
        font-size: 1.4rem;
        cursor: pointer;
        line-height: 1;
      }
      .guest {
        flex: 1;
        display: grid;
        place-content: center;
        gap: 0.75rem;
        padding: 1.5rem;
        text-align: center;
        background: #fff;
      }
      .guest-lead {
        margin: 0;
        color: #0f172a;
        font-weight: 700;
        font-size: 1.05rem;
      }
      .guest-help {
        margin: 0;
        padding: 0;
        list-style: none;
        color: #475569;
        font-size: 0.88rem;
        display: grid;
        gap: 0.25rem;
      }
      .guest-help li::before {
        content: '· ';
        color: #075e54;
        font-weight: 800;
      }
      .guest-links {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        justify-content: center;
      }
      .guest p {
        margin: 0;
        color: #475569;
      }
      .guest .link {
        color: #075e54;
        font-weight: 700;
        text-decoration: none;
      }
      .quick-topics {
        flex: 0 0 auto;
        display: flex;
        flex-wrap: nowrap;
        gap: 0.35rem;
        padding: 0.35rem 0.75rem 0.45rem;
        background: #fff;
        border-bottom: 1px solid #e2e8f0;
        overflow-x: auto;
        scrollbar-width: thin;
      }
      .topic-chip {
        flex: 0 0 auto;
        border: 1px solid #cbd5e1;
        background: #f8fafc;
        border-radius: 999px;
        padding: 0.2rem 0.55rem;
        font-size: 0.72rem;
        font-weight: 600;
        color: #334155;
        cursor: pointer;
        white-space: nowrap;
      }
      .topic-chip:hover {
        border-color: #075e54;
        color: #075e54;
        background: #ecfdf5;
      }
      .conv-bar {
        flex: 0 0 auto;
        background: #fff;
        padding: 0.4rem 0.65rem;
        border-bottom: 1px solid #e2e8f0;
        display: flex;
        gap: 0.4rem;
        align-items: center;
      }
      .conv-select {
        flex: 1;
        border: 1px solid #cbd5e1;
        border-radius: 8px;
        padding: 0.35rem 0.5rem;
        font: inherit;
        font-size: 0.82rem;
      }
      .close-conv {
        border: 1px solid #fecaca;
        background: #fef2f2;
        color: #b91c1c;
        border-radius: 8px;
        padding: 0.35rem 0.55rem;
        font: inherit;
        font-size: 0.75rem;
        font-weight: 700;
        cursor: pointer;
        white-space: nowrap;
      }
      .toolbar {
        flex: 0 0 auto;
        background: #f8fafc;
        padding: 0.35rem 0.75rem;
        border-bottom: 1px solid #e2e8f0;
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .toolbar label {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.8rem;
        font-weight: 600;
        color: #475569;
        flex: 1;
        min-width: 140px;
      }
      .toolbar select {
        flex: 1;
        border: 1px solid #cbd5e1;
        border-radius: 8px;
        padding: 0.3rem 0.45rem;
        font: inherit;
      }
      .pedido-tag {
        font-size: 0.75rem;
        font-weight: 700;
        color: #075e54;
        background: #d1fae5;
        padding: 0.2rem 0.5rem;
        border-radius: 999px;
      }
      .brand small b {
        font-weight: 800;
        color: #a7f3d0;
      }
      .bubbles-wrap {
        flex: 1 1 auto;
        min-height: 0;
        position: relative;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }
      .bubbles {
        flex: 1 1 auto;
        min-height: 0;
        overflow-y: auto;
        overflow-x: hidden;
        padding: 0.85rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        scroll-behavior: smooth;
        -webkit-overflow-scrolling: touch;
      }
      .jump-latest {
        position: absolute;
        left: 50%;
        bottom: 0.55rem;
        transform: translateX(-50%);
        border: none;
        border-radius: 999px;
        background: #075e54;
        color: #fff;
        font: inherit;
        font-size: 0.78rem;
        font-weight: 700;
        padding: 0.35rem 0.75rem;
        box-shadow: 0 4px 14px rgba(7, 94, 84, 0.35);
        cursor: pointer;
        z-index: 2;
      }
      .bubble {
        max-width: 82%;
        padding: 0.5rem 0.7rem;
        border-radius: 12px;
        background: #fff;
        box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06);
      }
      .bubble .author {
        display: block;
        font-size: 0.72rem;
        font-weight: 800;
        color: #075e54;
        margin-bottom: 0.15rem;
      }
      .bubble.me {
        align-self: flex-end;
        background: #dcf8c6;
      }
      .bubble.them {
        align-self: flex-start;
      }
      .bubble p {
        margin: 0;
        white-space: pre-wrap;
        font-size: 0.92rem;
      }
      .bubble small {
        display: block;
        margin-top: 0.2rem;
        text-align: right;
        color: #64748b;
        font-size: 0.68rem;
      }
      .bubble img {
        max-width: 180px;
        border-radius: 8px;
        display: block;
        margin-bottom: 0.3rem;
      }
      .mute,
      .err {
        margin: 0.5rem 0;
        font-size: 0.9rem;
        text-align: center;
      }
      .mute {
        color: #64748b;
      }
      .err {
        color: #b91c1c;
      }
      .preview-box {
        flex: 0 0 auto;
        background: #fff;
        border-top: 1px solid #e2e8f0;
        padding: 0.55rem 0.75rem;
        display: grid;
        gap: 0.45rem;
      }
      .preview-box img {
        max-height: 120px;
        border-radius: 8px;
        object-fit: contain;
        justify-self: center;
      }
      .preview-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.45rem;
      }
      .composer {
        flex: 0 0 auto;
        display: grid;
        gap: 0.25rem;
        padding: 0.4rem 0.5rem 0.35rem;
        background: #f0f2f5;
        border-top: 1px solid #d1d5db;
      }
      .composer-row {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 0.4rem;
        align-items: end;
      }
      .emoji-bar {
        display: flex;
        gap: 0.1rem;
        overflow-x: auto;
        scrollbar-width: none;
      }
      .emoji-bar::-webkit-scrollbar {
        display: none;
      }
      .emoji-btn {
        width: 28px;
        height: 28px;
        border: 0;
        border-radius: 8px;
        background: transparent;
        font-size: 1rem;
        cursor: pointer;
        flex: 0 0 auto;
      }
      .emoji-btn:hover {
        background: #fff;
        transform: scale(1.1);
      }
      .attach {
        width: 42px;
        height: 42px;
        border-radius: 50%;
        background: #fff;
        display: grid;
        place-items: center;
        cursor: pointer;
        box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06);
      }
      .reply-box {
        min-height: 42px;
        max-height: 110px;
        height: 42px;
        resize: none;
        border: none;
        border-radius: 21px;
        padding: 0.6rem 0.9rem;
        font: inherit;
        line-height: 1.35;
        overflow-y: auto;
        outline: none;
        box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06);
      }
      .send {
        width: 42px;
        height: 42px;
        border: none;
        border-radius: 50%;
        background: #25d366;
        color: #fff;
        cursor: pointer;
        font-size: 1rem;
        display: grid;
        place-items: center;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.18);
      }
      .send:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .file-hint {
        margin: 0;
        padding: 0 0.25rem;
        font-size: 0.65rem;
        color: #64748b;
        text-align: center;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        border: 0;
      }
      @media (max-width: 720px) {
        .scw {
          right: 0.5rem;
          bottom: 0.5rem;
        }
        .panel {
          width: calc(100vw - 1rem);
          height: calc(100dvh - 5.5rem);
          max-height: calc(100dvh - 5.5rem);
        }
        .fab-label {
          display: none;
        }
      }
    `,
  ],
})
export class SupportChatWidgetComponent implements OnInit, OnDestroy {
  @ViewChild('scrollBox') scrollBox?: ElementRef<HTMLDivElement>;
  @ViewChild('draftInput') draftInput?: ElementRef<HTMLTextAreaElement>;

  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthModalService);
  private readonly session = inject(SessionService);
  private readonly facade = inject(SupportChatFacade);
  private readonly hub = inject(StorefrontChatHubService);

  private bgPoll?: ReturnType<typeof setInterval>;
  private openPoll?: ReturnType<typeof setInterval>;
  private lastKnownSoporteIds = new Set<number>();
  private bgPollInitialized = false;
  private stickToBottom = true;
  private hubReady = false;

  private readonly mini = inject(MiniCartService);
  private readonly router = inject(Router);
  readonly path = signal(this.router.url);

  readonly dockBlocked = computed(() => {
    if (this.mini.open()) return true;
    const p = this.path().split('?')[0];
    return /\/(checkout|carrito|pago)(\/|$)/.test(p);
  });
  readonly open = signal(false);
  readonly messages = signal<ChatMensaje[]>([]);
  readonly conversaciones = signal<ChatConversacion[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly unread = signal(0);
  readonly logged = signal(isLoggedIn() || this.session.loggedIn());
  readonly previewUrl = signal<string | null>(null);
  readonly hasNewBelow = signal(false);
  readonly activeConvId = signal<number | null>(null);
  readonly quickEmojis = ['😊', '👍', '🙏', '✅', '📦', '🚚', '📷', '⚠️'];

  readonly agenteAsignado = computed(() => {
    const id = this.activeConvId();
    if (id == null) return null;
    const c = this.conversaciones().find((x) => x.id === id);
    return c?.nombreAdminAsignado?.trim() || null;
  });

  draft = '';
  asunto = 'consulta';
  convPicker: number | 'nueva' = 'nueva';
  idPedidoMovil: number | null = null;
  readonly quickTopics = [
    '¿Dónde está mi pedido?',
    'Consulta de stock',
    'Quiero hacer una devolución',
    'Problema con el pago',
  ];
  private convId: number | null = null;
  private pendingFile: File | null = null;
  private draftingNueva = false;

  constructor() {
    effect(() => {
      if (this.facade.openRequest()) {
        const req = this.facade.consume();
        if (req) this.openForPedido(req.idPedido ?? null);
      }
    });

    effect(() => {
      const logged = this.session.loggedIn() || isLoggedIn();
      this.logged.set(logged);
      if (logged && !this.bgPoll) this.startBackgroundPoll();

      if (logged && this.auth.reopenSupportChat()) {
        if (this.auth.consumeReopenChat()) {
          const pedido = this.auth.consumePendingPedido();
          if (pedido != null) this.applyPedidoContext(pedido);
          queueMicrotask(() => this.openChat());
        }
      }
    });
  }

  ngOnInit(): void {
    this.logged.set(isLoggedIn() || this.session.loggedIn());
    if (this.logged()) this.startBackgroundPoll();
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => this.path.set(e.urlAfterRedirects));
  }

  ngOnDestroy(): void {
    this.stopBackgroundPoll();
    this.stopOpenPoll();
    this.revokePreview();
    void this.hub.stop();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) this.close();
  }

  openForPedido(idPedido: number | null): void {
    if (idPedido != null) this.applyPedidoContext(idPedido);
    this.openChat();
  }

  toggle(): void {
    if (this.open()) this.close();
    else this.openChat();
  }

  openChat(): void {
    this.open.set(true);
    this.logged.set(isLoggedIn() || this.session.loggedIn());
    if (this.logged()) {
      const pedido = this.auth.consumePendingPedido();
      if (pedido != null) this.applyPedidoContext(pedido);
      this.loadConversaciones();
      this.reload(true);
      this.startOpenPoll();
      void this.connectHub();
      setTimeout(() => this.draftInput?.nativeElement?.focus(), 80);
    }
  }

  private async connectHub(): Promise<void> {
    const c = await this.hub.ensureConnected();
    if (!c) return;
    this.hubReady = true;
    this.hub.onMensaje(() => {
      if (this.open()) this.reload(false);
      else this.pollBackground();
    });
    const id = this.activeConvId();
    if (id) await this.hub.joinConversacion(id);
  }

  close(): void {
    this.open.set(false);
    this.stopOpenPoll();
    this.cancelPreview();
  }

  askLogin(): void {
    this.auth.showLogin({ reopenChat: true, idPedidoMovil: this.idPedidoMovil });
    this.open.set(false);
    this.stopOpenPoll();
  }

  insertTopic(text: string): void {
    this.draft = text;
    const lower = text.toLowerCase();
    if (lower.includes('devoluc')) this.asunto = 'reclamo';
    else if (lower.includes('pedido')) this.asunto = 'pedido';
    else this.asunto = 'consulta';
    queueMicrotask(() => {
      this.autoGrowDraft();
      this.draftInput?.nativeElement?.focus();
    });
  }

  insertEmoji(emoji: string): void {
    this.draft = `${this.draft}${this.draft && !this.draft.endsWith(' ') ? ' ' : ''}${emoji} `;
    setTimeout(() => this.autoGrowDraft(), 0);
  }

  autoGrowDraft(): void {
    const el = this.draftInput?.nativeElement;
    if (!el) return;
    el.style.height = '42px';
    el.style.height = `${Math.min(el.scrollHeight, 110)}px`;
  }

  sendOnEnter(event: Event): void {
    const keyboard = event as KeyboardEvent;
    if (keyboard.shiftKey) return;
    keyboard.preventDefault();
    this.send();
  }

  private resetDraft(): void {
    this.draft = '';
    setTimeout(() => {
      const el = this.draftInput?.nativeElement;
      if (el) el.style.height = '42px';
    }, 0);
  }

  onConvPick(value: number | 'nueva'): void {
    if (value === 'nueva' || value == null) {
      this.convId = null;
      this.activeConvId.set(null);
      this.convPicker = 'nueva';
      this.draftingNueva = true;
      this.messages.set([]);
      this.hasNewBelow.set(false);
      this.error.set(null);
      this.stickToBottom = true;
      return;
    }
    this.draftingNueva = false;
    this.convId = Number(value);
    this.activeConvId.set(this.convId);
    const c = this.conversaciones().find((x) => x.id === this.convId);
    if (c) {
      this.asunto = c.asunto || 'consulta';
      if (c.idPedidoMovil) this.idPedidoMovil = c.idPedidoMovil;
    }
    this.stickToBottom = true;
    void this.hub.joinConversacion(this.convId);
    this.reload(true);
  }

  mediaUrl(url: string | null | undefined): string | null {
    return this.api.resolveMediaUrl(url);
  }

  cerrarConversacion(): void {
    if (!this.convId || this.busy()) return;
    this.busy.set(true);
    this.api.cerrarChat(this.convId).subscribe({
      next: () => {
        this.busy.set(false);
        this.draftingNueva = true;
        this.convId = null;
        this.activeConvId.set(null);
        this.convPicker = 'nueva';
        this.messages.set([]);
        this.loadConversaciones();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo cerrar');
      },
    });
  }

  onBubblesScroll(): void {
    const el = this.scrollBox?.nativeElement;
    if (!el) return;
    const dist = el.scrollHeight - el.scrollTop - el.clientHeight;
    this.stickToBottom = dist <= STICK_THRESHOLD_PX;
    if (this.stickToBottom) this.hasNewBelow.set(false);
  }

  jumpToLatest(): void {
    this.stickToBottom = true;
    this.hasNewBelow.set(false);
    this.scrollBottom(true);
  }

  send(): void {
    const text = this.draft.trim();
    if (!text || this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.api
      .enviarChat(text, this.idPedidoMovil ?? undefined, this.asunto, this.convId ?? undefined, this.draftingNueva)
      .subscribe({
        next: (m) => {
          this.busy.set(false);
          this.resetDraft();
          this.draftingNueva = false;
          if (m.idConversacion) {
            this.convId = m.idConversacion;
            this.activeConvId.set(m.idConversacion);
            this.convPicker = m.idConversacion;
          }
          this.messages.update((rows) => [...rows, m]);
          this.loadConversaciones();
          this.stickToBottom = true;
          setTimeout(() => this.scrollBottom(true), 40);
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set(e?.error?.error || e.message || 'No se pudo enviar');
        },
      });
  }

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (file.size > MAX_FILE_BYTES) {
      this.error.set('La foto supera 5 MB.');
      return;
    }
    this.cancelPreview();
    this.pendingFile = file;
    this.previewUrl.set(URL.createObjectURL(file));
  }

  confirmSendPhoto(): void {
    const file = this.pendingFile;
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.api
      .enviarChatFoto(
        file,
        this.draft.trim() || undefined,
        this.asunto,
        this.convId ?? undefined,
        this.idPedidoMovil ?? undefined,
        this.draftingNueva,
      )
      .subscribe({
        next: (m) => {
          this.busy.set(false);
          this.resetDraft();
          this.draftingNueva = false;
          this.cancelPreview();
          if (m.idConversacion) {
            this.convId = m.idConversacion;
            this.activeConvId.set(m.idConversacion);
            this.convPicker = m.idConversacion;
          }
          this.messages.update((rows) => [...rows, m]);
          this.loadConversaciones();
          this.stickToBottom = true;
          setTimeout(() => this.scrollBottom(true), 40);
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set(e?.error?.error || e.message || 'No se pudo subir la foto');
        },
      });
  }

  cancelPreview(): void {
    this.revokePreview();
    this.pendingFile = null;
  }

  private applyPedidoContext(idPedido: number): void {
    this.idPedidoMovil = idPedido;
    this.asunto = 'pedido';
    this.convId = null;
    this.activeConvId.set(null);
    this.convPicker = 'nueva';
  }

  private loadConversaciones(): void {
    this.api.getChatConversaciones().subscribe({
      next: (rows) => {
        this.conversaciones.set(rows);
        if (this.convId && !rows.some((c) => c.id === this.convId)) {
          this.convPicker = this.convId;
        }
      },
      error: () => undefined,
    });
  }

  private reload(initial = false): void {
    if (this.draftingNueva || this.convId == null) {
      if (initial) this.loading.set(false);
      return;
    }
    if (initial) this.loading.set(true);
    this.api.getChat(this.idPedidoMovil ?? undefined, this.convId ?? undefined).subscribe({
      next: (rows) => {
        this.loading.set(false);
        const prev = this.messages();
        const prevLast = prev.at(-1)?.id;
        const nextLast = rows.at(-1)?.id;
        const grew = rows.length > prev.length || (nextLast != null && nextLast !== prevLast);
        this.messages.set(rows);
        const last = rows[rows.length - 1];
        if (last?.idConversacion) {
          this.convId = last.idConversacion;
          this.activeConvId.set(last.idConversacion);
          this.convPicker = last.idConversacion;
        }
        if (this.open()) {
          this.unread.set(0);
          if (this.convId) {
            this.api.marcarChatLeidos(this.convId).subscribe({ error: () => undefined });
          }
        }
        if (initial || this.stickToBottom) {
          this.hasNewBelow.set(false);
          setTimeout(() => this.scrollBottom(!initial), 50);
        } else if (grew) {
          this.hasNewBelow.set(true);
        }
      },
      error: (e) => {
        this.loading.set(false);
        if (e?.status === 401) {
          this.onAuthLost();
          this.error.set('Sesión vencida. Volvé a iniciar sesión para chatear.');
          return;
        }
        if (e?.status === 429) return;
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  private pollBackground(): void {
    if (!(isLoggedIn() || this.session.loggedIn())) {
      this.stopBackgroundPoll();
      this.logged.set(false);
      return;
    }

    this.api.getChatConversaciones().subscribe({
      next: (rows) => {
        this.conversaciones.set(rows);
        const total = rows.reduce((s, c) => s + (c.noLeidosCliente ?? 0), 0);
        const prev = this.unread();

        if (!this.open()) {
          this.api.getChat().subscribe({
            next: (msgs) => {
              const soporteUnread = msgs.filter((m) => m.origen === 'soporte' && !m.leido);
              const newIds = soporteUnread.map((m) => m.id).filter((id) => !this.lastKnownSoporteIds.has(id));
              if (this.bgPollInitialized && newIds.length > 0) this.playBeep();
              this.lastKnownSoporteIds = new Set(soporteUnread.map((m) => m.id));
              this.bgPollInitialized = true;
              const badge = Math.max(total, soporteUnread.length);
              if (badge > prev) this.playBeep();
              this.unread.set(badge);
            },
            error: (e) => {
              if (e?.status === 401) {
                this.onAuthLost();
                return;
              }
              if (total > prev) this.playBeep();
              this.unread.set(total);
            },
          });
        } else if (this.convId) {
          this.reload(false);
        }
      },
      error: (e) => {
        if (e?.status === 401) this.onAuthLost();
      },
    });
  }

  private onAuthLost(): void {
    this.stopBackgroundPoll();
    this.stopOpenPoll();
    this.logged.set(false);
    this.unread.set(0);
  }

  private startBackgroundPoll(): void {
    this.stopBackgroundPoll();
    this.pollBackground();
    this.bgPoll = setInterval(() => this.pollBackground(), BG_POLL_MS);
  }

  private stopBackgroundPoll(): void {
    if (this.bgPoll) {
      clearInterval(this.bgPoll);
      this.bgPoll = undefined;
    }
  }

  private startOpenPoll(): void {
    this.stopOpenPoll();
    let n = 0;
    this.openPoll = setInterval(() => {
      if (!this.open() || !(isLoggedIn() || this.session.loggedIn())) return;
      if (this.hubReady) {
        // SignalR activo: solo refrescar lista de convos ocasionalmente
        n += 1;
        if (n % 3 === 0) this.loadConversaciones();
        return;
      }
      if (this.draftingNueva || this.convId == null) {
        this.loadConversaciones();
        return;
      }
      this.reload(false);
      n += 1;
      if (n % 2 === 0) this.loadConversaciones();
    }, OPEN_POLL_MS);
  }

  private stopOpenPoll(): void {
    if (this.openPoll) {
      clearInterval(this.openPoll);
      this.openPoll = undefined;
    }
  }

  private playBeep(): void {
    try {
      const ctx = new AudioContext();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.frequency.value = 880;
      gain.gain.value = 0.08;
      osc.start();
      osc.stop(ctx.currentTime + 0.12);
      osc.onended = () => void ctx.close();
    } catch {
      /* audio no disponible */
    }
  }

  private revokePreview(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
    this.previewUrl.set(null);
  }

  private scrollBottom(smooth = false): void {
    const el = this.scrollBox?.nativeElement;
    if (!el) return;
    if (smooth) {
      el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    } else {
      el.scrollTop = el.scrollHeight;
    }
  }
}
