import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, FavoritoItem, FavoritoSeccion, getCanal, isLoggedIn } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { MiniCartService } from '../core/mini-cart.service';
import { DxIconComponent } from '../shared/dx-icon.component';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, FormsModule, DxIconComponent],
  template: `
    <section class="fav" aria-labelledby="fav-title">
      <header class="toolbar">
        <div>
          <h1 id="fav-title"><dx-icon name="heart" [size]="26" [filled]="true" /> Mis favoritos</h1>
          <p class="sub">
            Listas por sección
            @if (emailHint()) {
              · <strong>{{ emailHint() }}</strong>
            }
            · {{ canalLabel() }}
          </p>
        </div>
        <div class="toolbar-links">
          <a routerLink="/seguidos"><dx-icon name="star" [size]="14" /> Seguidos</a>
          <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
        </div>
      </header>

      @if (!logged) {
        <div class="empty-box">
          <dx-icon name="heart" [size]="40" />
          <p>Iniciá sesión para guardar productos en listas (ej. bebidas).</p>
          <button type="button" class="dx-btn" (click)="auth.showLogin()">
            <dx-icon name="login" [size]="16" /> Iniciar sesión
          </button>
        </div>
      } @else {
        <div class="new-sec">
          <label class="sr-only" for="nueva">Nueva sección</label>
          <dx-icon name="tag" [size]="18" />
          <input id="nueva" class="dx-input" [(ngModel)]="nuevaSeccion" placeholder='Nueva lista · ej. "bebidas"' />
          <button type="button" class="dx-btn" (click)="crearSeccion()">Crear lista</button>
        </div>

        @if (loading()) {
          <div class="skel-list" role="status" aria-busy="true">
            @for (i of [1, 2]; track i) {
              <div class="skel-card"></div>
            }
          </div>
        } @else {
          @for (sec of secciones(); track sec.id) {
            <article class="sec" [style.borderLeftColor]="sec.color || 'var(--dx-red)'">
              <div class="sec-head">
                <h2>
                  {{ sec.nombreLista }}
                  <small>{{ sec.itemsCount }} ítem{{ sec.itemsCount === 1 ? '' : 's' }} · {{ sec.canalEcommerce }}</small>
                </h2>
                <button type="button" class="link-muted" (click)="eliminarSeccion(sec.id)">Eliminar lista</button>
              </div>
              @if (sec.items.length === 0) {
                <div class="sec-empty">
                  <p>Lista vacía.</p>
                  <a class="dx-btn dx-btn--ghost" routerLink="/catalogo">Agregar desde el catálogo</a>
                </div>
              } @else {
                <ul>
                  @for (it of sec.items; track it.idDetalle) {
                    <li>
                      <a class="thumb-link" [routerLink]="['/producto', it.idArticulo]" [attr.aria-label]="it.nombre">
                        <span
                          class="img"
                          [style.backgroundImage]="it.imagenUrl ? 'url(' + mediaUrl(it.imagenUrl) + ')' : 'none'"
                        ></span>
                      </a>
                      <div class="info">
                        <a [routerLink]="['/producto', it.idArticulo]">{{ it.nombre }}</a>
                        <small>{{ it.sku || 'Sin SKU' }}</small>
                        @if (it.precioDesde != null) {
                          <strong class="price">{{ it.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                        } @else {
                          <span class="ask">Consultar precio</span>
                        }
                      </div>
                      <div class="actions">
                        <button
                          type="button"
                          class="dx-btn"
                          (click)="addToCart(it)"
                          [disabled]="adding() === it.idDetalle"
                        >
                          <dx-icon name="cart" [size]="14" />
                          {{ adding() === it.idDetalle ? '…' : 'Al carrito' }}
                        </button>
                        <button type="button" class="link-muted" (click)="quitar(it.idDetalle, it.nombre)">Quitar</button>
                      </div>
                    </li>
                  }
                </ul>
              }
            </article>
          } @empty {
            <div class="empty-box">
              <dx-icon name="tag" [size]="36" />
              <p>Todavía no tenés listas. Creá la primera (ej. “bebidas”).</p>
              <a class="dx-btn" routerLink="/catalogo">Explorar catálogo</a>
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
        margin-bottom: 1.25rem;
        gap: 1rem;
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
        color: #c62828;
      }
      .sub {
        margin: 0.35rem 0 0;
        color: var(--dx-ink-soft);
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
      .new-sec {
        display: flex;
        gap: 0.55rem;
        flex-wrap: wrap;
        align-items: center;
        margin-bottom: 1rem;
        padding: 0.85rem 1rem;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
      }
      .new-sec > dx-icon {
        color: var(--dx-red);
      }
      .new-sec .dx-input {
        flex: 1;
        min-width: 180px;
      }
      .sec {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-left: 4px solid var(--dx-red);
        border-radius: 14px;
        padding: 1rem 1.1rem;
        margin-bottom: 0.85rem;
      }
      .sec-head {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 0.75rem;
      }
      .sec-head h2 {
        margin: 0;
        font-size: 1.1rem;
      }
      .sec-head small {
        color: var(--dx-mute);
        font-weight: 500;
        margin-left: 0.35rem;
      }
      .sec-empty {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.65rem;
        margin-top: 0.75rem;
        color: var(--dx-mute);
      }
      .sec-empty p {
        margin: 0;
      }
      ul {
        list-style: none;
        padding: 0;
        margin: 0.85rem 0 0;
        display: grid;
        gap: 0.65rem;
      }
      li {
        display: grid;
        grid-template-columns: 64px 1fr auto;
        gap: 0.85rem;
        align-items: center;
        padding: 0.55rem;
        border-radius: 12px;
        border: 1px solid var(--dx-line);
        background: #fafafa;
      }
      @media (max-width: 640px) {
        li {
          grid-template-columns: 56px 1fr;
        }
        .actions {
          grid-column: 1 / -1;
          justify-content: stretch;
        }
        .actions .dx-btn {
          flex: 1;
        }
      }
      .img {
        display: block;
        width: 64px;
        height: 64px;
        border-radius: 10px;
        background: #f3f3f3 center/cover no-repeat;
      }
      .info {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }
      .info a {
        font-weight: 700;
        text-decoration: none;
        color: var(--dx-ink);
      }
      .info a:hover {
        color: var(--dx-red);
      }
      .info small {
        color: var(--dx-mute);
      }
      .price {
        color: var(--dx-price, var(--dx-red));
        font-family: var(--dx-display);
        font-size: 1.05rem;
      }
      .ask {
        color: var(--dx-mute);
        font-size: 0.88rem;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
        align-items: center;
        justify-content: flex-end;
      }
      .actions .dx-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
      }
      .link-muted {
        background: none;
        border: none;
        color: var(--dx-mute);
        font: inherit;
        font-weight: 650;
        cursor: pointer;
        padding: 0.25rem;
      }
      .link-muted:hover {
        color: var(--dx-warn, #c62828);
      }
      .empty-box {
        display: grid;
        justify-items: center;
        gap: 0.75rem;
        text-align: center;
        padding: 2.25rem 1rem;
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        color: var(--dx-ink-soft);
      }
      .empty-box dx-icon {
        color: var(--dx-mute);
      }
      .skel-list {
        display: grid;
        gap: 0.75rem;
      }
      .skel-card {
        height: 120px;
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
    `,
  ],
})
export class FavoritesPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly miniCart = inject(MiniCartService);
  private readonly session = inject(SessionService);
  readonly auth = inject(AuthModalService);

  logged = isLoggedIn();
  nuevaSeccion = '';
  readonly secciones = signal<FavoritoSeccion[]>([]);
  readonly emailHint = signal<string | null>(null);
  readonly loading = signal(false);
  readonly adding = signal<number | null>(null);

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  ngOnInit(): void {
    if (!this.logged) return;
    this.api.me().subscribe({
      next: (p) => this.emailHint.set(p.email),
      error: () => undefined,
    });
    this.reload();
  }

  canalLabel(): string {
    return getCanal() === 'B2B' ? 'Mayorista' : 'Minorista';
  }

  reload(): void {
    this.loading.set(true);
    this.api.getFavoritos(getCanal()).subscribe({
      next: (rows) => {
        this.secciones.set(rows);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.toast.error(e?.error?.error || e.message || 'No se pudo cargar');
      },
    });
  }

  crearSeccion(): void {
    const nombre = this.nuevaSeccion.trim();
    if (!nombre) {
      this.toast.info('Escribí un nombre para la lista');
      return;
    }
    this.api.crearSeccionFavorito(nombre).subscribe({
      next: () => {
        this.nuevaSeccion = '';
        this.toast.success('Lista creada');
        this.reload();
      },
      error: (e) => this.toast.error(e?.error?.error || e.message),
    });
  }

  eliminarSeccion(id: number): void {
    if (!confirm('¿Eliminar esta lista y sus ítems?')) return;
    this.api.eliminarSeccionFavorito(id).subscribe({
      next: () => {
        this.toast.info('Lista eliminada');
        this.reload();
      },
      error: (e) => this.toast.error(e?.error?.error || e.message),
    });
  }

  quitar(idDetalle: number, nombre?: string): void {
    this.api.quitarFavorito(idDetalle).subscribe({
      next: () => {
        this.toast.info(nombre ? `Se quitó “${nombre}”` : 'Se quitó de favoritos');
        this.reload();
      },
      error: (e) => this.toast.error(e?.error?.error || e.message),
    });
  }

  addToCart(it: FavoritoItem): void {
    this.adding.set(it.idDetalle);
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
