import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AdminApiService,
  AdminCupon,
  CmsBlock,
  CmsBlockItem,
  CmsNavMenu,
  CmsNavMenuItem,
  UpsertCmsNavMenuRequest,
} from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { storefrontUrl } from '../core/storefront-url';

const DEFAULT_HEADER_ITEMS: Omit<CmsNavMenuItem, 'id'>[] = [
  { itemType: 'link', label: 'Inicio', url: '/catalogo', target: '_self', badgeText: null, displayOrder: 1, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Catálogo', url: '/catalogo?browse=1', target: '_self', badgeText: null, displayOrder: 2, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Ofertas', url: '/catalogo#ofertas', target: '_self', badgeText: null, displayOrder: 3, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Sucursales', url: '/sucursales', target: '_self', badgeText: null, displayOrder: 4, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Mis listas', url: '/favoritos', target: '_self', badgeText: null, displayOrder: 5, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Seguidos', url: '/seguidos', target: '_self', badgeText: null, displayOrder: 6, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Mi cuenta', url: '/mi-cuenta', target: '_self', badgeText: null, displayOrder: 7, idGrupoArticulo: null, activo: true },
  { itemType: 'link', label: 'Mis pedidos', url: '/mis-pedidos', target: '_self', badgeText: null, displayOrder: 8, idGrupoArticulo: null, activo: true },
];

const BLOCK_META: Record<string, { label: string; icon: string; hint: string }> = {
  top_info_bar: {
    label: 'Barra superior',
    icon: 'fa-bullhorn',
    hint: 'Avisos cortos arriba del header.',
  },
  hero_banner: {
    label: 'Hero principal',
    icon: 'fa-image',
    hint: 'Banner grande del home. Podés agregar un ítem con imagen de fondo.',
  },
  hero_slider: {
    label: 'Carrusel hero',
    icon: 'fa-images',
    hint: 'Varias diapositivas en el hero.',
  },
  promo_banner_grid: {
    label: 'Publicidades / flyers',
    icon: 'fa-ad',
    hint: 'Con imagen URL se muestran en home. Sin imagen, la tienda usa Promociones.',
  },
  featured_categories: {
    label: 'Categorías destacadas',
    icon: 'fa-th-large',
    hint: 'Atajos opcionales. Si no hay ítems, el home usa categorías del catálogo.',
  },
  feature_list: {
    label: 'Beneficios / trust',
    icon: 'fa-shield-alt',
    hint: 'Franja de beneficios bajo el hero.',
  },
  coupon_strip: {
    label: 'Franja de cupón',
    icon: 'fa-ticket-alt',
    hint: 'Código en Badge (Marketplace). Para mostrarlo en la tienda también debe estar ON en Personalización → Layout home → Franja de cupón.',
  },
  product_carousel: {
    label: 'Carrusel de productos',
    icon: 'fa-box-open',
    hint: 'Solo título/subtítulo de la sección (recomendados / más vendidos).',
  },
  newsletter: {
    label: 'Newsletter',
    icon: 'fa-envelope',
    hint: 'Textos de la franja de suscripción.',
  },
};

