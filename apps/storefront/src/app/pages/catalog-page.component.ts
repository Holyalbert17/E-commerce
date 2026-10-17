import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Subject, Subscription, debounceTime, distinctUntilChanged, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import {
  ApiService,
  Canal,
  CatalogFacet,
  CatalogMarca,
  CollectionItem,
  MarketingSlot,
  PagedResult,
  ProductListItem,
  Recomendacion,
  StorefrontCmsBlock,
  getCanal,
  setCanal,
} from '../core/api.service';
import { SessionService } from '../core/session.service';
import { AuthModalService } from '../core/auth-modal.service';
import { BrandingService } from '../core/branding.service';
import { PersonalizacionService } from '../core/personalizacion.service';
import { ToastService } from '../core/toast.service';
import { MiniCartService } from '../core/mini-cart.service';
import { DxIconComponent } from '../shared/dx-icon.component';
import { HeaderSearchComponent } from '../shared/header-search.component';
import { ProductRailCarouselComponent } from '../shared/product-rail-carousel.component';

export interface CategoryRail {
  collection: CollectionItem;
  products: ProductListItem[];
}

@Component({
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    CurrencyPipe,
    DecimalPipe,
    DxIconComponent,
    HeaderSearchComponent,
    ProductRailCarouselComponent,
  ],
  template: `
    <section class="shop" [attr.aria-labelledby]="listingMode() ? 'list-title' : 'hero-brand'">
      <!-- ===== HOME DINÁMICO (orden admin) ===== -->
      @if (!listingMode()) {
        @if (pers.showTopBar()) {
          @if (cmsTopBar(); as top) {
            <div class="cms-topbar" role="note">
              <span>{{ top.title }}</span>
              @for (it of top.items; track it.id) {
                @if (it.linkUrl) {
                  <a [href]="it.linkUrl">{{ it.title }}</a>
                }
              }
            </div>
          }
        }

        @for (sec of pers.orderedSections(); track sec.id) {
          @switch (sec.id) {
            @case ('hero') {
              <div
                class="shop-hero"
                [class.shop-hero--banner]="!!heroBannerUrl()"
                [class.shop-hero--featured]="!heroBannerUrl() && !!featuredOffer()"
                [style.backgroundImage]="heroBannerUrl() ? 'url(' + heroBannerUrl() + ')' : null"
              >
                @if (heroBannerUrl()) {
                  <a
                    class="shop-hero__hit"
                    [href]="heroBannerHref()"
                    [attr.target]="heroBannerExternal() ? '_blank' : null"
                    [attr.rel]="heroBannerExternal() ? 'noopener' : null"
                    (click)="onHeroBannerClick($event)"
                  >
                    <span class="sr-only" id="hero-brand">{{ brandName() }} — ir al catálogo</span>
                  </a>
                } @else {
                  <div class="shop-hero__copy">
                    <p class="brand" id="hero-brand">{{ brandName() }}</p>
                    <h1>{{ cmsHero()?.title || 'Ofertas claras. Stock real. Compra fácil.' }}</h1>
                    <p class="lead">
                      {{
                        cmsHero()?.subtitle ||
                          'Explorá categorías, aprovechá promociones vigentes y llevá tu pedido en minutos.'
                      }}
                    </p>
                    <div class="hero-cta">
                      @if (pers.sectionVisible('flyers')) {
                        <a class="dx-btn dx-btn--lg" href="#ofertas" (click)="scrollOfertas($event)">
                          <dx-icon name="percent" /> Ver ofertas
                        </a>
                      }
                      <button type="button" class="dx-btn dx-btn--ghost dx-btn--lg" (click)="openBrowse()">
                        <dx-icon name="grid" /> Catálogo
                      </button>
                    </div>
                    <nav class="hero-jumps" aria-label="Ir a secciones">
                      @if (pers.sectionVisible('categorias')) {
                        <a href="#categorias" (click)="scrollId('categorias', $event)">Categorías</a>
                      }
                      @if (pers.sectionVisible('publicidad') && publicidadItems().length) {
                        <a href="#publicidad" (click)="scrollId('publicidad', $event)">Publicidad</a>
                      }
                      @if (pers.sectionVisible('flyers')) {
                        <a href="#ofertas" (click)="scrollOfertas($event)">Ofertas</a>
                      }
                      @if (pers.sectionVisible('masVendidos') && masVendidos().length) {
                        <a href="#mas-vendidos" (click)="scrollId('mas-vendidos', $event)">Más vendidos</a>
                      }
                    </nav>
                  </div>
                  @if (featuredOffer(); as feat) {
                    <a
                      class="hero-feature"
                      [href]="feat.linkUrl || '#ofertas'"
                      [attr.target]="feat.external ? '_blank' : null"
                      [attr.rel]="feat.external ? 'noopener' : null"
                      (click)="onFeatureClick(feat, $event)"
                      [style.backgroundImage]="
                        feat.imageUrl
                          ? 'linear-gradient(180deg,rgba(0,0,0,.12),rgba(0,0,0,.72)), url(' + mediaUrl(feat.imageUrl) + ')'
                          : null
                      "
                    >
                      <span class="flyer-tag">{{ feat.badge || 'Oferta' }}</span>
                      <strong>{{ feat.title }}</strong>
                      @if (feat.sub) {
                        <span class="hero-feature__sub">{{ feat.sub }}</span>
                      }
                      <span class="hero-feature__cta">Ver promoción <dx-icon name="chevron-right" [size]="14" /></span>
                    </a>
                  } @else if (cmsHero()?.items?.length) {
                    <div class="hero-slides" role="list">
                      @for (it of cmsHero()!.items.slice(0, 3); track it.id) {
                        <a
                          class="hero-slide"
                          role="listitem"
                          [href]="it.linkUrl || '#categorias'"
                          [style.backgroundImage]="it.imageUrl ? 'url(' + mediaUrl(it.imageUrl) + ')' : null"
                        >
                          <span>{{ it.title || it.badgeText || 'Ver' }}</span>
                        </a>
                      }
                    </div>
                  }
                }
              </div>
              @if (brand.branding()?.horarioAtencion; as horario) {
                <p class="horario-hero">
                  <dx-icon name="shield" [size]="14" /> Horario de atención: {{ horario }}
                </p>
              }
              @if (pers.showTrust() && trustItems().length) {
                <section class="trust" aria-label="Beneficios">
                  @for (t of trustItems(); track t.title) {
                    <article class="trust-item">
                      <span class="trust-ico" aria-hidden="true"><dx-icon [name]="t.icon" [size]="18" /></span>
                      <div>
                        <strong>{{ t.title }}</strong>
                        <p>{{ t.sub }}</p>
                      </div>
                    </article>
                  }
                </section>
              }
            }
            @case ('categorias') {
              <section id="categorias" class="block" aria-labelledby="cat-title">
                <div class="block-head">
                  <div>
                    <p class="block-kicker">Comprá por rubro</p>
                    <h2 id="cat-title" class="dx-section-title">
                      {{ cmsFeaturedCats()?.title || 'Categorías' }}
                    </h2>
                  </div>
                  <button type="button" class="see-all" (click)="openBrowse('categorias')">
                    Ver todo <dx-icon name="chevron-right" [size]="16" />
                  </button>
                </div>
                <app-product-rail-carousel>
                  @if (cmsFeaturedCats()?.items?.length) {
                    @for (it of cmsFeaturedCats()!.items; track it.id) {
                      <a
                        class="cat-chip"
                        role="listitem"
                        [href]="it.linkUrl || '/catalogo?browse=1&focus=categorias'"
                        [style.backgroundImage]="it.imageUrl ? 'url(' + mediaUrl(it.imageUrl) + ')' : null"
                      >
                        <span class="cat-chip__label">{{ it.title }}</span>
                      </a>
                    }
                  } @else if (homeCategoryChips().length) {
                    @for (c of homeCategoryChips(); track c.key) {
                      <button type="button" class="cat-chip" role="listitem" (click)="openCategoryChip(c)">
                        <span
                          class="cat-chip__media"
                          [style.backgroundImage]="c.imageUrl ? 'url(' + mediaUrl(c.imageUrl) + ')' : 'none'"
                        >
                          @if (!c.imageUrl) {
                            <dx-icon name="tag" [size]="20" />
                          }
                        </span>
                        <span class="cat-chip__label">{{ c.nombre }}</span>
                        @if (c.count != null) {
                          <small>{{ c.count }}</small>
                        }
                      </button>
                    }
                  } @else {
                    <p class="empty-inline">Aún no hay categorías publicadas.</p>
                  }
                </app-product-rail-carousel>
              </section>
            }
            @case ('publicidad') {
              @if (publicidadItems().length) {
                <section id="publicidad" class="block ads-home" aria-labelledby="ads-title">
                  <div class="block-head">
                    <div>
                      <p class="block-kicker"><dx-icon name="tag" [size]="14" /> Espacio publicitario</p>
                      <h2 id="ads-title" class="dx-section-title">Publicidades</h2>
                    </div>
                  </div>
                  <div class="ads-stage" role="list">
                    @for (ad of publicidadItems(); track ad.key; let i = $index) {
                      <a
                        class="ad-banner"
                        [class.ad-banner--hero]="i === 0"
                        role="listitem"
                        [attr.href]="ad.external ? ad.link : null"
                        [attr.target]="ad.external ? '_blank' : null"
                        [attr.rel]="ad.external ? 'noopener' : null"
                        [routerLink]="ad.external ? null : ad.link"
                        [style.backgroundImage]="
                          'linear-gradient(100deg,rgba(0,0,0,.55),rgba(0,0,0,.15)), url(' +
                          (ad.imageUrl ? mediaUrl(ad.imageUrl) : '') +
                          ')'
                        "
                      >
                        <span class="ad-badge">Publicidad</span>
                        <strong>{{ ad.title }}</strong>
                        @if (ad.sub) {
                          <span class="ad-sub">{{ ad.sub }}</span>
                        }
                        <span class="ad-cta">Ver más <dx-icon name="chevron-right" [size]="14" /></span>
                      </a>
                    }
                  </div>
                </section>
              }
            }
            @case ('flyers') {
              <section id="ofertas" class="block ofertas-home" aria-labelledby="promo-title">
                <div class="block-head">
                  <div>
                    <p class="block-kicker"><dx-icon name="percent" [size]="14" /> Promociones</p>
                    <h2 id="promo-title" class="dx-section-title">
                      {{ cmsPromoGrid()?.title || 'Ofertas de la semana' }}
                    </h2>
                  </div>
                  <button type="button" class="see-all" (click)="openBrowse('ofertas')">
                    Ver catálogo <dx-icon name="chevron-right" [size]="16" />
                  </button>
                </div>

                @if (pers.showCupon()) {
                  @if (cmsCoupon(); as coupon) {
                    <div class="coupon-strip coupon-strip--hero">
                      <div class="coupon-strip__copy">
                        <span class="coupon-code">{{ couponCode(coupon) }}</span>
                        <strong>{{ coupon.title || 'Cupón disponible' }}</strong>
                        <span>{{ coupon.subtitle || coupon.description || 'Aplicá el código en el checkout.' }}</span>
                      </div>
                      <button type="button" class="dx-btn" (click)="applyHomeCoupon(coupon)">
                        Usar en checkout
                      </button>
                    </div>
                  }
                }

                @if (promoVisualItems().length) {
                  <div class="promo-grid promo-grid--home" role="list">
                    @for (it of promoVisualItems(); track it.key; let i = $index) {
                      <a
                        class="promo-tile"
                        [class.promo-tile--wide]="i === 0"
                        role="listitem"
                        [attr.href]="it.external ? it.link : null"
                        [attr.target]="it.external ? '_blank' : null"
                        [routerLink]="it.external ? null : it.link"
                        [style.backgroundImage]="
                          'linear-gradient(180deg,rgba(0,0,0,.05),rgba(0,0,0,.72)), url(' +
                          (it.imageUrl ? mediaUrl(it.imageUrl) : '') +
                          ')'
                        "
                      >
                        <div class="promo-tile__copy">
                          @if (it.badge) {
                            <span class="flyer-tag">{{ it.badge }}</span>
                          }
                          <h3>{{ it.title }}</h3>
                          @if (it.sub) {
                            <p>{{ it.sub }}</p>
                          }
                        </div>
                      </a>
                    }
                  </div>
                }

                @if (pers.showProductosOferta() && ofertaProducts().length) {
                  <div class="deal-rail-wrap">
                    <div class="block-head">
                      <h3 class="rail-title">Productos en oferta</h3>
                      <button type="button" class="see-all" (click)="openBrowse('ofertas')">
                        Ver más <dx-icon name="chevron-right" [size]="16" />
                      </button>
                    </div>
                    <app-product-rail-carousel>
                      @for (p of ofertaProducts(); track p.id) {
                        <article class="rail-card rail-card--deal" role="listitem">
                          <a [routerLink]="['/producto', p.slugSeo || p.id]">
                            <div class="rail-card__media" [class.empty]="!p.imagenPrincipalUrl || brokenImg()[p.id]">
                              @if (p.imagenPrincipalUrl && !brokenImg()[p.id]) {
                                <img
                                  [src]="mediaUrl(p.imagenPrincipalUrl)"
                                  [alt]="p.nombre"
                                  loading="lazy"
                                  (error)="markBroken(p.id)"
                                />
                              } @else {
                                <dx-icon name="package" [size]="28" />
                              }
                              @if (discountLabel(p); as pct) {
                                <span class="dx-badge dx-badge--promo">−{{ pct }}%</span>
                              } @else {
                                <span class="dx-badge dx-badge--promo">Oferta</span>
                              }
                            </div>
                            <div class="rail-card__body">
                              <span class="brand-line">{{ p.marca || 'Marca' }}</span>
                              <h3>{{ p.nombre }}</h3>
                              @if (p.precioDesde != null) {
                                <p class="price price--deal">
                                  @if (p.precioLista && p.precioLista > p.precioDesde) {
                                    <s>{{ p.precioLista | currency: 'PYG':'symbol-narrow':'1.0-0' }}</s>
                                  }
                                  <strong>{{ p.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                                </p>
                              } @else {
                                <p class="price ask">Ver precio</p>
                              }
                            </div>
                          </a>
                          <button
                            type="button"
                            class="quick-add"
                            [disabled]="!p.stockOk || addingId === p.id"
                            (click)="quickAdd($event, p)"
                          >
                            <dx-icon name="cart" [size]="16" />
                            {{ p.stockOk ? 'Agregar' : 'Sin stock' }}
                          </button>
                        </article>
                      }
                    </app-product-rail-carousel>
                  </div>
                } @else if (!promoVisualItems().length && !pers.showCupon()) {
                  <div class="ofertas-empty">
                    <dx-icon name="percent" [size]="28" />
                    <p>No hay ofertas publicadas ahora.</p>
                    <button type="button" class="dx-btn" (click)="openBrowse('catalogo')">Explorar catálogo</button>
                  </div>
                }
              </section>
            }
            @case ('recomendados') {
              <section class="block" aria-labelledby="rec-title">
                <div class="block-head">
                  <h2 id="rec-title" class="dx-section-title">
                    {{ cmsCarousel()?.title || 'Recomendados para vos' }}
                  </h2>
                  <button type="button" class="see-all" (click)="openBrowse('recomendados')">
                    Ver más <dx-icon name="chevron-right" [size]="16" />
                  </button>
                </div>
                @if (promos().length) {
                  <app-product-rail-carousel>
                    @for (x of promos(); track x.id) {
                      <article class="rail-card" role="listitem">
                        <a [routerLink]="['/producto', x.slugSeo || x.id]">
                          <div class="rail-card__media" [class.empty]="!x.imagenUrl || brokenImg()[x.id]">
                            @if (x.imagenUrl && !brokenImg()[x.id]) {
                              <img [src]="mediaUrl(x.imagenUrl)" [alt]="x.nombre" loading="lazy" (error)="markBroken(x.id)" />
                            } @else {
                              <dx-icon name="package" [size]="28" />
                            }
                            <span class="dx-badge">Popular</span>
                          </div>
                          <div class="rail-card__body">
                            <h3>{{ x.nombre }}</h3>
                            @if (x.precioDesde != null) {
                              <p class="price">{{ x.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</p>
                            } @else {
                              <p class="price ask">Ver precio</p>
                            }
                          </div>
                        </a>
                        <button
                          type="button"
                          class="quick-add"
                          [disabled]="addingId === x.id"
                          (click)="quickAddReco($event, x)"
                        >
                          <dx-icon name="cart" [size]="16" /> Agregar
                        </button>
                      </article>
                    }
                  </app-product-rail-carousel>
                }
                @for (rail of categoryRails(); track rail.collection.id) {
                  <div class="rail-block">
                    <div class="block-head">
                      <h3 class="rail-title">{{ rail.collection.nombre }}</h3>
                      <button type="button" class="see-all" (click)="selectGrupo(rail.collection.id)">
                        Ver todos ({{ rail.collection.articulosCount }})
                        <dx-icon name="chevron-right" [size]="16" />
                      </button>
                    </div>
                    <app-product-rail-carousel>
                      @for (p of rail.products; track p.id) {
                        <article class="rail-card" role="listitem">
                          <a [routerLink]="['/producto', p.slugSeo || p.id]">
                            <div class="rail-card__media" [class.empty]="!p.imagenPrincipalUrl || brokenImg()[p.id]">
                              @if (p.imagenPrincipalUrl && !brokenImg()[p.id]) {
                                <img [src]="mediaUrl(p.imagenPrincipalUrl)" [alt]="p.nombre" loading="lazy" (error)="markBroken(p.id)" />
                              } @else {
                                <dx-icon name="package" [size]="28" />
                              }
                            </div>
                            <div class="rail-card__body">
                              <span class="brand-line">{{ p.marca || 'Marca' }}</span>
                              <h3>{{ p.nombre }}</h3>
                              @if (p.precioDesde != null) {
                                <p class="price">{{ p.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</p>
                              } @else {
                                <p class="price ask">Consultar</p>
                              }
                            </div>
                          </a>
                          <button
                            type="button"
                            class="quick-add"
                            [disabled]="!p.stockOk || addingId === p.id"
                            (click)="quickAdd($event, p)"
                            [attr.aria-label]="'Agregar ' + p.nombre + ' al carrito'"
                          >
                            <dx-icon name="cart" [size]="16" />
                            {{ p.stockOk ? 'Agregar' : 'Sin stock' }}
                          </button>
                        </article>
                      }
                    </app-product-rail-carousel>
                  </div>
                }
              </section>
            }
            @case ('masVendidos') {
              @if (masVendidos().length) {
                <section id="mas-vendidos" class="block" aria-labelledby="mv-title">
                  <div class="block-head">
                    <div>
                      <p class="block-kicker">Lo que más se lleva</p>
                      <h2 id="mv-title" class="dx-section-title">Los más vendidos</h2>
                    </div>
                    <button type="button" class="see-all" (click)="openBrowse('masVendidos')">
                      Ver todo <dx-icon name="chevron-right" [size]="16" />
                    </button>
                  </div>
                  <app-product-rail-carousel>
                    @for (x of masVendidos(); track x.id) {
                      <article class="rail-card" role="listitem">
                        <a [routerLink]="['/producto', x.slugSeo || x.id]">
                          <div class="rail-card__media" [class.empty]="!x.imagenPrincipalUrl || brokenImg()[x.id]">
                            @if (x.imagenPrincipalUrl && !brokenImg()[x.id]) {
                              <img
                                [src]="mediaUrl(x.imagenPrincipalUrl)"
                                [alt]="x.nombre"
                                loading="lazy"
                                (error)="markBroken(x.id)"
                              />
                            } @else {
                              <dx-icon name="package" [size]="28" />
                            }
                            @if (discountLabel(x); as pct) {
                              <span class="dx-badge dx-badge--promo">−{{ pct }}%</span>
                            }
                          </div>
                          <div class="rail-card__body">
                            <h3>{{ x.nombre }}</h3>
                            @if (x.precioDesde != null) {
                              <p class="price price--deal">
                                @if (x.precioLista && x.precioLista > x.precioDesde) {
                                  <s>{{ x.precioLista | currency: 'PYG':'symbol-narrow':'1.0-0' }}</s>
                                }
                                <strong>{{ x.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                              </p>
                            } @else {
                              <p class="price ask">Ver precio</p>
                            }
                          </div>
                        </a>
                        <button
                          type="button"
                          class="quick-add"
                          [disabled]="!x.stockOk || addingId === x.id"
                          (click)="quickAdd($event, x)"
                        >
                          <dx-icon name="cart" [size]="16" />
                          {{ x.stockOk ? 'Agregar' : 'Sin stock' }}
                        </button>
                      </article>
                    }
                  </app-product-rail-carousel>
                </section>
              }
            }
            @case ('newsletter') {
              @if (cmsNewsletter(); as news) {
                <section class="newsletter" aria-labelledby="news-title">
                  <div>
                    <h2 id="news-title">{{ news.title || 'Novedades' }}</h2>
                    <p>{{ news.subtitle || news.description || 'Enterate de ofertas y lanzamientos.' }}</p>
                  </div>
                  <form class="news-form" (ngSubmit)="subscribeNewsletter()">
                    <label class="sr-only" for="news-email">Email</label>
                    <input
                      id="news-email"
                      class="dx-input"
                      type="email"
                      name="newsEmail"
                      [(ngModel)]="newsEmail"
                      placeholder="Tu email"
                      required
                      [disabled]="newsBusy"
                    />
                    <button type="submit" class="dx-btn" [disabled]="newsBusy || !newsEmail.trim()">
                      Suscribirme
                    </button>
                  </form>
                </section>
              }
            }
            @case ('catalogoCta') {
              <div class="browse-cta browse-cta--home">
                <div>
                  <h2>Catálogo completo</h2>
                  <p>Filtrá por categoría, marca y stock. Miles de SKUs con precio y disponibilidad al día.</p>
                </div>
                <button type="button" class="dx-btn dx-btn--lg" (click)="openBrowse()">
                  <dx-icon name="grid" /> Abrir catálogo
                </button>
              </div>
            }
          }
        }
      }

      <!-- ===== LISTING AMAZON ===== -->
      @if (listingMode()) {
        <div class="az" id="productos">
          <button type="button" class="az-filters-toggle" (click)="filtersOpen = !filtersOpen">
            <dx-icon name="grid" [size]="16" />
            {{ filtersOpen ? 'Ocultar filtros' : 'Comprar por' }}
            @if (hasActiveFilters()) {
              <span class="az-filters-dot" aria-hidden="true"></span>
            }
          </button>

          <aside class="az-side" [class.open]="filtersOpen" aria-label="Comprar por">
            <p class="az-side__eyebrow">Comprar por</p>

            <div class="az-side__block">
              <button type="button" class="az-acc" (click)="sideCatOpen = !sideCatOpen" [attr.aria-expanded]="sideCatOpen">
                <span>Categoría</span>
                <dx-icon name="chevron-down" [size]="14" [class.flip]="sideCatOpen" />
              </button>
              @if (sideCatOpen) {
                <div class="az-checks">
                  <label class="az-check">
                    <input
                      type="checkbox"
                      [checked]="grupoId == null"
                      (change)="selectGrupo(null)"
                    />
                    Todas
                  </label>
                  @for (c of visibleCategorias(); track c.id) {
                    <label class="az-check">
                      <input
                        type="checkbox"
                        [checked]="grupoId === c.id"
                        (change)="selectGrupo(grupoId === c.id ? null : c.id)"
                      />
                      <span class="az-check__txt" [title]="c.nombre">{{ c.nombre }}</span>
                    </label>
                  }
                  @if (shopCollections().length > catLimit) {
                    <button type="button" class="az-more" (click)="catsExpanded = !catsExpanded">
                      {{ catsExpanded ? 'Ver menos' : 'Ver (' + (shopCollections().length - catLimit) + ') más' }}
                    </button>
                  }
                </div>
              }
            </div>

            <div class="az-side__block">
              <button type="button" class="az-acc" (click)="sideMarcaOpen = !sideMarcaOpen" [attr.aria-expanded]="sideMarcaOpen">
                <span>Marca</span>
                <dx-icon name="chevron-down" [size]="14" [class.flip]="sideMarcaOpen" />
              </button>
              @if (sideMarcaOpen) {
                <div class="az-checks">
                  <input
                    class="dx-input az-marca-search"
                    type="search"
                    placeholder="Buscar"
                    [(ngModel)]="marcaSearch"
                    aria-label="Buscar marca"
                  />
                  <label class="az-check">
                    <input type="checkbox" [checked]="marcaId == null" (change)="selectMarca(null)" />
                    Todas
                  </label>
                  @for (m of visibleMarcas(); track m.id) {
                    <label class="az-check">
                      <input
                        type="checkbox"
                        [checked]="marcaId === m.id"
                        (change)="selectMarca(marcaId === m.id ? null : m.id)"
                      />
                      {{ m.nombre }}
                    </label>
                  }
                  @if (filteredMarcas().length > marcaLimit) {
                    <button type="button" class="az-more" (click)="marcasExpanded = !marcasExpanded">
                      {{
                        marcasExpanded
                          ? 'Ver menos'
                          : 'Ver (' + (filteredMarcas().length - marcaLimit) + ') más'
                      }}
                    </button>
                  }
                </div>
              }
            </div>

            @if (lineas().length) {
              <div class="az-side__block">
                <h2>Línea</h2>
                <select
                  class="dx-select az-select"
                  [ngModel]="lineaId"
                  (ngModelChange)="selectLinea($event)"
                  aria-label="Línea"
                >
                  <option [ngValue]="null">Todas</option>
                  @for (l of lineas(); track l.id) {
                    <option [ngValue]="l.id">{{ l.nombre }}</option>
                  }
                </select>
              </div>
            }

            @if (tipos().length) {
              <div class="az-side__block">
                <h2>Tipo</h2>
                <select
                  class="dx-select az-select"
                  [ngModel]="tipoId"
                  (ngModelChange)="selectTipo($event)"
                  aria-label="Tipo"
                >
                  <option [ngValue]="null">Todos</option>
                  @for (t of tipos(); track t.id) {
                    <option [ngValue]="t.id">{{ t.nombre }}</option>
                  }
                </select>
              </div>
            }

            <div class="az-side__block">
              <button type="button" class="az-acc" (click)="sideStockOpen = !sideStockOpen" [attr.aria-expanded]="sideStockOpen">
                <span>Quiero solo</span>
                <dx-icon name="chevron-down" [size]="14" [class.flip]="sideStockOpen" />
              </button>
              @if (sideStockOpen) {
                <div class="az-checks">
                  <label class="az-check">
                    <input type="checkbox" [(ngModel)]="soloStock" (ngModelChange)="applyStockFilter()" />
                    Con stock
                  </label>
                </div>
              }
            </div>

            @if (session.mayoristaAprobado()) {
              <div class="az-side__block">
                <h2>Lista de precios</h2>
                <select
                  class="dx-select az-select"
                  [ngModel]="canal"
                  (ngModelChange)="onCanalSelect($event)"
                  aria-label="Canal"
                >
                  <option value="B2C">Minorista</option>
                  <option value="B2B">Mayorista</option>
                </select>
              </div>
            }

            @if (hasActiveFilters()) {
              <button type="button" class="dx-btn dx-btn--ghost az-clear" (click)="clearFilters()">
                Limpiar filtros
              </button>
            }

            <div class="az-wish">
              <strong>Mi lista de deseos</strong>
              <a routerLink="/favoritos">Ver favoritos</a>
            </div>
          </aside>

          <div class="az-main">
            <nav class="az-crumb" aria-label="Miga de pan">
              <a routerLink="/catalogo" [queryParams]="{}">Inicio</a>
              <span aria-hidden="true">/</span>
              <span>{{ listingTitle() }}</span>
            </nav>

            <header class="az-head">
              <div class="az-head__title">
                <h1 id="list-title">{{ listingTitle() }}</h1>
                <p class="az-count">
                  @if (loading()) {
                    Cargando…
                  } @else if (page()) {
                    {{ page()!.totalCount | number }} productos
                    · pág. {{ page()!.page }}/{{ page()!.totalPages || 1 }}
                  }
                </p>
              </div>
              <div class="az-toolbar">
                <div class="az-search-row">
                  <span class="az-tool__lbl">Buscar</span>
                  <app-header-search
                    variant="catalog"
                    [query]="search"
                    [navigateOnSubmit]="false"
                    placeholder="Nombre, SKU, marca…"
                    (committed)="applyCatalogSearch($event)"
                  />
                </div>
                <div class="az-tools">
                  <label class="az-tool">
                    <span class="az-tool__lbl">Ordenar por</span>
                    <select class="dx-select az-tool__ctrl" [(ngModel)]="sort" (change)="load(1)" aria-label="Ordenar">
                      <option value="nombre">Nombre A-Z</option>
                      <option value="nombre_desc">Nombre Z-A</option>
                      <option value="nuevo">Más nuevos</option>
                      <option value="sku">SKU</option>
                    </select>
                  </label>
                  <label class="az-tool">
                    <span class="az-tool__lbl">Mostrar</span>
                    <select class="dx-select az-tool__ctrl az-tool__ctrl--sm" [(ngModel)]="pageSize" (ngModelChange)="load(1)" aria-label="Por página">
                      <option [ngValue]="20">20</option>
                      <option [ngValue]="24">24</option>
                      <option [ngValue]="36">36</option>
                      <option [ngValue]="48">48</option>
                    </select>
                  </label>
                  <div class="az-tool" role="group" aria-label="Ver como">
                    <span class="az-tool__lbl">Ver como</span>
                    <div class="az-view az-tool__ctrl">
                      <button
                        type="button"
                        class="az-view__btn"
                        [class.on]="viewMode === 'grid'"
                        (click)="viewMode = 'grid'"
                        title="Cuadrícula"
                        aria-label="Cuadrícula"
                      >
                        <dx-icon name="grid" [size]="16" />
                      </button>
                      <button
                        type="button"
                        class="az-view__btn"
                        [class.on]="viewMode === 'list'"
                        (click)="viewMode = 'list'"
                        title="Lista"
                        aria-label="Lista"
                      >
                        <dx-icon name="list" [size]="16" />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </header>

            @if (error()) {
              <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
            }

            @if (loading()) {
              <div class="az-grid skel" role="status" aria-busy="true">
                @for (i of [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]; track i) {
                  <div class="skel-card">
                    <div class="skel-media"></div>
                    <div class="skel-line"></div>
                    <div class="skel-line short"></div>
                  </div>
                }
              </div>
            } @else {
              <div class="az-grid" [class.az-grid--list]="viewMode === 'list'" role="list">
                @for (p of page()?.items ?? []; track p.id) {
                  <article class="az-card" role="listitem" [class.az-card--list]="viewMode === 'list'">
                    <a [routerLink]="['/producto', p.slugSeo || p.id]">
                      <div class="az-card__media" [class.empty]="!p.imagenPrincipalUrl || brokenImg()[p.id]">
                        @if (p.imagenPrincipalUrl && !brokenImg()[p.id]) {
                          <img
                            [src]="mediaUrl(p.imagenPrincipalUrl)"
                            [alt]="p.nombre"
                            loading="lazy"
                            decoding="async"
                            (error)="markBroken(p.id)"
                          />
                        } @else {
                          <dx-icon name="package" [size]="36" />
                        }
                        @if (!p.stockOk) {
                          <span class="az-stock out">Sin stock</span>
                        }
                      </div>
                      <div class="az-card__body">
                        <h2 class="az-title">
                          @if (titleParts(p); as tp) {
                            <span class="az-title__lead">{{ tp.lead }}</span>
                            @if (tp.rest) {
                              <span class="az-title__rest"> {{ tp.rest }}</span>
                            }
                          }
                        </h2>
                        <p class="az-price">
                          @if (p.precioDesde != null) {
                            {{ p.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                          } @else {
                            <span class="ask">Consultar</span>
                          }
                        </p>
                      </div>
                    </a>
                    <button
                      type="button"
                      class="quick-add az-add"
                      [disabled]="!p.stockOk || addingId === p.id"
                      (click)="quickAdd($event, p)"
                    >
                      <dx-icon name="cart" [size]="16" />
                      {{ addingId === p.id ? '…' : p.stockOk ? 'Agregar' : 'Sin stock' }}
                    </button>
                  </article>
                } @empty {
                  <div class="empty-box">
                    <dx-icon name="package" [size]="36" />
                    @if (hasActiveFilters()) {
                      <p>No hay productos con esos filtros.</p>
                      <button type="button" class="dx-btn" (click)="clearFilters()">Limpiar</button>
                    } @else {
                      <p>No hay productos publicados.</p>
                    }
                  </div>
                }
              </div>
            }

            @if (page(); as pg) {
              <nav class="pager" aria-label="Paginación">
                <button
                  type="button"
                  class="dx-btn dx-btn--ghost"
                  [disabled]="pg.page <= 1 || loading()"
                  (click)="load(pg.page - 1)"
                >
                  Anterior
                </button>
                <div class="pager-pages">
                  @for (n of pagerPages(); track n) {
                    @if (n < 0) {
                      <span class="pager-ellipsis">…</span>
                    } @else {
                      <button
                        type="button"
                        class="pager-num"
                        [class.on]="n === pg.page"
                        (click)="load(n)"
                      >
                        {{ n }}
                      </button>
                    }
                  }
                </div>
                <button
                  type="button"
                  class="dx-btn dx-btn--ghost"
                  [disabled]="pg.page >= pg.totalPages || loading()"
                  (click)="load(pg.page + 1)"
                >
                  Siguiente
                </button>
              </nav>
            }
          </div>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .shop {
        animation: rise 0.3s ease;
        min-width: 0;
        max-width: 100%;
        overflow-x: clip;
      }
      .cms-topbar {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem 1rem;
        align-items: center;
        justify-content: space-between;
        padding: 0.5rem 0.85rem;
        margin-bottom: 0.75rem;
        border-radius: 6px;
        background: #232f3e;
        color: #fff;
        font-size: clamp(0.75rem, 2.2vw, 0.82rem);
        font-weight: 600;
        min-width: 0;
      }
      .cms-topbar a {
        color: #ffd814;
        text-decoration: none;
      }
      .shop-hero {
        display: grid;
        gap: 0.85rem;
        margin-bottom: 0.85rem;
        border-radius: 14px;
        overflow: hidden;
        min-height: clamp(180px, 32vh, 320px);
        border: 1px solid var(--dx-line);
        background: linear-gradient(125deg, #fff 0%, #f3f4f6 48%, #fff5f5 100%);
        background-size: cover;
        background-position: center;
        box-shadow: 0 10px 28px rgba(15, 23, 42, 0.06);
      }
      .shop-hero--banner {
        min-height: clamp(200px, 38vw, 420px);
        grid-template-columns: 1fr;
        background-color: #111;
      }
      .shop-hero__hit {
        display: block;
        min-height: inherit;
        height: 100%;
        text-decoration: none;
      }
      @media (min-width: 900px) {
        .shop-hero:has(.hero-slides),
        .shop-hero--featured {
          grid-template-columns: 1.35fr 1fr;
          align-items: stretch;
        }
      }
      .shop-hero--banner .shop-hero__copy.on-dark,
      .shop-hero__copy.on-dark {
        color: #fff;
      }
      .shop-hero__copy.on-dark .lead {
        color: rgba(255, 255, 255, 0.9);
      }
      .shop-hero__copy.on-dark .brand {
        color: #ffd814;
      }
      .shop-hero__copy {
        padding: clamp(1.25rem, 3.2vw, 2.1rem);
        display: grid;
        align-content: center;
        gap: 0.5rem;
        max-width: 38rem;
      }
      .brand {
        margin: 0;
        font-size: 0.78rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--dx-red);
      }
      .shop-hero h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.55rem, 3.4vw, 2.35rem);
        line-height: 1.12;
        font-weight: 800;
        letter-spacing: -0.02em;
      }
      .lead {
        margin: 0;
        color: var(--dx-ink-soft);
        font-size: 1rem;
        line-height: 1.45;
        max-width: 36ch;
      }
      .hero-cta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-top: 0.35rem;
      }
      .hero-cta a.dx-btn {
        text-decoration: none;
      }
      .hero-jumps {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
        margin-top: 0.35rem;
      }
      .hero-jumps a {
        color: inherit;
        text-decoration: none;
        font-size: 0.82rem;
        font-weight: 700;
        opacity: 0.85;
        border-bottom: 1px solid currentColor;
      }
      .shop-hero__copy.on-dark .hero-jumps a {
        color: #fff;
      }
      .hero-feature {
        margin: 0.65rem;
        min-height: 200px;
        border-radius: 12px;
        background: #1f2937 center / cover no-repeat;
        text-decoration: none;
        color: #fff;
        display: grid;
        align-content: end;
        gap: 0.35rem;
        padding: 1rem 1.1rem;
        box-shadow: inset 0 -80px 60px rgba(0, 0, 0, 0.45);
      }
      .hero-feature strong {
        font-family: var(--dx-display);
        font-size: clamp(1.15rem, 2vw, 1.45rem);
        line-height: 1.2;
      }
      .hero-feature__sub {
        font-size: 0.88rem;
        opacity: 0.9;
      }
      .hero-feature__cta {
        display: inline-flex;
        align-items: center;
        gap: 0.2rem;
        margin-top: 0.25rem;
        font-size: 0.85rem;
        font-weight: 750;
      }
      .horario-hero {
        margin: 0.65rem 0 0;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.45rem 0.75rem;
        border-radius: 10px;
        background: #fff8f0;
        border: 1px solid #f5d9b0;
        color: #7a4a12;
        font-size: 0.88rem;
        font-weight: 650;
      }
      .hero-slides {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 0.4rem;
        padding: 0.65rem;
      }
      .hero-slide {
        min-height: 110px;
        border-radius: 10px;
        background: #e8e8e8 center / cover no-repeat;
        display: grid;
        align-items: end;
        padding: 0.5rem;
        text-decoration: none;
        color: #fff;
        font-size: 0.75rem;
        font-weight: 700;
        text-shadow: 0 1px 3px rgba(0, 0, 0, 0.5);
      }
      .block-kicker {
        margin: 0 0 0.15rem;
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        font-size: 0.75rem;
        font-weight: 750;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--dx-red);
      }
      .ofertas-home .block-head {
        margin-bottom: 0.75rem;
      }
      .coupon-strip--hero {
        border-style: solid;
        border-width: 1px;
        border-radius: 12px;
        padding: 0.85rem 1rem;
        margin-bottom: 0.85rem;
        background: linear-gradient(90deg, #fff5f5, #fff);
      }
      .coupon-code {
        display: inline-block;
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
        font-weight: 800;
        font-size: 0.95rem;
        letter-spacing: 0.06em;
        color: var(--dx-red);
        background: #ffe8e8;
        padding: 0.15rem 0.45rem;
        border-radius: 6px;
        margin-bottom: 0.2rem;
        width: fit-content;
      }
      .promo-grid--home {
        grid-template-columns: 1fr;
        gap: 0.65rem;
      }
      @media (min-width: 720px) {
        .promo-grid--home {
          grid-template-columns: 1.4fr 1fr 1fr;
        }
        .promo-tile--wide {
          grid-row: span 2;
          min-height: 260px;
        }
      }
      .promo-tile p,
      .flyer p {
        margin: 0.25rem 0 0;
        font-size: 0.82rem;
        opacity: 0.92;
        text-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
      }
      .promo-tile,
      .flyer {
        min-height: 140px;
        border-radius: 12px;
        transition: transform 0.18s ease, box-shadow 0.18s ease;
      }
      .promo-tile:hover,
      .flyer:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 24px rgba(15, 23, 42, 0.12);
      }
      .deal-rail-wrap {
        margin-top: 1.15rem;
      }
      .product-rail--deals {
        grid-auto-columns: minmax(160px, 180px);
      }
      .rail-card--deal {
        border-color: #fecaca;
      }
      .price--deal {
        display: flex;
        flex-wrap: wrap;
        align-items: baseline;
        gap: 0.35rem;
      }
      .price--deal s {
        color: var(--dx-mute);
        font-size: 0.78rem;
        font-weight: 500;
      }
      .price--deal strong {
        color: var(--dx-price, var(--dx-red));
        font-family: var(--dx-display);
        font-size: 1rem;
      }
      .ofertas-empty {
        display: grid;
        justify-items: center;
        gap: 0.55rem;
        text-align: center;
        padding: 1.75rem 1rem;
        border: 1px dashed var(--dx-line);
        border-radius: 12px;
        background: #fff;
        color: var(--dx-ink-soft);
      }
      .ofertas-empty dx-icon {
        color: var(--dx-red);
      }
      .ads-home .block-head {
        margin-bottom: 0.75rem;
      }
      .ads-stage {
        display: grid;
        gap: 0.65rem;
      }
      @media (min-width: 800px) {
        .ads-stage {
          grid-template-columns: 1.6fr 1fr;
          grid-auto-rows: minmax(140px, auto);
        }
        .ad-banner--hero {
          grid-row: span 2;
          min-height: 280px;
        }
      }
      .ad-banner {
        position: relative;
        display: grid;
        align-content: end;
        gap: 0.25rem;
        min-height: 140px;
        padding: 1rem 1.1rem;
        border-radius: 14px;
        background: #111827 center / cover no-repeat;
        text-decoration: none;
        color: #fff;
        overflow: hidden;
        border: 1px solid var(--dx-line);
        transition: transform 0.18s ease, box-shadow 0.18s ease;
      }
      .ad-banner:hover {
        transform: translateY(-2px);
        box-shadow: 0 14px 28px rgba(15, 23, 42, 0.14);
      }
      .ad-badge {
        display: inline-block;
        width: fit-content;
        font-size: 0.65rem;
        font-weight: 800;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        background: rgba(255, 255, 255, 0.2);
        padding: 0.15rem 0.45rem;
        border-radius: 4px;
      }
      .ad-banner strong {
        font-family: var(--dx-display);
        font-size: clamp(1.1rem, 2vw, 1.45rem);
        line-height: 1.2;
        text-shadow: 0 1px 4px rgba(0, 0, 0, 0.45);
      }
      .ad-sub {
        font-size: 0.88rem;
        opacity: 0.92;
      }
      .ad-cta {
        display: inline-flex;
        align-items: center;
        gap: 0.15rem;
        margin-top: 0.2rem;
        font-size: 0.85rem;
        font-weight: 750;
      }
      .browse-cta--home {
        border-radius: 14px;
        padding: 1.25rem 1.35rem;
        background: linear-gradient(120deg, #111827, #1f2937);
        color: #fff;
        border: 0;
      }
      .browse-cta--home h2 {
        color: #fff;
      }
      .browse-cta--home p {
        color: rgba(255, 255, 255, 0.78);
      }
      .trust {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.45rem;
        margin: 0 0 1rem;
      }
      @media (min-width: 800px) {
        .trust {
          grid-template-columns: repeat(4, minmax(0, 1fr));
        }
      }
      .trust-item {
        display: flex;
        gap: 0.45rem;
        padding: 0.55rem 0.6rem;
        border: 1px solid var(--dx-line);
        border-radius: 6px;
        background: #fff;
      }
      .trust-ico {
        width: 32px;
        height: 32px;
        border-radius: 6px;
        background: #f3f3f3;
        display: grid;
        place-items: center;
        flex-shrink: 0;
      }
      .trust-item strong {
        display: block;
        font-size: 0.8rem;
      }
      .trust-item p {
        margin: 0.1rem 0 0;
        font-size: 0.72rem;
        color: var(--dx-mute);
      }
      .block {
        margin: 0 0 1.25rem;
      }
      .block-head {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.55rem;
      }
      .dx-section-title {
        margin: 0;
        font-size: 1.15rem;
        font-weight: 800;
      }
      .rail-title {
        margin: 0;
        font-size: 1rem;
        font-weight: 700;
      }
      .see-all {
        border: 0;
        background: none;
        color: var(--dx-red);
        font-weight: 700;
        font-size: 0.85rem;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 0.15rem;
      }
      .cat-strip {
        display: grid;
        grid-auto-flow: column;
        grid-auto-columns: minmax(108px, 128px);
        gap: 0.65rem;
        overflow-x: auto;
        padding: 0.15rem 0 0.45rem;
        scroll-snap-type: x mandatory;
      }
      .cat-chip {
        scroll-snap-align: start;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        background: #fff;
        padding: 0.55rem 0.45rem;
        text-align: center;
        cursor: pointer;
        text-decoration: none;
        color: inherit;
        display: grid;
        gap: 0.3rem;
        justify-items: center;
        min-width: 108px;
        transition: border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease;
      }
      .cat-chip:hover {
        border-color: #f5c2c0;
        box-shadow: 0 8px 18px rgba(15, 23, 42, 0.08);
        transform: translateY(-1px);
      }
      .cat-chip__media {
        width: 64px;
        height: 64px;
        border-radius: 12px;
        background: #f0f0f0 center / cover no-repeat;
        display: grid;
        place-items: center;
        color: var(--dx-mute);
      }
      .cat-chip__label {
        font-size: 0.75rem;
        font-weight: 700;
        line-height: 1.2;
      }
      .cat-chip small {
        color: var(--dx-mute);
        font-size: 0.68rem;
      }
      .coupon-strip {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem 1rem;
        align-items: center;
        justify-content: space-between;
        padding: 0.55rem 0.75rem;
        margin-bottom: 0.55rem;
        border-radius: 6px;
        border: 1px dashed var(--dx-red);
        background: #fff8f8;
        font-size: 0.85rem;
      }
      .coupon-strip .dx-btn {
        flex-shrink: 0;
        white-space: nowrap;
      }
      .coupon-strip .dx-btn:hover {
        background: color-mix(in srgb, var(--dx-red) 72%, #000);
        color: #fff !important;
      }
      .coupon-strip__copy {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }
      .news-form {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
        align-items: center;
      }
      .news-form .dx-input {
        min-width: 200px;
        width: min(280px, 100%);
        min-height: 40px;
      }
      .promo-grid,
      .flyer-rail {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 0.55rem;
      }
      .promo-tile,
      .flyer {
        min-height: 120px;
        border-radius: 8px;
        background: #333 center / cover no-repeat;
        text-decoration: none;
        color: #fff;
        display: grid;
        align-items: end;
        padding: 0.65rem;
        border: 1px solid var(--dx-line);
      }
      .flyer-tag {
        display: inline-block;
        font-size: 0.65rem;
        font-weight: 800;
        text-transform: uppercase;
        background: var(--dx-red);
        padding: 0.15rem 0.35rem;
        border-radius: 3px;
        margin-bottom: 0.25rem;
      }
      .promo-tile h3,
      .flyer h3 {
        margin: 0;
        font-size: 0.95rem;
        text-shadow: 0 1px 4px rgba(0, 0, 0, 0.45);
      }
      .product-rail {
        display: grid;
        grid-auto-flow: column;
        grid-auto-columns: minmax(148px, 168px);
        gap: 0.55rem;
        overflow-x: auto;
        padding-bottom: 0.35rem;
        scroll-snap-type: x mandatory;
      }
      .rail-block {
        margin-top: 1rem;
      }
      .rail-card {
        scroll-snap-align: start;
        border: 1px solid var(--dx-line);
        border-radius: 8px;
        background: #fff;
        overflow: hidden;
        display: grid;
        grid-template-rows: 1fr auto;
      }
      .rail-card > a {
        text-decoration: none;
        color: inherit;
        display: grid;
      }
      .rail-card__media {
        aspect-ratio: 1;
        background: #f3f3f3;
        position: relative;
        display: grid;
        place-items: center;
        overflow: hidden;
      }
      .rail-card__media img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }
      .rail-card__media.empty {
        color: var(--dx-mute);
      }
      .rail-card__media .dx-badge {
        position: absolute;
        top: 0.35rem;
        left: 0.35rem;
      }
      .quick-add {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.3rem;
        width: 100%;
        border: 0;
        border-top: 1px solid var(--dx-line);
        background: #232f3e;
        color: #fff;
        font-weight: 700;
        font-size: 0.78rem;
        padding: 0.45rem 0.5rem;
        cursor: pointer;
      }
      .quick-add:hover:not(:disabled) {
        background: #37475a;
      }
      .quick-add:disabled {
        opacity: 0.55;
        cursor: not-allowed;
        background: #9e9e9e;
      }
      .rail-card__body {
        padding: 0.45rem 0.5rem 0.55rem;
        display: grid;
        gap: 0.15rem;
      }
      .brand-line {
        font-size: 0.68rem;
        color: var(--dx-mute);
        text-transform: uppercase;
        font-weight: 700;
      }
      .rail-card h3 {
        margin: 0;
        font-size: 0.8rem;
        line-height: 1.25;
        font-weight: 600;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        min-height: 2em;
      }
      .price {
        margin: 0;
        font-weight: 800;
        color: var(--dx-price, var(--dx-red));
        font-size: 0.95rem;
      }
      .price.ask,
      .ask {
        font-weight: 600;
        color: var(--dx-mute);
        font-size: 0.85rem;
      }
      .newsletter,
      .browse-cta {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 1rem 1.1rem;
        margin: 1rem 0;
        border-radius: 8px;
        border: 1px solid var(--dx-line);
        background: #fff;
      }
      .newsletter h2,
      .browse-cta h2 {
        margin: 0 0 0.2rem;
        font-size: 1.05rem;
      }
      .newsletter p,
      .browse-cta p {
        margin: 0;
        color: var(--dx-ink-soft);
        font-size: 0.88rem;
      }
      .empty-inline,
      .empty-box {
        grid-column: 1 / -1;
        text-align: center;
        padding: 1.5rem;
        color: var(--dx-ink-soft);
      }
      .empty-box {
        display: grid;
        justify-items: center;
        gap: 0.45rem;
        border: 1px dashed var(--dx-line);
        border-radius: 8px;
        background: #fff;
      }

      /* —— Amazon listing —— */
      .az {
        display: grid;
        gap: 1rem;
        align-items: start;
      }
      .az-filters-toggle {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        border: 1px solid var(--dx-line);
        background: #fff;
        border-radius: 4px;
        padding: 0.5rem 0.75rem;
        font-weight: 700;
        font-size: 0.85rem;
        cursor: pointer;
      }
      .az-filters-dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: var(--dx-red);
      }
      @media (min-width: 960px) {
        .az {
          grid-template-columns: 240px minmax(0, 1fr);
          gap: 1.25rem;
        }
        .az-filters-toggle {
          display: none;
        }
      }
      .az-side {
        display: none;
        position: sticky;
        top: 0.75rem;
        align-content: start;
        gap: 0;
        padding: 0.65rem 0.75rem 0.85rem;
        border: 1px solid #e5e5e5;
        background: #fff;
        max-height: calc(100vh - 1.5rem);
        overflow: auto;
      }
      .az-side.open {
        display: grid;
      }
      @media (min-width: 960px) {
        .az-side {
          display: grid;
        }
      }
      .az-side__eyebrow {
        margin: 0 0 0.35rem;
        font-size: 0.95rem;
        font-weight: 800;
        color: #222;
        text-transform: uppercase;
        letter-spacing: 0.02em;
      }
      .az-side__block {
        border-top: 1px solid #ececec;
        padding: 0.35rem 0 0.55rem;
      }
      .az-side__block h2 {
        margin: 0.4rem 0;
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: #555;
      }
      .az-acc {
        display: flex;
        width: 100%;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        border: 0;
        background: transparent;
        padding: 0.45rem 0;
        font-size: 0.78rem;
        font-weight: 800;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: #444;
        cursor: pointer;
      }
      .az-acc dx-icon {
        transition: transform 0.15s ease;
        color: #888;
      }
      .az-acc dx-icon.flip {
        transform: rotate(180deg);
      }
      .az-checks {
        display: grid;
        gap: 0.28rem;
        padding: 0 0 0.25rem;
        max-height: 220px;
        overflow: auto;
      }
      .az-marca-search {
        width: 100%;
        margin-bottom: 0.25rem;
        font-size: 0.82rem;
        padding: 0.35rem 0.5rem;
      }
      .az-select {
        width: 100%;
      }
      .az-check {
        display: flex;
        align-items: flex-start;
        gap: 0.45rem;
        font-size: 0.82rem;
        line-height: 1.25;
        cursor: pointer;
        color: #333;
      }
      .az-check input {
        margin-top: 0.15rem;
        flex-shrink: 0;
      }
      .az-check__txt {
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
      }
      .az-more {
        border: 0;
        background: transparent;
        color: var(--dx-focus);
        font-size: 0.78rem;
        font-weight: 700;
        text-align: left;
        padding: 0.2rem 0;
        cursor: pointer;
      }
      .az-clear {
        width: 100%;
        margin-top: 0.5rem;
      }
      .az-wish {
        margin-top: 0.75rem;
        padding-top: 0.75rem;
        border-top: 1px solid #ececec;
        display: grid;
        gap: 0.25rem;
      }
      .az-wish strong {
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.03em;
      }
      .az-wish a {
        font-size: 0.82rem;
        color: var(--dx-focus);
        text-decoration: none;
        font-weight: 600;
      }
      .az-wish a:hover {
        text-decoration: underline;
      }
      .az-crumb {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        align-items: center;
        font-size: 0.8rem;
        margin-bottom: 0.55rem;
        color: var(--dx-mute);
      }
      .az-crumb a {
        color: var(--dx-focus);
        text-decoration: none;
        font-weight: 700;
      }
      .az-crumb a:hover {
        text-decoration: underline;
      }
      .az-head {
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
        margin-bottom: 0.85rem;
        padding-bottom: 0.75rem;
        border-bottom: 1px solid #e5e5e5;
      }
      .az-head__title h1 {
        margin: 0;
        font-size: 1.35rem;
        font-weight: 800;
        color: #1a1a1a;
      }
      .az-count {
        margin: 0.2rem 0 0;
        font-size: 0.8rem;
        color: var(--dx-mute);
      }
      .az-toolbar {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-end;
        gap: 0.75rem 1rem;
        width: 100%;
      }
      .az-search-row {
        flex: 1 1 280px;
        min-width: 0;
        max-width: 520px;
        position: relative;
        z-index: 5;
        display: grid;
        grid-template-rows: auto auto;
        gap: 0.28rem;
        align-content: end;
      }
      .az-search-row ::ng-deep .search,
      .az-search-row ::ng-deep .search-wrap {
        width: 100%;
      }
      .az-search-row ::ng-deep .search--catalog .search-field {
        min-height: 40px;
        height: 40px;
        border-radius: 6px;
        padding: 0 0.25rem 0 0.75rem;
      }
      .az-search-row ::ng-deep .search--catalog .search-field .dx-input {
        min-height: 40px;
        height: 40px;
        padding: 0 0.4rem 0 0 !important;
        font-size: 0.82rem;
      }
      .az-search-row ::ng-deep .search--catalog .clear,
      .az-search-row ::ng-deep .search--catalog .glass {
        width: 34px;
        height: 34px;
      }
      .az-tools {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem 0.85rem;
        align-items: flex-end;
        margin-left: auto;
        flex: 0 1 auto;
      }
      .az-tool {
        display: grid;
        grid-template-rows: auto auto;
        gap: 0.28rem;
        align-content: end;
        min-width: 0;
      }
      .az-tool__lbl {
        display: block;
        font-size: 0.72rem;
        line-height: 1.2;
        color: var(--dx-mute);
        font-weight: 600;
        letter-spacing: 0.01em;
        white-space: nowrap;
      }
      .az-tool__ctrl {
        height: 40px;
        min-height: 40px;
        box-sizing: border-box;
        font-size: 0.82rem;
      }
      .az-tool .dx-select.az-tool__ctrl {
        min-width: 148px;
        width: 100%;
        min-height: 40px;
        height: 40px;
        padding: 0 0.75rem;
        border-radius: 6px;
        border-color: #ddd;
      }
      .az-tool .dx-select.az-tool__ctrl--sm {
        min-width: 72px;
        width: 72px;
      }
      .az-view.az-tool__ctrl {
        display: inline-flex;
        align-items: stretch;
        gap: 0;
        padding: 0;
        border: 1px solid #ddd;
        border-radius: 6px;
        overflow: hidden;
        background: #fff;
        width: auto;
        height: 40px;
        min-height: 40px;
      }
      .az-view__btn {
        display: grid;
        place-items: center;
        width: 40px;
        height: 100%;
        border: 0;
        border-right: 1px solid #ddd;
        background: transparent;
        color: #666;
        cursor: pointer;
        margin: 0;
        border-radius: 0;
      }
      .az-view__btn:last-child {
        border-right: 0;
      }
      .az-view__btn.on {
        background: #f0f0f0;
        color: #111;
      }
      .az-view__btn:hover:not(.on) {
        background: #fafafa;
        color: #333;
      }
      @media (max-width: 720px) {
        .az-toolbar {
          flex-direction: column;
          align-items: stretch;
        }
        .az-search-row {
          max-width: none;
        }
        .az-tools {
          margin-left: 0;
          justify-content: flex-start;
        }
      }
      .az-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem 0.5rem;
      }
      @media (min-width: 700px) {
        .az-grid {
          grid-template-columns: repeat(3, minmax(0, 1fr));
        }
      }
      @media (min-width: 1000px) {
        .az-grid {
          grid-template-columns: repeat(4, minmax(0, 1fr));
        }
      }
      @media (min-width: 1280px) {
        .az-grid {
          grid-template-columns: repeat(5, minmax(0, 1fr));
        }
      }
      .az-grid--list {
        grid-template-columns: 1fr !important;
        gap: 0.65rem;
      }
      .az-card {
        display: grid;
        grid-template-rows: 1fr auto;
        border: 0;
        background: #fff;
        overflow: hidden;
        transition: box-shadow 0.15s ease;
        position: relative;
      }
      .az-card > a {
        text-decoration: none;
        color: inherit;
        display: grid;
        grid-template-rows: auto 1fr;
      }
      .az-card:hover {
        box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
      }
      .az-card--list > a {
        grid-template-columns: 160px minmax(0, 1fr);
        grid-template-rows: auto;
        gap: 0.85rem;
        align-items: center;
      }
      .az-card__media {
        aspect-ratio: 1;
        background: #fff;
        position: relative;
        display: grid;
        place-items: center;
        overflow: hidden;
        padding: 0.5rem;
      }
      .az-card--list .az-card__media {
        aspect-ratio: 1;
        max-height: 140px;
      }
      .az-card__media img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }
      .az-card__media.empty {
        color: #bdbdbd;
        background: #f7f7f7;
      }
      .az-add {
        opacity: 0;
        transform: translateY(4px);
        transition: opacity 0.15s ease, transform 0.15s ease;
        border-radius: 0;
        margin: 0 0.35rem 0.35rem;
      }
      .az-card:hover .az-add,
      .az-card:focus-within .az-add {
        opacity: 1;
        transform: none;
      }
      @media (max-width: 900px) {
        .az-add {
          opacity: 1;
          transform: none;
        }
      }
      .az-stock {
        position: absolute;
        top: 0.4rem;
        left: 0.4rem;
        font-size: 0.65rem;
        font-weight: 800;
        padding: 0.15rem 0.4rem;
        border-radius: 2px;
        letter-spacing: 0.02em;
      }
      .az-stock.out {
        background: #c62828;
        color: #fff;
      }
      .az-card__body {
        padding: 0.35rem 0.4rem 0.55rem;
        display: grid;
        gap: 0.35rem;
        align-content: start;
      }
      .az-title {
        margin: 0;
        font-size: 0.84rem;
        font-weight: 600;
        line-height: 1.3;
        display: -webkit-box;
        -webkit-line-clamp: 3;
        -webkit-box-orient: vertical;
        overflow: hidden;
        min-height: 2.5em;
      }
      .az-title__lead {
        color: var(--dx-focus);
        font-weight: 700;
      }
      .az-title__rest {
        color: #6b6b6b;
        font-weight: 500;
      }
      .az-card:hover .az-title__lead {
        text-decoration: underline;
      }
      .az-price {
        margin: 0;
        font-size: 1.12rem;
        font-weight: 800;
        color: var(--dx-price, #c62828);
        letter-spacing: -0.01em;
      }
      .az-price .ask {
        font-size: 0.9rem;
        color: var(--dx-mute);
        font-weight: 700;
      }
      .skel-card {
        border: 1px solid var(--dx-line);
        border-radius: 6px;
        overflow: hidden;
        background: #fff;
      }
      .skel-media {
        aspect-ratio: 1;
        background: linear-gradient(90deg, #eee, #f7f7f7, #eee);
        background-size: 200% 100%;
        animation: shimmer 1.2s infinite;
      }
      .skel-line {
        height: 9px;
        margin: 0.5rem;
        border-radius: 3px;
        background: #eee;
      }
      .skel-line.short {
        width: 40%;
      }
      @keyframes shimmer {
        0% {
          background-position: 100% 0;
        }
        100% {
          background-position: -100% 0;
        }
      }
      .pager {
        margin-top: 1.25rem;
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .pager-pages {
        display: flex;
        flex-wrap: wrap;
        gap: 0.25rem;
      }
      .pager-num {
        min-width: 34px;
        height: 34px;
        border-radius: 4px;
        border: 1px solid var(--dx-line);
        background: #fff;
        font-weight: 700;
        cursor: pointer;
      }
      .pager-num.on {
        background: #232f3e;
        border-color: #232f3e;
        color: #fff;
      }
      .pager-ellipsis {
        padding: 0 0.2rem;
        color: var(--dx-mute);
      }
      @keyframes rise {
        from {
          opacity: 0;
          transform: translateY(6px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }
    `,
  ],
})
export class CatalogPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly session = inject(SessionService);
  readonly auth = inject(AuthModalService);
  readonly brand = inject(BrandingService);
  readonly pers = inject(PersonalizacionService);
  private readonly toast = inject(ToastService);
  private readonly miniCart = inject(MiniCartService);
  newsEmail = '';
  newsBusy = false;

  search = '';
  sort = 'nombre';
  canal: Canal = getCanal();
  grupoId: number | null = null;
  marcaId: number | null = null;
  lineaId: number | null = null;
  tipoId: number | null = null;
  soloStock = false;
  pageSize = 20;
  browse = false;
  /** Listing feed from home “Ver más” (recomendados / más vendidos / ofertas). */
  feedMode: 'recomendados' | 'masVendidos' | 'ofertas' | null = null;
  loggedIn = false;
  filtersOpen = false;
  viewMode: 'grid' | 'list' = 'grid';
  sideCatOpen = true;
  sideMarcaOpen = true;
  sideStockOpen = true;
  catsExpanded = false;
  marcasExpanded = false;
  marcaSearch = '';
  readonly catLimit = 8;
  readonly marcaLimit = 8;
  addingId: number | null = null;
  readonly brokenImg = signal<Record<number, boolean>>({});

  readonly searchMode = signal(false);
  readonly page = signal<PagedResult<ProductListItem> | null>(null);
  readonly collections = signal<CollectionItem[]>([]);
  readonly categoryRails = signal<CategoryRail[]>([]);
  readonly promos = signal<Recomendacion[]>([]);
  readonly marketing = signal<MarketingSlot[]>([]);
  readonly marcas = signal<CatalogMarca[]>([]);
  readonly lineas = signal<CatalogFacet[]>([]);
  readonly tipos = signal<CatalogFacet[]>([]);
  readonly masVendidos = signal<ProductListItem[]>([]);
  readonly ofertaProducts = signal<ProductListItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly cmsBlocks = signal<StorefrontCmsBlock[]>([]);
  private rawItems: ProductListItem[] = [];
  private syncingFromUrl = false;
  private skipNextUrlLoad = false;
  private readonly search$ = new Subject<string>();
  private searchSub?: Subscription;

  brandName(): string {
    return this.brand.branding()?.nombreTienda?.trim() || 'Casa Imperial';
  }

  heroBannerUrl(): string | null {
    const fromCms = this.cmsHero()?.items?.find((i) => i.imageUrl)?.imageUrl;
    if (fromCms) return this.mediaUrl(fromCms);
    const branding = this.brand.branding()?.bannerUrl;
    if (branding) return this.brand.media(branding);
    const tema = this.pers.temaBannerUrl();
    return tema ? this.mediaUrl(tema) : null;
  }

  heroBannerHref(): string {
    const raw = (this.cmsHero()?.items?.find((i) => i.imageUrl)?.linkUrl || '').trim();
    if (!raw) return '#ofertas';
    if (/^https?:\/\//i.test(raw) || raw.startsWith('/') || raw.startsWith('#')) return raw;
    return `/${raw}`;
  }

  heroBannerExternal(): boolean {
    return /^https?:\/\//i.test(this.heroBannerHref());
  }

  onHeroBannerClick(ev: Event): void {
    const href = this.heroBannerHref();
    if (href.startsWith('#')) {
      ev.preventDefault();
      this.scrollId(href.slice(1), ev);
    }
  }

  shopCollections(): CollectionItem[] {
    return this.collections()
      .filter((c) => c.articulosCount > 0)
      .slice()
      .sort((a, b) => b.articulosCount - a.articulosCount);
  }

  /** Chips for home categorías: grupos con stock, o líneas como fallback. */
  homeCategoryChips(): {
    key: string;
    nombre: string;
    count: number | null;
    imageUrl: string | null;
    grupoId?: number;
    lineaId?: number;
  }[] {
    const cols = this.shopCollections();
    if (cols.length) {
      return cols.slice(0, 16).map((c) => ({
        key: `g-${c.id}`,
        nombre: c.nombre,
        count: c.articulosCount,
        imageUrl: c.imagenUrl,
        grupoId: c.id,
      }));
    }
    return this.lineas()
      .slice(0, 16)
      .map((l) => ({
        key: `l-${l.id}`,
        nombre: l.nombre,
        count: null as number | null,
        imageUrl: null as string | null,
        lineaId: l.id,
      }));
  }

  openCategoryChip(c: {
    grupoId?: number;
    lineaId?: number;
  }): void {
    if (c.grupoId != null) {
      this.selectGrupo(c.grupoId);
      return;
    }
    if (c.lineaId != null) {
      this.lineaId = c.lineaId;
      this.grupoId = null;
      this.browse = true;
      this.feedMode = null;
      this.load(1);
      queueMicrotask(() =>
        document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
      );
    }
  }

  listingMode(): boolean {
    if (this.searchMode()) return true;
    if (this.browse) return true;
    if (this.feedMode) return true;
    if (this.grupoId != null || this.marcaId != null || this.lineaId != null || this.tipoId != null) return true;
    if (this.search.trim()) return true;
    if (this.soloStock) return true;
    return false;
  }

  activeGrupoName(): string | null {
    if (this.grupoId == null) return null;
    return this.collections().find((c) => c.id === this.grupoId)?.nombre ?? null;
  }

  listingTitle(): string {
    if (this.feedMode === 'recomendados') return 'Recomendados para vos';
    if (this.feedMode === 'masVendidos') return 'Los más vendidos';
    if (this.feedMode === 'ofertas') return 'Productos en oferta';
    if (this.search.trim()) return `Resultados para “${this.search.trim()}”`;
    const g = this.activeGrupoName();
    if (g) return g;
    if (this.lineaId != null) {
      return this.lineas().find((l) => l.id === this.lineaId)?.nombre ?? 'Línea';
    }
    return 'Todos los productos';
  }

  visibleCategorias(): CollectionItem[] {
    const all = this.shopCollections();
    return this.catsExpanded ? all : all.slice(0, this.catLimit);
  }

  filteredMarcas(): CatalogMarca[] {
    const q = this.marcaSearch.trim().toLowerCase();
    const all = this.marcas();
    if (!q) return all;
    return all.filter((m) => m.nombre.toLowerCase().includes(q));
  }

  visibleMarcas(): CatalogMarca[] {
    const all = this.filteredMarcas();
    return this.marcasExpanded ? all : all.slice(0, this.marcaLimit);
  }

  titleParts(p: ProductListItem): { lead: string; rest: string } {
    const name = (p.nombre || '').trim();
    const marca = (p.marca || '').trim();
    if (marca) {
      const re = new RegExp(`^${marca.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'i');
      if (re.test(name)) {
        const rest = name.replace(re, '').trim();
        return { lead: marca, rest };
      }
      return { lead: marca, rest: name };
    }
    const words = name.split(/\s+/);
    if (words.length <= 3) return { lead: name, rest: '' };
    return { lead: words.slice(0, 3).join(' '), rest: words.slice(3).join(' ') };
  }

  selectMarca(id: number | null): void {
    this.marcaId = id;
    this.browse = true;
    this.load(1);
  }

  selectLinea(id: number | null): void {
    this.lineaId = id;
    this.browse = true;
    this.load(1);
  }

  selectTipo(id: number | null): void {
    this.tipoId = id;
    this.browse = true;
    this.load(1);
  }

  private cmsByType(...types: string[]): StorefrontCmsBlock | null {
    return this.cmsBlocks().find((b) => types.includes(b.blockType)) ?? null;
  }

  cmsTopBar(): StorefrontCmsBlock | null {
    return this.cmsByType('top_info_bar');
  }

  cmsHero(): StorefrontCmsBlock | null {
    return this.cmsByType('hero_banner', 'hero_slider');
  }

  cmsFeatures(): StorefrontCmsBlock | null {
    return this.cmsByType('feature_list');
  }

  cmsFeaturedCats(): StorefrontCmsBlock | null {
    return this.cmsByType('featured_categories');
  }

  cmsPromoGrid(): StorefrontCmsBlock | null {
    return this.cmsByType('promo_banner_grid');
  }

  /** Ofertas visuales: CMS con imagen, o slots TipoGrupo=3 (promoción). */
  promoVisualItems(): {
    key: string;
    title: string;
    sub: string | null;
    badge: string | null;
    imageUrl: string | null;
    link: string;
    external: boolean;
  }[] {
    if (!this.pers.sectionVisible('flyers')) return [];
    const cmsItems = this.cmsPromoGrid()?.items || [];
    const cmsWithImg = cmsItems.filter((i) => !!(i.imageUrl || '').trim());
    if (cmsWithImg.length) {
      return cmsWithImg.map((it) => {
        const link = (it.linkUrl || '/catalogo#ofertas').trim();
        const external = /^https?:\/\//i.test(link);
        return {
          key: `cms-${it.id}`,
          title: it.title || 'Promo',
          sub: it.subtitle || it.description,
          badge: it.badgeText,
          imageUrl: it.imageUrl,
          link: link.startsWith('/') || external ? link : `/${link}`,
          external,
        };
      });
    }
    return this.marketing()
      .filter((s) => s.tipoGrupo === 3)
      .map((slot) => {
        const link = this.slotLink(slot);
        return {
          key: `mkt-${slot.id}`,
          title: slot.titulo,
          sub: slot.subtitulo,
          badge: slot.tipoNombre || 'Oferta',
          imageUrl: slot.imagenUrl,
          link,
          external: this.slotExternal(slot),
        };
      });
  }

  /** Publicidades TipoGrupo=2 (solo si la sección está activa). */
  publicidadItems(): {
    key: string;
    title: string;
    sub: string | null;
    imageUrl: string | null;
    link: string;
    external: boolean;
  }[] {
    if (!this.pers.sectionVisible('publicidad')) return [];
    return this.marketing()
      .filter((s) => s.tipoGrupo === 2 && !!(s.imagenUrl || '').trim())
      .map((slot) => {
        const link = this.slotLink(slot);
        return {
          key: `ad-${slot.id}`,
          title: slot.titulo,
          sub: slot.subtitulo,
          imageUrl: slot.imagenUrl,
          link,
          external: this.slotExternal(slot),
        };
      });
  }

  cmsCoupon(): StorefrontCmsBlock | null {
    return this.cmsByType('coupon_strip');
  }

  cmsCarousel(): StorefrontCmsBlock | null {
    return this.cmsByType('product_carousel');
  }

  cmsNewsletter(): StorefrontCmsBlock | null {
    return this.cmsByType('newsletter');
  }

  trustItems(): { icon: string; title: string; sub: string }[] {
    const feat = this.cmsFeatures();
    if (feat?.items?.length) {
      const icons = ['truck', 'shield', 'check', 'chat'];
      return feat.items.slice(0, 4).map((it, i) => ({
        icon: icons[i % icons.length],
        title: it.title || 'Beneficio',
        sub: it.description || '',
      }));
    }
    return [
      { icon: 'truck', title: 'Envíos y retiro', sub: 'Delivery o retiro según depósito.' },
      { icon: 'shield', title: 'Compra segura', sub: 'Pago y seguimiento de pedido.' },
      { icon: 'check', title: 'Stock real', sub: 'Disponibilidad del depósito activo.' },
      { icon: 'chat', title: 'Asesoramiento', sub: 'Metros, medidas y presentaciones.' },
    ];
  }

  hasActiveFilters(): boolean {
    return !!(
      this.search.trim() ||
      this.marcaId != null ||
      this.grupoId != null ||
      this.lineaId != null ||
      this.tipoId != null ||
      this.soloStock
    );
  }

  pagerPages(): number[] {
    const pg = this.page();
    if (!pg) return [];
    const total = Math.max(1, pg.totalPages || 1);
    const cur = pg.page;
    if (total <= 9) return Array.from({ length: total }, (_, i) => i + 1);

    const set = new Set<number>([1, total, cur, cur - 1, cur + 1, cur - 2, cur + 2]);
    const nums = [...set].filter((n) => n >= 1 && n <= total).sort((a, b) => a - b);
    const out: number[] = [];
    let prev = 0;
    for (const n of nums) {
      if (prev && n - prev > 1) out.push(-1);
      out.push(n);
      prev = n;
    }
    return out;
  }

  ngOnInit(): void {
    this.searchMode.set(this.route.snapshot.data['searchMode'] === true);
    this.loggedIn = this.session.loggedIn();
    this.canal = this.session.mayoristaAprobado() ? this.session.canal() : 'B2C';

    this.searchSub = this.search$.pipe(debounceTime(350), distinctUntilChanged()).subscribe(() => this.load(1));

    this.api.collections().subscribe({
      next: (c) => {
        this.collections.set(c);
        if (!this.listingMode()) this.loadCategoryRails();
      },
      error: () => this.collections.set([]),
    });

    if (this.listingMode()) this.ensureMarcas();
    this.ensureFacets();

    if (!this.searchMode()) {
      this.api.recomendaciones().subscribe({
        next: (r) => this.promos.set(r.slice(0, 12)),
        error: () => this.promos.set([]),
      });
      this.api.getMarketingSlots().subscribe({
        next: (slots) => this.marketing.set(slots),
        error: () => this.marketing.set([]),
      });
      this.api.getHomeContent().subscribe({
        next: (h) => this.cmsBlocks.set(h.blocks ?? []),
        error: () => this.cmsBlocks.set([]),
      });
      this.api.getMasVendidos(10, this.canal).subscribe({
        next: (rows) => this.masVendidos.set(rows),
        error: () => this.masVendidos.set([]),
      });
      if (this.pers.sectionVisible('flyers') && this.pers.showProductosOferta()) {
        this.api.getOfertas(12, this.canal).subscribe({
          next: (rows) => this.ofertaProducts.set(rows),
          error: () => this.ofertaProducts.set([]),
        });
      } else {
        this.ofertaProducts.set([]);
      }
    }

    this.route.queryParamMap.subscribe((params) => {
      const urlQ = params.get('q') ?? params.get('search') ?? '';
      const marcaRaw = params.get('marca');
      const urlMarca = marcaRaw != null && marcaRaw !== '' ? Number(marcaRaw) : null;
      const grupoRaw = params.get('grupo');
      const urlGrupo = grupoRaw != null && grupoRaw !== '' ? Number(grupoRaw) : null;
      const lineaRaw = params.get('linea');
      const urlLinea = lineaRaw != null && lineaRaw !== '' ? Number(lineaRaw) : null;
      const tipoRaw = params.get('tipo');
      const urlTipo = tipoRaw != null && tipoRaw !== '' ? Number(tipoRaw) : null;
      const urlStock = params.get('soloStock') === '1' || params.get('soloStock') === 'true';
      const urlSort = params.get('sort') || 'nombre';
      const urlBrowse = params.get('browse') === '1' || params.get('browse') === 'true';
      const urlFeedRaw = params.get('feed');
      const urlFeed =
        urlFeedRaw === 'recomendados' || urlFeedRaw === 'masVendidos' || urlFeedRaw === 'ofertas'
          ? urlFeedRaw
          : null;
      const urlPageSize = Number(params.get('pageSize'));
      const pageNum = Math.max(1, Number(params.get('page')) || 1);

      if (this.skipNextUrlLoad) {
        this.skipNextUrlLoad = false;
        const same =
          urlQ.trim() === this.search.trim() &&
          (Number.isNaN(urlMarca as number) ? null : urlMarca) === this.marcaId &&
          (Number.isNaN(urlGrupo as number) ? null : urlGrupo) === this.grupoId &&
          (Number.isNaN(urlLinea as number) ? null : urlLinea) === this.lineaId &&
          (Number.isNaN(urlTipo as number) ? null : urlTipo) === this.tipoId &&
          urlStock === this.soloStock &&
          urlBrowse === this.browse &&
          urlFeed === this.feedMode &&
          (params.get('sort') ? urlSort === this.sort : true);
        if (same) return;
      }

      this.syncingFromUrl = true;
      this.search = urlQ;
      this.marcaId = urlMarca != null && !Number.isNaN(urlMarca) ? urlMarca : null;
      this.grupoId = urlGrupo != null && !Number.isNaN(urlGrupo) ? urlGrupo : null;
      this.lineaId = urlLinea != null && !Number.isNaN(urlLinea) ? urlLinea : null;
      this.tipoId = urlTipo != null && !Number.isNaN(urlTipo) ? urlTipo : null;
      this.soloStock = urlStock;
      this.browse = urlBrowse || this.searchMode() || !!urlFeed;
      this.feedMode = urlFeed;
      if (params.get('sort')) this.sort = urlSort;
      if ([20, 24, 36, 48].includes(urlPageSize)) this.pageSize = urlPageSize;
      this.syncingFromUrl = false;

      if (urlFeed) {
        this.ensureMarcas();
        this.ensureFacets();
        this.loadFeed(urlFeed, false);
      } else if (this.listingMode()) {
        this.ensureMarcas();
        this.ensureFacets();
        this.load(pageNum, false);
        queueMicrotask(() =>
          document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
        );
      } else {
        this.page.set(null);
        if (!this.categoryRails().length) this.loadCategoryRails();
      }
    });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  scrollOfertas(ev: Event): void {
    this.scrollId('ofertas', ev);
  }

  scrollId(id: string, ev: Event): void {
    const el = document.getElementById(id);
    if (!el) return;
    ev.preventDefault();
    el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  featuredOffer(): {
    title: string;
    sub: string | null;
    badge: string;
    imageUrl: string | null;
    linkUrl: string | null;
    external: boolean;
  } | null {
    if (!this.pers.showHeroFeature()) return null;

    if (this.pers.sectionVisible('flyers')) {
      const cms = this.cmsPromoGrid()?.items?.find((i) => i.imageUrl) || this.cmsPromoGrid()?.items?.[0];
      if (cms?.imageUrl) {
        const link = (cms.linkUrl || '').trim();
        const external = /^https?:\/\//i.test(link);
        return {
          title: cms.title || 'Oferta destacada',
          sub: cms.subtitle || cms.description,
          badge: cms.badgeText || 'Destacado',
          imageUrl: cms.imageUrl,
          linkUrl: link || '/catalogo#ofertas',
          external,
        };
      }
      const slot = this.marketing().find((s) => s.tipoGrupo === 3 && s.imagenUrl);
      if (slot) {
        const link = (slot.urlDestino || '').trim();
        const external = /^https?:\/\//i.test(link);
        return {
          title: slot.titulo,
          sub: slot.subtitulo,
          badge: slot.tipoNombre || 'Oferta',
          imageUrl: slot.imagenUrl,
          linkUrl: link || `/catalogo?grupo=${slot.id}`,
          external,
        };
      }
    }

    if (this.pers.sectionVisible('publicidad')) {
      const ad = this.publicidadItems()[0];
      if (ad) {
        return {
          title: ad.title,
          sub: ad.sub,
          badge: 'Publicidad',
          imageUrl: ad.imageUrl,
          linkUrl: ad.link,
          external: ad.external,
        };
      }
    }

    return null;
  }

  onFeatureClick(
    feat: { linkUrl: string | null; external: boolean },
    ev: Event,
  ): void {
    if (feat.external) return;
    const u = (feat.linkUrl || '').trim();
    if (u.includes('#')) {
      const id = u.split('#')[1]?.split('?')[0];
      if (id) {
        ev.preventDefault();
        this.scrollId(id, ev);
        return;
      }
    }
    if (u.startsWith('/')) {
      ev.preventDefault();
      void this.router.navigateByUrl(u);
    }
  }

  discountLabel(p: ProductListItem): number | null {
    if (p.porcentajeDescuento != null && p.porcentajeDescuento > 0) {
      return Math.round(Number(p.porcentajeDescuento));
    }
    if (p.precioLista != null && p.precioDesde != null && p.precioLista > p.precioDesde) {
      return Math.round((1 - p.precioDesde / p.precioLista) * 100);
    }
    return null;
  }

  couponCode(coupon: StorefrontCmsBlock): string {
    return (
      (coupon.items?.[0]?.badgeText || '').trim() ||
      (coupon.items?.[0]?.title || '').trim() ||
      (coupon.title || '').trim() ||
      'CUPÓN'
    );
  }

  openBrowse(
    mode: 'catalogo' | 'categorias' | 'recomendados' | 'masVendidos' | 'ofertas' = 'catalogo',
  ): void {
    this.browse = true;
    this.grupoId = null;
    this.search = '';
    this.marcaId = null;
    this.lineaId = null;
    this.tipoId = null;
    this.soloStock = false;
    this.filtersOpen = true;
    this.sideCatOpen = true;

    if (mode === 'categorias') {
      this.feedMode = null;
      this.catsExpanded = true;
      this.load(1);
      queueMicrotask(() => {
        document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        const side = document.querySelector('.az-side');
        side?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      });
      return;
    }

    if (mode === 'recomendados' || mode === 'masVendidos' || mode === 'ofertas') {
      this.loadFeed(mode);
      return;
    }

    this.feedMode = null;
    this.load(1);
    queueMicrotask(() =>
      document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
    );
  }

  private loadFeed(mode: 'recomendados' | 'masVendidos' | 'ofertas', syncUrl = true): void {
    this.feedMode = mode;
    this.browse = true;
    this.error.set(null);
    this.loading.set(true);
    if (syncUrl) this.syncQueryParams(1);

    const finish = (items: ProductListItem[]) => {
      this.rawItems = items;
      this.page.set({
        items,
        page: 1,
        pageSize: items.length || this.pageSize,
        totalCount: items.length,
        totalPages: 1,
      });
      this.loading.set(false);
      queueMicrotask(() =>
        document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
      );
    };

    if (mode === 'recomendados') {
      this.api.recomendaciones().subscribe({
        next: (rows) =>
          finish(
            rows.map((r) => ({
              id: r.id,
              sku: r.sku,
              nombre: r.nombre,
              nombreCorto: r.nombre,
              marca: null,
              estadoEcommerce: 'publicado',
              imagenPrincipalUrl: r.imagenUrl,
              precioDesde: r.precioDesde,
              embalajesCount: 1,
              canal: this.canal,
              listaPrecio: null,
              stockLibre: null,
              stockOk: true,
              slugSeo: r.slugSeo,
            })),
          ),
        error: (e) => {
          this.loading.set(false);
          this.error.set(e?.error?.error || e.message || 'Error al cargar recomendados');
        },
      });
      return;
    }

    if (mode === 'masVendidos') {
      this.api.getMasVendidos(48, this.canal).subscribe({
        next: (rows) => finish(rows),
        error: (e) => {
          this.loading.set(false);
          this.error.set(e?.error?.error || e.message || 'Error al cargar más vendidos');
        },
      });
      return;
    }

    this.api.getOfertas(48, this.canal).subscribe({
      next: (rows) => finish(rows),
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || e.message || 'Error al cargar ofertas');
      },
    });
  }

  applyHomeCoupon(coupon: StorefrontCmsBlock): void {
    const code = this.couponCode(coupon);
    if (!code || code === 'CUPÓN') {
      this.toast.info('No hay código de cupón configurado. Cargalo en checkout.');
      void this.router.navigate(['/checkout']);
      return;
    }
    try {
      localStorage.setItem('ecom_pending_coupon', code);
    } catch {
      /* ignore */
    }
    this.toast.success(`Cupón ${code} listo para checkout`);
    void this.router.navigate(['/checkout']);
  }

  subscribeNewsletter(): void {
    const email = this.newsEmail.trim().toLowerCase();
    if (!email || !email.includes('@') || !email.includes('.')) {
      this.toast.error('Ingresá un email válido');
      return;
    }
    this.newsBusy = true;
    const localKey = 'ecom_newsletter_emails';
    try {
      const prev = JSON.parse(localStorage.getItem(localKey) || '[]') as string[];
      if (Array.isArray(prev) && prev.includes(email)) {
        this.newsBusy = false;
        this.toast.info('Este email ya está suscripto en este dispositivo.');
        return;
      }
    } catch {
      /* ignore */
    }

    this.api
      .enviarContacto({
        nombre: 'Newsletter',
        email,
        mensaje: `Suscripción a newsletter desde la tienda. Email: ${email}`,
      })
      .subscribe({
        next: (r) => {
          try {
            const prev = JSON.parse(localStorage.getItem(localKey) || '[]') as string[];
            const next = Array.from(new Set([...(Array.isArray(prev) ? prev : []), email]));
            localStorage.setItem(localKey, JSON.stringify(next));
          } catch {
            /* ignore */
          }
          this.newsBusy = false;
          this.newsEmail = '';
          this.toast.success(r?.mensaje || '¡Listo! Te avisaremos de novedades.');
        },
        error: (e) => {
          this.newsBusy = false;
          this.toast.error(e?.error?.error || e.message || 'No se pudo suscribir');
        },
      });
  }

  onSearchChange(value: string): void {
    this.search = value;
    if (this.syncingFromUrl) return;
    if (!this.listingMode() && value.trim()) this.browse = true;
    this.search$.next(value.trim());
  }

  /** Buscador del listado: mismo motor que el header (suggest + commit). */
  applyCatalogSearch(q: string): void {
    this.search = (q || '').trim();
    this.browse = true;
    this.load(1);
  }

  clearFilters(): void {
    this.search = '';
    this.marcaId = null;
    this.soloStock = false;
    this.grupoId = null;
    this.lineaId = null;
    this.tipoId = null;
    this.sort = 'nombre';
    this.browse = true;
    this.load(1);
  }

  private syncQueryParams(pageNum: number): void {
    if (this.syncingFromUrl) return;
    this.skipNextUrlLoad = true;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: this.search.trim() || null,
        marca: this.marcaId ?? null,
        grupo: this.grupoId ?? null,
        linea: this.lineaId ?? null,
        tipo: this.tipoId ?? null,
        soloStock: this.soloStock ? '1' : null,
        sort: this.sort !== 'nombre' ? this.sort : null,
        feed: this.feedMode,
        browse:
          this.browse &&
          !this.searchMode() &&
          this.grupoId == null &&
          this.lineaId == null &&
          this.tipoId == null &&
          !this.search.trim()
            ? '1'
            : null,
        pageSize: this.pageSize !== 20 ? this.pageSize : null,
        page: pageNum > 1 ? pageNum : null,
      },
      queryParamsHandling: this.searchMode() ? '' : 'merge',
      replaceUrl: true,
    });
  }

  selectGrupo(id: number | null): void {
    this.grupoId = id;
    this.browse = true;
    this.feedMode = null;
    this.load(1);
    queueMicrotask(() =>
      document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
    );
  }

  onCanalSelect(c: Canal): void {
    if (c === 'B2B' && !this.session.mayoristaAprobado()) {
      this.canal = 'B2C';
      this.session.setCanal('B2C');
      setCanal('B2C');
      return;
    }
    this.canal = c;
    this.session.setCanal(c);
    setCanal(c);
    this.load(1);
  }

  applyStockFilter(): void {
    const current = this.page();
    if (!current) {
      this.load(1);
      return;
    }
    const items = this.soloStock ? this.rawItems.filter((p) => p.stockOk) : this.rawItems;
    this.page.set({ ...current, items });
    this.syncQueryParams(current.page);
  }

  load(pageNum: number, syncUrl = true): void {
    this.feedMode = null;
    this.error.set(null);
    this.loading.set(true);
    if (syncUrl) this.syncQueryParams(pageNum);
    this.api
      .products({
        search: this.search || undefined,
        grupoId: this.grupoId ?? undefined,
        marcaId: this.marcaId ?? undefined,
        lineaId: this.lineaId ?? undefined,
        tipoId: this.tipoId ?? undefined,
        sort: this.sort,
        canal: this.canal,
        page: pageNum,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (r) => {
          this.rawItems = r.items;
          const items = this.soloStock ? r.items.filter((p) => p.stockOk) : r.items;
          this.page.set({ ...r, items });
          this.loading.set(false);
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set(e?.error?.error || e.message || 'Error de catálogo');
        },
      });
  }

  private loadCategoryRails(): void {
    const top = this.shopCollections().slice(0, 3);
    if (!top.length) {
      this.categoryRails.set([]);
      return;
    }
    forkJoin(
      top.map((collection) =>
        this.api
          .products({
            grupoId: collection.id,
            canal: this.canal,
            page: 1,
            pageSize: 8,
            sort: 'nuevo',
          })
          .pipe(
            map((r) => ({ collection, products: r.items })),
            catchError(() => of({ collection, products: [] as ProductListItem[] })),
          ),
      ),
    ).subscribe({
      next: (rails) => this.categoryRails.set(rails.filter((r) => r.products.length > 0)),
      error: () => this.categoryRails.set([]),
    });
  }

  private marcasLoaded = false;
  private ensureMarcas(): void {
    if (this.marcasLoaded) return;
    this.marcasLoaded = true;
    this.api.getMarcas().subscribe({
      next: (m) => this.marcas.set(m),
      error: () => this.marcas.set([]),
    });
  }

  private facetsLoaded = false;
  private ensureFacets(): void {
    if (this.facetsLoaded) return;
    this.facetsLoaded = true;
    this.api.getLineas().subscribe({
      next: (rows) => this.lineas.set(rows),
      error: () => this.lineas.set([]),
    });
    this.api.getTipos().subscribe({
      next: (rows) => this.tipos.set(rows),
      error: () => this.tipos.set([]),
    });
  }

  markBroken(id: number): void {
    this.brokenImg.update((m) => ({ ...m, [id]: true }));
  }

  quickAdd(ev: Event, p: ProductListItem): void {
    ev.preventDefault();
    ev.stopPropagation();
    if (!p.stockOk || this.addingId === p.id) return;
    this.addingId = p.id;
    this.api.addToCart(p.id, 1, undefined, getCanal()).subscribe({
      next: (cart) => {
        const n = cart.itemsCount ?? cart.items?.length ?? 0;
        this.session.setCartCount(n);
        this.session.setCartTotal(cart.montoTotal ?? 0);
        this.addingId = null;
        this.api.track(p.id, 'add_cart').subscribe({ error: () => undefined });
        this.miniCart.show(cart, p.id);
      },
      error: (e) => {
        this.addingId = null;
        this.toast.error(e?.error?.error || e.message || 'No se pudo agregar');
      },
    });
  }

  quickAddReco(ev: Event, x: Recomendacion): void {
    ev.preventDefault();
    ev.stopPropagation();
    const asProduct: ProductListItem = {
      id: x.id,
      sku: null,
      nombre: x.nombre,
      nombreCorto: x.nombre,
      marca: null,
      estadoEcommerce: 'activo',
      imagenPrincipalUrl: x.imagenUrl,
      precioDesde: x.precioDesde,
      embalajesCount: 0,
      canal: getCanal(),
      listaPrecio: null,
      stockLibre: null,
      stockOk: true,
      slugSeo: x.slugSeo,
    };
    this.quickAdd(ev, asProduct);
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  slotExternal(slot: MarketingSlot): boolean {
    const u = (slot.urlDestino || '').trim().toLowerCase();
    return u.startsWith('http://') || u.startsWith('https://');
  }

  slotLink(slot: MarketingSlot): string {
    const u = (slot.urlDestino || '/catalogo?browse=1').trim();
    if (this.slotExternal(slot)) return u;
    return u.startsWith('/') ? u : `/${u}`;
  }
}
