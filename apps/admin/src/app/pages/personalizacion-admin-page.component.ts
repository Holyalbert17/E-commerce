import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { AdminApiService, PersonalizacionItem } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { storefrontUrl } from '../core/storefront-url';
import { environment } from '../../environments/environment';

const CLAVES = [
  { key: 'home.layout', label: 'Layout home', hint: 'Secciones visibles del catálogo' },
  { key: 'tema.activo', label: 'Tema / colores', hint: 'Paleta de campaña' },
  { key: 'carrito.textos', label: 'Textos carrito', hint: 'Microcopy del carrito y CTA a checkout' },
  { key: 'checkout.textos', label: 'Textos checkout', hint: 'Microcopy de compra' },
  { key: 'widgets.toggles', label: 'Widgets', hint: 'Activar / desactivar módulos' },
] as const;

const SECTION_LABELS: Record<string, string> = {
  hero: 'Hero principal',
  categorias: 'Categorías',
  publicidad: 'Publicidades (TipoGrupo 2)',
  flyers: 'Ofertas / promos (TipoGrupo 3 + CMS)',
  recomendados: 'Recomendados',
  masVendidos: 'Más vendidos',
  newsletter: 'Newsletter',
  catalogoCta: 'CTA catálogo completo',
};

const DEFAULT_SECTIONS = [
  { id: 'hero', visible: true, orden: 1 },
  { id: 'categorias', visible: true, orden: 2 },
  { id: 'publicidad', visible: true, orden: 3 },
  { id: 'flyers', visible: true, orden: 4 },
  { id: 'recomendados', visible: true, orden: 5 },
  { id: 'masVendidos', visible: true, orden: 6 },
  { id: 'newsletter', visible: true, orden: 7 },
  { id: 'catalogoCta', visible: true, orden: 8 },
];

interface LayoutForm {
  sections: { id: string; visible: boolean; orden: number }[];
  mostrarBannerMayorista: boolean;
  mostrarTopBar: boolean;
  mostrarTrust: boolean;
  mostrarCupon: boolean;
  mostrarProductosOferta: boolean;
  mostrarHeroFeature: boolean;
}

interface TemaForm {
  nombre: string;
  primary: string;
  accent: string;
  ink: string;
  paper: string;
  displayFont: string;
  bannerUrl: string;
}

interface CheckoutForm {
  titulo: string;
  subtitulo: string;
  ctaConfirmar: string;
  hintMapa: string;
  msgExito: string;
}

interface CarritoForm {
  titulo: string;
  seguirComprando: string;
  vacioTitulo: string;
  vacioSub: string;
  ctaCatalogo: string;
  hintEnvio: string;
  ctaVaciar: string;
  ctaGuardarLista: string;
  ctaCheckout: string;
  ctaStockBlock: string;
}