type ImageSource = 'upload' | 'url' | 'erp';

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Contenido</h1>
        <p class="dx-page-sub">Bloques del home y menú de la tienda, sin tocar código.</p>
      </div>
      <div class="dx-filters">
        <a class="btn btn-outline-secondary btn-sm" [href]="shopUrl" target="_blank" rel="noopener">
          <i class="fas fa-external-link-alt mr-1"></i> Tienda
        </a>
        <a routerLink="/personalizacion" class="btn btn-outline-secondary btn-sm">Personalización</a>
        <button type="button" class="btn btn-outline-primary btn-sm" (click)="ensureHomeBlocks()"
          [disabled]="loading() || ensuring()">
          <i class="fas fa-magic mr-1"></i> Completar bloques
        </button>
        <button type="button" class="btn btn-primary btn-sm" (click)="refresh()" [disabled]="loading()">
          <i class="fas fa-sync-alt mr-1"></i> Actualizar
        </button>
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }
    @if (ok()) {
      <div class="alert alert-success alert-dismissible">
        <button type="button" class="close" (click)="ok.set(null)"><span>&times;</span></button>
        {{ ok() }}
      </div>
    }

    <div class="cms-status">
      <button type="button" class="cms-chip" [class.on]="statusFilter() === ''" (click)="setStatusFilter('')">
        {{ blocks().length }} bloques
      </button>
      <button type="button" class="cms-chip ok" [class.on]="statusFilter() === 'published'" (click)="setStatusFilter('published')">
        {{ publishedBlocks() }} publicados
      </button>
      <button type="button" class="cms-chip warn" [class.on]="statusFilter() === 'draft'" (click)="setStatusFilter('draft')">
        {{ draftBlocks() }} borrador
      </button>
      <span class="cms-chip muted">{{ menus().length }} menú(s)</span>
      <span class="cms-chip muted">{{ cuponesActivos().length }} cupones activos</span>
    </div>

    <nav class="cms-tabs" aria-label="Secciones de contenido">
      <button type="button" [class.on]="tab() === 'blocks'" (click)="setTab('blocks')">
        <i class="fas fa-th-large mr-1"></i> Bloques home
        <span class="cms-tab-count">{{ blocks().length }}</span>
      </button>
      <button type="button" [class.on]="tab() === 'menus'" (click)="setTab('menus')">
        <i class="fas fa-bars mr-1"></i> Menú tienda
        <span class="cms-tab-count">{{ menus().length }}</span>
      </button>
      <button type="button" [class.on]="tab() === 'audit'" (click)="setTab('audit')">
        <i class="fas fa-history mr-1"></i> Auditoría
      </button>
    </nav>

    @if (tab() === 'blocks') {
      <div class="row">
        <div class="col-lg-4">
          <div class="card cms-list-card">
            <div class="card-header">
              <div class="cms-list-head">
                <strong>Bloques</strong>
                <input class="form-control form-control-sm" [ngModel]="blockFilter()"
                  (ngModelChange)="blockFilter.set($event)" name="bf" placeholder="Filtrar…" />
              </div>
            </div>
            <div class="list-group list-group-flush cms-scroll">
              @for (b of filteredBlocks(); track b.id) {
                <button type="button" class="list-group-item list-group-item-action cms-row"
                  [class.active]="selected()?.id === b.id" (click)="select(b)">
                  @if (blockPreviewUrl(b); as thumb) {
                    <img class="cms-row__thumb" [src]="thumb" alt=""
                         (error)="$any($event.target).classList.add('is-broken')" />
                  } @else {
                    <div class="cms-row__icon"><i class="fas" [class]="blockIcon(b.blockType)"></i></div>
                  }
                  <div class="cms-row__body">
                    <div class="cms-row__title">{{ blockLabel(b.blockType) }}</div>
                    <div class="cms-row__sub">{{ b.title || b.internalName }}</div>
                    <div class="cms-row__meta">
                      <span>#{{ b.displayOrder }}</span>
                      @if (supportsItems(b.blockType)) {
                        <span>· {{ b.items?.length || 0 }} ítems</span>
                      }
                    </div>
                  </div>
                  <span class="cms-badge" [class.pub]="b.contentStatus === 'published' && b.activo"
                    [class.draft]="b.contentStatus === 'draft'"
                    [class.off]="!b.activo || b.contentStatus === 'archived'">
                    {{ statusLabel(b.contentStatus, b.activo) }}
                  </span>
                </button>
              } @empty {
                <div class="p-4 text-muted text-center">
                  @if (loading()) { Cargando bloques… }
                  @else if (blockFilter().trim() || statusFilter()) { Ningún bloque coincide con el filtro. }
                  @else {
                    Sin bloques.
                    <button type="button" class="btn btn-link btn-sm p-0" (click)="ensureHomeBlocks()">
                      Completar bloques del home
                    </button>
                  }
                </div>
              }
            </div>
          </div>
          <p class="text-muted small mt-2 mb-0">
            Visibilidad del home también se controla en
            <a routerLink="/personalizacion">Personalización</a>.
            Flyers con productos: <a routerLink="/promociones">Promociones</a>.
            Cupones: <a routerLink="/marketplace">Marketplace</a>.
          </p>
        </div>

        <div class="col-lg-8">
          @if (selected(); as b) {
            <div class="card">
              <div class="card-header cms-editor-head">
                <div>
                  <span class="cms-editor-title">
                    <i class="fas mr-1" [class]="blockIcon(b.blockType)"></i>
                    {{ blockLabel(b.blockType) }}
                  </span>
                  <small class="text-muted d-block">{{ b.internalName }} · zona {{ b.pageZone || '—' }}</small>
                </div>
                <div class="cms-editor-actions">
                  <button type="button" class="btn btn-outline-secondary btn-sm" (click)="select(b)" title="Descartar cambios locales">
                    Reset
                  </button>
                  <button type="button" class="btn btn-primary btn-sm" (click)="publish(b)"
                    [disabled]="saving() === b.id">
                    @if (saving() === b.id) { Guardando… } @else { <i class="fas fa-save mr-1"></i> Guardar }
                  </button>
                </div>
              </div>
              <div class="card-body">
                <p class="cms-hint">{{ blockHint(b.blockType) }}</p>

                @if (isVisualBanner(b.blockType) && blockPreviewUrl(b); as hero) {
                  <div class="cms-hero-preview" [class.cms-hero-preview--grid]="b.blockType === 'promo_banner_grid'">
                    <img [src]="hero" [alt]="b.title || 'Banner'"
                         (error)="$any($event.target).classList.add('is-broken')" />
                    <span>Así se ve en la tienda (guardá para publicar)</span>
                  </div>
                }

                <div class="form-row">
                  <div class="form-group col-md-8">
                    <label>Título visible</label>
                    <input class="form-control" [(ngModel)]="b.title" name="btitle" />
                  </div>
                  <div class="form-group col-md-4">
                    <label>Orden en home</label>
                    <input class="form-control" type="number" [(ngModel)]="b.displayOrder" name="bord" />
                  </div>
                </div>
                <div class="form-group">
                  <label>Subtítulo</label>
                  <input class="form-control" [(ngModel)]="b.subtitle" name="bsub" />
                </div>
                <div class="form-group">
                  <label>Descripción</label>
                  <textarea class="form-control" rows="2" [(ngModel)]="b.description" name="bdesc"></textarea>
                </div>

                <div class="cms-switches">
                  <div class="custom-control custom-switch">
                    <input type="checkbox" class="custom-control-input" id="cmsActivo" [(ngModel)]="b.activo" name="bact" />
                    <label class="custom-control-label" for="cmsActivo">Activo en vitrina</label>
                  </div>
                  <div class="form-group mb-0 flex-grow-1">
                    <label class="mb-1">Estado</label>
                    <select class="custom-select custom-select-sm" [(ngModel)]="b.contentStatus" name="bst">
                      <option value="draft">Borrador</option>
                      <option value="published">Publicado</option>
                      <option value="archived">Archivado</option>
                    </select>
                  </div>
                </div>
                <div class="form-row cms-dates">
                  <div class="form-group col-md-6 mb-0">
                    <label>Inicio de vigencia <small class="text-muted">(opcional)</small></label>
                    <input type="datetime-local" class="form-control form-control-sm"
                      [ngModel]="toLocalDateTime(b.startDate)"
                      (ngModelChange)="b.startDate = fromLocalDateTime($event)" name="bstart" />
                  </div>
                  <div class="form-group col-md-6 mb-0">
                    <label>Fin de vigencia <small class="text-muted">(opcional)</small></label>
                    <input type="datetime-local" class="form-control form-control-sm"
                      [ngModel]="toLocalDateTime(b.endDate)"
                      (ngModelChange)="b.endDate = fromLocalDateTime($event)" name="bend" />
                  </div>
                </div>
                <small class="text-muted d-block mt-2">Fuera de este período, el bloque no se muestra aunque esté publicado.</small>

                @if (supportsItems(b.blockType)) {
                  <hr />
                  <div class="d-flex justify-content-between align-items-center mb-2 flex-wrap">
                    <div>
                      <strong>Ítems</strong>
                      <small class="text-muted ml-1">({{ b.items?.length || 0 }})</small>
                    </div>
                    <button type="button" class="btn btn-outline-secondary btn-sm" (click)="addBlockItem(b)">
                      <i class="fas fa-plus mr-1"></i> Agregar ítem
                    </button>
                  </div>

                  @if (b.blockType === 'coupon_strip') {
                    <p class="small text-muted">
                      El código debe existir en <a routerLink="/marketplace">Marketplace → Cupones</a>.
                    </p>
                  }

                  @for (it of b.items; track trackBlockItem(it, i); let i = $index) {
                    <div class="cms-item" [class.off]="!it.activo">
                      <div class="cms-item__head">
                        <strong>Ítem {{ i + 1 }}</strong>
                        <div class="btn-group btn-group-sm">
                          <button type="button" class="btn btn-outline-secondary" [disabled]="i === 0"
                            (click)="moveBlockItem(b, i, -1)" title="Subir">↑</button>
                          <button type="button" class="btn btn-outline-secondary"
                            [disabled]="i >= (b.items?.length || 0) - 1"
                            (click)="moveBlockItem(b, i, 1)" title="Bajar">↓</button>
                          <button type="button" class="btn btn-outline-danger" (click)="removeBlockItem(b, i)">
                            Quitar
                          </button>
                        </div>
                      </div>
                      <div class="form-row">
                        <div class="form-group col-md-4 mb-2">
                          <label class="small mb-0">Título</label>
                          <input class="form-control form-control-sm" [(ngModel)]="it.title" [name]="'bt' + i" />
                        </div>
                        <div class="form-group col-md-4 mb-2">
                          <label class="small mb-0">{{ b.blockType === 'coupon_strip' ? 'Código cupón' : 'Badge' }}</label>
                          @if (b.blockType === 'coupon_strip') {
                            <select class="custom-select custom-select-sm" [(ngModel)]="it.badgeText" [name]="'bb' + i"
                              (ngModelChange)="onCouponPicked(it, $event)">
                              <option [ngValue]="''">— Elegir cupón —</option>
                              @for (c of cuponesActivos(); track c.id) {
                                <option [ngValue]="c.codigo">{{ c.codigo }} · {{ c.descripcion }}</option>
                              }
                            </select>
                            @if (it.badgeText && !cuponExists(it.badgeText)) {
                              <small class="text-warning d-block">
                                «{{ it.badgeText }}» no está en cupones activos.
                              </small>
                            }
                          } @else {
                            <input class="form-control form-control-sm" [(ngModel)]="it.badgeText" [name]="'bb' + i"
                              placeholder="Oferta" />
                          }
                        </div>
                        <div class="form-group col-md-4 mb-2">
                          <label class="small mb-0">Orden</label>
                          <input class="form-control form-control-sm" type="number" [(ngModel)]="it.displayOrder"
                            [name]="'bo' + i" />
                        </div>
                      </div>
                      <div class="form-row">
                        <div class="form-group col-md-6 mb-2">
                          <label class="small mb-0">Subtítulo</label>
                          <input class="form-control form-control-sm" [(ngModel)]="it.subtitle" [name]="'bs' + i" />
                        </div>
                        <div class="form-group col-md-6 mb-2">
                          <label class="small mb-0">Link</label>
                          <input class="form-control form-control-sm" [(ngModel)]="it.linkUrl" [name]="'bl' + i"
                            placeholder="/catalogo#ofertas" />
                        </div>
                      </div>
                      <div class="form-group mb-2">
                        <label class="small mb-0">Descripción</label>
                        <input class="form-control form-control-sm" [(ngModel)]="it.description" [name]="'bd' + i" />
                      </div>
                      <div class="form-group mb-2">
                        <div class="d-flex justify-content-between align-items-center mb-1 flex-wrap">
                          <label class="small mb-0">Imagen</label>
                          <div class="btn-group btn-group-xs">
                            <button type="button" class="btn btn-outline-secondary"
                              [class.active]="imageSource(it) === 'upload'"
                              (click)="setImageSource(it, 'upload')">Subir</button>
                            <button type="button" class="btn btn-outline-secondary"
                              [class.active]="imageSource(it) === 'url'"
                              (click)="setImageSource(it, 'url')">URL</button>
                            <button type="button" class="btn btn-outline-secondary"
                              [class.active]="imageSource(it) === 'erp'"
                              (click)="setImageSource(it, 'erp')">ERP</button>
                          </div>
                        </div>
                        @if (imageSource(it) === 'upload') {
                          <div class="cms-upload">
                            <input #cmsImageFile type="file" class="form-control-file" accept="image/jpeg,image/png,image/webp,image/gif,image/avif"
                              (change)="uploadCmsImage(it, $event)" [disabled]="uploadingItem() === it" />
                            <small class="text-muted">Se guarda en el servidor de imágenes configurado (máx. 20 MB).</small>
                            @if (uploadingItem() === it) { <span class="text-info small ml-1">Subiendo…</span> }
                          </div>
                        } @else if (imageSource(it) === 'erp') {
                          <input class="form-control form-control-sm" [(ngModel)]="it.imageUrl" [name]="'bi' + i"
                            placeholder="/media/erp/nombre-archivo.jpg" />
                          <small class="text-muted">Usá una ruta ya sincronizada desde ERP. No copia el archivo.</small>
                        } @else {
                          <input class="form-control form-control-sm" [(ngModel)]="it.imageUrl" [name]="'bi' + i"
                            placeholder="https://cdn…/imagen.jpg" />
                          <small class="text-muted">La imagen queda alojada en el servidor externo indicado.</small>
                        }
                        @if (it.imageUrl) {
                          <div class="cms-thumb mt-1">
                            <img [src]="mediaUrl(it.imageUrl)" [alt]="it.title || 'Banner'" />
                          </div>
                        }
                      </div>
                      <div class="form-group mb-2">
                        <label class="small mb-0">Imagen móvil <span class="text-muted">(opcional)</span></label>
                        <div class="input-group input-group-sm">
                          <input class="form-control" [(ngModel)]="it.mobileImageUrl" [name]="'bmi' + i"
                            placeholder="URL o /media/… para pantallas pequeñas" />
                          <div class="input-group-append">
                            <label class="btn btn-outline-secondary mb-0">
                              <i class="fas fa-upload"></i>
                              <input type="file" class="d-none" accept="image/jpeg,image/png,image/webp,image/gif,image/avif"
                                (change)="uploadCmsImage(it, $event, true)" [disabled]="uploadingItem() === it" />
                            </label>
                          </div>
                        </div>
                        @if (it.mobileImageUrl) {
                          <div class="cms-thumb cms-thumb--mobile mt-1">
                            <img [src]="mediaUrl(it.mobileImageUrl)" [alt]="it.title || 'Banner móvil'" />
                          </div>
                        }
                      </div>
                      <div class="custom-control custom-switch">
                        <input type="checkbox" class="custom-control-input" [id]="'ba' + i" [(ngModel)]="it.activo"
                          [name]="'ba' + i" />
                        <label class="custom-control-label" [for]="'ba' + i">Ítem activo</label>
                      </div>
                    </div>
                  } @empty {
                    <div class="cms-empty">Sin ítems. Agregá uno según el tipo de bloque.</div>
                  }
                }
              </div>
            </div>
          } @else {
            <div class="card">
              <div class="card-body cms-empty">
                Seleccioná un bloque a la izquierda para editarlo.
              </div>
            </div>
          }
        </div>
      </div>
    }

    @if (tab() === 'menus') {
      <div class="row">
        <div class="col-lg-4">
          <div class="card cms-list-card">
            <div class="card-header d-flex justify-content-between align-items-center">
              <strong>Menús</strong>
              <button type="button" class="btn btn-outline-primary btn-sm" (click)="ensureHeaderMenu()"
                [disabled]="menuSaving()">
                Plantilla header
              </button>
            </div>
            <div class="list-group list-group-flush cms-scroll">
              @for (m of menus(); track m.id) {
                <button type="button" class="list-group-item list-group-item-action cms-row"
                  [class.active]="selectedMenu()?.id === m.id" (click)="selectMenu(m)">
                  <div class="cms-row__icon"><i class="fas fa-bars"></i></div>
                  <div class="cms-row__body">
                    <div class="cms-row__title">{{ m.name }}</div>
                    <div class="cms-row__sub"><code>{{ m.code }}</code> · {{ m.menuType }}</div>
                    <div class="cms-row__meta">{{ m.items?.length || 0 }} ítems</div>
                  </div>
                  <span class="cms-badge" [class.pub]="m.contentStatus === 'published' && m.activo"
                    [class.draft]="m.contentStatus === 'draft'"
                    [class.off]="!m.activo || m.contentStatus === 'archived'">
                    {{ statusLabel(m.contentStatus, m.activo) }}
                  </span>
                </button>
              } @empty {
                <div class="p-4 text-muted text-center">
                  {{ loading() ? 'Cargando…' : 'Sin menús. Usá «Plantilla header».' }}
                </div>
              }
            </div>
          </div>
          <p class="text-muted small mt-2 mb-0">
            Con ≥5 ítems publicados, el storefront usa este menú en lugar del menú fijo.
          </p>
        </div>

        <div class="col-lg-8">
          @if (selectedMenu(); as m) {
            <div class="card">
              <div class="card-header cms-editor-head">
                <div>
                  <span class="cms-editor-title">{{ m.name }}</span>
                  <small class="text-muted d-block">{{ m.code }} · {{ m.menuType }}</small>
                </div>
                <div class="cms-editor-actions">
                  <button type="button" class="btn btn-outline-secondary btn-sm" (click)="addMenuItem(m)">
                    + Ítem
                  </button>
                  <button type="button" class="btn btn-primary btn-sm" (click)="saveMenu(m)" [disabled]="menuSaving()">
                    @if (menuSaving()) { Guardando… } @else { <i class="fas fa-save mr-1"></i> Guardar }
                  </button>
                </div>
              </div>
              <div class="card-body">
                <div class="form-row">
                  <div class="form-group col-md-4">
                    <label>Código</label>
                    <input class="form-control" [(ngModel)]="m.code" name="mcode" />
                  </div>
                  <div class="form-group col-md-5">
                    <label>Nombre</label>
                    <input class="form-control" [(ngModel)]="m.name" name="mname" />
                  </div>
                  <div class="form-group col-md-3">
                    <label>Orden</label>
                    <input class="form-control" type="number" [(ngModel)]="m.displayOrder" name="mord" />
                  </div>
                </div>
                <div class="form-row">
                  <div class="form-group col-md-4">
                    <label>Tipo</label>
                    <select class="form-control" [(ngModel)]="m.menuType" name="mtype">
                      <option value="header">Header</option>
                      <option value="footer">Footer</option>
                      <option value="utility">Utility</option>
                    </select>
                  </div>
                  <div class="form-group col-md-4">
                    <label>Estado</label>
                    <select class="form-control" [(ngModel)]="m.contentStatus" name="mst">
                      <option value="draft">Borrador</option>
                      <option value="published">Publicado</option>
                      <option value="archived">Archivado</option>
                    </select>
                  </div>
                  <div class="form-group col-md-4 d-flex align-items-end">
                    <div class="custom-control custom-switch mb-2">
                      <input type="checkbox" class="custom-control-input" id="menuActivo" [(ngModel)]="m.activo" name="mact" />
                      <label class="custom-control-label" for="menuActivo">Activo</label>
                    </div>
                  </div>
                </div>

                <hr />
                <h5 class="mb-2">Ítems del menú</h5>
                @for (it of m.items; track trackItem(it, i); let i = $index) {
                  <div class="cms-item" [class.off]="!it.activo">
                    <div class="cms-item__head">
                      <strong>{{ it.label || 'Ítem ' + (i + 1) }}</strong>
                      <div class="btn-group btn-group-sm">
                        <button type="button" class="btn btn-outline-secondary" [disabled]="i === 0"
                          (click)="moveMenuItem(m, i, -1)">↑</button>
                        <button type="button" class="btn btn-outline-secondary"
                          [disabled]="i >= (m.items?.length || 0) - 1"
                          (click)="moveMenuItem(m, i, 1)">↓</button>
                        <button type="button" class="btn btn-outline-danger" (click)="removeMenuItem(m, i)">
                          Quitar
                        </button>
                      </div>
                    </div>
                    <div class="form-row">
                      <div class="form-group col-md-3 mb-2">
                        <label class="small mb-0">Etiqueta</label>
                        <input class="form-control form-control-sm" [(ngModel)]="it.label" [name]="'ml' + i" />
                      </div>
                      <div class="form-group col-md-4 mb-2">
                        <label class="small mb-0">URL</label>
                        <input class="form-control form-control-sm" [(ngModel)]="it.url" [name]="'mu' + i"
                          placeholder="/catalogo" />
                      </div>
                      <div class="form-group col-md-2 mb-2">
                        <label class="small mb-0">Badge</label>
                        <input class="form-control form-control-sm" [(ngModel)]="it.badgeText" [name]="'mb' + i" />
                      </div>
                      <div class="form-group col-md-1 mb-2">
                        <label class="small mb-0">Ord.</label>
                        <input class="form-control form-control-sm" type="number" [(ngModel)]="it.displayOrder"
                          [name]="'mo' + i" />
                      </div>
                      <div class="form-group col-md-2 mb-2 d-flex align-items-end">
                        <div class="custom-control custom-switch">
                          <input type="checkbox" class="custom-control-input" [id]="'miAct' + i"
                            [(ngModel)]="it.activo" [name]="'ma' + i" />
                          <label class="custom-control-label" [for]="'miAct' + i">Activo</label>
                        </div>
                      </div>
                    </div>
                  </div>
                } @empty {
                  <div class="cms-empty">Sin ítems. Agregá uno o cargá la plantilla.</div>
                }
              </div>
            </div>
          } @else {
            <div class="card">
              <div class="card-body cms-empty">
                Seleccioná un menú o creá la plantilla header.
              </div>
            </div>
          }
        </div>
      </div>
    }

    @if (tab() === 'audit') {
      <div class="card">
        <div class="card-header"><strong>Auditoría reciente</strong></div>
        <div class="card-body table-responsive p-0">
          <table class="table table-sm table-hover mb-0">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Usuario</th>
                <th>Acción</th>
                <th>Entidad</th>
                <th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              @for (a of audit(); track a.id) {
                <tr>
                  <td>{{ a.fecha | date: 'short' }}</td>
                  <td>{{ a.usuario }}</td>
                  <td>{{ a.accion }}</td>
                  <td>{{ a.entidad }} #{{ a.entidadId }}</td>
                  <td>{{ a.detalle || '—' }}</td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="5" class="text-muted text-center py-4">Sin eventos aún</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .cms-status {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        margin-bottom: 0.85rem;
      }
      .cms-chip {
        display: inline-flex;
        align-items: center;
        padding: 0.25rem 0.65rem;
        border-radius: 999px;
        font-size: 0.78rem;
        font-weight: 600;
        background: #f1f5f9;
        color: #334155;
        border: 1px solid transparent;
        cursor: pointer;
      }
      .cms-chip.on {
        box-shadow: 0 0 0 2px #99f6e4;
      }
      .cms-chip.ok {
        background: #dcfce7;
        color: #166534;
      }
      .cms-chip.warn {
        background: #fef3c7;
        color: #92400e;
      }
      .cms-chip.muted {
        background: #f8fafc;
        color: #64748b;
        cursor: default;
      }
      .cms-tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        margin-bottom: 1rem;
      }
      .cms-tabs button {
        border: 1px solid #e2e8f0;
        background: #fff;
        border-radius: 8px;
        padding: 0.45rem 0.85rem;
        font-weight: 600;
        color: #475569;
        cursor: pointer;
      }
      .cms-tabs button.on {
        background: #0f766e;
        border-color: #0f766e;
        color: #fff;
      }
      .cms-tab-count {
        margin-left: 0.35rem;
        opacity: 0.85;
        font-size: 0.8em;
      }
      .cms-list-head {
        display: grid;
        gap: 0.45rem;
      }
      .cms-scroll {
        max-height: min(68dvh, 640px);
        overflow: auto;
      }
      .cms-row {
        display: flex !important;
        align-items: flex-start;
        gap: 0.65rem;
        text-align: left;
      }
      .cms-row__icon {
        width: 2.5rem;
        height: 2.5rem;
        border-radius: 8px;
        background: #f1f5f9;
        display: grid;
        place-items: center;
        color: #0f766e;
        flex-shrink: 0;
      }
      .cms-row__thumb {
        width: 2.75rem;
        height: 2.75rem;
        border-radius: 8px;
        object-fit: cover;
        flex-shrink: 0;
        border: 1px solid #e2e8f0;
        background: #f8fafc;
      }
      .cms-row__thumb.is-broken {
        opacity: 0.3;
        filter: grayscale(1);
      }
      .cms-row.active .cms-row__thumb {
        box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.7);
      }
      .cms-row.active .cms-row__icon {
        background: rgba(255, 255, 255, 0.2);
        color: #fff;
      }
      .cms-row__body {
        flex: 1;
        min-width: 0;
      }
      .cms-row__title {
        font-weight: 700;
        font-size: 0.92rem;
      }
      .cms-row__sub {
        font-size: 0.8rem;
        opacity: 0.85;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .cms-row__meta {
        font-size: 0.72rem;
        opacity: 0.7;
        margin-top: 0.1rem;
      }
      .cms-badge {
        font-size: 0.68rem;
        font-weight: 700;
        padding: 0.15rem 0.45rem;
        border-radius: 999px;
        background: #e2e8f0;
        color: #475569;
        white-space: nowrap;
      }
      .cms-badge.pub {
        background: #dcfce7;
        color: #166534;
      }
      .cms-badge.draft {
        background: #fef3c7;
        color: #92400e;
      }
      .cms-badge.off {
        background: #fee2e2;
        color: #991b1b;
      }
      .cms-editor-head {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.75rem;
        flex-wrap: wrap;
      }
      .cms-editor-title {
        font-weight: 700;
        font-size: 1.05rem;
      }
      .cms-editor-actions {
        display: flex;
        gap: 0.35rem;
        flex-wrap: wrap;
      }
      .cms-hint {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 0.55rem 0.75rem;
        font-size: 0.86rem;
        color: #475569;
      }
      .cms-switches {
        display: flex;
        align-items: flex-end;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 0.5rem;
      }
      .cms-item {
        border: 1px solid #e2e8f0;
        border-radius: 10px;
        padding: 0.75rem;
        margin-bottom: 0.65rem;
        background: #fff;
      }
      .cms-item.off {
        opacity: 0.62;
      }
      .cms-item__head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 0.5rem;
        gap: 0.5rem;
      }
      .cms-thumb {
        height: 120px;
        border-radius: 8px;
        border: 1px solid #e2e8f0;
        overflow: hidden;
        background: #0f172a;
      }
      .cms-thumb img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
      }
      .cms-thumb--mobile {
        width: min(180px, 100%);
        aspect-ratio: 9 / 16;
        height: auto;
      }
      .cms-hero-preview {
        position: relative;
        border-radius: 12px;
        overflow: hidden;
        border: 1px solid #e2e8f0;
        margin-bottom: 1rem;
        min-height: 160px;
        background: #0f172a;
      }
      .cms-hero-preview img {
        width: 100%;
        height: 180px;
        object-fit: cover;
        display: block;
      }
      .cms-hero-preview img.is-broken {
        opacity: 0.25;
        filter: grayscale(1);
        object-fit: contain;
        background: #1e293b;
      }
      .cms-hero-preview span {
        position: absolute;
        left: 0.65rem;
        bottom: 0.55rem;
        background: rgba(15, 23, 42, 0.72);
        color: #fff;
        font-size: 0.72rem;
        padding: 0.15rem 0.45rem;
        border-radius: 999px;
      }
      .cms-dates {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 0.65rem 0.35rem;
        margin: 0;
      }
      .btn-group-xs .btn {
        padding: 0.12rem 0.42rem;
        font-size: 0.72rem;
      }
      .cms-upload {
        border: 1px dashed #cbd5e1;
        border-radius: 7px;
        padding: 0.4rem 0.55rem;
        background: #f8fafc;
      }
      .cms-empty {
        padding: 1.5rem;
        text-align: center;
        color: #64748b;
      }
    `,
  ],
})
export class ContentCmsAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly shopUrl = storefrontUrl(this.session.tenantSlug());
  readonly tab = signal<'blocks' | 'menus' | 'audit'>('blocks');
  readonly blocks = signal<CmsBlock[]>([]);
  readonly selected = signal<CmsBlock | null>(null);
  readonly menus = signal<CmsNavMenu[]>([]);
  readonly selectedMenu = signal<CmsNavMenu | null>(null);
  readonly cupones = signal<AdminCupon[]>([]);
  readonly cuponesActivos = signal<AdminCupon[]>([]);
  readonly audit = signal<
    {
      id: number;
      usuario: string;
      accion: string;
      entidad: string;
      entidadId: string | null;
      detalle: string | null;
      fecha: string;
    }[]
  >([]);
  readonly loading = signal(false);
  readonly ensuring = signal(false);
  readonly saving = signal<number | null>(null);
  readonly menuSaving = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly uploadingItem = signal<CmsBlockItem | null>(null);

  readonly blockFilter = signal('');
  readonly statusFilter = signal<'' | 'published' | 'draft'>('');
  private readonly itemImageSources = new WeakMap<CmsBlockItem, ImageSource>();

  readonly publishedBlocks = computed(
    () => this.blocks().filter((b) => b.contentStatus === 'published' && b.activo).length,
  );
  readonly draftBlocks = computed(
    () => this.blocks().filter((b) => b.contentStatus === 'draft').length,
  );
  readonly filteredBlocks = computed(() => {
    const q = this.blockFilter().trim().toLowerCase();
    const st = this.statusFilter();
    const rows = [...this.blocks()].sort((a, b) => a.displayOrder - b.displayOrder || a.id - b.id);
    return rows.filter((b) => {
      if (st === 'published' && !(b.contentStatus === 'published' && b.activo)) return false;
      if (st === 'draft' && b.contentStatus !== 'draft') return false;
      if (!q) return true;
      const hay = `${b.blockType} ${b.internalName} ${b.title || ''} ${this.blockLabel(b.blockType)}`.toLowerCase();
      return hay.includes(q);
    });
  });

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((q) => {
      const t = q.get('tab');
      if (t === 'blocks' || t === 'menus' || t === 'audit') this.tab.set(t);
      const st = q.get('st');
      this.statusFilter.set(st === 'published' || st === 'draft' ? st : '');
    });
    this.refresh();
    this.api.listCupones().subscribe({
      next: (rows) => {
        this.cupones.set(rows);
        this.cuponesActivos.set(rows.filter((c) => c.activo));
      },
      error: () => undefined,
    });
  }

  setTab(t: 'blocks' | 'menus' | 'audit'): void {
    this.tab.set(t);
    this.syncCmsQuery();
  }

  setStatusFilter(st: '' | 'published' | 'draft'): void {
    this.statusFilter.set(st);
    this.tab.set('blocks');
    this.syncCmsQuery();
  }

  private syncCmsQuery(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        tab: this.tab() === 'blocks' ? null : this.tab(),
        st: this.statusFilter() || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  blockLabel(type: string): string {
    return BLOCK_META[type]?.label || type;
  }

  blockIcon(type: string): string {
    return BLOCK_META[type]?.icon || 'fa-cube';
  }

  blockHint(type: string): string {
    return BLOCK_META[type]?.hint || '';
  }

  statusLabel(status: string, activo: boolean): string {
    if (!activo) return 'Inactivo';
    if (status === 'published') return 'Publicado';
    if (status === 'archived') return 'Archivado';
    return 'Borrador';
  }

  imageSource(item: CmsBlockItem): ImageSource {
    const known = this.itemImageSources.get(item);
    if (known) return known;
    return item.imageUrl?.startsWith('/media/erp/') ? 'erp' : item.imageUrl?.startsWith('/media/') ? 'upload' : 'url';
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  isVisualBanner(type: string): boolean {
    return type === 'hero_banner' || type === 'hero_slider' || type === 'promo_banner_grid';
  }

  blockPreviewUrl(block: CmsBlock): string | null {
    const items = block.items || [];
    const hit = items.find((i) => i.activo !== false && i.imageUrl) || items.find((i) => !!i.imageUrl);
    return hit?.imageUrl ? this.mediaUrl(hit.imageUrl) : null;
  }

  setImageSource(item: CmsBlockItem, source: ImageSource): void {
    this.itemImageSources.set(item, source);
  }

  uploadCmsImage(item: CmsBlockItem, event: Event, mobile = false): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.error.set('El archivo debe ser una imagen.');
      return;
    }
    if (file.size > 20 * 1024 * 1024) {
      this.error.set('La imagen supera el máximo de 20 MB.');
      return;
    }
    this.uploadingItem.set(item);
    this.error.set(null);
    this.api.uploadCmsImage(file).subscribe({
      next: (stored) => {
        if (mobile) item.mobileImageUrl = stored.url;
        else {
          item.imageUrl = stored.url;
          this.itemImageSources.set(item, 'upload');
        }
        this.touchSelected();
        this.uploadingItem.set(null);
        this.ok.set(`Imagen «${stored.originalFileName}» subida. Guardá el bloque para publicarla.`);
      },
      error: (e) => {
        this.uploadingItem.set(null);
        this.error.set(e?.error?.error || e.message || 'No se pudo subir la imagen.');
      },
    });
  }

  toLocalDateTime(value: string | null | undefined): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  fromLocalDateTime(value: string): string | null {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  cuponExists(codigo: string | null | undefined): boolean {
    const c = (codigo || '').trim().toUpperCase();
    if (!c) return false;
    return this.cupones().some((x) => x.codigo.toUpperCase() === c);
  }

  onCouponPicked(it: { badgeText: string | null; title: string | null }, codigo: string): void {
    const c = this.cupones().find((x) => x.codigo === codigo);
    if (c && !it.title) it.title = c.descripcion || c.codigo;
  }

  trackItem(it: CmsNavMenuItem, i: number): string | number {
    return it.id > 0 ? it.id : `new-${i}`;
  }

  trackBlockItem(it: { id: number }, i: number): string | number {
    return it.id > 0 ? it.id : `new-b-${i}`;
  }

  supportsItems(blockType: string): boolean {
    return [
      'top_info_bar',
      'hero_banner',
      'hero_slider',
      'promo_banner_grid',
      'featured_categories',
      'feature_list',
      'coupon_strip',
    ].includes(blockType);
  }

  addBlockItem(b: CmsBlock): void {
    const ord = (b.items?.length || 0) + 1;
    const itemType =
      b.blockType === 'coupon_strip'
        ? 'coupon'
        : b.blockType === 'feature_list'
          ? 'feature'
          : b.blockType === 'featured_categories'
            ? 'category'
            : 'banner';
    b.items = [
      ...(b.items || []),
      {
        id: 0,
        itemType,
        title: '',
        subtitle: null,
        description: null,
        imageUrl: null,
        mobileImageUrl: null,
        linkUrl: b.blockType === 'coupon_strip' ? '/checkout' : '/catalogo#ofertas',
        linkTarget: '_self',
        badgeText: b.blockType === 'coupon_strip' ? '' : null,
        priceText: null,
        displayOrder: ord,
        activo: true,
      },
    ];
  }

  removeBlockItem(b: CmsBlock, index: number): void {
    b.items = (b.items || []).filter((_, i) => i !== index);
    this.reindexBlockItems(b);
  }

  moveBlockItem(b: CmsBlock, index: number, delta: number): void {
    const next = index + delta;
    if (!b.items || next < 0 || next >= b.items.length) return;
    const copy = [...b.items];
    const [row] = copy.splice(index, 1);
    copy.splice(next, 0, row);
    b.items = copy;
    this.reindexBlockItems(b);
  }

  private reindexBlockItems(b: CmsBlock): void {
    (b.items || []).forEach((it, i) => (it.displayOrder = i + 1));
  }

  ensureHomeBlocks(): void {
    this.ensuring.set(true);
    this.error.set(null);
    this.api.ensureHomeCmsBlocks().subscribe({
      next: (r) => {
        this.ensuring.set(false);
        this.ok.set(r.mensaje);
        this.refresh();
      },
      error: (e) => {
        this.ensuring.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  select(b: CmsBlock): void {
    const fresh = this.blocks().find((x) => x.id === b.id) || b;
    this.selected.set(structuredClone(fresh));
    this.ok.set(null);
  }

  private touchSelected(): void {
    const cur = this.selected();
    if (!cur) return;
    const items = (cur.items || []).map((i) => ({ ...i }));
    this.selected.set({ ...cur, items });
    this.blocks.update((rows) =>
      rows.map((r) => (r.id === cur.id ? { ...r, items: items.map((i) => ({ ...i })) } : r)),
    );
  }

  selectMenu(m: CmsNavMenu): void {
    this.selectedMenu.set(structuredClone(m));
    this.ok.set(null);
  }

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listCmsBlocks('home').subscribe({
      next: (rows) => {
        this.blocks.set(rows);
        this.loading.set(false);
        const cur = this.selected();
        if (cur) {
          const fresh = rows.find((r) => r.id === cur.id);
          this.selected.set(fresh ? structuredClone(fresh) : null);
        } else if (rows.length) {
          this.selected.set(structuredClone(rows.slice().sort((a, b) => a.displayOrder - b.displayOrder)[0]));
        }
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
    this.api.listCmsMenus().subscribe({
      next: (rows) => {
        this.menus.set(rows);
        const cur = this.selectedMenu();
        if (cur) {
          const fresh = rows.find((r) => r.id === cur.id);
          this.selectedMenu.set(fresh ? structuredClone(fresh) : null);
        }
      },
      error: () => this.menus.set([]),
    });
    this.api.listAudit(50).subscribe({
      next: (rows) => this.audit.set(rows),
      error: () => undefined,
    });
  }

  publish(b: CmsBlock): void {
    this.saving.set(b.id);
    this.ok.set(null);
    this.api
      .updateCmsBlock(b.id, {
        pageCode: b.pageCode,
        pageZone: b.pageZone,
        blockType: b.blockType,
        internalName: b.internalName,
        title: b.title,
        subtitle: b.subtitle,
        description: b.description,
        displayOrder: b.displayOrder,
        contentStatus: b.contentStatus,
        activo: b.activo,
        isReadOnlyWhenIntegrated: b.isReadOnlyWhenIntegrated,
        startDate: b.startDate,
        endDate: b.endDate,
        settingsJson: b.settingsJson,
        items: b.items.map((i) => ({
          id: i.id > 0 ? i.id : undefined,
          itemType: i.itemType,
          title: i.title,
          subtitle: i.subtitle,
          description: i.description,
          imageUrl: i.imageUrl,
          mobileImageUrl: i.mobileImageUrl,
          linkUrl: i.linkUrl,
          linkTarget: i.linkTarget,
          badgeText: i.badgeText,
          priceText: i.priceText,
          displayOrder: i.displayOrder,
          activo: i.activo,
        })),
      })
      .subscribe({
        next: () => {
          this.saving.set(null);
          this.ok.set(`Bloque «${this.blockLabel(b.blockType)}» guardado`);
          this.refresh();
        },
        error: (e) => {
          this.saving.set(null);
          this.error.set(e?.error?.error || e.message);
        },
      });
  }

  addMenuItem(m: CmsNavMenu): void {
    const ord = (m.items?.length || 0) + 1;
    m.items = [
      ...(m.items || []),
      {
        id: 0,
        itemType: 'link',
        label: 'Nuevo',
        url: '/catalogo',
        target: '_self',
        badgeText: null,
        displayOrder: ord,
        idGrupoArticulo: null,
        activo: true,
      },
    ];
    this.selectedMenu.set({ ...m });
  }

  removeMenuItem(m: CmsNavMenu, index: number): void {
    m.items = m.items.filter((_, i) => i !== index);
    this.reindexMenuItems(m);
    this.selectedMenu.set({ ...m });
  }

  moveMenuItem(m: CmsNavMenu, index: number, delta: number): void {
    const next = index + delta;
    if (!m.items || next < 0 || next >= m.items.length) return;
    const copy = [...m.items];
    const [row] = copy.splice(index, 1);
    copy.splice(next, 0, row);
    m.items = copy;
    this.reindexMenuItems(m);
    this.selectedMenu.set({ ...m });
  }

  private reindexMenuItems(m: CmsNavMenu): void {
    (m.items || []).forEach((it, i) => (it.displayOrder = i + 1));
  }

  ensureHeaderMenu(): void {
    const existing = this.menus().find((x) => x.code === 'main_header');
    if (existing) {
      const draft = structuredClone(existing);
      if (!draft.items?.length || draft.items.length < 5) {
        draft.items = DEFAULT_HEADER_ITEMS.map((it, i) => ({
          id: 0,
          ...it,
          displayOrder: i + 1,
        }));
      }
      draft.contentStatus = 'published';
      draft.activo = true;
      this.selectedMenu.set(draft);
      this.tab.set('menus');
      this.ok.set('Plantilla cargada en el editor — guardá para publicar en la tienda.');
      return;
    }

    this.menuSaving.set(true);
    const body: UpsertCmsNavMenuRequest = {
      code: 'main_header',
      name: 'Menú principal',
      menuType: 'header',
      pageCode: 'global',
      pageZone: 'header',
      displayOrder: 1,
      contentStatus: 'published',
      activo: true,
      items: DEFAULT_HEADER_ITEMS.map((it, i) => ({
        itemType: it.itemType,
        label: it.label,
        url: it.url,
        target: it.target,
        badgeText: it.badgeText,
        displayOrder: i + 1,
        idGrupoArticulo: null,
        activo: true,
      })),
    };
    this.api.createCmsMenu(body).subscribe({
      next: (m) => {
        this.menuSaving.set(false);
        this.ok.set('Menú header creado y publicado');
        this.tab.set('menus');
        this.refresh();
        this.selectedMenu.set(structuredClone(m));
      },
      error: (e) => {
        this.menuSaving.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  saveMenu(m: CmsNavMenu): void {
    this.menuSaving.set(true);
    this.ok.set(null);
    const body: UpsertCmsNavMenuRequest = {
      code: m.code,
      name: m.name,
      menuType: m.menuType,
      pageCode: m.pageCode,
      pageZone: m.pageZone,
      displayOrder: m.displayOrder,
      contentStatus: m.contentStatus,
      activo: m.activo,
      items: (m.items || []).map((it) => ({
        id: it.id > 0 ? it.id : undefined,
        itemType: it.itemType || 'link',
        label: it.label,
        url: it.url,
        target: it.target || '_self',
        badgeText: it.badgeText,
        displayOrder: it.displayOrder,
        idGrupoArticulo: it.idGrupoArticulo,
        activo: it.activo,
      })),
    };
    this.api.updateCmsMenu(m.id, body).subscribe({
      next: (saved) => {
        this.menuSaving.set(false);
        this.ok.set(`Menú «${saved.name}» guardado — recargá la tienda para verlo`);
        this.selectedMenu.set(structuredClone(saved));
        this.refresh();
      },
      error: (e) => {
        this.menuSaving.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }
}
