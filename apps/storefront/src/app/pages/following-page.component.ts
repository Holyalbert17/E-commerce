import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ApiService, SeguidosItem, getCanal, isLoggedIn } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { MiniCartService } from '../core/mini-cart.service';
import { SeguidosAlert, SeguidosAlertsService } from '../core/seguidos-alerts.service';
import { DxIconComponent } from '../shared/dx-icon.component';

type FilterKey = 'all' | 'in' | 'out' | 'news';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, DecimalPipe, DxIconComponent],
  template: `
    <section class="seg" aria-labelledby="seg-title">
      <header class="toolbar">
        <div>
          <h1 id="seg-title"><dx-icon name="star" [size]="26" [filled]="true" /> Productos que sigo</h1>
          <p class="sub">
            Precio y stock al día. Te marcamos acá si vuelve stock o cambia el precio (el email llega cuando SMTP esté activo).
          </p>
        </div>
        <div class="toolbar-links">
          <a routerLink="/favoritos"><dx-icon name="heart" [size]="14" /> Favoritos</a>
          <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
        </div>
      </header>

      @if (!logged) {
        <div class="empty-box">
          <dx-icon name="star" [size]="40" />
          <p>Iniciá sesión para ver los productos que seguís.</p>
          <button type="button" class="dx-btn" (click)="auth.showLogin()">
            <dx-icon name="login" [size]="16" /> Iniciar sesión
          </button>
        </div>
      } @else if (error()) {
        <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
      } @else if (loading()) {
        <div class="skel-list" role="status" aria-busy="true">
          @for (i of [1, 2, 3]; track i) {
            <div class="skel-card"></div>
          }
        </div>
      } @else if (items().length === 0) {
        <div class="empty-box">
          <dx-icon name="star" [size]="40" />
          <p>Todavía no seguís ningún producto.</p>
          <p class="hint">En la ficha tocá la estrella para agregarlo acá.</p>
          <a class="dx-btn" routerLink="/catalogo">Ir al catálogo</a>
        </div>
      } @else {
        @if (alertCount() > 0) {
          <div class="alert-banner" role="status">
            <div>
              <strong>{{ alertCount() }} novedad{{ alertCount() === 1 ? '' : 'es' }}</strong>
              <span>Cambios de stock o precio desde tu última revisión.</span>
            </div>
            <button type="button" class="dx-btn dx-btn--ghost" (click)="dismissAlerts()">Marcar vistas</button>
          </div>
        }

        <div class="filters" role="tablist" aria-label="Filtrar seguidos">
          @for (f of filters; track f.key) {
            <button
              type="button"
              role="tab"
              class="chip"
              [class.on]="filter() === f.key"
              [attr.aria-selected]="filter() === f.key"
              (click)="filter.set(f.key)"
            >
              {{ f.label }}
              <span class="n">{{ f.count() }}</span>
            </button>
          }
        </div>

        @if (visible().length === 0) {
          <div class="empty-box compact">
            <p>No hay productos en este filtro.</p>
            <button type="button" class="link-muted" (click)="filter.set('all')">Ver todos</button>
          </div>
        } @else {
          <p class="count">
            {{ visible().length }} producto{{ visible().length === 1 ? '' : 's' }}
            @if (filter() !== 'all') {
              <button type="button" class="link-muted inline" (click)="filter.set('all')">· Quitar filtro</button>
            }
          </p>
          <ul class="list">
            @for (it of visible(); track it.idArticulo) {
              <li [class.out]="it.stockOk === false" [class.has-alert]="!!alerts().get(it.idArticulo)?.length">
                <a class="row" [routerLink]="['/producto', it.idArticulo]">
                  <span
                    class="thumb"
                    [style.backgroundImage]="it.imagenUrl ? 'url(' + mediaUrl(it.imagenUrl) + ')' : 'none'"
                  ></span>
                  <span class="meta">
                    <strong>
                      <dx-icon name="star" [size]="14" [filled]="true" class="star-mark" />
                      {{ it.nombre }}
                    </strong>
                    <small>
                      {{ it.sku || 'Sin SKU' }} · desde {{ it.fechaSeguimiento | date: 'd MMM y' }}
                    </small>
                    @if (alerts().get(it.idArticulo); as tags) {
                      <span class="tags">
                        @for (a of tags; track a.kind) {
                          <span class="tag" [attr.data-kind]="a.kind">{{ a.label }}</span>
                        }
                      </span>
                    }
                    <span class="facts">
                      @if (it.precioDesde != null) {
                        <em class="price">{{ it.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</em>
                      } @else {
                        <em class="ask">Consultar precio</em>
                      }
                      <span class="badge" [class.ok]="it.stockOk !== false" [class.bad]="it.stockOk === false">
                        @if (it.stockOk === false) {
                          Sin stock
                        } @else if (it.stockLibre != null) {
                          Stock {{ it.stockLibre | number: '1.0-0' }}
                        } @else {
                          Disponible
                        }
                      </span>
                    </span>
                  </span>
                </a>
                <div class="actions">
                  <button
                    type="button"
                    class="dx-btn"
                    (click)="addToCart(it)"
                    [disabled]="adding() === it.idArticulo || it.stockOk === false"
                  >
                    <dx-icon name="cart" [size]="14" />
                    {{ adding() === it.idArticulo ? '…' : 'Al carrito' }}
                  </button>
                  <a class="dx-btn dx-btn--ghost" [routerLink]="['/producto', it.idArticulo]">Ver</a>
                  <button type="button" class="link-muted" (click)="unfollow(it)">Dejar de seguir</button>
                </div>
              </li>
            }
          </ul>
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
        margin-bottom: 1.25rem;
        flex-wrap: wrap;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 2rem);
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
      }
      h1 dx-icon {
        color: #f59e0b;
      }
      .sub {
        margin: 0.35rem 0 0;
        color: var(--dx-ink-soft);
        max-width: 52ch;
      }
      .toolbar-links {
        display: flex;
        gap: 0.85rem;
      }
      .toolbar-links a {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        font-weight: 700;
        text-decoration: none;
        color: var(--dx-red);
      }
      .alert-banner {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 1rem;
        padding: 0.85rem 1rem;
        border-radius: 12px;
        border: 1px solid #fde68a;
        background: #fffbeb;
      }
      .alert-banner strong {
        display: block;
        color: #92400e;
      }
      .alert-banner span {
        color: #a16207;
        font-size: 0.9rem;
      }
      .filters {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
        margin-bottom: 0.85rem;
      }
      .chip {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        border: 1px solid var(--dx-line);
        background: #fff;
        border-radius: 999px;
        padding: 0.4rem 0.75rem;
        font: inherit;
        font-size: 0.86rem;
        font-weight: 650;
        color: var(--dx-ink-soft);
        cursor: pointer;
      }
      .chip.on {
        border-color: var(--dx-red);
        color: var(--dx-red);
        background: #fff5f5;
      }
      .chip .n {
        font-size: 0.75rem;
        color: var(--dx-mute);
        font-weight: 700;
      }
      .chip.on .n {
        color: var(--dx-red);
      }
      .count {
        margin: 0 0 0.65rem;
        color: var(--dx-mute);
        font-weight: 650;
        font-size: 0.9rem;
      }
      .list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.65rem;
      }
      li {
        display: flex;
        gap: 0.75rem;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 0.85rem 1rem;
      }
      li.out {
        background: #fffafa;
        border-color: #f5c2c0;
      }
      li.has-alert {
        border-color: #fbbf24;
        box-shadow: 0 0 0 1px rgba(251, 191, 36, 0.25);
      }
      .row {
        display: flex;
        gap: 0.75rem;
        align-items: center;
        text-decoration: none;
        color: inherit;
        min-width: 0;
        flex: 1;
      }
      .thumb {
        width: 64px;
        height: 64px;
        border-radius: 10px;
        background: #f3f3f3 center/cover no-repeat;
        flex-shrink: 0;
      }
      .meta {
        display: grid;
        gap: 0.2rem;
        min-width: 0;
      }
      .meta strong {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
      .star-mark {
        color: #f59e0b;
        flex-shrink: 0;
      }
      .meta small {
        color: var(--dx-mute);
      }
      .tags {
        display: flex;
        flex-wrap: wrap;
        gap: 0.3rem;
      }
      .tag {
        display: inline-flex;
        padding: 0.12rem 0.45rem;
        border-radius: 6px;
        font-size: 0.7rem;
        font-weight: 750;
        background: #fef3c7;
        color: #92400e;
      }
      .tag[data-kind='back_in_stock'] {
        background: #dcfce7;
        color: #166534;
      }
      .tag[data-kind='price_down'] {
        background: #dbeafe;
        color: #1e40af;
      }
      .tag[data-kind='price_up'] {
        background: #ffedd5;
        color: #9a3412;
      }
      .facts {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
        align-items: center;
      }
      .price {
        font-style: normal;
        font-family: var(--dx-display);
        font-weight: 800;
        color: var(--dx-price, var(--dx-red));
      }
      .ask {
        font-style: normal;
        color: var(--dx-mute);
        font-size: 0.88rem;
      }
      .badge {
        display: inline-flex;
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 750;
      }
      .badge.ok {
        background: #e8f5e9;
        color: #2e7d32;
      }
      .badge.bad {
        background: #fdecea;
        color: #b71c1c;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        align-items: center;
      }
      .actions .dx-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        text-decoration: none;
      }
      .link-muted {
        background: none;
        border: none;
        color: var(--dx-mute);
        font: inherit;
        font-weight: 650;
        cursor: pointer;
        padding: 0.35rem 0.25rem;
      }
      .link-muted.inline {
        padding: 0;
        margin-left: 0.25rem;
      }
      .link-muted:hover {
        color: var(--dx-warn, #c62828);
      }
      .empty-box {
        display: grid;
        justify-items: center;
        gap: 0.5rem;
        text-align: center;
        padding: 2.25rem 1rem;
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        color: var(--dx-ink-soft);
      }
      .empty-box.compact {
        padding: 1.5rem 1rem;
      }
      .empty-box .hint {
        margin: 0;
        font-size: 0.9rem;
        color: var(--dx-mute);
      }
      .empty-box dx-icon {
        color: var(--dx-mute);
      }
      .skel-list {
        display: grid;
        gap: 0.65rem;
      }
      .skel-card {
        height: 88px;
        border-radius: 14px;
        background: linear-gradient(90deg, #f1f5f9 25%, #e2e8f0 50%, #f1f5f9 75%);
        background-size: 200% 100%;
        animation: shimmer 1.2s infinite;
      }
      @keyframes shimmer {
        0% {
          background-position: 200% 0;
        }
        100% {
          background-position: -200% 0;
        }
      }
    `,
  ],
})
export class FollowingPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly miniCart = inject(MiniCartService);
  private readonly session = inject(SessionService);
  private readonly alertsSvc = inject(SeguidosAlertsService);
  readonly auth = inject(AuthModalService);

  readonly items = signal<SeguidosItem[]>([]);
  readonly alerts = signal<Map<number, SeguidosAlert[]>>(new Map());
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly adding = signal<number | null>(null);
  readonly filter = signal<FilterKey>('all');
  logged = isLoggedIn();

  readonly alertCount = computed(() => this.alerts().size);

  readonly filters = [
    { key: 'all' as const, label: 'Todos', count: computed(() => this.items().length) },
    {
      key: 'in' as const,
      label: 'Con stock',
      count: computed(() => this.items().filter((i) => i.stockOk !== false).length),
    },
    {
      key: 'out' as const,
      label: 'Sin stock',
      count: computed(() => this.items().filter((i) => i.stockOk === false).length),
    },
    {
      key: 'news' as const,
      label: 'Novedades',
      count: computed(() => this.alerts().size),
    },
  ];

  readonly visible = computed(() => {
    const f = this.filter();
    const map = this.alerts();
    let rows = this.items();
    if (f === 'in') rows = rows.filter((i) => i.stockOk !== false);
    else if (f === 'out') rows = rows.filter((i) => i.stockOk === false);
    else if (f === 'news') rows = rows.filter((i) => map.has(i.idArticulo));
    return [...rows].sort((a, b) => {
      const aa = map.has(a.idArticulo) ? 0 : 1;
      const bb = map.has(b.idArticulo) ? 0 : 1;
      if (aa !== bb) return aa - bb;
      return 0;
    });
  });

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  ngOnInit(): void {
    if (!this.logged) return;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getSeguidos().subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.alerts.set(this.alertsSvc.evaluate(rows));
        this.loading.set(false);
        if (this.alerts().size && this.filter() === 'all') {
          /* keep default; banner is enough */
        }
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo cargar');
      },
    });
  }

  dismissAlerts(): void {
    this.alertsSvc.dismiss(this.items());
    this.alerts.set(new Map());
    if (this.filter() === 'news') this.filter.set('all');
    this.toast.info('Novedades marcadas como vistas');
  }

  unfollow(it: SeguidosItem): void {
    this.api.dejarSeguir(it.idArticulo).subscribe({
      next: () => {
        this.items.update((list) => list.filter((x) => x.idArticulo !== it.idArticulo));
        this.alerts.update((m) => {
          const next = new Map(m);
          next.delete(it.idArticulo);
          return next;
        });
        this.toast.info(`Dejaste de seguir “${it.nombre}”`);
      },
      error: (e) => {
        this.error.set(e?.error?.error || e.message);
        this.toast.error(e?.error?.error || 'No se pudo dejar de seguir');
      },
    });
  }

  addToCart(it: SeguidosItem): void {
    this.adding.set(it.idArticulo);
    this.api.product(it.idArticulo, getCanal()).subscribe({
      next: (p) => {
        const emb = p.embalajes?.[0];
        const venta = emb?.cantidadMinimaVenta && emb.cantidadMinimaVenta > 0 ? emb.cantidadMinimaVenta : 1;
        const moq = emb?.cantidadMinimaMayorista && emb.cantidadMinimaMayorista > 0 ? emb.cantidadMinimaMayorista : 1;
        const qty = getCanal() === 'B2B' ? Math.max(venta, moq) : venta;
        this.api.addToCart(it.idArticulo, qty, emb?.id, getCanal()).subscribe({
          next: (cart) => {
            this.adding.set(null);
            const n = cart.itemsCount ?? cart.items?.length ?? 0;
            this.session.setCartCount(n);
            this.session.setCartTotal(cart.montoTotal ?? 0);
            this.miniCart.show(cart, it.idArticulo);
          },
          error: (e) => {
            this.adding.set(null);
            this.toast.error(e?.error?.error || e.message || 'No se pudo agregar');
          },
        });
      },
      error: (e) => {
        this.adding.set(null);
        this.toast.error(e?.error?.error || 'No se pudo leer el producto');
      },
    });
  }
}