interface WidgetsForm {
  chat: boolean;
  resenas: boolean;
  favoritos: boolean;
  seguidos: boolean;
  bannerMayorista: boolean;
  facebook: boolean;
  instagram: boolean;
  whatsapp: boolean;
  youtube: boolean;
  twitter: boolean;
  tiktok: boolean;
}

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Personalización</h1>
        <p class="dx-page-sub">Marketing edita layout, tema, textos y widgets sin tocar código.</p>
      </div>
      <a class="btn btn-outline-secondary btn-sm" [href]="shopUrl" target="_blank" rel="noopener">
        Abrir tienda
      </a>
    </div>

    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (ok()) { <div class="alert alert-success">{{ ok() }}</div> }

    <ul class="nav nav-tabs mb-3">
      @for (c of claves; track c.key) {
        <li class="nav-item">
          <a class="nav-link" [class.active]="tab() === c.key" href="#" (click)="selectTab(c.key, $event)">{{ c.label }}</a>
        </li>
      }
    </ul>

    <div class="row">
      <div class="col-lg-8">
        <div class="card card-outline card-primary">
          <div class="card-header d-flex justify-content-between align-items-center flex-wrap">
            <div>
              <h3 class="card-title mb-0">
                {{ labelOf(tab()) }}
                @if (dirty()) {
                  <span class="badge badge-warning ml-1">Borrador sin publicar</span>
                } @else {
                  <span class="badge badge-success ml-1">Publicado</span>
                }
              </h3>
              <small class="text-muted">
                {{ hintOf(tab()) }}
                @if (current(); as cur) {
                  · v{{ cur.version }} · {{ cur.fechaCreacion | date: 'short' }}
                  @if (cur.creadoPor) { · {{ cur.creadoPor }} }
                }
              </small>
            </div>
            <div>
              <button type="button" class="btn btn-sm btn-outline-secondary mr-1" (click)="loadDefault()">Defaults</button>
              <button type="button" class="btn btn-sm btn-outline-dark" (click)="showJson = !showJson">
                {{ showJson ? 'Formulario' : 'JSON avanzado' }}
              </button>
            </div>
          </div>

          <div class="card-body">
            @if (showJson) {
              <div class="form-group">
                <label>Valor JSON</label>
                <textarea class="form-control font-monospace" rows="14" [(ngModel)]="draftJson" name="json"
                  (ngModelChange)="syncFormsFromJson()"></textarea>
              </div>
            } @else if (tab() === 'home.layout') {
              <p class="text-muted mb-2">
                Activá u ocultá cada bloque del home. Si está apagado, <strong>no aparece en la tienda</strong>
                aunque haya contenido en CMS o Promociones. Orden = posición en la página.
              </p>
              @for (s of layout.sections; track s.id; let i = $index) {
                <div class="d-flex align-items-center mb-2 sec-row">
                  <div class="custom-control custom-switch flex-grow-1 mb-0">
                    <input type="checkbox" class="custom-control-input" [id]="'sec-' + s.id"
                      [(ngModel)]="s.visible" [name]="'sec-' + s.id" (ngModelChange)="syncJsonFromForms()" />
                    <label class="custom-control-label" [for]="'sec-' + s.id">
                      {{ sectionLabel(s.id) }} <code class="text-muted">{{ s.id }}</code>
                      <span class="badge badge-light ml-1">#{{ s.orden }}</span>
                    </label>
                  </div>
                  <div class="btn-group btn-group-sm ml-2">
                    <button type="button" class="btn btn-outline-secondary" [disabled]="i === 0"
                      (click)="moveSection(i, -1)" title="Subir">↑</button>
                    <button type="button" class="btn btn-outline-secondary"
                      [disabled]="i >= layout.sections.length - 1"
                      (click)="moveSection(i, 1)" title="Bajar">↓</button>
                  </div>
                </div>
              }
              <hr />
              <p class="text-muted small mb-2">Extras dentro de secciones activas</p>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="topBar"
                  [(ngModel)]="layout.mostrarTopBar" name="topBar" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="topBar">Top bar (avisos CMS)</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="trust"
                  [(ngModel)]="layout.mostrarTrust" name="trust" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="trust">Beneficios / trust bajo el hero</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="heroFeat"
                  [(ngModel)]="layout.mostrarHeroFeature" name="heroFeat" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="heroFeat">Panel destacado en el hero</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="cupon"
                  [(ngModel)]="layout.mostrarCupon" name="cupon" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="cupon">
                  Franja de cupón en Promociones (ej. BIENVENIDO10)
                </label>
              </div>
              <p class="text-muted small mb-2">
                Apagá este switch para ocultar la tarjeta del cupón en el home.
                Encendido: además el bloque <strong>Franja de cupón</strong> debe estar
                <em>Activo</em> en <a routerLink="/contenido">Contenido</a> y el código
                creado en <a routerLink="/marketplace">Marketplace → Cupones</a>.
              </p>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="prodOf"
                  [(ngModel)]="layout.mostrarProductosOferta" name="prodOf" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="prodOf">Productos en oferta (Promociones TipoGrupo 3)</label>
              </div>
              <div class="custom-control custom-switch mt-3">
                <input type="checkbox" class="custom-control-input" id="banMay"
                  [(ngModel)]="layout.mostrarBannerMayorista" name="banMay" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="banMay">Banner mayorista (si el cliente está aprobado)</label>
              </div>
              <p class="text-muted small mt-3 mb-0">
                Contenido: <strong>Contenido</strong> · Flyers/productos:
                <strong>Promociones</strong> · Cupones reales: <strong>Marketplace</strong>.
              </p>
            } @else if (tab() === 'tema.activo') {
              <div class="form-row">
                <div class="form-group col-md-6">
                  <label>Nombre del tema</label>
                  <input class="form-control" [(ngModel)]="tema.nombre" name="tnom" (ngModelChange)="syncJsonFromForms()" />
                </div>
                <div class="form-group col-md-6">
                  <label>Tipografía display (CSS)</label>
                  <input class="form-control" [(ngModel)]="tema.displayFont" name="tfont"
                    placeholder="ej. 'Fraunces', Georgia, serif" (ngModelChange)="syncJsonFromForms()" />
                </div>
              </div>
              <div class="form-row">
                <div class="form-group col-6 col-md-3">
                  <label>Primario</label>
                  <input type="color" class="form-control form-control-color w-100" [(ngModel)]="tema.primary"
                    name="tp" (ngModelChange)="syncJsonFromForms()" />
                  <input class="form-control form-control-sm mt-1" [(ngModel)]="tema.primary" name="tps"
                    (ngModelChange)="syncJsonFromForms()" />
                </div>
                <div class="form-group col-6 col-md-3">
                  <label>Acento</label>
                  <input type="color" class="form-control form-control-color w-100" [(ngModel)]="tema.accent"
                    name="ta" (ngModelChange)="syncJsonFromForms()" />
                  <input class="form-control form-control-sm mt-1" [(ngModel)]="tema.accent" name="tas"
                    (ngModelChange)="syncJsonFromForms()" />
                </div>
                <div class="form-group col-6 col-md-3">
                  <label>Texto</label>
                  <input type="color" class="form-control form-control-color w-100" [(ngModel)]="tema.ink"
                    name="ti" (ngModelChange)="syncJsonFromForms()" />
                  <input class="form-control form-control-sm mt-1" [(ngModel)]="tema.ink" name="tis"
                    (ngModelChange)="syncJsonFromForms()" />
                </div>
                <div class="form-group col-6 col-md-3">
                  <label>Fondo</label>
                  <input type="color" class="form-control form-control-color w-100" [(ngModel)]="tema.paper"
                    name="tpa" (ngModelChange)="syncJsonFromForms()" />
                  <input class="form-control form-control-sm mt-1" [(ngModel)]="tema.paper" name="tpas"
                    (ngModelChange)="syncJsonFromForms()" />
                </div>
              </div>
              <div class="form-group">
                <label>Banner URL (opcional)</label>
                <div class="upload-row">
                  <input class="form-control" [(ngModel)]="tema.bannerUrl" name="tbanner"
                    placeholder="https://… o subí un archivo" (ngModelChange)="syncJsonFromForms()" />
                  <label class="btn btn-outline-secondary mb-0">
                    <i class="fas fa-upload"></i>
                    <input type="file" class="d-none" accept="image/jpeg,image/png,image/webp,image/gif,image/avif"
                      (change)="uploadTemaBanner($event)" [disabled]="uploadingBanner()" />
                  </label>
                </div>
                @if (uploadingBanner()) {
                  <small class="text-muted">Subiendo banner…</small>
                }
                @if (tema.bannerUrl) {
                  <img class="tema-banner-preview mt-2" [src]="mediaUrl(tema.bannerUrl)" alt="Vista previa banner" />
                }
              </div>
              <div class="tema-preview" [style.background]="tema.paper" [style.color]="tema.ink">
                <span class="chip" [style.background]="tema.primary">Primario</span>
                <span class="chip" [style.background]="tema.accent">Acento</span>
                <strong [style.fontFamily]="tema.displayFont || 'inherit'">{{ tema.nombre || 'Vista previa' }}</strong>
                <p class="mb-0 small">Así se verán los acentos en la tienda.</p>
              </div>
            } @else if (tab() === 'checkout.textos') {
              <div class="form-group">
                <label>Título</label>
                <input class="form-control" [(ngModel)]="checkout.titulo" name="ctit" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Subtítulo</label>
                <input class="form-control" [(ngModel)]="checkout.subtitulo" name="csub" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Botón confirmar</label>
                <input class="form-control" [(ngModel)]="checkout.ctaConfirmar" name="ccta" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Hint del mapa</label>
                <input class="form-control" [(ngModel)]="checkout.hintMapa" name="cmap" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Mensaje de éxito (post-compra)</label>
                <input class="form-control" [(ngModel)]="checkout.msgExito" name="cok" (ngModelChange)="syncJsonFromForms()" />
              </div>
            } @else if (tab() === 'carrito.textos') {
              <div class="form-group">
                <label>Título</label>
                <input class="form-control" [(ngModel)]="carrito.titulo" name="kartit" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Seguir comprando</label>
                <input class="form-control" [(ngModel)]="carrito.seguirComprando" name="kseg" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Carrito vacío (título)</label>
                <input class="form-control" [(ngModel)]="carrito.vacioTitulo" name="kvacio" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Carrito vacío (subtítulo)</label>
                <input class="form-control" [(ngModel)]="carrito.vacioSub" name="kvaciosub" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Botón ir al catálogo</label>
                <input class="form-control" [(ngModel)]="carrito.ctaCatalogo" name="kcat" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Hint de envío</label>
                <input class="form-control" [(ngModel)]="carrito.hintEnvio" name="kenv" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Vaciar</label>
                <input class="form-control" [(ngModel)]="carrito.ctaVaciar" name="kvac" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Guardar como lista</label>
                <input class="form-control" [(ngModel)]="carrito.ctaGuardarLista" name="klista" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Botón continuar al checkout</label>
                <input class="form-control" [(ngModel)]="carrito.ctaCheckout" name="kcta" (ngModelChange)="syncJsonFromForms()" />
              </div>
              <div class="form-group">
                <label>Bloqueo por stock</label>
                <input class="form-control" [(ngModel)]="carrito.ctaStockBlock" name="kstock" (ngModelChange)="syncJsonFromForms()" />
              </div>
            } @else if (tab() === 'widgets.toggles') {
              <p class="text-muted">Apagá módulos de la tienda sin deploy.</p>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="wchat"
                  [(ngModel)]="widgets.chat" name="wchat" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="wchat">Chat de soporte</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="wres"
                  [(ngModel)]="widgets.resenas" name="wres" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="wres">Reseñas de producto</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="wfav"
                  [(ngModel)]="widgets.favoritos" name="wfav" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="wfav">Favoritos / listas</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="wseg"
                  [(ngModel)]="widgets.seguidos" name="wseg" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="wseg">Productos seguidos</label>
              </div>
              <div class="custom-control custom-switch mb-2">
                <input type="checkbox" class="custom-control-input" id="wban"
                  [(ngModel)]="widgets.bannerMayorista" name="wban" (ngModelChange)="syncJsonFromForms()" />
                <label class="custom-control-label" for="wban">Banner mayorista</label>
              </div>
              <hr />
              <div class="d-flex align-items-center justify-content-between flex-wrap mb-2">
                <div>
                  <strong>Redes sociales del footer</strong>
                  <small class="d-block text-muted">Las URLs se configuran en Diseño / logo.</small>
                </div>
                <a class="btn btn-xs btn-outline-secondary" href="/branding">Editar URLs</a>
              </div>
              <div class="widget-social-grid">
                <div class="custom-control custom-switch">
                  <input type="checkbox" class="custom-control-input" id="wfb"
                    [(ngModel)]="widgets.facebook" name="wfb" (ngModelChange)="syncJsonFromForms()" />
                  <label class="custom-control-label" for="wfb"><i class="fab fa-facebook text-primary mr-1"></i> Facebook</label>
                </div>
                <div class="custom-control custom-switch">
                  <input type="checkbox" class="custom-control-input" id="wig"
                    [(ngModel)]="widgets.instagram" name="wig" (ngModelChange)="syncJsonFromForms()" />
                  <label class="custom-control-label" for="wig"><i class="fab fa-instagram text-danger mr-1"></i> Instagram</label>
                </div>
                <div class="custom-control custom-switch">
                  <input type="checkbox" class="custom-control-input" id="wwa"
                    [(ngModel)]="widgets.whatsapp" name="wwa" (ngModelChange)="syncJsonFromForms()" />
                  <label class="custom-control-label" for="wwa"><i class="fab fa-whatsapp text-success mr-1"></i> WhatsApp</label>
                </div>
                <div class="custom-control custom-switch">
                  <input type="checkbox" class="custom-control-input" id="wyt"
                    [(ngModel)]="widgets.youtube" name="wyt" (ngModelChange)="syncJsonFromForms()" />
                  <label class="custom-control-label" for="wyt"><i class="fab fa-youtube text-danger mr-1"></i> YouTube</label>
                </div>
                <div class="custom-control custom-switch">
                  <input type="checkbox" class="custom-control-input" id="wtx"
                    [(ngModel)]="widgets.twitter" name="wtx" (ngModelChange)="syncJsonFromForms()" />
                  <label class="custom-control-label" for="wtx"><i class="fab fa-twitter mr-1"></i> X / Twitter</label>
                </div>
                <div class="custom-control custom-switch">
                  <input type="checkbox" class="custom-control-input" id="wtt"
                    [(ngModel)]="widgets.tiktok" name="wtt" (ngModelChange)="syncJsonFromForms()" />
                  <label class="custom-control-label" for="wtt"><i class="fab fa-tiktok mr-1"></i> TikTok</label>
                </div>
              </div>
            }

            <hr />
            <div class="form-row">
              <div class="form-group col-md-4">
                <label>Inicio vigencia</label>
                <input type="datetime-local" class="form-control" [(ngModel)]="inicio" name="ini" />
              </div>
              <div class="form-group col-md-4">
                <label>Fin vigencia</label>
                <input type="datetime-local" class="form-control" [(ngModel)]="fin" name="fin" />
              </div>
              <div class="form-group col-md-4">
                <label>Nota / campaña</label>
                <input class="form-control" [(ngModel)]="nota" name="nota" placeholder="Black Friday 2026" />
              </div>
            </div>
          </div>

          <div class="card-footer d-flex align-items-center flex-wrap">
            <button type="button" class="btn btn-primary" [disabled]="busy() || !dirty()" (click)="save()">
              Publicar ahora
            </button>
            <button type="button" class="btn btn-outline-info ml-2" [disabled]="busy() || !inicio || !fin" (click)="programar()">
              Programar campaña
            </button>
            <small class="text-muted ml-2">
              @if (!dirty()) { Sin cambios pendientes. }
              @else { La tienda pública no cambia hasta publicar. }
            </small>
          </div>
        </div>
      </div>

      <div class="col-lg-4">
        <div class="card">
          <div class="card-header">
            <h3 class="card-title mb-0">Historial de versiones</h3>
          </div>
          <div class="card-body p-0">
            @if (versiones().length === 0) {
              <p class="p-3 text-muted mb-0">Sin versiones guardadas (usando default).</p>
            } @else {
              <ul class="list-group list-group-flush ver-list">
                @for (v of versiones(); track v.version) {
                  <li class="list-group-item">
                    <div class="d-flex justify-content-between align-items-start">
                      <div>
                        <strong>v{{ v.version }}</strong>
                        @if (v.activo) { <span class="badge badge-success ml-1">activa</span> }
                        <div class="small text-muted">
                          {{ v.fechaCreacion | date: 'short' }}
                          @if (v.nota) { · {{ v.nota }} }
                        </div>
                        @if (v.fechaInicioVigencia) {
                          <div class="small text-info">
                            Campaña {{ v.fechaInicioVigencia | date: 'shortDate' }} → {{ v.fechaFinVigencia | date: 'shortDate' }}
                          </div>
                        }
                      </div>
                      <button type="button" class="btn btn-xs btn-outline-primary" (click)="restore(v)">Restaurar</button>
                    </div>
                  </li>
                }
              </ul>
            }
          </div>
        </div>

        <div class="card mt-3">
          <div class="card-header prev-head">
            <div class="prev-head__text">
              <span class="prev-head__title">Vista previa</span>
              <small class="text-muted d-block">
                {{ previewPageLabel() }} · {{ previewMode() === 'draft' ? 'borrador' : 'publicado' }}
              </small>
            </div>
            <div class="prev-head__actions">
              @if (previewMode() === 'draft') {
                <button type="button" class="btn btn-xs btn-outline-secondary" (click)="clearPreview()">Ver publicado</button>
              } @else {
                <button type="button" class="btn btn-xs btn-outline-secondary" (click)="sendDraftToPreview()">Ver borrador</button>
              }
              <button type="button" class="btn btn-xs btn-outline-primary" (click)="refreshPreview()">
                <i class="fas fa-sync-alt mr-1"></i>Recargar
              </button>
              <a class="btn btn-xs btn-outline-dark" [href]="previewSrc()" target="_blank" rel="noopener">Abrir</a>
            </div>
          </div>
          <div class="card-body p-2">
            <iframe #previewFrame class="preview-frame" [src]="previewSafeSrc()" title="Vista previa de la tienda"
              (load)="onPreviewLoad()"></iframe>
            @if (previewError()) {
              <p class="small text-danger mb-0 mt-2">
                <i class="fas fa-triangle-exclamation mr-1"></i>
                No se pudo conectar con la tienda en {{ storefrontBase }}. Verificá que esté levantada y usá “Recargar”.
              </p>
            } @else {
              <p class="small mb-0 mt-2" [class.text-success]="previewReady() && previewMode() === 'draft'" [class.text-muted]="!previewReady() || previewMode() === 'published'">
                @if (!previewReady()) { <i class="fas fa-spinner fa-spin mr-1"></i>Conectando {{ previewPageLabel() }}… }
                @else if (previewMode() === 'draft') { <i class="fas fa-eye mr-1"></i>Borrador en {{ previewPageLabel() }} · no se publica hasta que guardes. }
                @else { Mostrando la versión publicada en {{ previewPageLabel() }}. }
              </p>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .tema-preview {
        border-radius: 10px;
        padding: 1rem;
        border: 1px solid #e2e8f0;
        display: grid;
        gap: 0.5rem;
      }
      .upload-row {
        display: grid;
        grid-template-columns: 1fr auto;
        gap: 0.5rem;
        align-items: center;
      }
      .tema-banner-preview {
        width: 100%;
        max-height: 140px;
        object-fit: cover;
        border-radius: 8px;
        border: 1px solid #e2e8f0;
        background: #0f172a;
      }
      .chip {
        display: inline-block;
        color: #fff;
        padding: 0.2rem 0.55rem;
        border-radius: 6px;
        margin-right: 0.35rem;
        font-size: 0.8rem;
      }
      .ver-list {
        max-height: 280px;
        overflow: auto;
      }
      .btn-xs {
        padding: 0.15rem 0.45rem;
        font-size: 0.75rem;
      }
      .preview-frame {
        width: 100%;
        height: min(56dvh, 560px);
        min-height: 320px;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        background: #fff;
      }
      .form-control-color {
        min-height: 2.4rem;
        padding: 0.2rem;
      }
      .widget-social-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 0.7rem;
      }
      .prev-head {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .prev-head__title {
        font-weight: 700;
        font-size: 1.05rem;
      }
      .prev-head__actions {
        display: flex;
        gap: 0.35rem;
        flex-wrap: wrap;
      }
    `,
  ],
})
export class PersonalizacionAdminPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly claves = CLAVES;
  readonly shopUrl = storefrontUrl(this.session.tenantSlug());
  readonly previewSrc = signal(storefrontUrl(this.session.tenantSlug(), '/catalogo'));
  /** Angular bloquea iframe[src] sin marcarlo como resource URL confiable. */
  readonly previewSafeSrc = computed<SafeResourceUrl>(() =>
    this.sanitizer.bypassSecurityTrustResourceUrl(this.previewSrc()),
  );
  readonly previewReady = signal(false);
  readonly previewError = signal(false);
  readonly previewMode = signal<'draft' | 'published'>('draft');
  readonly dirty = signal(false);
  readonly storefrontBase = environment.storefrontBaseUrl;
  @ViewChild('previewFrame') private previewFrame?: ElementRef<HTMLIFrameElement>;
  private publishedJson = '';
  private previewTimer?: ReturnType<typeof setTimeout>;

  readonly tab = signal<string>('home.layout');
  readonly current = signal<PersonalizacionItem | null>(null);
  readonly versiones = signal<PersonalizacionItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly busy = signal(false);
  readonly uploadingBanner = signal(false);

  showJson = false;
  draftJson = '';
  inicio = '';
  fin = '';
  nota = '';
  private defaults: Record<string, string> = {};

  layout: LayoutForm = {
    sections: DEFAULT_SECTIONS.map((s) => ({ ...s })),
    mostrarBannerMayorista: true,
    mostrarTopBar: true,
    mostrarTrust: true,
    mostrarCupon: false,
    mostrarProductosOferta: true,
    mostrarHeroFeature: true,
  };
  tema: TemaForm = {
    nombre: 'default',
    primary: '#0f766e',
    accent: '#b45309',
    ink: '#1c1917',
    paper: '#fafaf9',
    displayFont: '',
    bannerUrl: '',
  };
  checkout: CheckoutForm = {
    titulo: 'Finalizar compra',
    subtitulo: '',
    ctaConfirmar: 'Confirmar pedido',
    hintMapa: '',
    msgExito: '',
  };
  carrito: CarritoForm = {
    titulo: 'Carrito',
    seguirComprando: 'Seguir comprando',
    vacioTitulo: 'Tu carrito está vacío.',
    vacioSub: 'Agregá productos desde el catálogo para empezar.',
    ctaCatalogo: 'Ir al catálogo',
    hintEnvio: 'El costo de envío se calcula en el checkout.',
    ctaVaciar: 'Vaciar',
    ctaGuardarLista: 'Guardar como lista',
    ctaCheckout: 'Continuar al checkout',
    ctaStockBlock: 'Revisá stock para continuar',
  };
  widgets: WidgetsForm = {
    chat: true,
    resenas: true,
    favoritos: true,
    seguidos: true,
    bannerMayorista: true,
    facebook: true,
    instagram: true,
    whatsapp: true,
    youtube: true,
    twitter: true,
    tiktok: true,
  };

  ngOnInit(): void {
    this.watchPreviewTimeout();
    this.api.personalizacionSchemas().subscribe({
      next: (s) => {
        this.defaults = s || {};
        this.load();
      },
      error: () => this.load(),
    });
  }

  ngOnDestroy(): void {
    if (this.previewTimer) clearTimeout(this.previewTimer);
  }

  selectTab(key: string, ev: Event): void {
    ev.preventDefault();
    this.tab.set(key);
    this.showJson = false;
    this.previewMode.set('draft');
    this.refreshPreview();
    this.load();
  }

  previewPageLabel(): string {
    const path = this.previewPath();
    if (path === '/carrito') return 'Carrito';
    if (path === '/checkout') return 'Checkout';
    return 'Home / catálogo';
  }

  labelOf(key: string): string {
    return CLAVES.find((c) => c.key === key)?.label || key;
  }

  hintOf(key: string): string {
    return CLAVES.find((c) => c.key === key)?.hint || '';
  }

  sectionLabel(id: string): string {
    return SECTION_LABELS[id] || id;
  }

  private mergeSections(
    stored: { id: string; visible?: boolean; orden?: number }[] | undefined,
  ): { id: string; visible: boolean; orden: number }[] {
    const list = Array.isArray(stored) ? stored : [];
    const byId = new Map(list.map((s) => [s.id, s]));
    const out: { id: string; visible: boolean; orden: number }[] = [];
    for (const def of DEFAULT_SECTIONS) {
      const s = byId.get(def.id);
      if (s) {
        out.push({
          id: def.id,
          visible: s.visible !== false,
          orden: typeof s.orden === 'number' ? s.orden : def.orden,
        });
        byId.delete(def.id);
      } else {
        out.push({ ...def });
      }
    }
    for (const s of byId.values()) {
      out.push({
        id: s.id,
        visible: s.visible !== false,
        orden: typeof s.orden === 'number' ? s.orden : 99,
      });
    }
    return out.sort((a, b) => a.orden - b.orden);
  }

  moveSection(index: number, delta: number): void {
    const next = index + delta;
    if (next < 0 || next >= this.layout.sections.length) return;
    const copy = [...this.layout.sections];
    const [row] = copy.splice(index, 1);
    copy.splice(next, 0, row);
    copy.forEach((s, i) => (s.orden = i + 1));
    this.layout.sections = copy;
    this.syncJsonFromForms();
  }

  load(): void {
    this.error.set(null);
    this.api.getPersonalizacion(this.tab()).subscribe({
      next: (item) => {
        this.current.set(item);
        this.draftJson = this.pretty(item.valorJson);
        this.publishedJson = this.draftJson;
        this.nota = item.nota || '';
        this.syncFormsFromJson();
        this.markDirty();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.api.listPersonalizacionVersiones(this.tab()).subscribe({
      next: (rows) => this.versiones.set(rows || []),
      error: () => this.versiones.set([]),
    });
  }

  loadDefault(): void {
    const d = this.defaults[this.tab()];
    if (d) {
      this.draftJson = this.pretty(d);
      this.syncFormsFromJson();
    }
  }

  restore(v: PersonalizacionItem): void {
    this.draftJson = this.pretty(v.valorJson);
    this.nota = (v.nota ? `Restaurado de v${v.version}: ${v.nota}` : `Restaurado de v${v.version}`);
    this.syncFormsFromJson();
    this.ok.set(`Cargada v${v.version} en el editor. Publicá para activarla.`);
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  uploadTemaBanner(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.error.set('El archivo debe ser una imagen.');
      return;
    }
    this.uploadingBanner.set(true);
    this.error.set(null);
    this.api.uploadCmsImage(file).subscribe({
      next: (stored) => {
        this.tema.bannerUrl = stored.url;
        this.uploadingBanner.set(false);
        this.ok.set(`Banner «${stored.originalFileName}» subido. Publicá el tema para verlo en la tienda.`);
        this.syncJsonFromForms();
      },
      error: (e) => {
        this.uploadingBanner.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo subir el banner.');
      },
    });
  }

  syncJsonFromForms(): void {
    const key = this.tab();
    let obj: unknown;
    if (key === 'home.layout') obj = this.layout;
    else if (key === 'tema.activo') {
      obj = {
        ...this.tema,
        displayFont: this.tema.displayFont || null,
        bannerUrl: this.tema.bannerUrl || null,
      };
    } else if (key === 'checkout.textos') obj = this.checkout;
    else if (key === 'carrito.textos') obj = this.carrito;
    else if (key === 'widgets.toggles') obj = this.widgets;
    else return;
    this.draftJson = JSON.stringify(obj, null, 2);
    this.markDirty();
    this.sendDraftToPreview();
  }

  syncFormsFromJson(): void {
    try {
      const o = JSON.parse(this.draftJson || '{}');
      const key = this.tab();
      if (key === 'home.layout') {
        this.layout = {
          sections: this.mergeSections(o.sections),
          mostrarBannerMayorista: o.mostrarBannerMayorista !== false,
          mostrarTopBar: o.mostrarTopBar !== false,
          mostrarTrust: o.mostrarTrust !== false,
          mostrarCupon: o.mostrarCupon === true,
          mostrarProductosOferta: o.mostrarProductosOferta !== false,
          mostrarHeroFeature: o.mostrarHeroFeature !== false,
        };
      } else if (key === 'tema.activo') {
        this.tema = {
          nombre: o.nombre || 'default',
          primary: o.primary || '#0f766e',
          accent: o.accent || '#b45309',
          ink: o.ink || '#1c1917',
          paper: o.paper || '#fafaf9',
          displayFont: o.displayFont || '',
          bannerUrl: o.bannerUrl || '',
        };
      } else if (key === 'checkout.textos') {
        this.checkout = {
          titulo: o.titulo || '',
          subtitulo: o.subtitulo || '',
          ctaConfirmar: o.ctaConfirmar || '',
          hintMapa: o.hintMapa || '',
          msgExito: o.msgExito || '',
        };
      } else if (key === 'carrito.textos') {
        this.carrito = {
          titulo: o.titulo || 'Carrito',
          seguirComprando: o.seguirComprando || '',
          vacioTitulo: o.vacioTitulo || '',
          vacioSub: o.vacioSub || '',
          ctaCatalogo: o.ctaCatalogo || '',
          hintEnvio: o.hintEnvio || '',
          ctaVaciar: o.ctaVaciar || '',
          ctaGuardarLista: o.ctaGuardarLista || '',
          ctaCheckout: o.ctaCheckout || '',
          ctaStockBlock: o.ctaStockBlock || '',
        };
      } else if (key === 'widgets.toggles') {
        this.widgets = {
          chat: o.chat !== false,
          resenas: o.resenas !== false && o['reseñas'] !== false,
          favoritos: o.favoritos !== false,
          seguidos: o.seguidos !== false,
          bannerMayorista: o.bannerMayorista !== false,
          facebook: o.facebook !== false,
          instagram: o.instagram !== false,
          whatsapp: o.whatsapp !== false,
          youtube: o.youtube !== false,
          twitter: o.twitter !== false,
          tiktok: o.tiktok !== false,
        };
      }
      this.markDirty();
      this.sendDraftToPreview();
    } catch {
      /* ignore while typing JSON */
    }
  }

  save(): void {
    this.syncJsonFromForms();
    this.busy.set(true);
    this.ok.set(null);
    this.error.set(null);
    this.api.putPersonalizacion(this.tab(), {
      valorJson: this.draftJson,
      nota: this.nota || null,
      activo: true,
    }).subscribe({
      next: (item) => {
        this.busy.set(false);
        this.current.set(item);
        this.ok.set(`Publicado ${item.clave} v${item.version}`);
        this.publishedJson = this.pretty(item.valorJson);
        this.dirty.set(false);
        this.load();
        this.refreshPreview();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  programar(): void {
    if (!this.inicio || !this.fin) return;
    this.syncJsonFromForms();
    this.busy.set(true);
    this.api.programarPersonalizacion(this.tab(), {
      valorJson: this.draftJson,
      fechaInicioVigencia: new Date(this.inicio).toISOString(),
      fechaFinVigencia: new Date(this.fin).toISOString(),
      nota: this.nota || null,
    }).subscribe({
      next: (item) => {
        this.busy.set(false);
        this.ok.set(`Programado ${item.clave} v${item.version}`);
        this.load();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  private pretty(raw: string): string {
    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw;
    }
  }

  /** El storefront avisa cuando arranca, pero puede hacerlo antes de este evento. */
  onPreviewLoad(): void {
    if (this.previewMode() === 'draft') this.sendDraftToPreview();
    else this.clearPreview();
  }

  @HostListener('window:message', ['$event'])
  onPreviewMessage(event: MessageEvent<unknown>): void {
    if (event.source !== this.previewFrame?.nativeElement.contentWindow) return;
    const data = event.data;
    if (!data || typeof data !== 'object') return;
    const message = data as { type?: unknown; clave?: unknown };
    if (message.type === 'datamex:personalizacion-preview-ready') {
      this.onPreviewLoad();
      return;
    }
    if (
      message.type === 'datamex:personalizacion-preview-applied' &&
      message.clave === this.tab()
    ) {
      this.previewReady.set(true);
      this.previewError.set(false);
      if (this.previewTimer) clearTimeout(this.previewTimer);
    }
  }

  sendDraftToPreview(): void {
    const target = this.previewFrame?.nativeElement.contentWindow;
    if (!target || !this.draftJson) return;
    this.previewMode.set('draft');
    target.postMessage(
      { type: 'datamex:personalizacion-preview', clave: this.tab(), valorJson: this.draftJson },
      this.previewOrigin(),
    );
  }

  clearPreview(): void {
    this.previewMode.set('published');
    this.previewFrame?.nativeElement.contentWindow?.postMessage(
      { type: 'datamex:personalizacion-preview', clave: this.tab(), valorJson: null },
      this.previewOrigin(),
    );
  }

  refreshPreview(): void {
    this.previewReady.set(false);
    this.previewError.set(false);
    const url = new URL(storefrontUrl(this.session.tenantSlug(), this.previewPath()));
    url.searchParams.set('preview', Date.now().toString());
    this.previewSrc.set(url.toString());
    this.watchPreviewTimeout();
  }

  private watchPreviewTimeout(): void {
    if (this.previewTimer) clearTimeout(this.previewTimer);
    this.previewTimer = setTimeout(() => {
      if (!this.previewReady()) this.previewError.set(true);
    }, 12000);
  }

  private previewPath(): string {
    const key = this.tab();
    if (key === 'carrito.textos') return '/carrito';
    if (key === 'checkout.textos') return '/checkout';
    return '/catalogo';
  }

  private previewOrigin(): string {
    try {
      return new URL(environment.storefrontBaseUrl || 'http://127.0.0.1:4200').origin;
    } catch {
      return '*';
    }
  }

  private markDirty(): void {
    this.dirty.set(this.normalizeJson(this.draftJson) !== this.normalizeJson(this.publishedJson));
  }

  private normalizeJson(raw: string): string {
    try {
      return JSON.stringify(JSON.parse(raw || '{}'));
    } catch {
      return raw || '';
    }
  }
}
