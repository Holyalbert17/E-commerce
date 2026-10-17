import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Title, Meta } from '@angular/platform-browser';
import {
  ApiService,
  FavoritoSeccion,
  ProductDetail,
  Recomendacion,
  Resena,
  getCanal,
  isLoggedIn,
} from '../core/api.service';
import { DxIconComponent } from '../shared/dx-icon.component';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { BrandingService } from '../core/branding.service';
import { PersonalizacionService } from '../core/personalizacion.service';
import { ToastService } from '../core/toast.service';
import { MiniCartService } from '../core/mini-cart.service';

type PdpTab = 'desc' | 'opiniones' | 'relacionados';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DecimalPipe, FormsModule, DxIconComponent],
  template: `
    <nav class="crumb" aria-label="Miga de pan">
      <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
      <span aria-hidden="true">/</span>
      <span>{{ product()?.nombre || 'Producto' }}</span>
    </nav>

    @if (error()) {
      <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
    }

    @if (loading()) {
      <div class="pdp-skel" role="status" aria-busy="true" aria-label="Cargando producto">
        <div class="skel-gallery">
          <div class="skel-main"></div>
        </div>
        <div class="skel-buy">
          <div class="skel-line brand"></div>
          <div class="skel-line title"></div>
          <div class="skel-line short"></div>
          <div class="skel-line price"></div>
          <div class="skel-btn"></div>
        </div>
      </div>
    }

    @if (product(); as p) {
      <article class="pdp" id="producto-detalle">
        <div class="gallery">
          <div
            class="main"
            role="img"
            [attr.aria-label]="'Imagen de ' + p.nombre"
            [style.backgroundImage]="activeImage() ? 'url(' + mediaUrl(activeImage()) + ')' : 'none'"
            (mousemove)="onZoom($event)"
            (touchmove)="onZoomTouch($event)"
            (mouseleave)="zoomStyle.set(null)"
            (touchend)="zoomStyle.set(null)"
            [style.backgroundSize]="zoomStyle() ? '180%' : 'contain'"
            [style.backgroundPosition]="zoomStyle() || 'center'"
          ></div>
          @if (p.imagenes.length > 1) {
            <div class="thumbs" role="list">
              @for (img of p.imagenes; track img.id) {
                <button
                  type="button"
                  class="thumb"
                  role="listitem"
                  [class.active]="activeImage() === img.url"
                  [style.backgroundImage]="'url(' + mediaUrl(img.url) + ')'"
                  [attr.aria-label]="img.titulo || 'Miniatura'"
                  [attr.aria-pressed]="activeImage() === img.url"
                  (click)="activeImage.set(img.url)"
                ></button>
              }
            </div>
          }
        </div>

        <div class="buybox">
          <p class="brand">{{ p.marca || 'Sin marca' }}</p>
          <h1>{{ p.nombre }}</h1>

          <button type="button" class="rating" (click)="openTab('opiniones')" [attr.aria-label]="ratingAria(p)">
            <span aria-hidden="true">{{ starsLabel(p.promedioCalificacion || 0) }}</span>
            <span>{{ (p.promedioCalificacion || 0) | number: '1.1-1' }}</span>
            <span class="mute">({{ p.totalResenas || 0 }} reseñas)</span>
          </button>

          <p class="sku">{{ p.sku }} · {{ p.estadoEcommerce }} · {{ p.canal }}</p>

          <p class="price" aria-live="polite">
            @if (selectedEmbObj(); as emb) {
              @if (emb.porcentajeDescuento != null && emb.porcentajeDescuento > 0) {
                <span class="off-badge">{{ emb.porcentajeDescuento | number: '1.0-0' }}% OFF</span>
              }
            }
            @if (selectedPrice() != null) {
              {{ selectedPrice()! | currency: 'PYG':'symbol-narrow':'1.0-0' }}
              @if (selectedEmbObj(); as emb) {
                @if (emb.precioLista != null && emb.precioLista > (emb.precio ?? 0)) {
                  <s class="list-price">{{ emb.precioLista | currency: 'PYG':'symbol-narrow':'1.0-0' }}</s>
                }
                @if (embPriceUnit(emb); as u) {
                  <small class="price-unit">/ {{ u }}</small>
                }
              } @else if (qtyUnitLabel()) {
                <small class="price-unit">/ {{ qtyUnitLabel() }}</small>
              }
            } @else {
              Consultar precio
            }
          </p>
          @if (lineEstimate() != null && isMeterSale()) {
            <p class="line-total" aria-live="polite">
              Total
              <strong>{{ lineEstimate()! | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
              <span class="mute">por {{ qty | number: '1.0-2' }} {{ qtyUnitPhrase(qty) }}</span>
            </p>
          }

          <p class="stock" [class.out]="!p.stockOk">
            @if (p.stockOk) {
              En stock
              @if (p.stockLibre != null) {
                · {{ formatStock(p.stockLibre) }} {{ stockUnitLabel(p.stockLibre) }} disponibles
              }
            } @else {
              Sin stock
            }
          </p>

          @if (p.embalajes.length) {
            <fieldset class="packs">
              <legend>{{ p.embalajes.length > 1 ? 'Elegí cómo comprar' : 'Cómo se vende' }}</legend>
              <div class="pack-list" [class.pack-list--single]="p.embalajes.length === 1">
                @for (e of p.embalajes; track e.id) {
                  <label class="pack" [class.selected]="selectedEmb() === e.id">
                    <input
                      type="radio"
                      name="emb"
                      [value]="e.id"
                      [checked]="selectedEmb() === e.id"
                      (change)="onEmbChange(e.id)"
                    />
                    <span>
                      <strong>{{ embTitle(e) }}</strong>
                      <small>{{ embSubtitle(e) }}</small>
                      @if (embStockText(p, e); as st) {
                        <small class="pack-stock">{{ st }}</small>
                      }
                    </span>
                    <em>
                      @if (e.precio != null) {
                        {{ e.precio | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                        @if (embPriceUnit(e); as u) {
                          <small>/ {{ u }}</small>
                        }
                      } @else {
                        —
                      }
                    </em>
                  </label>
                }
              </div>
            </fieldset>
          }

          <div class="qty-box" [class.qty-box--meter]="isMeterSale()">
            <label class="qty-label" for="pdp-qty">
              {{ isMeterSale() ? 'Cantidad (metros)' : 'Cantidad' }}
            </label>
            <div class="qty-row">
              <div class="qty" role="group" [attr.aria-label]="isMeterSale() ? 'Metros' : 'Cantidad'">
                <button
                  type="button"
                  class="qty-btn"
                  (click)="bumpQty(-1)"
                  [disabled]="!canBump(-1)"
                  [attr.aria-label]="'Restar ' + (qtyUnitLabel() || 'unidad')"
                >
                  −
                </button>
                <input
                  id="pdp-qty"
                  class="dx-input"
                  type="number"
                  [min]="qtyMin()"
                  [step]="isMeterSale() ? 'any' : qtyStep()"
                  [(ngModel)]="qty"
                  (ngModelChange)="onQtyChange($event)"
                  [attr.aria-label]="(isMeterSale() ? 'Metros' : 'Cantidad') + (qtyUnitLabel() ? ' en ' + qtyUnitLabel() : '')"
                />
                <button
                  type="button"
                  class="qty-btn"
                  (click)="bumpQty(1)"
                  [attr.aria-label]="'Sumar ' + (qtyUnitLabel() || 'unidad')"
                >
                  +
                </button>
              </div>
              @if (qtyUnitLabel()) {
                <span class="qty-unit">{{ qtyUnitPhrase(qty) }}</span>
              }
              @if (lineEstimate() != null && !isMeterSale()) {
                <p class="qty-total" aria-live="polite">
                  Subtotal
                  <strong>{{ lineEstimate()! | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                </p>
              }
            </div>
            @if (isMeterSale()) {
              <p class="qty-hint">
                Facturás exactamente lo pedido (ej. 7,9 m). Mín.
                {{ qtyMin() | number: '1.0-2' }} {{ qtyUnitPhrase(qtyMin()) }}
                · ±{{ qtyStep() | number: '1.0-2' }}
              </p>
            }
          </div>

          <div class="actions">
            <button type="button" class="dx-btn dx-btn--lg add-desk" [disabled]="!p.stockOk" (click)="addToCart()">
              <dx-icon name="cart" [size]="18" /> Agregar al carrito
            </button>
            @if (logged) {
              <button
                type="button"
                class="dx-btn dx-btn--ghost"
                [class.fav-on]="enFavoritos()"
                (click)="toggleFav(p.id)"
                [attr.title]="enFavoritos() ? 'Quitar de favoritos' : 'Agregar a favoritos'"
                [attr.aria-pressed]="enFavoritos()"
              >
                <dx-icon name="heart" [size]="16" [filled]="enFavoritos()" />
              </button>
              @if (pers.widgets().seguidos) {
                <button
                  type="button"
                  class="dx-btn dx-btn--ghost"
                  [class.seguido-on]="siguiendo()"
                  (click)="toggleSeguir(p.id)"
                  [attr.title]="siguiendo() ? 'Dejar de seguir' : 'Seguir producto'"
                  [attr.aria-pressed]="siguiendo()"
                >
                  <dx-icon name="star" [size]="16" [filled]="siguiendo()" />
                </button>
              }
            }
          </div>

          <div class="mobile-buy" aria-label="Comprar">
            <div class="mobile-buy__price">
              @if (lineEstimate() != null) {
                {{ lineEstimate()! | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                @if (isMeterSale()) {
                  <small>{{ qty | number: '1.0-2' }} {{ qtyUnitPhrase(qty) }}</small>
                }
              } @else if (selectedPrice() != null) {
                {{ selectedPrice()! | currency: 'PYG':'symbol-narrow':'1.0-0' }}
              } @else {
                Consultar
              }
            </div>
            <button type="button" class="dx-btn dx-btn--lg" [disabled]="!p.stockOk" (click)="addToCart()">
              <dx-icon name="cart" [size]="18" /> Agregar
            </button>
          </div>

          @if (!logged) {
            <p class="hint">
              <button type="button" class="linkish" (click)="auth.showLogin()">
                <dx-icon name="login" [size]="14" /> Ingresá
              </button>
              para favoritos y reseñas.
            </p>
          }
        </div>
      </article>

      <div class="tabs" role="tablist" aria-label="Detalle del producto">
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="tab() === 'desc'"
          [class.on]="tab() === 'desc'"
          (click)="openTab('desc')"
        >
          Descripción
        </button>
        @if (pers.widgets().resenas) {
          <button
            type="button"
            role="tab"
            [attr.aria-selected]="tab() === 'opiniones'"
            [class.on]="tab() === 'opiniones'"
            (click)="openTab('opiniones')"
          >
            Opiniones ({{ resenas().length || p.totalResenas || 0 }})
          </button>
        }
        @if (recs().length) {
          <button
            type="button"
            role="tab"
            [attr.aria-selected]="tab() === 'relacionados'"
            [class.on]="tab() === 'relacionados'"
            (click)="openTab('relacionados')"
          >
            Relacionados
          </button>
        }
      </div>

      @if (tab() === 'desc') {
        <section class="block" role="tabpanel" aria-label="Descripción">
          @if (p.descripcionExtensa) {
            <p class="desc" [class.clamp]="!descOpen() && longDesc(p.descripcionExtensa)">
              {{ p.descripcionExtensa }}
            </p>
            @if (longDesc(p.descripcionExtensa)) {
              <button type="button" class="more" (click)="descOpen.set(!descOpen())">
                {{ descOpen() ? 'Ver menos' : 'Ver más' }}
              </button>
            }
          } @else {
            <p class="mute">Sin descripción adicional.</p>
          }

          @if (logged) {
            <div class="fav-row">
              <select class="dx-select" [(ngModel)]="listaFavId" aria-label="Sección de favoritos">
                <option [ngValue]="null">Mis favoritos</option>
                @for (s of secciones(); track s.id) {
                  <option [ngValue]="s.id">{{ s.nombreLista }}</option>
                }
              </select>
              <input class="dx-input" [(ngModel)]="nuevaSeccion" placeholder='Nueva sección (ej. "bebidas")' />
              <button
                type="button"
                class="dx-btn dx-btn--ghost"
                [class.fav-on]="enFavoritos()"
                (click)="toggleFav(p.id)"
                [attr.aria-pressed]="enFavoritos()"
              >
                <dx-icon name="heart" [size]="16" [filled]="enFavoritos()" />
                {{ enFavoritos() ? 'En favoritos' : 'Guardar' }}
              </button>
              @if (pers.widgets().seguidos) {
                <button
                  type="button"
                  class="dx-btn dx-btn--ghost"
                  [class.seguido-on]="siguiendo()"
                  (click)="toggleSeguir(p.id)"
                  [attr.aria-pressed]="siguiendo()"
                >
                  <dx-icon name="star" [size]="16" [filled]="siguiendo()" />
                  {{ siguiendo() ? 'Siguiendo' : 'Seguir' }}
                </button>
              }
            </div>
          }
        </section>
      }

      @if (tab() === 'opiniones' && pers.widgets().resenas) {
        <section class="block" role="tabpanel" aria-labelledby="rev-title">
          <h2 id="rev-title" class="sr-only">Opiniones de clientes</h2>
          @for (r of resenas(); track r.id) {
            <article class="rev">
              <header>
                <strong [attr.aria-label]="r.puntuacion + ' estrellas'">{{ starsLabel(r.puntuacion) }}</strong>
                <span>{{ r.nombreAutor || r.email }}</span>
              </header>
              @if (r.titulo) {
                <h3>{{ r.titulo }}</h3>
              }
              <p>{{ r.comentario }}</p>
            </article>
          } @empty {
            <p class="mute">Todavía no hay reseñas.</p>
          }
          <a routerLink="/mi-cuenta">Escribir reseña después de comprar</a>
        </section>
      }

      @if (tab() === 'relacionados' && recs().length) {
        <section class="block" role="tabpanel" aria-labelledby="rec-title">
          <h2 id="rec-title" class="dx-section-title">También te puede interesar</h2>
          <div class="dx-product-grid">
            @for (x of recs(); track x.id) {
              <a class="dx-card" [routerLink]="['/producto', x.id]">
                <div
                  class="dx-card__media"
                  [style.backgroundImage]="x.imagenUrl ? 'url(' + mediaUrl(x.imagenUrl) + ')' : 'none'"
                ></div>
                <div class="dx-card__body">
                  <h3 class="dx-card__title">{{ x.nombre }}</h3>
                  <p class="motivo">{{ x.motivo }}</p>
                  @if (x.precioDesde != null) {
                    <p class="dx-card__price">{{ x.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</p>
                  }
                </div>
              </a>
            }
          </div>
        </section>
      }
    }
  `,
  styles: [
    `
      .crumb {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 0.75rem;
        font-size: 0.9rem;
        color: var(--dx-mute);
      }
      .crumb a {
        color: var(--dx-ink-soft);
        text-decoration: none;
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
      }
      .crumb a:hover {
        color: var(--dx-red);
      }
      .pdp-skel {
        display: grid;
        grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr);
        gap: clamp(1rem, 2.5vw, 1.75rem);
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: clamp(0.9rem, 2vw, 1.25rem);
      }
      @media (max-width: 860px) {
        .pdp-skel {
          grid-template-columns: 1fr;
        }
      }
      .skel-main {
        aspect-ratio: 1;
        border-radius: 12px;
        background: linear-gradient(90deg, #f1f5f9 25%, #e2e8f0 50%, #f1f5f9 75%);
        background-size: 200% 100%;
        animation: shimmer 1.2s infinite;
      }
      .skel-buy {
        display: grid;
        gap: 0.65rem;
        align-content: start;
      }
      .skel-line {
        height: 14px;
        border-radius: 7px;
        background: #e2e8f0;
      }
      .skel-line.brand {
        width: 30%;
      }
      .skel-line.title {
        height: 28px;
        width: 85%;
      }
      .skel-line.short {
        width: 50%;
      }
      .skel-line.price {
        height: 32px;
        width: 40%;
      }
      .skel-btn {
        height: 44px;
        width: 180px;
        border-radius: 10px;
        background: #e2e8f0;
        margin-top: 0.5rem;
      }
      @keyframes shimmer {
        0% {
          background-position: 200% 0;
        }
        100% {
          background-position: -200% 0;
        }
      }
      .pdp {
        display: grid;
        grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr);
        gap: clamp(1rem, 2.5vw, 1.75rem);
        align-items: start;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: clamp(0.9rem, 2vw, 1.25rem);
      }
      @media (max-width: 860px) {
        .pdp {
          grid-template-columns: 1fr;
        }
      }
      .gallery {
        position: sticky;
        top: 0.75rem;
      }
      @media (max-width: 860px) {
        .gallery {
          position: static;
        }
      }
      .main {
        width: 100%;
        max-height: min(48vh, 420px);
        aspect-ratio: 1;
        border-radius: 12px;
        background: #f6f6f6 center/contain no-repeat;
        border: 1px solid var(--dx-line);
        cursor: zoom-in;
      }
      .thumbs {
        display: flex;
        gap: 0.45rem;
        margin-top: 0.6rem;
        flex-wrap: wrap;
      }
      .thumb {
        width: 52px;
        height: 52px;
        border-radius: 8px;
        border: 2px solid transparent;
        background: #f3f3f3 center/cover no-repeat;
        cursor: pointer;
      }
      .thumb.active {
        border-color: var(--dx-red);
      }
      .buybox {
        display: grid;
        gap: 0.45rem;
        align-content: start;
      }
      .brand {
        margin: 0;
        color: var(--dx-red);
        font-weight: 700;
        text-transform: uppercase;
        font-size: 0.78rem;
        letter-spacing: 0.05em;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.25rem, 2.6vw, 1.85rem);
        line-height: 1.15;
      }
      .rating {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        width: fit-content;
        margin: 0;
        padding: 0;
        border: none;
        background: none;
        color: #f59e0b;
        font: inherit;
        cursor: pointer;
      }
      .rating:hover .mute {
        color: var(--dx-red);
        text-decoration: underline;
      }
      .mute {
        color: var(--dx-mute);
      }
      .sku {
        margin: 0;
        color: var(--dx-mute);
        font-size: 0.85rem;
      }
      .price {
        margin: 0.15rem 0;
        font-family: var(--dx-display);
        font-size: clamp(1.45rem, 3vw, 1.9rem);
        font-weight: 800;
        color: var(--dx-price, var(--dx-red));
        line-height: 1.1;
        display: flex;
        flex-wrap: wrap;
        align-items: baseline;
        gap: 0.45rem;
      }
      .off-badge {
        display: inline-block;
        padding: 0.15rem 0.45rem;
        border-radius: 6px;
        background: var(--dx-red);
        color: #fff;
        font-size: 0.72rem;
        font-weight: 800;
        letter-spacing: 0.02em;
      }
      .list-price {
        font-size: 0.95rem;
        color: var(--dx-mute);
        font-weight: 600;
      }
      .price-unit {
        margin-left: 0.25rem;
        font-size: 0.55em;
        font-weight: 700;
        color: var(--dx-ink-soft);
        vertical-align: middle;
      }
      .line-total {
        margin: 0;
        font-size: 0.95rem;
        color: var(--dx-ink-soft);
      }
      .line-total strong {
        color: var(--dx-ink);
        font-size: 1.05rem;
        margin: 0 0.25rem;
      }
      .stock {
        margin: 0;
        font-weight: 700;
        color: var(--dx-ok);
        font-size: 0.95rem;
      }
      .stock.out {
        color: var(--dx-warn);
      }
      .packs {
        border: none;
        margin: 0.35rem 0 0;
        padding: 0;
      }
      .packs legend {
        font-weight: 700;
        margin-bottom: 0.35rem;
        font-size: 0.9rem;
      }
      .pack-list {
        display: grid;
        gap: 0.4rem;
        max-height: 9.5rem;
        overflow: auto;
      }
      .pack-list--single {
        max-height: none;
        overflow: visible;
      }
      .pack {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 0.65rem;
        align-items: center;
        padding: 0.55rem 0.65rem;
        border: 1px solid var(--dx-line);
        border-radius: 10px;
        cursor: pointer;
        font-size: 0.92rem;
      }
      .pack.selected {
        border-color: var(--dx-red);
        background: var(--dx-red-soft);
      }
      .pack small {
        display: block;
        color: var(--dx-mute);
      }
      .pack-stock {
        color: var(--dx-ink, #334155) !important;
        font-weight: 600;
      }
      .pack-hint {
        margin: 0.45rem 0 0;
        font-size: 0.8rem;
        color: var(--dx-mute);
      }
      .pack em {
        font-style: normal;
        font-family: var(--dx-display);
        font-weight: 700;
        color: var(--dx-price, var(--dx-red));
        white-space: nowrap;
      }
      .qty-box {
        display: grid;
        gap: 0.4rem;
        min-width: 0;
        margin-top: 0.75rem;
        padding-top: 0.75rem;
        border-top: 1px solid var(--dx-line);
      }
      .qty-box--meter {
        background: transparent;
        border-left: none;
        border-right: none;
        border-bottom: none;
        border-radius: 0;
        padding-left: 0;
        padding-right: 0;
      }
      .qty-label {
        font-size: 0.9rem;
        font-weight: 700;
        color: var(--dx-ink);
      }
      .qty-row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.55rem 0.75rem;
      }
      .qty {
        display: inline-flex;
        align-items: center;
        gap: 0;
        border: 1px solid var(--dx-line);
        border-radius: 10px;
        overflow: hidden;
        background: #fff;
      }
      .qty .dx-input {
        width: 88px;
        text-align: center;
        font-weight: 700;
        border: none;
        border-radius: 0;
        border-left: 1px solid var(--dx-line);
        border-right: 1px solid var(--dx-line);
        height: 40px;
        box-shadow: none;
      }
      .qty-btn {
        width: 40px;
        height: 40px;
        border: none;
        border-radius: 0;
        background: #fff;
        font-size: 1.2rem;
        font-weight: 700;
        cursor: pointer;
        color: var(--dx-ink);
        line-height: 1;
      }
      .qty-btn:hover:not(:disabled) {
        color: var(--dx-red);
        background: var(--dx-red-soft);
      }
      .qty-btn:disabled {
        opacity: 0.4;
        cursor: not-allowed;
      }
      .qty-unit {
        font-weight: 700;
        color: var(--dx-ink-soft);
        font-size: 0.95rem;
      }
      .qty-hint {
        margin: 0;
        font-size: 0.78rem;
        color: var(--dx-mute);
        line-height: 1.35;
        max-width: 28rem;
      }
      .qty-total {
        margin: 0 0 0 auto;
        font-size: 0.92rem;
        color: var(--dx-ink-soft);
      }
      .qty-total strong {
        color: var(--dx-price, var(--dx-red));
        font-family: var(--dx-display);
        font-size: 1.05rem;
        margin-left: 0.25rem;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        align-items: center;
        margin-top: 0.85rem;
        position: static;
        padding: 0;
        background: none;
      }
      .actions .add-desk {
        flex: 1 1 200px;
        min-width: min(100%, 12rem);
      }
      .mobile-buy {
        display: none;
      }
      @media (max-width: 700px) {
        .actions .add-desk {
          display: none;
        }
        .mobile-buy {
          display: flex;
          position: fixed;
          left: 0;
          right: 0;
          bottom: 0;
          z-index: 40;
          gap: 0.75rem;
          align-items: center;
          padding: 0.75rem 1rem calc(0.75rem + env(safe-area-inset-bottom, 0px));
          background: #fff;
          border-top: 1px solid var(--dx-line);
          box-shadow: 0 -8px 24px rgba(0, 0, 0, 0.08);
        }
        .mobile-buy__price {
          flex: 1;
          font-family: var(--dx-display);
          font-size: 1.2rem;
          font-weight: 800;
          color: var(--dx-price, var(--dx-red));
        }
        .mobile-buy .dx-btn {
          flex: 1.2;
        }
        :host {
          padding-bottom: 5.5rem;
        }
      }
      .mobile-buy__price small {
        display: block;
        font-size: 0.72rem;
        font-weight: 600;
        color: var(--dx-mute);
        margin-top: 0.1rem;
      }
      .hint {
        margin: 0.15rem 0 0;
        font-size: 0.9rem;
        color: var(--dx-ink-soft);
      }
      .hint .linkish {
        font-weight: 700;
        display: inline-flex;
        align-items: center;
        gap: 0.2rem;
        background: none;
        border: none;
        color: var(--dx-red);
        font: inherit;
        cursor: pointer;
        padding: 0;
      }
      .tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        margin: 1.1rem 0 0.65rem;
      }
      .tabs button {
        padding: 0.5rem 0.95rem;
        border-radius: 999px;
        border: 1px solid var(--dx-line);
        background: #fff;
        color: var(--dx-ink);
        font: inherit;
        font-weight: 650;
        cursor: pointer;
      }
      .tabs button.on {
        background: var(--dx-red);
        color: #fff;
        border-color: transparent;
      }
      .block {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.1rem 1.2rem;
      }
      .desc {
        margin: 0;
        color: var(--dx-ink-soft);
        white-space: pre-wrap;
        line-height: 1.5;
      }
      .desc.clamp {
        display: -webkit-box;
        -webkit-line-clamp: 5;
        -webkit-box-orient: vertical;
        overflow: hidden;
      }
      .more {
        margin-top: 0.65rem;
        border: none;
        background: none;
        color: var(--dx-red);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        padding: 0;
      }
      .fav-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        align-items: center;
        margin-top: 1rem;
        padding-top: 1rem;
        border-top: 1px solid var(--dx-line);
      }
      .seguido-on {
        color: #f59e0b;
        border-color: #fcd34d;
        background: #fffbeb;
      }
      .seguido-on dx-icon {
        color: #f59e0b;
      }
      .fav-on {
        color: #c62828;
        border-color: #ef9a9a;
        background: #ffebee;
      }
      .fav-on dx-icon {
        color: #c62828;
      }
      .motivo {
        margin: 0;
        color: var(--dx-mute);
        font-size: 0.8rem;
      }
      .rev {
        padding: 0.85rem 0;
        border-bottom: 1px solid var(--dx-line);
      }
      .rev header {
        display: flex;
        gap: 0.75rem;
        align-items: center;
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
export class ProductPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthModalService);
  private readonly session = inject(SessionService);
  private readonly brand = inject(BrandingService);
  readonly pers = inject(PersonalizacionService);
  private readonly toast = inject(ToastService);
  private readonly miniCart = inject(MiniCartService);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  readonly product = signal<ProductDetail | null>(null);
  readonly loading = signal(false);
  readonly activeImage = signal<string | null>(null);
  readonly zoomStyle = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly selectedEmb = signal<number | null>(null);
  readonly resenas = signal<Resena[]>([]);
  readonly recs = signal<Recomendacion[]>([]);
  readonly secciones = signal<FavoritoSeccion[]>([]);
  readonly siguiendo = signal(false);
  readonly enFavoritos = signal(false);
  readonly tab = signal<PdpTab>('desc');
  readonly descOpen = signal(false);

  qty = 1;
  logged = isLoggedIn();
  listaFavId: number | null = null;
  nuevaSeccion = '';

  private shopName(): string {
    return this.brand.branding()?.nombreTienda?.trim() || 'Casa Imperial';
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((pm) => {
      const key = pm.get('id');
      if (!key) {
        this.error.set('Producto inválido');
        return;
      }
      this.loadProduct(key);
    });
  }

  ngOnDestroy(): void {
    this.title.setTitle(this.shopName());
    this.meta.removeTag("name='description'");
  }

  private loadProduct(key: string): void {
    this.scrollToTop();
    this.tab.set('desc');
    this.descOpen.set(false);
    this.error.set(null);
    this.product.set(null);
    this.loading.set(true);
    this.title.setTitle(`Cargando… | ${this.shopName()}`);
    this.resenas.set([]);
    this.recs.set([]);
    this.siguiendo.set(false);
    this.enFavoritos.set(false);
    this.logged = isLoggedIn();

    this.api.productByKey(key).subscribe({
      next: (p) => {
        this.product.set(p);
        this.loading.set(false);
        this.title.setTitle(`${p.nombre} | ${this.shopName()}`);
        const desc =
          (p.nombreCorto || p.descripcionExtensa || p.nombre || '')
            .replace(/\s+/g, ' ')
            .trim()
            .slice(0, 160) || `${p.nombre} en ${this.shopName()}`;
        this.meta.updateTag({ name: 'description', content: desc });
        this.activeImage.set(p.imagenes.find((i) => i.esPrincipal)?.url ?? p.imagenes[0]?.url ?? null);
        this.selectedEmb.set(p.embalajes[0]?.id ?? null);
        this.qty = this.qtyMin();
        this.api.track(p.id, 'vista').subscribe({ error: () => undefined });
        this.loadEngagement(p.id);
        queueMicrotask(() => this.scrollToTop());
      },
      error: (e) => {
        this.loading.set(false);
        this.title.setTitle(`Producto no encontrado | ${this.shopName()}`);
        this.error.set(e?.error?.error || e.message || 'No encontrado');
      },
    });
  }

  private loadEngagement(id: number): void {
    this.api.getResenas(id).subscribe({ next: (r) => this.resenas.set(r) });
    this.api.recomendaciones(id).subscribe({ next: (r) => this.recs.set(r) });
    if (this.logged) {
      this.api.getFavoritos(getCanal()).subscribe({ next: (s) => this.secciones.set(s) });
      this.api.seguidoContains(id).subscribe({
        next: (r) => this.siguiendo.set(!!r.seguido),
        error: () => this.siguiendo.set(false),
      });
      this.api.favoritoContains(id).subscribe({
        next: (r) => this.enFavoritos.set(!!r.enFavoritos),
        error: () => this.enFavoritos.set(false),
      });
    }
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
    document.getElementById('contenido')?.scrollTo?.({ top: 0 });
  }

  openTab(t: PdpTab): void {
    this.tab.set(t);
  }

  longDesc(text: string): boolean {
    return (text?.length || 0) > 280;
  }

  selectedPrice(): number | null {
    const p = this.product();
    if (!p) return null;
    const emb = p.embalajes.find((e) => e.id === this.selectedEmb());
    return emb?.precio ?? p.embalajes[0]?.precio ?? null;
  }

  selectedEmbObj() {
    const p = this.product();
    if (!p) return null;
    return p.embalajes.find((e) => e.id === this.selectedEmb()) ?? p.embalajes[0] ?? null;
  }

  qtyStep(): number {
    return this.selectedEmbObj()?.incrementoVenta || 1;
  }

  qtyMin(): number {
    const e = this.selectedEmbObj();
    if (!e) return 1;
    const venta = (e.cantidadMinimaVenta ?? 0) > 0 ? e.cantidadMinimaVenta! : 1;
    const moq = (e.cantidadMinimaMayorista ?? 0) > 0 ? e.cantidadMinimaMayorista! : 1;
    return getCanal() === 'B2B' ? Math.max(venta, moq) : venta;
  }

  qtyUnitLabel(): string {
    const e = this.selectedEmbObj();
    if (!e) return '';
    return this.unitLabelFor(e);
  }

  qtyUnitPhrase(qty: number | string): string {
    const e = this.selectedEmbObj();
    if (!e) return '';
    return this.unitPhrase(e, Number(qty));
  }

  stockUnitLabel(stock: number): string {
    const e = this.selectedEmbObj();
    if (!e) return stock === 1 ? 'unidad' : 'unidades';
    if (this.isFractionalEmb(e)) return this.unitPhrase(e, stock);
    return stock === 1 ? 'unidad' : 'unidades';
  }

  formatStock(n: number): string {
    if (!Number.isFinite(n)) return '—';
    const rounded = Math.round(n * 1000) / 1000;
    if (Number.isInteger(rounded)) return String(rounded);
    return rounded.toLocaleString('es-PY', { maximumFractionDigits: 3 });
  }

  embTitle(e: ProductDetail['embalajes'][number]): string {
    if (this.isFractionalEmb(e)) {
      const code = (e.codigoUnidadMedida || '').toUpperCase();
      if (code === 'M') return 'Por metro';
      if (code === 'M2') return 'Por metro cuadrado';
      if (code === 'KG') return 'Por kilogramo';
      if (code === 'L') return 'Por litro';
      const u = (e.unidadNombre || e.unidadSimbolo || '').trim();
      if (u) return `Por ${u.toLowerCase()}`;
      return 'Venta fraccionada';
    }
    const desc = (e.descripcion || '').trim();
    if (desc && !/^metro$/i.test(desc) && !/^m$/i.test(desc)) return desc;
    if (e.cantidadPorEmbalaje > 1) return `Pack x${this.formatStock(e.cantidadPorEmbalaje)}`;
    return e.unidadNombre || e.descripcion || 'Unidad';
  }

  embSubtitle(e: ProductDetail['embalajes'][number]): string {
    const moq =
      e.cantidadMinimaMayorista > 1 && getCanal() === 'B2B'
        ? ` · mínimo mayorista ${e.cantidadMinimaMayorista}`
        : '';
    if (this.isFractionalEmb(e)) {
      const code = (e.codigoUnidadMedida || '').toUpperCase();
      if (code === 'M') return `Pedís la cantidad en metros${moq}`;
      if (code === 'M2') return `Pedís la cantidad en m²${moq}`;
      if (code === 'KG') return `Pedís la cantidad en kg${moq}`;
      if (code === 'L') return `Pedís la cantidad en litros${moq}`;
      return `Precio por ${this.unitLabelFor(e) || 'unidad'}${moq}`;
    }
    if (e.cantidadPorEmbalaje > 1) {
      return `Cada uno trae ${this.formatStock(e.cantidadPorEmbalaje)} unidades${moq}`;
    }
    return `Se vende por unidad${moq}`;
  }

  embStockText(p: ProductDetail, e: ProductDetail['embalajes'][number]): string | null {
    if (p.stockLibre == null) return null;
    if (this.isFractionalEmb(e)) {
      return `${this.formatStock(p.stockLibre)} ${this.unitPhrase(e, p.stockLibre)} disponibles`;
    }
    if (e.cantidadPorEmbalaje <= 0) return null;
    const packs = Math.floor(p.stockLibre / e.cantidadPorEmbalaje);
    const label = packs === 1 ? 'unidad' : 'unidades';
    return `${packs} ${label} disponibles`;
  }

  embPriceUnit(e: ProductDetail['embalajes'][number]): string {
    if (this.isFractionalEmb(e)) {
      const code = (e.codigoUnidadMedida || '').toUpperCase();
      if (code === 'M') return 'metro';
      if (code === 'M2') return 'm²';
      if (code === 'KG') return 'kg';
      if (code === 'L') return 'litro';
      return this.unitLabelFor(e) || 'unidad';
    }
    return '';
  }

  private isFractionalEmb(e: ProductDetail['embalajes'][number]): boolean {
    const code = (e.codigoUnidadMedida || '').toUpperCase();
    return !!e.permiteFraccion || code === 'M' || code === 'M2' || code === 'KG' || code === 'L';
  }

  private unitLabelFor(e: ProductDetail['embalajes'][number]): string {
    const code = (e.codigoUnidadMedida || '').toUpperCase();
    if (code === 'M') return 'metros';
    if (code === 'M2') return 'm²';
    if (code === 'KG') return 'kg';
    if (code === 'L') return 'litros';
    return e.unidadNombre || e.unidadSimbolo || '';
  }

  private unitPhrase(e: ProductDetail['embalajes'][number], qty: number): string {
    const code = (e.codigoUnidadMedida || '').toUpperCase();
    const n = Number.isFinite(qty) ? qty : 0;
    const one = Math.abs(n - 1) < 1e-9;
    if (code === 'M') return one ? 'metro' : 'metros';
    if (code === 'M2') return 'm²';
    if (code === 'KG') return 'kg';
    if (code === 'L') return one ? 'litro' : 'litros';
    const name = (e.unidadNombre || e.unidadSimbolo || '').trim();
    if (!name) return one ? 'unidad' : 'unidades';
    if (/^metros?$/i.test(name)) return one ? 'metro' : 'metros';
    return name;
  }

  isMeterSale(): boolean {
    const e = this.selectedEmbObj();
    if (!e) return false;
    return this.isFractionalEmb(e);
  }

  onEmbChange(id: number): void {
    this.selectedEmb.set(id);
    this.qty = this.qtyMin();
  }

  onQtyChange(raw: number | string): void {
    const n = Number(raw);
    if (!Number.isFinite(n)) return;
    // Medida exacta: no forzar paso; solo limpiar float a 3 decimales.
    this.qty = Math.round(n * 1000) / 1000;
  }

  canBump(dir: -1 | 1): boolean {
    if (dir > 0) return true;
    const next = (Number(this.qty) || 0) - this.qtyStep();
    return next >= this.qtyMin() - 1e-9;
  }

  bumpQty(dir: -1 | 1): void {
    const step = this.qtyStep();
    const min = this.qtyMin();
    let q = Number(this.qty) || min;
    q = Math.round((q + dir * step) * 1000) / 1000;
    if (q < min) q = min;
    this.qty = q;
  }

  lineEstimate(): number | null {
    const price = this.selectedPrice();
    if (price == null) return null;
    const q = Number(this.qty);
    if (!Number.isFinite(q) || q <= 0) return null;
    return price * q;
  }

  ratingAria(p: ProductDetail): string {
    return `Calificación ${(p.promedioCalificacion || 0).toFixed(1)} de 5 · ver ${p.totalResenas || 0} reseñas`;
  }

  starsLabel(n: number): string {
    const full = Math.round(n);
    return '★★★★★'.slice(0, full) + '☆☆☆☆☆'.slice(0, Math.max(0, 5 - full));
  }

  addToCart(): void {
    const p = this.product();
    if (!p) return;
    const min = this.qtyMin();
    let q = Number(this.qty);
    if (!Number.isFinite(q) || q <= 0) q = min;
    if (q < min) q = min;
    // Venta fraccionada (metro/kg/…): facturar la medida justa pedida.
    // Productos enteros: redondear al paso solo si hace falta.
    if (!this.isMeterSale()) {
      const step = this.qtyStep();
      if (step > 0 && Math.abs(((q - min) / step) % 1) > 1e-6) {
        q = min + Math.ceil((q - min) / step) * step;
      }
    }
    q = Math.round(q * 1000) / 1000;
    this.qty = q;
    this.error.set(null);
    this.api.addToCart(p.id, q, this.selectedEmb() ?? undefined, getCanal()).subscribe({
      next: (cart) => {
        const n = cart.itemsCount ?? cart.items?.length ?? 0;
        this.session.setCartCount(n);
        this.session.setCartTotal(cart.montoTotal ?? 0);
        this.api.track(p.id, 'add_cart').subscribe({ error: () => undefined });
        this.miniCart.show(cart, p.id);
      },
      error: (e) => {
        const msg = e?.error?.error || e.message || 'No se pudo agregar';
        this.error.set(msg);
        this.toast.error(msg);
      },
    });
  }

  toggleFav(idArticulo: number): void {
    if (this.enFavoritos()) {
      this.api.quitarFavoritoPorArticulo(idArticulo).subscribe({
        next: () => {
          this.enFavoritos.set(false);
          this.toast.info('Se quitó de favoritos');
          this.api.getFavoritos(getCanal()).subscribe({ next: (s) => this.secciones.set(s) });
        },
        error: (e) => {
          const msg = e?.error?.error || e.message || 'No se pudo quitar';
          this.error.set(msg);
          this.toast.error(msg);
        },
      });
      return;
    }
    const nombre = this.nuevaSeccion.trim() || undefined;
    this.api.agregarFavorito(idArticulo, this.listaFavId ?? undefined, nombre).subscribe({
      next: () => {
        this.enFavoritos.set(true);
        this.nuevaSeccion = '';
        this.toast.success(nombre ? `Se agregó a “${nombre}”` : 'Se agregó a favoritos');
        this.api.getFavoritos(getCanal()).subscribe({ next: (s) => this.secciones.set(s) });
        this.api.track(idArticulo, 'favorito').subscribe({ error: () => undefined });
      },
      error: (e) => {
        const msg = e?.error?.error || e.message || 'No se pudo guardar';
        this.error.set(msg);
        this.toast.error(msg);
      },
    });
  }

  toggleSeguir(idArticulo: number): void {
    if (this.siguiendo()) {
      this.api.dejarSeguir(idArticulo).subscribe({
        next: () => {
          this.siguiendo.set(false);
          this.toast.info('Dejaste de seguir este producto');
        },
        error: (e) => {
          this.error.set(e?.error?.error || e.message || 'No se pudo dejar de seguir');
          this.toast.error(e?.error?.error || 'No se pudo dejar de seguir');
        },
      });
      return;
    }
    this.api.seguirProducto(idArticulo).subscribe({
      next: () => {
        this.siguiendo.set(true);
        this.toast.success('Se agregó a Seguidos');
        this.api.track(idArticulo, 'seguir').subscribe({ error: () => undefined });
      },
      error: (e) => {
        this.error.set(e?.error?.error || e.message || 'No se pudo seguir');
        this.toast.error(e?.error?.error || 'No se pudo seguir');
      },
    });
  }

  onZoom(ev: MouseEvent): void {
    const el = ev.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    const x = ((ev.clientX - rect.left) / rect.width) * 100;
    const y = ((ev.clientY - rect.top) / rect.height) * 100;
    this.zoomStyle.set(`${x}% ${y}%`);
  }

  onZoomTouch(ev: TouchEvent): void {
    const t = ev.touches[0];
    if (!t) return;
    const el = ev.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    const x = ((t.clientX - rect.left) / rect.width) * 100;
    const y = ((t.clientY - rect.top) / rect.height) * 100;
    this.zoomStyle.set(`${x}% ${y}%`);
  }
}
