import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Cart, isLoggedIn } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { PersonalizacionService } from '../core/personalizacion.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from '../shared/dx-icon.component';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DecimalPipe, FormsModule, DxIconComponent],
  template: `
    <section class="cart" aria-labelledby="cart-title">
      <header>
        <h1 id="cart-title"><dx-icon name="cart" [size]="28" /> {{ txt().titulo || 'Carrito' }}</h1>
        <a routerLink="/catalogo" class="keep-shopping">
          <dx-icon name="grid" [size]="14" /> {{ txt().seguirComprando || 'Seguir comprando' }}
        </a>
      </header>

      @if (error()) {
        <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
      }

      @if (loading()) {
        <p class="loading" role="status" aria-busy="true">Cargando carrito…</p>
      } @else {
        @if (cart(); as c) {
          @if (c.items.length === 0) {
            <div class="empty">
              <dx-icon name="cart" [size]="40" />
              <p>{{ txt().vacioTitulo || 'Tu carrito está vacío.' }}</p>
              <p class="empty-sub">{{ txt().vacioSub || 'Agregá productos desde el catálogo para empezar.' }}</p>
              <a class="dx-btn" routerLink="/catalogo">
                <dx-icon name="grid" [size]="16" /> {{ txt().ctaCatalogo || 'Ir al catálogo' }}
              </a>
            </div>
          } @else {
            <ul class="lines">
              @for (line of c.items; track line.id) {
                <li [class.warn]="!line.stockOk">
                  <div
                    class="img"
                    role="img"
                    [attr.aria-label]="line.nombre"
                    [style.backgroundImage]="line.imagenUrl ? 'url(' + mediaUrl(line.imagenUrl) + ')' : 'none'"
                  ></div>
                  <div class="info">
                    <a [routerLink]="['/producto', line.idArticulo]">{{ line.nombre }}</a>
                    <small>{{ line.sku }} · {{ line.embalaje || 'Unidad' }}</small>
                    <small class="unit">
                      {{ line.precioUnitario | currency: 'PYG':'symbol-narrow':'1.0-0' }} × {{ line.cantidad }}
                    </small>
                    <small [class.out]="!line.stockOk">
                      Stock: {{ line.stockLibre | number: '1.0-0' }}
                      @if (!line.stockOk) {
                        · insuficiente
                      }
                    </small>
                  </div>
                  <div class="qty">
                    <label>
                      <span class="sr-only">Cantidad de {{ line.nombre }}</span>
                      <input
                        class="dx-input"
                        type="number"
                        min="0.01"
                        step="any"
                        [ngModel]="line.cantidad"
                        (change)="onQty(line.id, $event)"
                      />
                    </label>
                    <button type="button" class="link" (click)="remove(line.id)">Quitar</button>
                  </div>
                  <strong class="line-total">{{ line.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                </li>
              }
            </ul>

            <footer class="summary">
              <div class="totals">
                <div class="totals-rows">
                  <div class="row">
                    <span>Subtotal</span>
                    <span>{{ (c.montoTotal + (c.montoDescuento || 0)) | currency: 'PYG':'symbol-narrow':'1.0-0' }}</span>
                  </div>
                  @if (c.montoDescuento) {
                    <div class="row disc">
                      <span>Descuento</span>
                      <span>−{{ c.montoDescuento | currency: 'PYG':'symbol-narrow':'1.0-0' }}</span>
                    </div>
                  }
                  @if (c.montoIVA) {
                    <div class="row">
                      <span>IVA</span>
                      <span>{{ c.montoIVA | currency: 'PYG':'symbol-narrow':'1.0-0' }}</span>
                    </div>
                  }
                  <div class="row mute">
                    <span>{{ c.itemsCount }} ítem{{ c.itemsCount === 1 ? '' : 's' }}</span>
                    <span>Envío en el siguiente paso</span>
                  </div>
                </div>
                <div class="grand-block">
                  <span class="grand-label">Total</span>
                  <strong class="grand">{{ c.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                </div>
              </div>

              <div class="coupon">
                <label class="sr-only" for="cupon">Cupón</label>
                <input
                  id="cupon"
                  class="dx-input"
                  [(ngModel)]="cupon"
                  placeholder="¿Tenés un cupón?"
                  (keydown.enter)="aplicarCupon()"
                />
                <button type="button" class="dx-btn dx-btn--ghost" (click)="aplicarCupon()">Aplicar</button>
                @if (c.codigoCupon) {
                  <button type="button" class="link" (click)="quitarCupon()">Quitar {{ c.codigoCupon }}</button>
                }
              </div>

              <div class="checkout-panel">
                @if (hasStockIssue()) {
                  <p class="stock-warn" role="alert">
                    Hay productos sin stock suficiente. Ajustá las cantidades para continuar.
                  </p>
                  <button type="button" class="checkout-btn" disabled>
                    {{ txt().ctaStockBlock || 'Revisá stock para continuar' }}
                  </button>
                } @else {
                  <button type="button" class="checkout-btn" (click)="goCheckout()">
                    {{ txt().ctaCheckout || 'Continuar al checkout' }}
                    <dx-icon name="chevron-right" [size]="20" />
                  </button>
                  <p class="next-hint">Elegí entrega o retiro y confirmá el pedido.</p>
                }
              </div>

              <div class="secondary-actions">
                <a routerLink="/catalogo" class="sec-link">{{ txt().seguirComprando || 'Seguir comprando' }}</a>
                <button type="button" class="sec-link" [disabled]="savingLista()" (click)="guardarComoLista()">
                  {{ savingLista() ? 'Guardando…' : txt().ctaGuardarLista || 'Guardar como lista' }}
                </button>
                <button type="button" class="sec-link sec-link--danger" (click)="clear()">
                  {{ txt().ctaVaciar || 'Vaciar carrito' }}
                </button>
              </div>
            </footer>
          }
        }
      }
    </section>
  `,
  styles: [
    `
      header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        margin-bottom: 1.25rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 2rem);
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
      }
      .keep-shopping {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        color: var(--dx-red);
        text-decoration: none;
        font-weight: 600;
      }
      .keep-shopping:hover {
        text-decoration: underline;
      }
      .empty {
        display: grid;
        justify-items: center;
        gap: 0.5rem;
        background: #fff;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        padding: 2.5rem 1rem;
        text-align: center;
        color: var(--dx-ink-soft);
      }
      .empty p {
        margin: 0;
      }
      .empty-sub {
        font-size: 0.9rem;
        color: var(--dx-mute);
        margin-bottom: 0.35rem !important;
      }
      .empty dx-icon {
        color: var(--dx-mute);
      }
      .lines {
        list-style: none;
        padding: 0;
        margin: 0;
        display: grid;
        gap: 0.75rem;
      }
      li {
        display: grid;
        grid-template-columns: 72px 1fr auto auto;
        gap: 0.85rem;
        align-items: center;
        padding: 0.85rem;
        border-radius: 12px;
        background: #fff;
        border: 1px solid var(--dx-line);
      }
      li.warn {
        border-color: #f5c2c0;
        background: #fff8f8;
      }
      .img {
        width: 72px;
        height: 72px;
        border-radius: 10px;
        background: #f3f3f3 center/cover no-repeat;
      }
      .info {
        display: grid;
        gap: 0.2rem;
      }
      .info a {
        color: var(--dx-ink);
        text-decoration: none;
        font-weight: 600;
      }
      .info a:hover {
        color: var(--dx-red);
      }
      .info small {
        color: var(--dx-mute);
      }
      .unit {
        color: var(--dx-ink-soft) !important;
        font-weight: 600;
      }
      .line-total {
        color: var(--dx-price, var(--dx-red));
        font-family: var(--dx-display);
      }
      .out {
        color: var(--dx-warn) !important;
      }
      .qty {
        display: grid;
        gap: 0.35rem;
        justify-items: center;
      }
      .qty .dx-input {
        width: 72px;
      }
      .link {
        background: none;
        border: none;
        color: var(--dx-red);
        cursor: pointer;
        font: inherit;
        font-weight: 600;
      }
      .summary {
        margin-top: 1.25rem;
        padding: 1.25rem 1.35rem;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        display: grid;
        gap: 1.1rem;
        box-shadow: var(--dx-shadow);
      }
      .totals-rows {
        display: grid;
        gap: 0.4rem;
      }
      .totals .row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        font-size: 0.95rem;
        color: var(--dx-ink-soft);
      }
      .totals .row.disc {
        color: #065f46;
        font-weight: 650;
      }
      .totals .row.mute {
        font-size: 0.82rem;
        color: var(--dx-mute);
      }
      .grand-block {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 1rem;
        margin-top: 0.65rem;
        padding-top: 0.75rem;
        border-top: 1px solid var(--dx-line);
      }
      .grand-label {
        font-weight: 700;
        font-size: 1.05rem;
        color: var(--dx-ink);
      }
      .grand {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.55rem, 4vw, 1.9rem);
        color: var(--dx-price);
        font-weight: 800;
        letter-spacing: -0.02em;
      }
      .coupon {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        align-items: center;
      }
      .coupon .dx-input {
        flex: 1 1 160px;
        min-width: 0;
      }
      .checkout-panel {
        display: grid;
        gap: 0.45rem;
      }
      .checkout-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.45rem;
        width: 100%;
        min-height: 54px;
        padding: 0.85rem 1.25rem;
        border: none;
        border-radius: 12px;
        background: var(--dx-red);
        color: #fff !important;
        font-family: var(--dx-font);
        font-size: 1.08rem;
        font-weight: 800;
        cursor: pointer;
        box-shadow: 0 8px 20px rgba(198, 40, 40, 0.22);
        transition: background 0.18s ease, transform 0.15s ease, box-shadow 0.18s ease;
      }
      .checkout-btn:hover:not(:disabled) {
        background: var(--dx-red-dark);
        color: #fff !important;
        box-shadow: 0 10px 24px rgba(142, 0, 0, 0.28);
      }
      .checkout-btn:active:not(:disabled) {
        transform: translateY(1px);
      }
      .checkout-btn:disabled {
        opacity: 0.55;
        cursor: not-allowed;
        box-shadow: none;
      }
      .checkout-btn dx-icon {
        color: #fff !important;
      }
      .next-hint {
        margin: 0;
        text-align: center;
        font-size: 0.82rem;
        color: var(--dx-mute);
      }
      .stock-warn {
        margin: 0;
        padding: 0.65rem 0.75rem;
        border-radius: 10px;
        background: #fff5f5;
        border: 1px solid #f5c2c0;
        color: var(--dx-warn);
        font-size: 0.88rem;
        font-weight: 600;
      }
      .secondary-actions {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 0.35rem 1rem;
        padding-top: 0.15rem;
        border-top: 1px dashed var(--dx-line);
      }
      .sec-link {
        background: none;
        border: none;
        padding: 0.25rem 0;
        color: var(--dx-ink-soft);
        font: inherit;
        font-size: 0.9rem;
        font-weight: 600;
        text-decoration: none;
        cursor: pointer;
      }
      .sec-link:hover:not(:disabled) {
        color: var(--dx-red);
        text-decoration: underline;
      }
      .sec-link:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .sec-link--danger:hover:not(:disabled) {
        color: var(--dx-warn);
      }
      .loading {
        color: var(--dx-mute, var(--dx-ink-soft));
        padding: 1.5rem 0;
      }
      @media (max-width: 700px) {
        li {
          grid-template-columns: 64px 1fr;
        }
        .qty,
        .line-total {
          grid-column: 2;
        }
        .summary {
          position: sticky;
          bottom: 0;
          z-index: 20;
          margin-left: -0.25rem;
          margin-right: -0.25rem;
          border-radius: 16px 16px 0 0;
          box-shadow: 0 -10px 28px rgba(0, 0, 0, 0.1);
          padding-bottom: calc(1rem + env(safe-area-inset-bottom, 0px));
        }
        .secondary-actions {
          justify-content: space-between;
        }
      }
    `,
  ],
})
export class CartPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthModalService);
  private readonly pers = inject(PersonalizacionService);
  private readonly router = inject(Router);

  readonly txt = this.pers.carritoTextos;
  readonly cart = signal<Cart | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly savingLista = signal(false);
  cupon = '';

  ngOnInit(): void {
    this.reload();
  }

  goCheckout(): void {
    void this.router.navigateByUrl('/checkout');
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  hasStockIssue(): boolean {
    return (this.cart()?.items ?? []).some((i) => !i.stockOk);
  }

  private fail(msg: string): void {
    this.error.set(msg);
    this.toast.error(msg);
  }

  private syncBadge(c: Cart): void {
    this.session.setCartCount(c.itemsCount ?? c.items?.length ?? 0);
    this.session.setCartTotal(c.montoTotal ?? 0);
  }

  guardarComoLista(): void {
    if (!isLoggedIn()) {
      this.toast.info('Iniciá sesión para guardar el carrito como lista');
      this.auth.showLogin();
      return;
    }
    this.savingLista.set(true);
    this.api.guardarCarritoComoLista().subscribe({
      next: (sec) => {
        this.savingLista.set(false);
        this.toast.success(
          `Lista «${sec.nombreLista}» creada con ${sec.itemsCount} productos. Ver en Favoritos.`,
        );
      },
      error: (e) => {
        this.savingLista.set(false);
        this.fail(e?.error?.error || e.message || 'No se pudo guardar la lista');
      },
    });
  }

  reload(): void {
    this.loading.set(true);
    this.api.getCart().subscribe({
      next: (c) => {
        this.cart.set(c);
        this.syncBadge(c);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.fail(e?.error?.error || e.message || 'Error al cargar carrito');
      },
    });
  }

  aplicarCupon(): void {
    const code = this.cupon.trim();
    if (!code) {
      this.toast.info('Ingresá un código de cupón');
      return;
    }
    this.api.aplicarCupon(code).subscribe({
      next: (r) => {
        if (!r.ok) {
          this.fail(r.mensaje || 'Cupón inválido');
        } else {
          this.error.set(null);
          this.toast.success(r.mensaje || 'Cupón aplicado');
        }
        this.reload();
      },
      error: (e) => this.fail(e?.error?.error || e.message || 'Cupón inválido'),
    });
  }

  quitarCupon(): void {
    this.api.quitarCupon().subscribe({
      next: () => {
        this.toast.info('Cupón quitado');
        this.reload();
      },
      error: (e) => this.fail(e?.error?.error || e.message || 'No se pudo quitar el cupón'),
    });
  }

  onQty(detalleId: number, ev: Event): void {
    const raw = (ev.target as HTMLInputElement).value;
    const value = Number(raw);
    if (!Number.isFinite(value) || value <= 0) {
      this.toast.error('Ingresá una cantidad válida (puede ser decimal si el producto se vende por metro)');
      this.reload();
      return;
    }
    this.api.updateCartItem(detalleId, value).subscribe({
      next: () => {
        this.error.set(null);
        this.toast.success('Cantidad actualizada');
        this.api.getCart(this.session.canal()).subscribe({
          next: (c) => {
            this.cart.set(c);
            this.syncBadge(c);
          },
          error: () => this.reload(),
        });
      },
      error: (e) => {
        this.fail(e?.error?.error || e.message || 'No se pudo actualizar');
        this.reload();
      },
    });
  }

  remove(detalleId: number): void {
    this.api.removeCartItem(detalleId).subscribe({
      next: (c) => {
        this.cart.set(c);
        this.syncBadge(c);
        this.toast.info('Producto quitado del carrito');
      },
      error: (e) => this.fail(e?.error?.error || e.message || 'No se pudo quitar'),
    });
  }

  clear(): void {
    this.api.clearCart().subscribe({
      next: () => {
        this.toast.info('Carrito vaciado');
        this.reload();
      },
      error: (e) => this.fail(e?.error?.error || e.message || 'No se pudo vaciar'),
    });
  }
}
