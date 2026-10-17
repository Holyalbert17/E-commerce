import { CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { MiniCartService } from '../core/mini-cart.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from './dx-icon.component';

@Component({
  selector: 'app-mini-cart-drawer',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DxIconComponent],
  template: `
    @if (mini.open()) {
      <div class="scrim" (click)="mini.close()" aria-hidden="true"></div>
      <aside class="drawer" role="dialog" aria-modal="true" aria-labelledby="mini-cart-title">
        <header class="head">
          <h2 id="mini-cart-title">
            <dx-icon name="cart" [size]="20" />
            Carrito
            @if (session.cartCount() > 0) {
              <span class="count">{{ session.cartCount() }}</span>
            }
          </h2>
          <button type="button" class="icon-btn" (click)="mini.close()" aria-label="Cerrar">
            <dx-icon name="close" [size]="18" />
          </button>
        </header>

        @if (mini.cart(); as c) {
          @if (!c.items.length) {
            <div class="empty">
              <p>Tu carrito está vacío.</p>
              <a routerLink="/catalogo" class="dx-btn" (click)="mini.close()">Ir al catálogo</a>
            </div>
          } @else {
            <ul class="lines">
              @for (line of c.items; track line.id) {
                <li [class.flash]="line.idArticulo === mini.lastAddedId()">
                  <div class="thumb" [class.empty]="!line.imagenUrl">
                    @if (line.imagenUrl) {
                      <img [src]="media(line.imagenUrl)" [alt]="line.nombre" />
                    } @else {
                      <dx-icon name="package" [size]="22" />
                    }
                  </div>
                  <div class="body">
                    <strong>{{ line.nombre }}</strong>
                    <span class="meta">
                      {{ line.cantidad }} × {{ line.precioUnitario | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                    </span>
                    @if (!line.stockOk) {
                      <span class="warn">Sin stock suficiente</span>
                    }
                  </div>
                  <div class="side">
                    <span class="line-total">{{ line.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</span>
                    <button type="button" class="linkish" (click)="remove(line.id)" aria-label="Quitar">
                      Quitar
                    </button>
                  </div>
                </li>
              }
            </ul>

            <footer class="foot">
              <div class="tot">
                <span>Total</span>
                <strong>{{ c.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
              </div>
              <div class="actions">
                <a routerLink="/carrito" class="dx-btn dx-btn--ghost" (click)="mini.close()">Ver carrito</a>
                <a routerLink="/checkout" class="dx-btn" (click)="mini.close()">Checkout</a>
              </div>
            </footer>
          }
        }
      </aside>
    }
  `,
  styles: [
    `
      .scrim {
        position: fixed;
        inset: 0;
        background: rgba(17, 17, 17, 0.35);
        z-index: 1290;
      }
      .drawer {
        position: fixed;
        top: 0;
        right: 0;
        bottom: 0;
        width: min(400px, 100vw);
        background: #fff;
        z-index: 1300;
        display: flex;
        flex-direction: column;
        box-shadow: -8px 0 28px rgba(0, 0, 0, 0.12);
        animation: slideIn 0.22s ease;
      }
      @keyframes slideIn {
        from {
          transform: translateX(12px);
          opacity: 0.6;
        }
        to {
          transform: translateX(0);
          opacity: 1;
        }
      }
      .head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 1rem 1.1rem;
        border-bottom: 1px solid #ececec;
      }
      .head h2 {
        margin: 0;
        font-size: 1.05rem;
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
      }
      .count {
        display: inline-grid;
        place-items: center;
        min-width: 1.4rem;
        height: 1.4rem;
        padding: 0 0.35rem;
        border-radius: 999px;
        background: var(--dx-red, #c62828);
        color: #fff;
        font-size: 0.75rem;
        font-weight: 700;
      }
      .icon-btn {
        width: 36px;
        height: 36px;
        border: 0;
        background: #f5f5f5;
        border-radius: 8px;
        cursor: pointer;
        display: grid;
        place-items: center;
      }
      .empty {
        padding: 2rem 1.25rem;
        text-align: center;
        display: grid;
        gap: 1rem;
        justify-items: center;
      }
      .lines {
        list-style: none;
        margin: 0;
        padding: 0.5rem 0;
        overflow: auto;
        flex: 1;
      }
      .lines li {
        display: grid;
        grid-template-columns: 56px 1fr auto;
        gap: 0.65rem;
        padding: 0.75rem 1.1rem;
        border-bottom: 1px solid #f2f2f2;
        align-items: start;
      }
      .lines li.flash {
        background: #fff8f8;
      }
      .thumb {
        width: 56px;
        height: 56px;
        border-radius: 8px;
        overflow: hidden;
        background: #f6f6f6;
        display: grid;
        place-items: center;
        color: #999;
      }
      .thumb img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
      .body {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }
      .body strong {
        font-size: 0.88rem;
        line-height: 1.3;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
      }
      .meta {
        font-size: 0.78rem;
        color: var(--dx-mute, #777);
      }
      .warn {
        font-size: 0.75rem;
        color: var(--dx-red, #c62828);
        font-weight: 600;
      }
      .side {
        display: grid;
        gap: 0.35rem;
        justify-items: end;
      }
      .line-total {
        font-size: 0.88rem;
        font-weight: 700;
      }
      .linkish {
        border: 0;
        background: none;
        color: var(--dx-mute, #777);
        font-size: 0.75rem;
        cursor: pointer;
        padding: 0;
        text-decoration: underline;
      }
      .foot {
        border-top: 1px solid #ececec;
        padding: 1rem 1.1rem 1.25rem;
        display: grid;
        gap: 0.85rem;
      }
      .tot {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        font-size: 0.95rem;
      }
      .tot strong {
        font-size: 1.15rem;
        color: var(--dx-red, #c62828);
      }
      .actions {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.55rem;
      }
      .actions a {
        text-align: center;
        text-decoration: none;
      }
    `,
  ],
})
export class MiniCartDrawerComponent {
  readonly mini = inject(MiniCartService);
  readonly session = inject(SessionService);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  media(url: string): string {
    return this.api.resolveMediaUrl(url) || url;
  }

  remove(detalleId: number): void {
    this.api.removeCartItem(detalleId, this.session.canal()).subscribe({
      next: (cart) => {
        this.session.setCartCount(cart.itemsCount ?? cart.items?.length ?? 0);
        this.session.setCartTotal(cart.montoTotal ?? 0);
        this.mini.cart.set(cart);
        if (!cart.items?.length) this.toast.info('Carrito vacío');
      },
      error: (e) => this.toast.error(e?.error?.error || e.message || 'No se pudo quitar'),
    });
  }
}
