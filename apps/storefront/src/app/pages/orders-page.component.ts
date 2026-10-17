import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountOrder, ApiService, Seguimiento, isLoggedIn, openPdfBlob, readPdfError } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { StorefrontChatHubService } from '../core/storefront-chat-hub.service';
import { SupportChatFacade } from '../core/support-chat.facade';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from '../shared/dx-icon.component';
import { AccountOrdersTabsComponent } from '../shared/account-orders-tabs.component';
import { TrackingTimelineComponent } from '../shared/tracking-timeline.component';

type OrderFilter = 'todos' | 'activos' | 'cancelados' | 'facturados' | 'delivery' | 'pickup';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, FormsModule, DxIconComponent, TrackingTimelineComponent, AccountOrdersTabsComponent],
  template: `
    <section class="orders" aria-labelledby="orders-title">
      <header class="toolbar">
        <div>
          <h1 id="orders-title"><dx-icon name="package" [size]="26" /> Mis pedidos</h1>
          <p class="sub">Historial, seguimiento, recompra y soporte por pedido.</p>
        </div>
        <div class="toolbar-actions">
          <a routerLink="/mi-cuenta"><dx-icon name="user" [size]="14" /> Mi cuenta</a>
          <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
        </div>
      </header>

      <app-account-orders-tabs />

      @if (!loggedIn()) {
        <div class="empty-box">
          <dx-icon name="login" [size]="36" />
          <p>Iniciá sesión para ver tu historial de pedidos.</p>
          <button type="button" class="dx-btn" (click)="auth.showLogin()">
            <dx-icon name="login" [size]="16" /> Iniciar sesión
          </button>
        </div>
      } @else {
        @if (error()) {
          <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
        }

        @if (loading()) {
          <div class="skel-list" role="status" aria-busy="true" aria-label="Cargando pedidos">
            @for (i of [1, 2, 3]; track i) {
              <div class="skel-card">
                <div class="skel-line title"></div>
                <div class="skel-line"></div>
                <div class="skel-line short"></div>
              </div>
            }
          </div>
        } @else if (orders().length === 0) {
          <div class="empty-box">
            <dx-icon name="package" [size]="40" />
            <p>Todavía no tenés pedidos.</p>
            <p class="empty-hint">Cuando confirmés una compra, aparece acá con seguimiento y recompra.</p>
            <a class="dx-btn" routerLink="/catalogo"><dx-icon name="grid" [size]="16" /> Ir al catálogo</a>
          </div>
        } @else {
          <div class="stats" aria-label="Resumen">
            <div class="stat">
              <span class="stat-lbl">Pedidos</span>
              <strong>{{ orders().length }}</strong>
            </div>
            <div class="stat">
              <span class="stat-lbl">Activos</span>
              <strong>{{ countActivos() }}</strong>
            </div>
            <div class="stat">
              <span class="stat-lbl">Facturados</span>
              <strong>{{ countFacturados() }}</strong>
            </div>
            <div class="stat">
              <span class="stat-lbl">Total gastado</span>
              <strong class="stat-money">{{ totalGastado() | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
            </div>
          </div>

          <div class="filters" role="search">
            <div class="chips" role="tablist" aria-label="Filtrar pedidos">
              @for (f of filters; track f.id) {
                <button
                  type="button"
                  role="tab"
                  class="chip"
                  [class.on]="filter() === f.id"
                  [attr.aria-selected]="filter() === f.id"
                  (click)="filter.set(f.id)"
                >
                  {{ f.label }}
                </button>
              }
            </div>
            <label class="search-wrap">
              <dx-icon name="search" [size]="16" />
              <input
                class="dx-input"
                type="search"
                [(ngModel)]="q"
                (ngModelChange)="onQuery($event)"
                placeholder="Buscar N° pedido…"
                aria-label="Buscar por número de pedido"
              />
            </label>
          </div>

          @if (filtered().length === 0) {
            <div class="empty-box compact">
              <dx-icon name="search" [size]="32" />
              <p>No hay pedidos con ese filtro.</p>
              <button type="button" class="dx-btn dx-btn--ghost" (click)="resetFilters()">Ver todos</button>
            </div>
          } @else {
            <div class="list">
              @for (o of filtered(); track o.idCarrito) {
                <article class="card" [class.card--cancel]="o.cancelado">
                  <div class="card-top">
                    <div class="id-block">
                      <a class="nro" [routerLink]="['/pedido', o.nroPedido]">Pedido N° {{ o.nroPedido }}</a>
                      <time [attr.datetime]="o.fechaPedido">{{ o.fechaPedido | date: "d MMM y · HH:mm" }}</time>
                    </div>
                    <div class="badges">
                      <span class="status" [attr.data-status]="statusKey(o)">{{ statusLabel(o) }}</span>
                      @if (o.puedeDescargarNotaCredito) {
                        <span class="pill fact-pill">
                          <dx-icon name="file-text" [size]="12" />
                          Nota de crédito
                        </span>
                      } @else if (o.facturado && o.nroFactura) {
                        <span class="pill fact-pill">
                          <dx-icon name="file-text" [size]="12" />
                          Factura {{ o.nroFactura }}
                        </span>
                      }
                      <span class="pill">
                        <dx-icon [name]="o.tipoEntrega === 0 ? 'truck' : 'store'" [size]="12" />
                        {{ o.tipoEntrega === 0 ? 'Delivery' : 'Pickup' }}
                      </span>
                      <span class="pill mute-pill">{{ o.tipoPago === 0 ? 'Paga en entrega' : 'Pago online' }}</span>
                    </div>
                  </div>

                  <div class="card-body">
                    <div class="info">
                      <p class="addr">
                        <dx-icon [name]="o.tipoEntrega === 0 ? 'truck' : 'store'" [size]="14" />
                        {{ o.direccion || 'Sin dirección' }}
                      </p>
                      @if (o.razonSocial) {
                        <p class="client">{{ o.razonSocial }} · Doc. {{ o.numeroDocumento || '—' }}</p>
                      }
                      @if (o.migrado) {
                        <p class="erp"><dx-icon name="check" [size]="12" /> Sincronizado con ERP</p>
                      }
                    </div>
                    <div class="amount">
                      <span class="amount-lbl">Total</span>
                      <strong>{{ o.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                    </div>
                  </div>

                  <div class="card-actions">
                    <a class="dx-btn" [routerLink]="['/pedido', o.nroPedido]">
                      <dx-icon name="package" [size]="14" /> Ver detalle
                    </a>
                    @if (o.puedeDescargarNotaCredito) {
                      <button
                        type="button"
                        class="dx-btn"
                        (click)="descargarNotaCredito(o)"
                        [disabled]="downloading() === o.nroPedido"
                      >
                        <dx-icon name="file-text" [size]="14" />
                        {{ downloading() === o.nroPedido ? 'Imprimiendo…' : 'Imprimir nota de crédito' }}
                      </button>
                    } @else if (o.puedeDescargarFactura) {
                      <button
                        type="button"
                        class="dx-btn"
                        (click)="descargarFactura(o)"
                        [disabled]="downloading() === o.nroPedido"
                      >
                        <dx-icon name="file-text" [size]="14" />
                        {{ downloading() === o.nroPedido ? 'Imprimiendo…' : 'Imprimir factura' }}
                      </button>
                    }
                    <button
                      type="button"
                      class="dx-btn dx-btn--ghost"
                      (click)="reorder(o)"
                      [disabled]="reordering() === o.nroPedido"
                    >
                      <dx-icon name="cart" [size]="14" />
                      {{ reordering() === o.nroPedido ? 'Agregando…' : 'Volver a pedir' }}
                    </button>
                    <button type="button" class="dx-btn dx-btn--ghost" (click)="consultarPedido(o)">
                      <dx-icon name="chat" [size]="14" /> Consultar
                    </button>
                    @if (o.idPedidoMovil && !o.cancelado) {
                      <a
                        class="dx-btn dx-btn--ghost"
                        [routerLink]="['/devoluciones']"
                        [queryParams]="{ pedido: o.idPedidoMovil }"
                      >
                        <dx-icon name="truck" [size]="14" /> Devolver
                      </a>
                    }
                    @if (o.idPedidoMovil) {
                      <button type="button" class="dx-btn dx-btn--ghost" (click)="toggleTrack(o)">
                        <dx-icon name="truck" [size]="14" />
                        {{ expanded() === o.idCarrito ? 'Ocultar' : 'Seguimiento' }}
                      </button>
                      <button
                        type="button"
                        class="dx-btn dx-btn--ghost"
                        (click)="compartir(o)"
                        [disabled]="sharing() === o.idPedidoMovil"
                      >
                        {{ sharing() === o.idPedidoMovil ? 'Generando…' : 'Compartir' }}
                      </button>
                    }
                    @if (o.puedeCancelar) {
                      <button
                        type="button"
                        class="link-danger"
                        (click)="cancelarPedido(o)"
                        [disabled]="canceling() === o.nroPedido"
                      >
                        {{ canceling() === o.nroPedido ? 'Cancelando…' : 'Cancelar pedido' }}
                      </button>
                    } @else if (!o.cancelado && o.facturado) {
                      <span class="hint-cancel">Facturado: usá Devolver</span>
                    }
                  </div>

                  @if (expanded() === o.idCarrito) {
                    <div class="timeline" role="status">
                      <dx-tracking-timeline
                        [events]="track()[o.idCarrito] || []"
                        [loading]="trackLoading() === o.idCarrito"
                        mode="detailed"
                      />
                    </div>
                  }
                </article>
              }
            </div>
          }
        }
      }
    </section>
  `,
  styles: [
    `
      .toolbar {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 1.25rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 2rem);
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
      }
      .sub {
        margin: 0.35rem 0 0;
        color: var(--dx-ink-soft);
        font-size: 0.95rem;
      }
      .toolbar-actions {
        display: flex;
        gap: 0.85rem;
        flex-wrap: wrap;
      }
      .toolbar-actions a {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        font-weight: 700;
        text-decoration: none;
        color: var(--dx-red);
      }

      .stats {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 0.65rem;
        margin-bottom: 1rem;
      }
      @media (max-width: 720px) {
        .stats {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }
      @media (max-width: 420px) {
        .stats {
          grid-template-columns: 1fr;
        }
      }
      .stat {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        padding: 0.85rem 1rem;
        display: grid;
        gap: 0.2rem;
      }
      .stat-lbl {
        font-size: 0.78rem;
        font-weight: 700;
        color: var(--dx-mute);
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .stat strong {
        font-family: var(--dx-display);
        font-size: 1.35rem;
        color: var(--dx-ink);
      }
      .stat-money {
        color: var(--dx-price) !important;
      }

      .filters {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
      }
      .chips {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
      }
      .chip {
        border: 1px solid var(--dx-line);
        background: #fff;
        color: var(--dx-ink-soft);
        font: inherit;
        font-weight: 650;
        font-size: 0.88rem;
        padding: 0.4rem 0.85rem;
        border-radius: 999px;
        cursor: pointer;
      }
      .chip.on {
        background: var(--dx-red);
        border-color: transparent;
        color: #fff;
      }
      .search-wrap {
        position: relative;
        display: flex;
        align-items: center;
        min-width: min(240px, 100%);
      }
      .search-wrap dx-icon {
        position: absolute;
        left: 0.75rem;
        color: var(--dx-mute);
        pointer-events: none;
      }
      .search-wrap .dx-input {
        padding-left: 2.25rem;
        width: 100%;
      }

      .list {
        display: grid;
        gap: 0.85rem;
      }
      .card {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 16px;
        padding: 1.1rem 1.2rem;
        display: grid;
        gap: 0.9rem;
        box-shadow: var(--dx-shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.05));
        transition: border-color 0.15s ease, box-shadow 0.15s ease;
      }
      .card:hover {
        border-color: #e0b4b4;
        box-shadow: 0 6px 20px rgba(198, 40, 40, 0.06);
      }
      .card--cancel {
        background: #fffafa;
        border-color: #f5c2c0;
      }
      .card-top {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        flex-wrap: wrap;
        align-items: flex-start;
      }
      .id-block {
        display: grid;
        gap: 0.2rem;
      }
      .nro {
        font-family: var(--dx-display);
        font-size: 1.15rem;
        font-weight: 800;
        color: var(--dx-ink);
        text-decoration: none;
      }
      .nro:hover {
        color: var(--dx-red);
      }
      time {
        color: var(--dx-mute);
        font-size: 0.88rem;
      }
      .badges {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        justify-content: flex-end;
      }
      .status {
        display: inline-flex;
        align-items: center;
        padding: 0.25rem 0.65rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 800;
        letter-spacing: 0.02em;
      }
      .status[data-status='cancelado'] {
        background: #fdecea;
        color: #b71c1c;
      }
      .status[data-status='activo'] {
        background: #fff8e1;
        color: #f57f17;
      }
      .status[data-status='cerrado'] {
        background: #e8f5e9;
        color: #2e7d32;
      }
      .status[data-status='facturado'] {
        background: #e3f2fd;
        color: #1565c0;
      }
      .fact-pill {
        background: #e3f2fd;
        color: #1565c0;
      }
      .pill {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        padding: 0.25rem 0.55rem;
        border-radius: 999px;
        background: #f5f5f5;
        color: var(--dx-ink-soft);
        font-size: 0.75rem;
        font-weight: 700;
      }
      .mute-pill {
        background: #fafafa;
        color: var(--dx-mute);
      }

      .card-body {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
        align-items: flex-end;
      }
      .info {
        display: grid;
        gap: 0.25rem;
        min-width: 0;
        flex: 1;
      }
      .addr,
      .client,
      .erp {
        margin: 0;
        display: flex;
        align-items: flex-start;
        gap: 0.35rem;
        color: var(--dx-ink-soft);
        font-size: 0.92rem;
      }
      .erp {
        color: #2e7d32;
        font-weight: 650;
        font-size: 0.82rem;
      }
      .amount {
        text-align: right;
      }
      .amount-lbl {
        display: block;
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--dx-mute);
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .amount strong {
        font-family: var(--dx-display);
        font-size: 1.45rem;
        color: var(--dx-price);
      }

      .card-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
        align-items: center;
        padding-top: 0.15rem;
        border-top: 1px solid var(--dx-line);
      }
      .card-actions .dx-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        text-decoration: none;
      }
      .link-danger {
        margin-left: auto;
        border: none;
        background: none;
        color: var(--dx-warn, #c62828);
        font: inherit;
        font-weight: 700;
        font-size: 0.88rem;
        cursor: pointer;
        padding: 0.35rem 0.25rem;
      }
      .link-danger:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }

      .timeline {
        border-top: 1px solid var(--dx-line);
        padding-top: 0.85rem;
      }

      .skel-list {
        display: grid;
        gap: 0.75rem;
      }
      .skel-card {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.1rem;
      }
      .skel-line {
        height: 12px;
        margin: 0.55rem 0;
        border-radius: 6px;
        background: linear-gradient(90deg, #f1f5f9 25%, #e2e8f0 50%, #f1f5f9 75%);
        background-size: 200% 100%;
        animation: shimmer 1.2s infinite;
      }
      .skel-line.title {
        width: 40%;
        height: 18px;
      }
      .skel-line.short {
        width: 28%;
      }
      @keyframes shimmer {
        0% {
          background-position: 200% 0;
        }
        100% {
          background-position: -200% 0;
        }
      }

      .empty-box {
        display: grid;
        justify-items: center;
        gap: 0.5rem;
        text-align: center;
        padding: 2.5rem 1rem;
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        color: var(--dx-ink-soft);
      }
      .empty-box.compact {
        padding: 1.75rem 1rem;
      }
      .empty-box p {
        margin: 0;
      }
      .empty-hint {
        font-size: 0.9rem;
        color: var(--dx-mute);
        max-width: 36ch;
        margin-bottom: 0.35rem !important;
      }
      .empty-box dx-icon {
        color: var(--dx-mute);
      }
    `,
  ],
})
export class OrdersPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);
  readonly auth = inject(AuthModalService);
  private readonly chatFacade = inject(SupportChatFacade);
  private readonly toast = inject(ToastService);
  private readonly hub = inject(StorefrontChatHubService);

  readonly filters: { id: OrderFilter; label: string }[] = [
    { id: 'todos', label: 'Todos' },
    { id: 'activos', label: 'Activos' },
    { id: 'facturados', label: 'Facturados' },
    { id: 'cancelados', label: 'Cancelados' },
    { id: 'delivery', label: 'Delivery' },
    { id: 'pickup', label: 'Pickup' },
  ];

  readonly orders = signal<AccountOrder[]>([]);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly loggedIn = signal(isLoggedIn());
  readonly expanded = signal<number | null>(null);
  readonly trackLoading = signal<number | null>(null);
  readonly track = signal<Record<number, Seguimiento[]>>({});
  readonly reordering = signal<number | null>(null);
  readonly canceling = signal<number | null>(null);
  readonly sharing = signal<number | null>(null);
  readonly downloading = signal<number | null>(null);
  readonly filter = signal<OrderFilter>('todos');
  readonly query = signal('');
  q = '';

  readonly filtered = computed(() => {
    const f = this.filter();
    const q = this.query().trim().toLowerCase();
    return this.orders().filter((o) => {
      if (f === 'activos' && (o.cancelado || o.facturado)) return false;
      if (f === 'cancelados' && !o.cancelado) return false;
      if (f === 'facturados' && !o.facturado) return false;
      if (f === 'delivery' && o.tipoEntrega !== 0) return false;
      if (f === 'pickup' && o.tipoEntrega === 0) return false;
      if (
        q &&
        !String(o.nroPedido).includes(q) &&
        !(o.razonSocial || '').toLowerCase().includes(q) &&
        !(o.nroFactura && String(o.nroFactura).includes(q))
      ) {
        return false;
      }
      return true;
    });
  });

  readonly countActivos = computed(() => this.orders().filter((o) => !o.cancelado && !o.facturado).length);
  readonly countFacturados = computed(() => this.orders().filter((o) => !!o.facturado).length);
  readonly totalGastado = computed(() =>
    this.orders().filter((o) => !o.cancelado).reduce((s, o) => s + (o.montoTotal || 0), 0),
  );

  ngOnInit(): void {
    if (!this.loggedIn()) return;
    this.loadOrders();
  }

  onQuery(value: string): void {
    this.query.set(value);
  }

  resetFilters(): void {
    this.filter.set('todos');
    this.q = '';
    this.query.set('');
  }

  statusKey(o: AccountOrder): 'cancelado' | 'activo' | 'cerrado' | 'facturado' {
    if (o.cancelado) return 'cancelado';
    if (o.facturado) return 'facturado';
    // Estado carrito: 1 abierto, 2 cerrado tipico; sin migrar = en proceso
    if (!o.migrado) return 'activo';
    return 'cerrado';
  }

  statusLabel(o: AccountOrder): string {
    const k = this.statusKey(o);
    if (k === 'cancelado') return o.puedeDescargarNotaCredito || o.facturado ? 'Devuelto' : 'Cancelado';
    if (k === 'facturado') return 'Facturado';
    if (k === 'activo') return 'En proceso';
    return 'Completado';
  }

  descargarFactura(o: AccountOrder): void {
    if (!isLoggedIn()) {
      this.auth.showLogin();
      this.toast.error('Iniciá sesión para imprimir la factura');
      return;
    }
    this.downloading.set(o.nroPedido);
    this.api.downloadInvoicePdf(o.nroPedido).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        const name = o.nroFactura
          ? `factura-${o.nroFactura}.pdf`
          : `factura-pedido-${o.nroPedido}.pdf`;
        if (!openPdfBlob(blob, name)) {
          void readPdfError(blob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.success(`Factura ${o.nroFactura ?? o.nroPedido} lista para imprimir`);
      },
      error: (e) => {
        this.downloading.set(null);
        if (e?.status === 401) {
          this.auth.showLogin();
          this.toast.error('Iniciá sesión para imprimir la factura');
          return;
        }
        const errBlob = e?.error;
        if (errBlob instanceof Blob) {
          void readPdfError(errBlob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.error(e?.error?.error || e.message || 'No se pudo cargar la factura');
      },
    });
  }

  descargarNotaCredito(o: AccountOrder): void {
    if (!isLoggedIn()) {
      this.auth.showLogin();
      this.toast.error('Iniciá sesión para imprimir la nota de crédito');
      return;
    }
    this.downloading.set(o.nroPedido);
    this.api.downloadCreditNotePdf(o.nroPedido).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        const name = o.nroNotaCredito
          ? `nota-credito-${o.nroNotaCredito}.pdf`
          : `nota-credito-pedido-${o.nroPedido}.pdf`;
        if (!openPdfBlob(blob, name)) {
          void readPdfError(blob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.success('Nota de crédito lista para imprimir');
      },
      error: (e) => {
        this.downloading.set(null);
        if (e?.status === 401) {
          this.auth.showLogin();
          this.toast.error('Iniciá sesión para imprimir la nota de crédito');
          return;
        }
        this.toast.error(e?.error?.error || e.message || 'No se pudo cargar la nota de crédito');
      },
    });
  }

  private loadOrders(): void {
    this.loading.set(true);
    this.api.getMyOrders().subscribe({
      next: (rows) => {
        this.orders.set(rows);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        const msg = e?.error?.error || e.message || 'No se pudieron cargar los pedidos';
        this.error.set(msg);
        this.toast.error(msg);
      },
    });
  }

  cancelarPedido(o: AccountOrder): void {
    const motivo = window.prompt(`¿Cancelar el pedido N° ${o.nroPedido}?\n\nMotivo (opcional):`, '');
    if (motivo === null) return;

    this.canceling.set(o.nroPedido);
    this.error.set(null);
    this.api.cancelOrder(o.nroPedido, motivo.trim() || undefined).subscribe({
      next: () => {
        this.canceling.set(null);
        this.toast.success(`Pedido N° ${o.nroPedido} cancelado`);
        this.loadOrders();
      },
      error: (e) => {
        this.canceling.set(null);
        this.toast.error(e?.error?.error || e.message || 'No se pudo cancelar');
      },
    });
  }

  toggleTrack(o: AccountOrder): void {
    if (this.expanded() === o.idCarrito) {
      this.expanded.set(null);
      return;
    }
    this.expanded.set(o.idCarrito);
    if (!o.idPedidoMovil) return;

    if (!this.track()[o.idCarrito]) {
      this.trackLoading.set(o.idCarrito);
      this.api.getTracking(o.idPedidoMovil).subscribe({
        next: (rows) => {
          this.track.update((m) => ({ ...m, [o.idCarrito]: rows }));
          this.trackLoading.set(null);
        },
        error: () => {
          this.track.update((m) => ({ ...m, [o.idCarrito]: [] }));
          this.trackLoading.set(null);
          this.toast.info('Sin eventos de seguimiento todavía');
        },
      });
    }

    void this.hub.joinPedido(o.idPedidoMovil);
    this.hub.onTrackingUpdated((raw) => {
      const evt = raw as Partial<Seguimiento> & { Id?: number; Estado?: string; FechaEstado?: string };
      if (!evt || !o.idPedidoMovil) return;
      const mapped: Seguimiento = {
        id: Number(evt.id ?? evt.Id ?? Date.now()),
        idPedidoMovil: o.idPedidoMovil,
        estado: String(evt.estado ?? evt.Estado ?? ''),
        mensaje: (evt.mensaje as string | null | undefined) ?? null,
        fechaEstado: String(evt.fechaEstado ?? evt.FechaEstado ?? new Date().toISOString()),
        codigoTracking: (evt.codigoTracking as string | null | undefined) ?? null,
      };
      if (!mapped.estado) return;
      this.track.update((m) => {
        const cur = m[o.idCarrito] || [];
        if (cur.some((x) => x.id === mapped.id)) return m;
        return { ...m, [o.idCarrito]: [mapped, ...cur] };
      });
    });
  }

  consultarPedido(o: AccountOrder): void {
    if (!o.idPedidoMovil) {
      this.toast.info('Este pedido aún no tiene chat de seguimiento disponible');
      return;
    }
    if (!isLoggedIn() && !this.session.loggedIn()) {
      this.auth.showLogin({ reopenChat: true, idPedidoMovil: o.idPedidoMovil });
      return;
    }
    this.chatFacade.open({ idPedido: o.idPedidoMovil });
    this.toast.info(`Chat abierto · pedido N° ${o.nroPedido}`);
  }

  compartir(o: AccountOrder): void {
    if (!o.idPedidoMovil) return;
    this.sharing.set(o.idPedidoMovil);
    this.api.shareTracking(o.idPedidoMovil).subscribe({
      next: (r) => {
        this.sharing.set(null);
        const url = `${window.location.origin}${r.path}`;
        void navigator.clipboard?.writeText(url);
        this.toast.success('Link de seguimiento copiado');
      },
      error: (e) => {
        this.sharing.set(null);
        this.toast.error(e?.error?.error || 'No se pudo generar el link');
      },
    });
  }

  reorder(o: AccountOrder): void {
    this.reordering.set(o.nroPedido);
    this.error.set(null);
    this.api.reorder(o.nroPedido).subscribe({
      next: (cart) => {
        this.reordering.set(null);
        this.session.cartCount.set(cart.itemsCount);
        this.toast.success(`Pedido N° ${o.nroPedido} agregado al carrito`);
        void this.router.navigate(['/carrito']);
      },
      error: (e) => {
        this.reordering.set(null);
        const msg = e?.error?.error || e.message || 'No se pudo volver a pedir';
        this.error.set(msg);
        this.toast.error(msg);
      },
    });
  }
}
