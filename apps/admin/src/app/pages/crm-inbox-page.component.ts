import { DatePipe } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdminApiService,
  AdminChatConversacion,
  AdminChatMensaje,
} from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { AdminChatHubService } from '../core/admin-chat-hub.service';
import { CrmBadgeService } from '../core/crm-badge.service';

const QUICK_REPLIES = [
  '👋 ¡Hola! Gracias por contactarnos.',
  '🔎 Estamos revisando tu pedido.',
  '📷 ¿Podés enviarnos una foto?',
  '✅ Tu reclamo fue registrado.',
  '🚚 Te confirmamos el estado del envío en breve.',
];

const QUICK_EMOJIS = ['😊', '👍', '🙏', '✅', '📦', '🚚', '📷', '⚠️'];

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <div class="crm-topbar">
      <div class="crm-title">
        <h1>Atención al cliente</h1>
        <div class="crm-kpis">
          <button type="button" class="kpi kpi-open" [class.on]="queueFilter === 'abiertas'" (click)="setFilter('abiertas')">
            <span class="kpi-icon">💬</span>
            <span><strong>{{ countAbiertas() }}</strong> Abiertas</span>
          </button>
          <button type="button" class="kpi kpi-alert" [class.on]="queueFilter === 'sin_asignar'" (click)="setFilter('sin_asignar')">
            <span class="kpi-icon">⏳</span>
            <span><strong>{{ countSinAsignar() }}</strong> Sin asignar</span>
          </button>
          <button type="button" class="kpi kpi-mine" [class.on]="queueFilter === 'mis_asignadas'" (click)="setFilter('mis_asignadas')">
            <span class="kpi-icon">🎧</span>
            <span>Mis chats</span>
          </button>
        </div>
      </div>
      <div class="crm-tools">
        <select [(ngModel)]="queueFilter" (ngModelChange)="applyQueueFilter()">
          <option value="todas">Todas</option>
          <option value="abiertas">Abiertas</option>
          <option value="sin_asignar">Sin asignar</option>
          <option value="mis_asignadas">Mis asignadas</option>
          <option value="cerradas">Cerradas</option>
        </select>
        <input placeholder="Buscar cliente o asunto…" [(ngModel)]="q" (ngModelChange)="onSearchChange()" (keyup.enter)="loadInbox()" />
        <button type="button" class="icon-btn" (click)="loadInbox()" title="Actualizar">
          <i class="fas fa-sync-alt"></i>
        </button>
      </div>
    </div>

    @if (ok()) {
      <div class="alert alert-success alert-dismissible py-2 mb-2">
        <button type="button" class="close" (click)="ok.set(null)"><span>&times;</span></button>
        {{ ok() }}
      </div>
    }
    @if (error()) {
      <div class="alert alert-danger alert-dismissible py-2 mb-2">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }

    <div class="crm-shell">
      <div class="crm-layout">
        <aside class="inbox">
          @for (c of inbox(); track c.id) {
            <button type="button" class="thread"
                    [class.on]="selected()?.id === c.id"
                    [class.needs-attention]="c.noLeidosSoporte > 0"
                    [class.unassigned]="!c.idAdminAsignado"
                    (click)="open(c)">
              <div class="avatar">{{ initials(c.nombreCliente || c.email) }}</div>
              <div class="meta">
                <div class="top">
                  <strong>{{ c.nombreCliente || c.email }}</strong>
                  <small>{{ c.fechaUltimoMensaje | date: 'shortTime' }}</small>
                </div>
                <div class="preview">
                  <span class="tag">{{ subjectEmoji(c.asunto) }} {{ c.asunto }}</span>
                  @if (c.nombreAdminAsignado) {
                    <span class="tag agent">🎧 {{ c.nombreAdminAsignado }}</span>
                  } @else {
                    <span class="tag waiting">⏳ Sin asignar</span>
                  }
                  {{ c.previewUltimoMensaje || '—' }}
                </div>
              </div>
              @if (c.noLeidosSoporte > 0) {
                <span class="unread">{{ c.noLeidosSoporte }}</span>
              }
            </button>
          } @empty {
            <div class="dx-empty py-4">
              @if (loadingInbox()) {
                <i class="fas fa-spinner fa-spin"></i>
                <p>Cargando conversaciones…</p>
              } @else {
                <i class="fas fa-headset"></i>
                <p>Sin conversaciones con ese filtro.</p>
                <small class="text-muted">Probá “Sin asignar” o “Abiertas”.</small>
              }
            </div>
          }
        </aside>

        <section class="chat">
          @if (brandingLogo(); as logo) {
            <img class="crm-watermark" [src]="logo" alt="" aria-hidden="true" />
          }
          @if (selected(); as sel) {
            <header class="chat-head">
              <div class="avatar avatar-head">{{ initials(sel.nombreCliente || sel.email) }}</div>
              <div class="who">
                <strong>{{ sel.nombreCliente || sel.email }}</strong>
                <small>✉️ {{ sel.email }} · {{ stateLabel(sel.estado) }}</small>
                @if (sel.nombreAdminAsignado) {
                  <span class="agent-tag">Atendido por {{ sel.nombreAdminAsignado }}</span>
                } @else {
                  <span class="agent-tag muted">Sin asignar</span>
                }
              </div>
              <div class="actions">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-light"
                  (click)="asignarme()"
                  [disabled]="isMine(sel)"
                  [title]="isMine(sel) ? 'Ya está asignado a vos' : 'Asignarme esta conversación'"
                >
                  {{ isMine(sel) ? '🎧 Asignado a vos' : '🎧 Asignarme' }}
                </button>
                <button type="button" class="btn btn-sm btn-warning" (click)="cerrarChat()" title="Cerrar conversación">
                  ✅ Resolver
                </button>
              </div>
            </header>

            <div class="bubbles-wrap">
              <div class="bubbles" #scrollBox (scroll)="onBubblesScroll()">
                @if (loadingMessages() && messages().length === 0) {
                  <p class="empty">Cargando mensajes…</p>
                }
                @for (m of messages(); track m.id) {
                  <div class="bubble" [class.me]="m.origen === 'soporte'" [class.them]="m.origen === 'cliente'">
                    @if (m.origen === 'soporte') {
                      <span class="author">{{ m.nombreAutor || 'Soporte' }}</span>
                    } @else {
                      <span class="author client">{{ sel.nombreCliente || 'Cliente' }}</span>
                    }
                    @if (m.adjunto?.url) {
                      <a [href]="mediaUrl(m.adjunto!.url)!" target="_blank" rel="noopener">
                        <img [src]="mediaUrl(m.adjunto!.url)!" [alt]="m.adjunto!.nombreArchivo || 'foto'" loading="lazy" />
                      </a>
                    }
                    @if ((m.mensaje && m.tipoMensaje !== 'imagen') || (m.mensaje && m.mensaje !== '[Foto]')) {
                      <p>{{ m.mensaje }}</p>
                    }
                    <small>{{ m.fechaMensaje | date: 'short' }}</small>
                  </div>
                }
              </div>
              @if (hasNewBelow()) {
                <button type="button" class="jump-latest" (click)="jumpToLatest()">Nuevos mensajes ↓</button>
              }
            </div>

            <footer class="composer">
              @if (sel.estado === 'cerrada') {
                <div class="alert alert-secondary mb-2 py-2">
                  Conversación cerrada.
                  <button type="button" class="btn btn-xs btn-outline-primary ml-2" (click)="reabrirChat()">
                    Reabrir
                  </button>
                </div>
              } @else {
                <div class="quick-replies">
                  @for (qr of quickReplies; track qr) {
                    <button type="button" class="chip" (click)="insertQuick(qr)">{{ qr }}</button>
                  }
                </div>
                <div class="emoji-bar" aria-label="Emojis rápidos">
                  @for (emoji of quickEmojis; track emoji) {
                    <button type="button" class="emoji-btn" (click)="insertEmoji(emoji)" [attr.aria-label]="'Insertar ' + emoji">
                      {{ emoji }}
                    </button>
                  }
                </div>
                <div class="composer-row">
                  <label class="attach" title="Enviar foto">
                    <i class="fas fa-paperclip"></i>
                    <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden (change)="onFile($event)" />
                  </label>
                  <textarea #draftBox class="form-control reply-box" [(ngModel)]="draft"
                            placeholder="Escribí un mensaje"
                            rows="1"
                            (input)="autoGrowDraft()"
                            (keydown.enter)="sendOnEnter($event)"></textarea>
                  <button type="button" class="send-btn" (click)="send()" [disabled]="busy() || !draft.trim()" title="Enviar">
                    <i class="fas fa-paper-plane"></i>
                  </button>
                </div>
              }
            </footer>
          } @else {
            <div class="placeholder">
              <i class="fas fa-comments"></i>
              <p>Seleccioná una conversación a la izquierda.</p>
              <small>Tip: filtrá por “Sin asignar” para tomar la siguiente.</small>
            </div>
          }
        </section>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        height: 100%;
        max-height: 100%;
        min-height: 0;
        overflow: hidden;
        box-sizing: border-box;
      }
      .crm-topbar {
        flex: 0 0 auto;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.65rem;
        flex-wrap: wrap;
        margin-bottom: 0.45rem;
      }
      .crm-title {
        display: flex;
        align-items: center;
        gap: 0.65rem;
        flex-wrap: wrap;
        min-width: 0;
      }
      .crm-title h1 {
        margin: 0;
        font-size: 1.05rem;
        font-weight: 700;
        letter-spacing: -0.01em;
        white-space: nowrap;
      }
      .crm-kpis {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
      }
      .kpi {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        min-height: 32px;
        padding: 0.2rem 0.55rem;
        border: 1px solid #e2e8f0;
        border-radius: 999px;
        background: #fff;
        color: #1e293b;
        font-size: 0.78rem;
        cursor: pointer;
      }
      .kpi strong { font-weight: 800; }
      .kpi-icon { font-size: 0.95rem; line-height: 1; }
      .kpi-open { border-color: #99f6e4; background: #f0fdfa; }
      .kpi-alert { border-color: #fcd34d; background: #fffbeb; }
      .kpi-mine { border-color: #bfdbfe; background: #eff6ff; }
      .kpi.on { box-shadow: 0 0 0 2px #0f766e inset; font-weight: 700; }
      .crm-tools {
        display: flex;
        align-items: center;
        gap: 0.35rem;
        flex-wrap: wrap;
      }
      .crm-tools select,
      .crm-tools input {
        height: 32px;
        border: 1px solid #ced4da;
        border-radius: 8px;
        padding: 0 0.55rem;
        font-size: 0.82rem;
        background: #fff;
      }
      .crm-tools input { width: min(200px, 42vw); }
      .crm-tools .icon-btn {
        width: 32px; height: 32px; border-radius: 8px;
        border: 1px solid #ced4da; background: #fff; cursor: pointer;
      }
      .alert { flex: 0 0 auto; }
      .crm-shell {
        flex: 1 1 auto;
        min-height: 0;
        height: auto;
        display: flex;
        flex-direction: column;
        overflow: hidden;
        border: 1px solid #dee2e6;
        border-radius: 12px;
        background: #fff;
        box-shadow: 0 1px 2px rgba(0,0,0,.04);
      }
      .crm-layout {
        flex: 1;
        min-height: 0;
        height: 100%;
        display: grid;
        grid-template-columns: minmax(240px, 300px) 1fr;
      }
      @media (max-width: 900px) {
        .crm-layout {
          grid-template-columns: 1fr;
          grid-template-rows: minmax(140px, 28%) 1fr;
        }
      }
      .inbox {
        border-right: 1px solid #e9ecef;
        overflow: auto;
        height: 100%;
        min-height: 0;
        background: #f8fafc;
      }
      .thread {
        width: 100%;
        display: grid;
        grid-template-columns: 38px 1fr auto;
        gap: 0.55rem;
        align-items: center;
        border: 0;
        border-bottom: 1px solid #eef2f7;
        background: transparent;
        text-align: left;
        padding: 0.55rem 0.7rem;
        cursor: pointer;
        position: relative;
      }
      .thread.on, .thread:hover { background: #e6f7f2; }
      .thread.on { box-shadow: inset 4px 0 #0f9d78; }
      .thread.needs-attention .top strong { color: #0f766e; font-weight: 800; }
      .thread.needs-attention { background: #f0fdfa; }
      .thread.unassigned::after {
        content: '';
        position: absolute;
        left: 0;
        top: 12px;
        bottom: 12px;
        width: 3px;
        border-radius: 0 3px 3px 0;
        background: #f59e0b;
      }
      .avatar {
        width: 38px; height: 38px; border-radius: 50%;
        background: #25d366; color: #fff; font-weight: 700; font-size: 0.78rem;
        display: grid; place-items: center;
      }
      .meta { min-width: 0; }
      .top { display: flex; justify-content: space-between; gap: 0.4rem; }
      .top strong { font-size: 0.86rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
      .preview { font-size: 0.75rem; color: #6c757d; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
      .tag {
        display: inline-block; font-size: 0.65rem; font-weight: 700;
        background: #e2e8f0; color: #334155; border-radius: 4px; padding: 0 0.25rem; margin-right: 0.2rem;
      }
      .tag.agent { background: #dbeafe; color: #1d4ed8; }
      .tag.waiting { background: #fef3c7; color: #92400e; }
      .unread {
        background: #25d366; color: #fff; border-radius: 999px;
        min-width: 1.25rem; height: 1.25rem; font-size: 0.7rem;
        display: grid; place-items: center; font-weight: 700;
      }
      .chat {
        display: flex; flex-direction: column; background: #ece5dd;
        height: 100%; min-height: 0; overflow: hidden;
        position: relative;
        isolation: isolate;
      }
      .crm-watermark {
        position: absolute;
        z-index: 0;
        width: min(42%, 340px);
        max-height: 42%;
        object-fit: contain;
        left: 50%;
        top: 42%;
        transform: translate(-50%, -50%);
        opacity: .055;
        filter: grayscale(1);
        pointer-events: none;
      }
      .chat-head {
        flex: 0 0 auto;
        display: flex; justify-content: space-between; gap: 0.75rem; flex-wrap: wrap;
        padding: 0.55rem 0.85rem; background: linear-gradient(110deg, #075e54, #0f766e); color: #fff;
        align-items: center;
        z-index: 2;
      }
      .avatar-head { width: 42px; height: 42px; background: rgba(255,255,255,.2); border: 2px solid rgba(255,255,255,.45); }
      .who { flex: 1; display: grid; gap: 0.1rem; min-width: 0; }
      .who strong { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
      .who small { color: #c8e6c9; font-size: 0.75rem; }
      .agent-tag {
        display: inline-block; margin-top: 0.15rem; font-size: 0.72rem; font-weight: 700;
        background: rgba(255,255,255,.15); padding: 0.1rem 0.45rem; border-radius: 999px;
      }
      .agent-tag.muted { opacity: 0.75; font-weight: 500; }
      .actions { display: flex; gap: 0.35rem; align-items: center; flex-wrap: wrap; }
      .bubbles-wrap {
        flex: 1 1 auto; min-height: 0; position: relative;
        display: flex; flex-direction: column; overflow: hidden;
        z-index: 1;
      }
      .bubbles {
        flex: 1 1 auto; min-height: 0;
        overflow-y: auto; overflow-x: hidden; padding: 0.85rem;
        display: flex; flex-direction: column; gap: 0.5rem;
        scroll-behavior: smooth; -webkit-overflow-scrolling: touch;
        background:
          radial-gradient(circle at 1px 1px, rgba(15, 118, 110, .045) 1px, transparent 0) 0 0 / 24px 24px;
      }
      .jump-latest {
        position: absolute; left: 50%; bottom: 0.55rem; transform: translateX(-50%);
        border: 0; border-radius: 999px; background: #075e54; color: #fff;
        font-size: 0.78rem; font-weight: 700; padding: 0.3rem 0.7rem;
        box-shadow: 0 4px 14px rgba(0,0,0,.2); cursor: pointer; z-index: 2;
      }
      .bubble {
        max-width: min(78%, 520px); padding: 0.55rem 0.7rem;
        border-radius: 12px; background: #fff; box-shadow: 0 1px 1px rgba(11,20,26,.13);
      }
      .bubble.me { align-self: flex-end; background: #d9fdd3; border-bottom-right-radius: 4px; }
      .bubble.them { align-self: flex-start; border-bottom-left-radius: 4px; }
      .bubble .author {
        display: block; font-size: 0.7rem; font-weight: 800; color: #075e54; margin-bottom: 0.12rem;
      }
      .bubble .author.client { color: #334155; }
      .bubble img { max-width: 220px; border-radius: 8px; display: block; margin-bottom: 0.3rem; }
      .bubble p { margin: 0; white-space: pre-wrap; word-break: break-word; }
      .bubble small { display: block; margin-top: 0.2rem; color: #6c757d; font-size: 0.7rem; text-align: right; }
      .composer {
        flex: 0 0 auto;
        display: grid;
        gap: 0.3rem;
        padding: 0.4rem 0.5rem 0.55rem;
        background: #f0f2f5;
        border-top: 1px solid #d1d5db;
        z-index: 5;
      }
      .composer-row {
        display: grid; grid-template-columns: auto 1fr auto; gap: 0.4rem;
        align-items: end;
      }
      .quick-replies {
        display: flex; flex-wrap: nowrap; gap: 0.3rem;
        overflow-x: auto; padding-bottom: 0.1rem;
        scrollbar-width: thin;
      }
      .chip {
        flex: 0 0 auto;
        border: 1px solid #cbd5e1; background: #fff; border-radius: 999px;
        padding: 0.15rem 0.5rem; font-size: 0.72rem; cursor: pointer; color: #334155;
        white-space: nowrap;
      }
      .chip:hover { background: #e8f5e9; border-color: #25d366; }
      .emoji-bar {
        display: flex;
        align-items: center;
        gap: .15rem;
        overflow-x: auto;
        scrollbar-width: none;
      }
      .emoji-bar::-webkit-scrollbar { display: none; }
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
      .emoji-btn:hover { background: #fff; transform: scale(1.12); }
      .reply-box {
        min-height: 42px;
        max-height: 120px;
        height: 42px;
        resize: none;
        border-radius: 21px;
        border: 0;
        padding: 0.6rem 0.9rem;
        line-height: 1.35;
        overflow-y: auto;
        box-shadow: 0 1px 1px rgba(0,0,0,.06);
      }
      .attach {
        width: 42px; height: 42px; border-radius: 50%; background: #fff;
        display: grid; place-items: center; cursor: pointer; color: #54656f;
        box-shadow: 0 1px 1px rgba(0,0,0,.06);
      }
      .send-btn {
        width: 42px; height: 42px; border-radius: 50%; border: 0;
        background: #25d366; color: #fff; cursor: pointer;
        display: grid; place-items: center;
        box-shadow: 0 1px 3px rgba(0,0,0,.18);
      }
      .send-btn:disabled { opacity: .45; cursor: default; }
      .send-btn:not(:disabled):hover { filter: brightness(1.05); }
      .placeholder {
        flex: 1; display: grid; place-items: center; text-align: center; color: #54656f; gap: 0.35rem;
        padding: 1.5rem;
      }
      .placeholder i { font-size: 2.4rem; color: #075e54; }
      .placeholder small { color: #94a3b8; font-size: 0.8rem; max-width: 22rem; }
      .empty { padding: 1.5rem; text-align: center; color: #6c757d; font-size: 0.9rem; }
      @media (max-width: 650px) {
        .chat-head { align-items: flex-start; }
        .bubble { max-width: 90%; }
        .crm-title h1 { font-size: 0.95rem; }
      }
    `,
  ],
})
export class CrmInboxPageComponent implements OnInit, OnDestroy {
  @ViewChild('scrollBox') scrollBox?: ElementRef<HTMLDivElement>;
  @ViewChild('draftBox') draftBox?: ElementRef<HTMLTextAreaElement>;

  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private readonly hub = inject(AdminChatHubService);
  private readonly crmBadge = inject(CrmBadgeService);
  private poll?: ReturnType<typeof setInterval>;
  private inboxPoll?: ReturnType<typeof setInterval>;
  private allInbox = signal<AdminChatConversacion[]>([]);
  private stickToBottom = true;
  private tick = 0;
  private hubReady = false;

  readonly inbox = signal<AdminChatConversacion[]>([]);
  readonly messages = signal<AdminChatMensaje[]>([]);
  readonly selected = signal<AdminChatConversacion | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly busy = signal(false);
  readonly loadingInbox = signal(false);
  readonly loadingMessages = signal(false);
  readonly countAbiertas = signal(0);
  readonly countSinAsignar = signal(0);
  readonly hasNewBelow = signal(false);
  readonly brandingLogo = signal<string | null>(null);
  readonly quickReplies = QUICK_REPLIES;
  readonly quickEmojis = QUICK_EMOJIS;
  private searchTimer?: ReturnType<typeof setTimeout>;

  queueFilter = 'todas';
  q = '';
  draft = '';

  ngOnInit(): void {
    document.body.classList.add('crm-wa-mode');
    this.loadInbox();
    this.api.getBranding().subscribe({
      next: (branding) => this.brandingLogo.set(this.mediaUrl(branding?.logoUrl)),
      error: () => this.brandingLogo.set(null),
    });
    void this.startHub();
    // Fallback poll más lento si SignalR no conecta.
    this.poll = setInterval(() => {
      if (this.hubReady) return;
      const s = this.selected();
      if (s) this.loadMessages(s.id, false);
      this.tick += 1;
      if (this.tick % 2 === 0) this.loadInbox(false);
    }, 15_000);
  }

  ngOnDestroy(): void {
    document.body.classList.remove('crm-wa-mode');
    if (this.poll) clearInterval(this.poll);
    if (this.inboxPoll) clearInterval(this.inboxPoll);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    void this.hub.stop();
  }

  private async startHub(): Promise<void> {
    const conn = await this.hub.ensureConnected();
    if (!conn) return;
    this.hubReady = true;
    const idEmpresa = this.session.idEmpresa();
    if (idEmpresa) await this.hub.joinEmpresa(idEmpresa);
    this.hub.onConversacionUpdated(() => {
      this.loadInbox(false);
      const s = this.selected();
      if (s) this.loadMessages(s.id, false);
    });
    this.hub.onMensaje(() => {
      const s = this.selected();
      if (s) this.loadMessages(s.id, false);
      this.loadInbox(false);
    });
  }

  initials(name: string): string {
    const p = (name || '?').trim().split(/\s+/);
    return ((p[0]?.[0] || '?') + (p[1]?.[0] || '')).toUpperCase();
  }

  subjectEmoji(subject: string | null | undefined): string {
    const value = (subject || '').toLowerCase();
    if (value.includes('pedido') || value.includes('orden')) return '📦';
    if (value.includes('envío') || value.includes('envio') || value.includes('delivery')) return '🚚';
    if (value.includes('pago') || value.includes('factura')) return '💳';
    if (value.includes('reclamo') || value.includes('problema')) return '⚠️';
    if (value.includes('producto') || value.includes('stock')) return '🛍️';
    return '💬';
  }

  stateLabel(state: string): string {
    switch (state) {
      case 'abierta': return '🟢 Abierta';
      case 'en_curso': return '🟡 En atención';
      case 'cerrada': return '✅ Resuelta';
      default: return state;
    }
  }

  setFilter(filter: string): void {
    this.queueFilter = this.queueFilter === filter ? 'todas' : filter;
    this.applyQueueFilter();
  }

  onSearchChange(): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.loadInbox(false), 300);
  }

  isMine(c: AdminChatConversacion): boolean {
    const myId = this.session.userId();
    if (myId != null && c.idAdminAsignado === myId) return true;
    const me = this.session.displayName().trim().toLowerCase();
    const name = c.nombreAdminAsignado?.trim().toLowerCase() || '';
    return !!me && name === me;
  }

  loadInbox(showErr = true): void {
    if (!this.allInbox().length) this.loadingInbox.set(true);
    this.api.listChatConversaciones(undefined, this.q || undefined).subscribe({
      next: (rows) => {
        this.loadingInbox.set(false);
        this.allInbox.set(rows);
        this.countAbiertas.set(rows.filter((c) => c.estado === 'abierta' || c.estado === 'en_curso').length);
        this.countSinAsignar.set(
          rows.filter(
            (c) =>
              (c.estado === 'abierta' || c.estado === 'en_curso') && !c.idAdminAsignado,
          ).length,
        );
        this.applyQueueFilter();
        const sel = this.selected();
        if (sel) {
          const fresh = rows.find((c) => c.id === sel.id);
          if (fresh) this.selected.set(fresh);
        }
        const total = rows.reduce((s, c) => s + (c.noLeidosSoporte ?? 0), 0);
        this.crmBadge.unread.set(total);
      },
      error: (e) => {
        this.loadingInbox.set(false);
        if (e?.status === 429) return;
        if (showErr) this.error.set(e?.error?.error || e.message);
      },
    });
  }

  applyQueueFilter(): void {
    let rows = this.allInbox();
    const myId = this.session.userId();
    const me = this.session.displayName().trim().toLowerCase();
    switch (this.queueFilter) {
      case 'abiertas':
        rows = rows.filter((c) => c.estado === 'abierta' || c.estado === 'en_curso');
        break;
      case 'sin_asignar':
        rows = rows.filter((c) => !c.idAdminAsignado);
        break;
      case 'mis_asignadas':
        rows = rows.filter((c) => {
          if (c.idAdminAsignado == null) return false;
          if (myId != null && c.idAdminAsignado === myId) return true;
          const name = c.nombreAdminAsignado?.trim().toLowerCase() || '';
          return !!me && name === me;
        });
        break;
      case 'cerradas':
        rows = rows.filter((c) => c.estado === 'cerrada');
        break;
    }
    this.inbox.set(rows);
  }

  insertQuick(text: string): void {
    this.draft = this.draft.trim() ? `${this.draft.trim()} ${text}` : text;
    setTimeout(() => this.autoGrowDraft(), 0);
  }

  insertEmoji(emoji: string): void {
    this.draft = `${this.draft}${this.draft && !this.draft.endsWith(' ') ? ' ' : ''}${emoji} `;
    setTimeout(() => this.autoGrowDraft(), 0);
  }

  autoGrowDraft(): void {
    const el = this.draftBox?.nativeElement;
    if (!el) return;
    el.style.height = '42px';
    el.style.height = `${Math.min(el.scrollHeight, 120)}px`;
  }

  private resetDraftBox(): void {
    this.draft = '';
    setTimeout(() => {
      const el = this.draftBox?.nativeElement;
      if (el) el.style.height = '42px';
    }, 0);
  }

  sendOnEnter(event: Event): void {
    const keyboard = event as KeyboardEvent;
    if (keyboard.shiftKey) return;
    keyboard.preventDefault();
    this.send();
  }

  open(c: AdminChatConversacion): void {
    this.selected.set(c);
    this.messages.set([]);
    this.stickToBottom = true;
    this.hasNewBelow.set(false);
    void this.hub.joinConversacion(c.id);
    this.loadMessages(c.id, true);
  }

  onBubblesScroll(): void {
    const el = this.scrollBox?.nativeElement;
    if (!el) return;
    const dist = el.scrollHeight - el.scrollTop - el.clientHeight;
    this.stickToBottom = dist <= 72;
    if (this.stickToBottom) this.hasNewBelow.set(false);
  }

  jumpToLatest(): void {
    this.stickToBottom = true;
    this.hasNewBelow.set(false);
    this.scrollBottom(true);
  }

  loadMessages(id: number, forceScroll = true): void {
    if (forceScroll) this.loadingMessages.set(true);
    this.api.getChatConversacion(id).subscribe({
      next: (rows) => {
        if (this.selected()?.id !== id) return;
        this.loadingMessages.set(false);
        const prev = this.messages();
        const grew =
          rows.length > prev.length || (rows.at(-1)?.id != null && rows.at(-1)?.id !== prev.at(-1)?.id);
        this.messages.set(rows);
        if (forceScroll || this.stickToBottom) {
          this.hasNewBelow.set(false);
          setTimeout(() => this.scrollBottom(!forceScroll), 50);
        } else if (grew) {
          this.hasNewBelow.set(true);
        }
        // Solo refrescar inbox al abrir (no en cada poll) para no saturar rate limit.
        if (forceScroll) this.loadInbox(false);
      },
      error: (e) => {
        this.loadingMessages.set(false);
        if (e?.status === 429) return;
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  send(): void {
    const s = this.selected();
    const text = this.draft.trim();
    if (!s || !text) return;
    this.busy.set(true);
    this.api.responderConversacion(s.id, text).subscribe({
      next: () => {
        this.busy.set(false);
        this.resetDraftBox();
        this.stickToBottom = true;
        this.loadMessages(s.id, true);
        this.crmBadge.refresh();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  onFile(ev: Event): void {
    const s = this.selected();
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!s || !file) return;
    this.busy.set(true);
    this.api.responderConversacionFoto(s.id, file, this.draft.trim() || undefined).subscribe({
      next: () => {
        this.busy.set(false);
        this.resetDraftBox();
        input.value = '';
        this.stickToBottom = true;
        this.loadMessages(s.id, true);
        this.crmBadge.refresh();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  setEstado(estado: string): void {
    const s = this.selected();
    if (!s) return;
    this.api.cambiarEstadoConversacion(s.id, estado).subscribe({
      next: () => {
        this.selected.set({ ...s, estado });
        this.loadInbox(false);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  asignarme(): void {
    const s = this.selected();
    if (!s) return;
    const idAdmin = this.session.userId();
    this.api
      .asignarConversacion(s.id, {
        idAdmin: idAdmin ?? undefined,
        nombreAdmin: this.session.displayName(),
      })
      .subscribe({
        next: () => {
          this.ok.set('Conversación asignada a vos.');
          this.loadInbox();
          this.crmBadge.refresh();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  cerrarChat(): void {
    const s = this.selected();
    if (!s) return;
    if (!confirm('¿Resolver esta conversación? El cliente no va a poder seguir escribiendo hasta reabrirla.')) return;
    this.api.cambiarEstadoConversacion(s.id, 'cerrada').subscribe({
      next: () => {
        this.selected.set({ ...s, estado: 'cerrada' });
        this.ok.set('Conversación resuelta.');
        this.loadInbox(false);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  reabrirChat(): void {
    const s = this.selected();
    if (!s) return;
    this.api.cambiarEstadoConversacion(s.id, 'abierta').subscribe({
      next: () => {
        this.selected.set({ ...s, estado: 'abierta' });
        this.loadInbox(false);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  mediaUrl(url: string | null | undefined): string | null {
    return this.api.resolveMediaUrl(url);
  }

  private scrollBottom(smooth = false): void {
    const el = this.scrollBox?.nativeElement;
    if (!el) return;
    if (smooth) el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    else el.scrollTop = el.scrollHeight;
  }
}
