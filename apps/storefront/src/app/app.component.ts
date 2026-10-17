import { Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ApiService, Canal, CatalogFacet, CollectionItem, setCanal } from './core/api.service';
import { AuthModalService } from './core/auth-modal.service';
import { BrandingService } from './core/branding.service';
import { PersonalizacionService } from './core/personalizacion.service';
import { SessionService } from './core/session.service';
import { setTenantSlug, getTenantSlug } from './core/tenant.interceptor';
import { AuthModalComponent } from './shared/auth-modal.component';
import { DxIconComponent } from './shared/dx-icon.component';
import { SupportChatWidgetComponent } from './shared/support-chat-widget.component';
import { ToastHostComponent } from './shared/toast-host.component';
import { HeaderSearchComponent } from './shared/header-search.component';
import { MiniCartDrawerComponent } from './shared/mini-cart-drawer.component';
import { MiniCartService } from './core/mini-cart.service';
import { isValidLatLng, mapsPinUrl, osmEmbedUrl } from './core/map.util';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    CurrencyPipe,
    DxIconComponent,
    AuthModalComponent,
    SupportChatWidgetComponent,
    ToastHostComponent,
    HeaderSearchComponent,
    MiniCartDrawerComponent,
  ],
  template: `
    <a class="skip-link" href="#contenido">Saltar al contenido</a>

    @if (session.welcomeMessage(); as welcome) {
      <div class="welcome-banner" role="status" aria-live="polite">
        <div class="dx-container welcome-inner">
          <p><dx-icon name="star" [size]="18" /> {{ welcome }}</p>
          <button type="button" class="dismiss" (click)="session.dismissWelcome()" aria-label="Cerrar">
            <dx-icon name="close" [size]="16" />
          </button>
        </div>
      </div>
    }

    @if (session.mayoristaAprobado() && personalizacion.showBannerMayorista()) {
      <div class="mayorista-banner" role="status">
        <div class="dx-container mayorista-inner">
          <p>
            @if (session.canal() === 'B2B') {
              Sos mayorista · precios B2B activos
            } @else {
              Sos mayorista · estás comprando en modo minorista
            }
          </p>
          <a routerLink="/mi-cuenta">Mi cuenta</a>
        </div>
      </div>
    }

    <div class="util-bar" role="navigation" aria-label="Cuenta">
      <div class="dx-container util-inner">
        <div class="util-actions">
          @if (session.mayoristaAprobado()) {
            <label class="canal">
              <span class="sr-only">Canal de compra</span>
              <select
                class="dx-select canal-select"
                [ngModel]="session.canal()"
                (ngModelChange)="changeCanal($event)"
                aria-label="Canal minorista o mayorista"
              >
                <option value="B2C">Minorista</option>
                <option value="B2B">Mayorista</option>
              </select>
            </label>
          }
          @if (session.loggedIn()) {
            <span class="hello">Hola, <strong>{{ session.displayName() }}</strong></span>
            <a routerLink="/mi-cuenta"><dx-icon name="user" [size]="14" /> Mi cuenta</a>
            <a routerLink="/mis-pedidos"><dx-icon name="package" [size]="14" /> Pedidos</a>
            @if (personalizacion.widgets().seguidos) {
              <a routerLink="/seguidos"><dx-icon name="star" [size]="14" /> Seguidos</a>
            }
            <button type="button" class="linkish" (click)="logout()">Salir</button>
          } @else {
            <button type="button" class="util-link linkish" (click)="auth.showLogin()">
              <dx-icon name="login" [size]="14" /> Iniciar sesión
            </button>
            <button type="button" class="util-register linkish" (click)="auth.showRegister()">
              <dx-icon name="user-plus" [size]="14" /> Registrarse
            </button>
          }
        </div>
      </div>
    </div>

    <header class="site-header" role="banner">
      @if (brand.branding()?.bannerUrl; as banner) {
        <div class="brand-banner" [style.backgroundImage]="'url(' + brand.media(banner) + ')'" aria-hidden="true"></div>
      }
      <div class="dx-container header-row">
        <div class="brand">
          <button
            type="button"
            class="menu-btn dx-btn dx-btn--ghost"
            (click)="toggleMenu()"
            [attr.aria-expanded]="menuOpen()"
            aria-controls="nav-principal"
            aria-label="Abrir menú"
          >
            <dx-icon name="menu" [size]="20" />
            <span class="menu-label">Menú</span>
          </button>

          <a routerLink="/catalogo" class="logo" [attr.aria-label]="(brand.branding()?.nombreTienda || 'Tienda') + ' — inicio'">
            @if (brand.branding()?.logoUrl; as logo) {
              <img class="logo-img" [src]="brand.media(logo)" [alt]="brand.branding()?.nombreTienda || 'Logo'" />
            } @else {
              <span class="logo-mark" aria-hidden="true">{{ brand.branding()?.logoMark || 'CI' }}</span>
            }
            <span class="logo-text">
              <strong>{{ brandName() }}</strong>
              <small>{{ brand.branding()?.tagline || 'Comprá online' }}</small>
            </span>
          </a>
        </div>

        <app-header-search />

        <nav class="quick" aria-label="Accesos rápidos">
          @if (session.loggedIn()) {
            <a routerLink="/mi-cuenta" class="action" routerLinkActive="on" [title]="firstName()">
              <span class="user-chip" aria-hidden="true">{{ initials() }}</span>
              <span class="action-label">{{ firstName() }}</span>
            </a>
          }
          @if (personalizacion.widgets().favoritos) {
            <a routerLink="/favoritos" class="action" routerLinkActive="on" title="Favoritos">
              <span class="action-ico" aria-hidden="true"><dx-icon name="heart" [size]="20" /></span>
              <span class="action-label">Favoritos</span>
            </a>
          }
          <button
            type="button"
            class="action action--cart"
            [class.on]="isCartRoute()"
            title="Carrito"
            (click)="openMiniCart()"
          >
            <span class="action-ico" aria-hidden="true">
              <dx-icon name="cart" [size]="20" />
              @if (session.cartCount() > 0) {
                <span class="cart-badge">{{ session.cartCount() > 99 ? '99+' : session.cartCount() }}</span>
              }
            </span>
            <span class="action-label">
              Carrito
              @if (session.cartCount() > 0) {
                <small class="cart-total">{{ session.cartTotal() | currency: 'PYG':'symbol-narrow':'1.0-0' }}</small>
              }
            </span>
          </button>
        </nav>
      </div>

      <nav id="nav-principal" class="cat-nav" [class.open]="menuOpen()" aria-label="Secciones de la tienda">
        <div class="dx-container cat-row">
          @if (cmsStoreNavItems().length) {
            @for (it of cmsStoreNavItems(); track it.id) {
              @if (isInternalNav(it.url)) {
                <a [routerLink]="navPath(it.url)" [queryParams]="navQuery(it.url)" (click)="closeMenu()">
                  {{ it.label }}
                  @if (it.badgeText) {
                    <span class="nav-badge">{{ it.badgeText }}</span>
                  }
                </a>
              } @else {
                <a [href]="it.url || '#'" [attr.target]="it.target || null" (click)="closeMenu()">
                  {{ it.label }}
                  @if (it.badgeText) {
                    <span class="nav-badge">{{ it.badgeText }}</span>
                  }
                </a>
              }
            }
          } @else {
            <a
              routerLink="/catalogo"
              [queryParams]="{}"
              routerLinkActive="on"
              [routerLinkActiveOptions]="{ exact: true }"
              (click)="closeMenu()"
            >
              <dx-icon name="home" [size]="16" /> Inicio
            </a>
            <a routerLink="/catalogo" [queryParams]="{ browse: '1' }" (click)="closeMenu()">
              <dx-icon name="grid" [size]="16" /> Catálogo
            </a>
            <div class="mega" [class.open]="catsOpen()">
              <button
                type="button"
                class="mega-btn"
                [attr.aria-expanded]="catsOpen()"
                (click)="catsOpen.set(!catsOpen())"
              >
                <dx-icon name="tag" [size]="16" /> Categorías
                <dx-icon name="chevron-down" [size]="14" class="mega-caret" />
              </button>
              @if (catsOpen()) {
                <div class="mega-panel" role="menu">
                  @for (c of navCats(); track c.kind + '-' + c.id) {
                    <a
                      role="menuitem"
                      [routerLink]="['/catalogo']"
                      [queryParams]="c.kind === 'linea' ? { browse: '1', linea: c.id } : { grupo: c.id }"
                      (click)="closeCats()"
                    >
                      <span>{{ c.nombre }}</span>
                      @if (c.count != null) {
                        <small>{{ c.count }}</small>
                      }
                    </a>
                  } @empty {
                    <p class="mega-empty">Sin categorías aún</p>
                  }
                  <a class="mega-all" routerLink="/catalogo" [queryParams]="{ browse: '1' }" (click)="closeCats()">
                    Ver catálogo completo
                  </a>
                </div>
              }
            </div>
            <a href="/catalogo#ofertas" (click)="goHash('ofertas', $event)">
              <dx-icon name="percent" [size]="16" /> Ofertas
            </a>
            <a routerLink="/sucursales" (click)="closeMenu()">
              <dx-icon name="map-pin" [size]="16" /> Sucursales
            </a>
            @if (personalizacion.widgets().favoritos) {
              <a class="nav-account" routerLink="/favoritos" (click)="closeMenu()">
                <dx-icon name="heart" [size]="16" /> Mis listas
              </a>
            }
            @if (personalizacion.widgets().seguidos) {
              <a class="nav-account" routerLink="/seguidos" (click)="closeMenu()">
                <dx-icon name="star" [size]="16" /> Seguidos
              </a>
            }
            <a class="nav-account" routerLink="/mi-cuenta" (click)="closeMenu()">
              <dx-icon name="user" [size]="16" /> Mi cuenta
            </a>
            <a routerLink="/mis-pedidos" routerLinkActive="on" (click)="closeMenu()">
              <dx-icon name="package" [size]="16" /> Mis pedidos
            </a>
          }
        </div>
      </nav>
      @if (menuOpen()) {
        <button type="button" class="menu-backdrop" aria-label="Cerrar menú" (click)="closeMenu()"></button>
      }
    </header>

    <main id="contenido" class="site-main" tabindex="-1">
      <router-outlet />
    </main>

    <app-mini-cart-drawer />
    <dx-auth-modal />

    <footer class="site-footer" role="contentinfo">
      <div class="dx-container footer-grid">
        <div class="foot-brand-col">
          <div class="foot-brand-row">
            @if (brand.branding()?.logoUrl; as logo) {
              <img class="foot-logo" [src]="brand.media(logo)" [alt]="brand.branding()?.nombreTienda || 'Logo'" />
            }
            <strong class="foot-brand">{{ brand.branding()?.nombreTienda || 'Casa Imperial' }}</strong>
          </div>
          <p>
            {{
              brand.branding()?.footerDescripcion ||
                brand.branding()?.tagline ||
                'Experiencia de compra clara: stock, precios y entrega en Paraguay.'
            }}
          </p>
          @if (hasSocial()) {
            <div class="social" aria-label="Redes sociales">
              @if (personalizacion.widgets().facebook && brand.branding()?.facebook; as u) {
                <a [href]="socialHref('facebook', u)" target="_blank" rel="noopener" title="Facebook" aria-label="Facebook">
                  <span class="soc fb"><dx-icon name="facebook" [size]="18" /></span>
                </a>
              }
              @if (personalizacion.widgets().instagram && brand.branding()?.instagram; as u) {
                <a [href]="socialHref('instagram', u)" target="_blank" rel="noopener" title="Instagram" aria-label="Instagram">
                  <span class="soc ig"><dx-icon name="instagram" [size]="18" /></span>
                </a>
              }
              @if (personalizacion.widgets().whatsapp && brand.branding()?.whatsapp; as u) {
                <a [href]="socialHref('whatsapp', u)" target="_blank" rel="noopener" title="WhatsApp" aria-label="WhatsApp">
                  <span class="soc wa"><dx-icon name="whatsapp" [size]="18" /></span>
                </a>
              }
              @if (personalizacion.widgets().youtube && brand.branding()?.youtube; as u) {
                <a [href]="socialHref('youtube', u)" target="_blank" rel="noopener" title="YouTube" aria-label="YouTube">
                  <span class="soc yt"><dx-icon name="youtube" [size]="18" /></span>
                </a>
              }
              @if (personalizacion.widgets().twitter && brand.branding()?.twitter; as u) {
                <a [href]="socialHref('twitter', u)" target="_blank" rel="noopener" title="X" aria-label="X / Twitter">
                  <span class="soc tw"><dx-icon name="twitter" [size]="16" /></span>
                </a>
              }
              @if (personalizacion.widgets().tiktok && brand.branding()?.tiktok; as u) {
                <a [href]="socialHref('tiktok', u)" target="_blank" rel="noopener" title="TikTok" aria-label="TikTok">
                  <span class="soc tt"><dx-icon name="tiktok" [size]="18" /></span>
                </a>
              }
            </div>
          }
        </div>

        <div>
          <h2>Comprar</h2>
          <ul>
            <li><a routerLink="/catalogo">Catálogo</a></li>
            <li><button type="button" class="foot-link" (click)="goHash('ofertas', $event)">Ofertas</button></li>
            <li><a routerLink="/sucursales">Sucursales</a></li>
            @if (personalizacion.widgets().favoritos) {
              <li><a routerLink="/favoritos">Favoritos</a></li>
            }
            @if (personalizacion.widgets().seguidos) {
              <li><a routerLink="/seguidos">Seguidos</a></li>
            }
          </ul>
        </div>

        <div>
          <h2>Ayuda</h2>
          <ul>
            <li><a routerLink="/como-comprar">Cómo comprar</a></li>
            <li><a routerLink="/faq">Preguntas frecuentes</a></li>
            <li><a routerLink="/terminos">Términos y condiciones</a></li>
            <li><a routerLink="/contacto">Contacto</a></li>
            <li><a routerLink="/sucursales">Sucursales</a></li>
          </ul>
        </div>

        <div>
          <h2>Cuenta</h2>
          <ul>
            @if (session.loggedIn()) {
              <li><a routerLink="/mi-cuenta">Mi cuenta</a></li>
              <li><a routerLink="/mis-pedidos">Mis pedidos</a></li>
              <li><a routerLink="/devoluciones">Devoluciones</a></li>
              <li><button type="button" class="foot-link" (click)="logout()">Salir</button></li>
            } @else {
              <li><button type="button" class="foot-link" (click)="auth.showLogin()">Iniciar sesión</button></li>
              <li><button type="button" class="foot-link" (click)="auth.showRegister()">Registrarse</button></li>
              <li><a routerLink="/mi-cuenta">Mi cuenta</a></li>
            }
          </ul>
          @if (paymentLabels().length) {
            <h2 class="pay-title">Formas de pago y entrega</h2>
            <ul class="pay-list">
              @for (p of paymentLabels(); track p) {
                <li>{{ p }}</li>
              }
            </ul>
          }
        </div>

        <div class="foot-contact">
          <h2>Contacto</h2>
          <ul class="contact-list">
            @if (brand.branding()?.direccionLocal; as dir) {
              <li>
                <dx-icon name="store" [size]="14" />
                <span>
                  {{ dir }}
                  @if (brand.branding()?.ciudadLocal; as ciu) {
                    <br /><span class="mute">{{ ciu }}</span>
                  }
                </span>
              </li>
            }
            @if (brand.branding()?.telefonoContacto; as tel) {
              <li>
                <dx-icon name="user" [size]="14" />
                <a [href]="'tel:' + tel">{{ tel }}</a>
              </li>
            }
            @if (brand.branding()?.emailContacto; as mail) {
              <li>
                <dx-icon name="tag" [size]="14" />
                <a [href]="'mailto:' + mail">{{ mail }}</a>
              </li>
            }
            @if (brand.branding()?.horarioAtencion; as hor) {
              <li>
                <dx-icon name="shield" [size]="14" />
                <span>{{ hor }}</span>
              </li>
            }
            @if (!hasContact()) {
              <li class="mute">Configurá dirección y contacto en Diseño de tienda (admin).</li>
            }
          </ul>
          @if (footerMapUrl(); as mapSrc) {
            <div class="foot-map">
              <iframe class="map-iframe" [src]="mapSrc" title="Ubicación del local" loading="lazy" referrerpolicy="no-referrer-when-downgrade"></iframe>
              @if (googleMapsUrl(); as g) {
                <a class="map-link" [href]="g" target="_blank" rel="noopener">
                  <dx-icon name="map-pin" [size]="14" /> Cómo llegar
                </a>
              }
            </div>
          } @else {
            @if (googleMapsUrl(); as g) {
              <a class="map-link map-link--solo" [href]="g" target="_blank" rel="noopener">
                <dx-icon name="map-pin" [size]="14" /> Ver ubicación
              </a>
            }
          }
        </div>
      </div>
      <p class="copy">
        © {{ year }} {{ brand.branding()?.nombreTienda || 'Casa Imperial' }}
        · Desarrollado por
        @if (brand.branding()?.creditoDesarrolladoPorUrl; as credUrl) {
          <a [href]="credUrl" target="_blank" rel="noopener">{{ brand.branding()?.creditoDesarrolladoPor || 'Datamex' }}</a>
        } @else {
          {{ brand.branding()?.creditoDesarrolladoPor || 'Datamex' }}
        }
      </p>
    </footer>

    <app-toast-host />
    @if (personalizacion.widgets().chat) {
      <app-support-chat-widget />
    }
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
      }
      .welcome-banner {
        background: linear-gradient(90deg, #1b5e20, #2e7d32);
        color: #fff;
        z-index: 70;
        animation: slideDown 0.35s ease;
      }
      .welcome-inner {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        padding: 0.75rem 0;
      }
      .welcome-inner p {
        margin: 0;
        font-weight: 700;
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
      }
      .dismiss {
        background: rgba(255, 255, 255, 0.2);
        border: none;
        color: #fff;
        width: 36px;
        height: 36px;
        border-radius: 8px;
        cursor: pointer;
        display: grid;
        place-items: center;
      }
      .mayorista-banner {
        background: #fff8e1;
        border-bottom: 1px solid #ffe082;
        color: #5d4037;
        font-size: 0.88rem;
      }
      .mayorista-inner {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        padding: 0.45rem 0;
        flex-wrap: wrap;
      }
      .mayorista-inner p {
        margin: 0;
        font-weight: 700;
      }
      .mayorista-inner a {
        color: var(--dx-red);
        font-weight: 700;
        text-decoration: none;
      }
      .mayorista-inner a:hover {
        text-decoration: underline;
      }
      .util-bar {
        background: #f7f7f7;
        color: var(--dx-ink-soft);
        font-size: 0.8rem;
        border-bottom: 1px solid var(--dx-line);
      }
      .util-inner {
        display: flex;
        justify-content: flex-end;
        align-items: center;
        gap: 0.75rem;
        padding: 0.4rem 0;
        min-height: 2.25rem;
      }
      .util-actions {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 0.85rem;
        flex-wrap: wrap;
      }
      .hello {
        color: var(--dx-ink);
      }
      .util-link,
      .util-actions a,
      .linkish {
        color: var(--dx-ink-soft);
        text-decoration: none;
        background: none;
        border: none;
        font: inherit;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        padding: 0;
      }
      .util-link:hover,
      .util-actions a:hover,
      .linkish:hover {
        color: var(--dx-red);
      }
      .util-register {
        background: var(--dx-red) !important;
        color: #fff !important;
        padding: 0.28rem 0.75rem !important;
        border-radius: 999px;
        font-weight: 700;
        gap: 0.35rem;
      }
      .util-register:hover {
        filter: brightness(0.95);
        color: #fff !important;
      }
      .foot-link {
        background: none;
        border: none;
        color: #ccc;
        font: inherit;
        cursor: pointer;
        padding: 0;
      }
      .foot-link:hover {
        color: #fff;
      }
      button.action {
        font: inherit;
        cursor: pointer;
        background: transparent;
      }
      .canal-select {
        min-height: 32px;
        padding: 0.2rem 0.5rem;
        font-size: 0.8rem;
        background: #fff;
        color: var(--dx-ink);
        border-color: var(--dx-line);
        width: auto;
      }
      .site-header {
        position: sticky;
        top: 0;
        z-index: 50;
        background: rgba(255, 255, 255, 0.98);
        backdrop-filter: blur(12px);
        border-bottom: 1px solid var(--dx-line);
        overflow: visible;
      }
      .brand-banner {
        height: clamp(44px, 7vw, 80px);
        background-size: cover;
        background-position: center;
      }
      .logo-img {
        height: 40px;
        width: auto;
        max-width: 148px;
        object-fit: contain;
      }
      .social {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        margin-top: 0.75rem;
      }
      .social a {
        color: #fff !important;
        text-decoration: none;
        line-height: 0;
      }
      .social a:hover .soc {
        transform: translateY(-2px);
        filter: brightness(1.08);
      }
      .header-row {
        display: grid;
        grid-template-columns: minmax(0, auto) minmax(0, 1fr);
        grid-template-areas:
          'brand actions'
          'search search';
        align-items: center;
        column-gap: clamp(0.5rem, 2vw, 2rem);
        row-gap: 0.75rem;
        padding: clamp(0.7rem, 1.6vw, 1.15rem) 0;
        min-width: 0;
      }
      .menu-btn {
        justify-self: start;
        min-height: 44px;
        padding: 0.45rem 0.75rem;
        gap: 0.4rem;
      }
      .brand {
        grid-area: brand;
        display: flex;
        align-items: center;
        gap: 0.55rem;
        min-width: 0;
      }
      .logo {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        text-decoration: none;
        color: var(--dx-ink);
        min-width: 0;
      }
      @media (min-width: 1100px) {
        .header-row {
          grid-template-columns: minmax(0, auto) minmax(0, 1fr) minmax(0, auto);
          grid-template-areas: 'brand search actions';
          column-gap: clamp(1rem, 2vw, 2rem);
        }
        .menu-btn {
          display: none;
        }
        .action-label {
          display: block !important;
        }
      }
      .menu-label {
        display: none;
      }
      @media (min-width: 480px) and (max-width: 1099px) {
        .menu-label {
          display: inline;
        }
      }
      .logo-mark {
        width: 44px;
        height: 44px;
        border-radius: 12px;
        background: linear-gradient(145deg, var(--dx-red), var(--dx-red-dark));
        color: #fff;
        display: grid;
        place-items: center;
        font-family: var(--dx-display);
        font-weight: 800;
        font-size: 1.15rem;
        flex-shrink: 0;
        box-shadow: 0 4px 12px rgba(198, 40, 40, 0.22);
      }
      .logo-text {
        display: none;
        flex-direction: column;
        line-height: 1.05;
        min-width: 0;
      }
      .logo-text strong,
      .logo-text small {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        max-width: min(28vw, 220px);
      }
      @media (min-width: 640px) {
        .logo-text {
          display: flex;
        }
      }
      .logo-text strong {
        font-family: var(--dx-display);
        font-size: 1.12rem;
        letter-spacing: 0.04em;
      }
      .logo-text small {
        color: var(--dx-mute);
        font-size: 0.68rem;
        text-transform: uppercase;
        letter-spacing: 0.12em;
      }
      .tenant-chip {
        display: none;
        margin-left: 0.4rem;
        padding: 0.18rem 0.55rem;
        border-radius: 999px;
        border: 1px solid var(--dx-line);
        background: #f8fafc;
        font-size: 0.68rem;
        font-weight: 700;
        color: #64748b;
      }
      @media (min-width: 900px) {
        .tenant-chip { display: inline-flex; align-items: center; }
      }
      .search,
      app-header-search {
        grid-area: search;
        display: block;
        min-width: 0;
        width: 100%;
      }
      .search-field {
        flex: 1;
        position: relative;
        display: flex;
        align-items: center;
        min-width: 0;
      }
      .search-field dx-icon {
        position: absolute;
        left: 0.9rem;
        color: var(--dx-mute);
        pointer-events: none;
        z-index: 1;
      }
      .search-field .dx-input {
        padding-left: 2.65rem;
        min-height: 48px;
        border-radius: 12px;
        font-size: 0.98rem;
      }
      .search-btn {
        flex-shrink: 0;
        min-height: 48px;
        padding: 0 1.1rem;
        gap: 0.4rem;
      }
      @media (max-width: 520px) {
        .search-btn span {
          display: none;
        }
        .search-btn {
          padding: 0 0.9rem;
        }
      }
      .quick {
        grid-area: actions;
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 0.5rem;
        min-width: 0;
      }
      .action {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.3rem;
        width: auto;
        min-width: 48px;
        max-width: 72px;
        padding: 0.35rem 0.25rem;
        text-decoration: none;
        color: var(--dx-ink-soft);
        border-radius: 12px;
        border: 1px solid transparent;
        background: transparent;
        transition:
          background 0.18s ease,
          border-color 0.18s ease,
          color 0.18s ease,
          box-shadow 0.18s ease;
      }
      .action-ico {
        width: 42px;
        height: 42px;
        border-radius: 12px;
        background: #f3f3f3;
        color: var(--dx-ink);
        display: grid;
        place-items: center;
        flex-shrink: 0;
        position: relative;
        transition: background 0.18s ease, color 0.18s ease, box-shadow 0.18s ease;
      }
      .cart-badge {
        position: absolute;
        top: -4px;
        right: -4px;
        min-width: 18px;
        height: 18px;
        padding: 0 4px;
        border-radius: 999px;
        background: var(--dx-red);
        color: #fff;
        font-size: 0.65rem;
        font-weight: 800;
        display: grid;
        place-items: center;
        line-height: 1;
        box-shadow: 0 0 0 2px #fff;
      }
      .user-chip {
        width: 42px;
        height: 42px;
        border-radius: 12px;
        background: var(--dx-red);
        color: #fff;
        font-size: 0.75rem;
        font-weight: 800;
        display: grid;
        place-items: center;
        flex-shrink: 0;
      }
      .action-label {
        display: none;
        font-size: 0.7rem;
        font-weight: 700;
        line-height: 1.1;
        text-align: center;
        white-space: nowrap;
      }
      .cart-total {
        display: block;
        font-size: 0.62rem;
        font-weight: 800;
        color: var(--dx-red);
        margin-top: 0.1rem;
      }
      .pay-title {
        margin-top: 1rem !important;
      }
      .pay-list li {
        color: #aaa;
        font-size: 0.88rem;
      }
      .action:hover,
      .action:focus-visible {
        color: var(--dx-red);
        background: #fff;
        border-color: #ffc9c9;
        outline: none;
        box-shadow: 0 4px 14px rgba(198, 40, 40, 0.1);
      }
      .action:hover .action-ico,
      .action:focus-visible .action-ico,
      .action.on .action-ico {
        background: var(--dx-red-soft);
        color: var(--dx-red);
        box-shadow: inset 0 0 0 1px #f0b4b4;
      }
      .action.on {
        color: var(--dx-red);
        background: var(--dx-red-soft);
        border-color: #f0b4b4;
      }
      @media (max-width: 420px) {
        .quick {
          gap: 0.2rem;
        }
        .action {
          width: 44px;
          min-width: 44px;
          max-width: 48px;
          padding: 0.15rem;
        }
        .action-ico,
        .user-chip {
          width: 38px;
          height: 38px;
        }
        .logo-text {
          display: none !important;
        }
      }
      .cat-nav {
        border-top: 1px solid var(--dx-line);
        background: #fafafa;
        display: none;
        overflow: hidden;
        position: relative;
        z-index: 45;
      }
      .cat-nav.open {
        display: block;
      }
      .menu-backdrop {
        position: fixed;
        inset: 0;
        z-index: 40;
        border: 0;
        background: rgba(15, 23, 42, 0.4);
        cursor: pointer;
      }
      @media (min-width: 1100px) {
        .cat-nav {
          display: block;
          overflow: visible;
        }
        .menu-backdrop {
          display: none;
        }
      }
      .cat-row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: center;
        gap: 0.35rem 0.4rem;
        padding: 0.55rem 0;
        overflow: visible;
        min-width: 0;
        max-width: 100%;
      }
      @media (min-width: 1100px) {
        .cat-row {
          flex-wrap: nowrap;
          gap: 0.2rem 0.35rem;
          justify-content: center;
        }
        .cat-row > a,
        .mega-btn {
          padding: 0.5rem 0.9rem;
          font-size: 0.9rem;
        }
        .nav-account {
          display: none !important;
        }
      }
      @media (max-width: 1099px) {
        .cat-row {
          flex-direction: column;
          align-items: stretch;
          gap: 0.25rem;
        }
      }
      .cat-row > a {
        flex: 0 0 auto;
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        padding: 0.65rem 1.05rem;
        text-decoration: none;
        color: var(--dx-ink);
        font-weight: 650;
        font-size: 0.92rem;
        border-radius: 999px;
        white-space: nowrap;
        border: 1px solid transparent;
        transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
      }
      @media (max-width: 1099px) {
        .cat-row > a,
        .mega-btn {
          width: 100%;
          justify-content: flex-start;
          border-radius: 10px;
        }
      }
      .cat-row > a dx-icon {
        color: var(--dx-red);
      }
      .nav-badge {
        display: inline-block;
        margin-left: 0.25rem;
        padding: 0.1rem 0.4rem;
        border-radius: 999px;
        background: #ffe8e8;
        color: var(--dx-red);
        font-size: 0.68rem;
        font-weight: 700;
      }
      .cat-row > a:hover,
      .cat-row > a.on {
        color: var(--dx-red);
        background: #fff;
        border-color: #f0b4b4;
        box-shadow: 0 2px 8px rgba(198, 40, 40, 0.08);
      }
      .mega {
        position: relative;
        flex: 0 0 auto;
        z-index: 60;
      }
      @media (max-width: 1099px) {
        .mega {
          width: 100%;
        }
      }
      .mega-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        padding: 0.65rem 1.05rem;
        border-radius: 999px;
        border: 1px solid transparent;
        background: transparent;
        font: inherit;
        font-weight: 650;
        font-size: 0.92rem;
        color: var(--dx-ink);
        cursor: pointer;
        white-space: nowrap;
      }
      .mega-btn dx-icon {
        color: var(--dx-red);
      }
      .mega.open .mega-btn,
      .mega-btn:hover {
        color: var(--dx-red);
        background: #fff;
        border-color: #f0b4b4;
      }
      .mega-caret {
        transition: transform 0.15s ease;
      }
      .mega.open .mega-caret {
        transform: rotate(180deg);
      }
      .mega-panel {
        position: absolute;
        top: calc(100% + 0.4rem);
        left: 0;
        z-index: 70;
        min-width: min(92vw, 340px);
        max-height: min(70vh, 440px);
        overflow: auto;
        display: grid;
        gap: 0.15rem;
        padding: 0.55rem;
        border-radius: 12px;
        border: 1px solid var(--dx-line);
        background: #fff;
        box-shadow: 0 16px 40px rgba(15, 23, 42, 0.16);
      }
      @media (max-width: 1099px) {
        .mega-panel {
          position: static;
          min-width: 0;
          max-height: none;
          margin-top: 0.25rem;
          box-shadow: none;
          background: #fff;
        }
      }
      .mega-panel a {
        display: flex !important;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        border-radius: 8px !important;
        padding: 0.55rem 0.7rem !important;
        white-space: normal !important;
        box-shadow: none !important;
        width: auto !important;
      }
      .mega-panel small {
        color: var(--dx-mute);
        font-weight: 600;
      }
      .mega-all {
        margin-top: 0.25rem;
        border-top: 1px solid var(--dx-line) !important;
        color: var(--dx-red) !important;
        font-weight: 800 !important;
      }
      .mega-empty {
        margin: 0;
        padding: 0.75rem;
        color: var(--dx-mute);
        font-size: 0.88rem;
      }
      .site-main {
        flex: 1;
        width: var(--dx-max);
        max-width: var(--dx-max);
        margin: clamp(1rem, 2vw, 1.5rem) auto clamp(2rem, 4vw, 3rem);
        padding-inline: 0;
        box-sizing: border-box;
        outline: none;
        min-width: 0;
      }
      .site-footer {
        margin-top: auto;
        background: #1a1a1a;
        color: #ddd;
        padding: clamp(2rem, 4vw, 2.75rem) 0 1.25rem;
      }
      .footer-grid {
        display: grid;
        gap: 1.75rem;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 200px), 1fr));
      }
      .foot-brand {
        font-family: var(--dx-display);
        font-size: 1.2rem;
        color: #fff;
      }
      .foot-brand-row {
        display: flex;
        align-items: center;
        gap: 0.65rem;
      }
      .foot-logo {
        width: 40px;
        height: 40px;
        object-fit: contain;
        border-radius: 8px;
        background: #fff;
        flex-shrink: 0;
      }
      .foot-brand-col p {
        margin: 0.5rem 0 0.85rem;
        color: #bbb;
        line-height: 1.45;
        max-width: 36ch;
      }
      .social {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
      }
      .soc {
        display: inline-grid;
        place-items: center;
        width: 36px;
        height: 36px;
        padding: 0;
        border-radius: 10px;
        background: #2a2a2a;
        color: #fff;
        transition: transform 0.15s ease, filter 0.15s ease;
      }
      .soc.fb { background: #1877f2; }
      .soc.ig { background: linear-gradient(45deg, #f58529, #dd2a7b, #8134af); }
      .soc.wa { background: #25d366; color: #fff; }
      .soc.yt { background: #ff0000; }
      .soc.tw { background: #000; border: 1px solid #444; }
      .soc.tt { background: #010101; border: 1px solid #444; }
      .site-footer h2 {
        margin: 0 0 0.5rem;
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: #fff;
      }
      .site-footer ul {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .site-footer li {
        margin: 0.4rem 0;
      }
      .site-footer a {
        color: #ccc;
        text-decoration: none;
      }
      .site-footer a:hover {
        color: #fff;
      }
      .foot-link {
        background: none;
        border: 0;
        padding: 0;
        color: #ccc;
        font: inherit;
        cursor: pointer;
        text-align: left;
      }
      .foot-link:hover {
        color: #fff;
      }
      .contact-list li {
        display: flex;
        gap: 0.5rem;
        align-items: flex-start;
        color: #ccc;
      }
      .contact-list dx-icon {
        margin-top: 0.15rem;
        color: #888;
        flex-shrink: 0;
      }
      .mute {
        color: #888;
      }
      .foot-map {
        margin-top: 0.85rem;
        border-radius: 12px;
        overflow: hidden;
        border: 1px solid #333;
        background: #111;
      }
      .foot-map iframe,
      .foot-map .map-iframe {
        display: block;
        width: 100%;
        height: 160px;
        border: 0;
      }
      .map-link {
        display: block;
        text-align: center;
        padding: 0.45rem;
        font-size: 0.78rem;
        font-weight: 700;
        background: #222;
        color: #fff !important;
      }
      .map-link--solo {
        margin-top: 0.75rem;
        border-radius: 10px;
        border: 1px solid #333;
      }
      @media (max-width: 600px) {
        .foot-map .map-iframe {
          display: none;
        }
      }
      .copy {
        width: var(--dx-max);
        max-width: var(--dx-max);
        margin: 1.5rem auto 0;
        padding: 1rem 0 0;
        box-sizing: border-box;
        border-top: 1px solid #333;
        font-size: 0.8rem;
        color: #999;
      }
      @keyframes slideDown {
        from {
          transform: translateY(-100%);
          opacity: 0;
        }
        to {
          transform: none;
          opacity: 1;
        }
      }
    `,
  ],
})
export class AppComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly session = inject(SessionService);
  readonly auth = inject(AuthModalService);
  readonly brand = inject(BrandingService);
  readonly personalizacion = inject(PersonalizacionService);
  private readonly miniCart = inject(MiniCartService);

  readonly menuOpen = signal(false);
  readonly catsOpen = signal(false);
  readonly navCollections = signal<CollectionItem[]>([]);
  readonly navLineas = signal<CatalogFacet[]>([]);
  /** Menú header desde CMS admin; se usa solo si tiene cobertura suficiente. */
  readonly cmsHeaderItems = signal<
    { id: number; label: string; url: string | null; target: string | null; badgeText: string | null }[]
  >([]);
  /** Header CMS sin Devoluciones: esa pantalla vive como pestaña de Mis pedidos. */
  readonly cmsStoreNavItems = computed(() =>
    this.cmsHeaderItems().filter((it) => !this.isDevolucionesNav(it.url)),
  );
  readonly year = new Date().getFullYear();

  ngOnInit(): void {
    // Admin “Abrir tienda” manda ?tenant=slug — aplicarlo antes de cargar branding/catálogo.
    // Legacy `demo` se migra a Casa Imperial dentro de setTenantSlug/getTenantSlug.
    const tree = this.router.parseUrl(this.router.url);
    const tenantQ = tree.queryParams['tenant'];
    if (tenantQ && typeof tenantQ === 'string' && tenantQ.trim()) {
      setTenantSlug(tenantQ.trim());
    } else {
      // Fuerza migración de localStorage `demo` → casa-imperial.
      setTenantSlug(getTenantSlug());
    }

    this.brand.load();
    this.personalizacion.load();
    this.personalizacion.watchFocusReload();
    queueMicrotask(() => {
      if (window.parent !== window) {
        window.parent.postMessage({ type: 'datamex:personalizacion-preview-ready' }, '*');
      }
    });
    this.refreshCartCount();
    this.loadCmsNav();
    this.api.collections().subscribe({
      next: (c) => this.navCollections.set(c),
      error: () => this.navCollections.set([]),
    });
    this.api.getLineas().subscribe({
      next: (rows) => this.navLineas.set(rows),
      error: () => this.navLineas.set([]),
    });
    if (this.session.loggedIn()) {
      this.api.me().subscribe({
        next: (p) => this.session.applyProfileCanal(p.estadoMayorista, p.canalPreferido),
        error: () => undefined,
      });
    }
    this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)).subscribe(() => {
      this.refreshCartCount();
      this.closeMenu();
    });

    const path = this.router.url.split('?')[0];
    if (path.endsWith('/login')) {
      void this.router.navigate(['/catalogo']).then(() => this.auth.showLogin());
    } else if (path.endsWith('/registro')) {
      void this.router.navigate(['/catalogo']).then(() => this.auth.showRegister());
    } else if (path.endsWith('/recuperar')) {
      const t = this.router.parseUrl(this.router.url);
      const c = t.queryParams['c'] || '';
      const k = t.queryParams['k'] || '';
      const tenant = t.queryParams['tenant'] || undefined;
      void this.router.navigate(['/catalogo']).then(() => this.auth.showReset(c, k, tenant));
    }
  }

  @HostListener('window:message', ['$event'])
  onPreviewMessage(event: MessageEvent<unknown>): void {
    if (window.parent === window || event.source !== window.parent) return;
    const data = event.data;
    if (!data || typeof data !== 'object') return;
    const message = data as { type?: unknown; clave?: unknown; valorJson?: unknown };
    if (message.type !== 'datamex:personalizacion-preview' || typeof message.clave !== 'string') return;
    this.personalizacion.setPreview(
      message.clave,
      typeof message.valorJson === 'string' ? message.valorJson : null,
    );
    window.parent.postMessage(
      { type: 'datamex:personalizacion-preview-applied', clave: message.clave },
      '*',
    );
  }

  brandName(): string {
    const n = this.brand.branding()?.nombreTienda?.trim();
    return n || 'Casa Imperial';
  }

  hasSocial(): boolean {
    const b = this.brand.branding();
    const w = this.personalizacion.widgets();
    return !!(
      (w.facebook && b?.facebook) ||
      (w.instagram && b?.instagram) ||
      (w.whatsapp && b?.whatsapp) ||
      (w.youtube && b?.youtube) ||
      (w.twitter && b?.twitter) ||
      (w.tiktok && b?.tiktok)
    );
  }

  socialHref(kind: string, raw: string): string {
    const v = (raw || '').trim();
    if (!v) return '#';
    if (/^https?:\/\//i.test(v)) return v;
    if (kind === 'whatsapp') {
      const digits = v.replace(/\D/g, '');
      return digits ? `https://wa.me/${digits}` : '#';
    }
    if (v.startsWith('@')) {
      const user = v.slice(1);
      if (kind === 'instagram') return `https://www.instagram.com/${user}/`;
      if (kind === 'twitter') return `https://twitter.com/${user}`;
      if (kind === 'tiktok') return `https://www.tiktok.com/@${user}`;
    }
    return v.startsWith('/') ? v : `https://${v}`;
  }

  hasContact(): boolean {
    const b = this.brand.branding();
    return !!(b?.direccionLocal || b?.telefonoContacto || b?.emailContacto || b?.horarioAtencion);
  }

  footerMapUrl(): SafeResourceUrl | null {
    const b = this.brand.branding();
    if (!b || b.mostrarMapa === false) return null;
    const lat = Number(b.latitud);
    const lng = Number(b.longitud);
    if (!isValidLatLng(lat, lng)) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(osmEmbedUrl(lat, lng));
  }

  googleMapsUrl(): string | null {
    const b = this.brand.branding();
    const lat = Number(b?.latitud);
    const lng = Number(b?.longitud);
    if (!isValidLatLng(lat, lng)) return null;
    const label = b?.nombreTienda || b?.direccionLocal || undefined;
    return mapsPinUrl(lat, lng, label);
  }

  firstName(): string {
    return this.session.displayName().split(/\s+/)[0] || 'Cuenta';
  }

  initials(): string {
    const parts = this.session.displayName().split(/\s+/).filter(Boolean);
    return ((parts[0]?.[0] || 'U') + (parts[1]?.[0] || '')).toUpperCase();
  }

  changeCanal(c: Canal): void {
    this.session.setCanal(c);
    setCanal(c);
    this.refreshCartCount();
    // Remonta la ruta actual para refrescar precios/catálogo/carrito con el canal nuevo.
    const url = this.router.url;
    void this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
      void this.router.navigateByUrl(url);
    });
  }

  refreshCartCount(): void {
    this.api.getCart(this.session.canal()).subscribe({
      next: (c) => {
        this.session.setCartCount(c.itemsCount ?? c.items?.length ?? 0);
        this.session.setCartTotal(c.montoTotal ?? 0);
      },
      error: () => {
        this.session.setCartCount(0);
        this.session.setCartTotal(0);
      },
    });
  }

  openMiniCart(): void {
    this.api.getCart(this.session.canal()).subscribe({
      next: (c) => {
        this.session.setCartCount(c.itemsCount ?? c.items?.length ?? 0);
        this.session.setCartTotal(c.montoTotal ?? 0);
        this.miniCart.show(c);
      },
      error: () => {
        this.session.setCartCount(0);
        this.session.setCartTotal(0);
        void this.router.navigate(['/carrito']);
      },
    });
  }

  isCartRoute(): boolean {
    return this.router.url.split('?')[0].startsWith('/carrito');
  }

  private loadCmsNav(): void {
    this.api.getHomeContent().subscribe({
      next: (h) => {
        const menus = h.menus ?? [];
        const header =
          menus.find((m) => (m.code || '').toLowerCase() === 'main_header') ||
          menus.find((m) => (m.menuType || '').toLowerCase().includes('header')) ||
          menus[0];
        const items = header?.items ?? [];
        // Seed mínimo (3 ítems) no reemplaza el menú completo; admin debe publicar cobertura útil.
        this.cmsHeaderItems.set(items.length >= 5 ? items : []);
      },
      error: () => this.cmsHeaderItems.set([]),
    });
  }

  isInternalNav(url: string | null | undefined): boolean {
    const u = (url || '').trim();
    return !!u && !/^https?:\/\//i.test(u) && !u.startsWith('mailto:') && !u.startsWith('tel:');
  }

  isDevolucionesNav(url: string | null | undefined): boolean {
    const p = this.navPath(url).toLowerCase();
    return p === '/devoluciones' || p.startsWith('/devoluciones/');
  }

  navPath(url: string | null | undefined): string {
    const raw = (url || '/catalogo').trim();
    const path = raw.split('?')[0].split('#')[0];
    return path.startsWith('/') ? path : `/${path}`;
  }

  navQuery(url: string | null | undefined): Record<string, string> | undefined {
    const raw = (url || '').trim();
    const q = raw.includes('?') ? raw.split('?')[1]?.split('#')[0] : '';
    if (!q) return undefined;
    const out: Record<string, string> = {};
    for (const part of q.split('&')) {
      const [k, v] = part.split('=');
      if (k) out[decodeURIComponent(k)] = decodeURIComponent(v || '');
    }
    return Object.keys(out).length ? out : undefined;
  }

  paymentLabels(): string[] {
    const b = this.brand.branding();
    if (!b) return [];
    const labels: string[] = [];
    if (b.cobraEnLaEntrega) labels.push('Contra entrega');
    if (b.cobraAlHacerPedido) labels.push('Pago online al pedir');
    if (b.realizaDelivery !== false) labels.push('Delivery');
    if (b.realizaPickup !== false) labels.push('Pickup / retiro');
    return labels;
  }

  closeMenu(): void {
    this.menuOpen.set(false);
    this.catsOpen.set(false);
    document.body.classList.remove('nav-open');
  }

  toggleMenu(): void {
    const next = !this.menuOpen();
    this.menuOpen.set(next);
    document.body.classList.toggle('nav-open', next);
  }

  @HostListener('window:resize')
  onViewportResize(): void {
    if (window.innerWidth >= 1100) this.closeMenu();
  }

  closeCats(): void {
    this.catsOpen.set(false);
    this.menuOpen.set(false);
  }

  navCats(): { id: number; nombre: string; count: number | null; kind: 'grupo' | 'linea' }[] {
    const grupos = this.navCollections()
      .filter((c) => c.articulosCount > 0)
      .slice()
      .sort((a, b) => b.articulosCount - a.articulosCount)
      .slice(0, 24)
      .map((c) => ({ id: c.id, nombre: c.nombre, count: c.articulosCount, kind: 'grupo' as const }));
    if (grupos.length) return grupos;
    return this.navLineas()
      .slice(0, 24)
      .map((l) => ({ id: l.id, nombre: l.nombre, count: null, kind: 'linea' as const }));
  }

  goHash(id: string, ev: Event): void {
    ev.preventDefault();
    this.closeMenu();
    void this.router.navigate(['/catalogo']).then(() => {
      window.setTimeout(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' }), 80);
    });
  }

  logout(): void {
    this.session.clear();
    this.refreshCartCount();
    void this.router.navigate(['/catalogo']);
  }
}
