import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AdminApiService, StorefrontBranding } from '../core/admin-api.service';

@Component({
  selector: 'app-branding-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Diseño de tienda</h1>
        <p class="dx-page-sub">
          Identidad, redes, footer, ubicación y mapa · paridad ERP branding.
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" [disabled]="busy()" (click)="guardar()">
        <i class="fas fa-save mr-1"></i> Guardar todo
      </button>
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

    <div class="row">
      <div class="col-lg-7">
        <div class="card card-outline card-primary">
          <div class="card-header"><h3 class="card-title">Identidad</h3></div>
          <div class="card-body">
            <div class="form-row">
              <div class="form-group col-md-8">
                <label>Nombre de tienda</label>
                <input class="form-control" [(ngModel)]="form.nombreTiendaDisplay" name="nombre" maxlength="80" />
              </div>
              <div class="form-group col-md-4">
                <label>Marca (1–2 letras)</label>
                <input class="form-control" [(ngModel)]="form.logoMark" name="mark" maxlength="2" />
              </div>
            </div>
            <div class="form-group">
              <label>Tagline / subtítulo (header)</label>
              <input class="form-control" [(ngModel)]="form.taglineTienda" name="tagline" maxlength="160" />
            </div>
            <div class="form-row colors">
              <div class="form-group col-md-4">
                <label>Color primario</label>
                <div class="color-row">
                  <input type="color" [ngModel]="hexOr(form.colorPrimario, '#c62828')" (ngModelChange)="form.colorPrimario = $event" name="c1" />
                  <input class="form-control" [(ngModel)]="form.colorPrimario" name="c1t" />
                </div>
              </div>
              <div class="form-group col-md-4">
                <label>Color secundario</label>
                <div class="color-row">
                  <input type="color" [ngModel]="hexOr(form.colorSecundario, '#8e0000')" (ngModelChange)="form.colorSecundario = $event" name="c2" />
                  <input class="form-control" [(ngModel)]="form.colorSecundario" name="c2t" />
                </div>
              </div>
              <div class="form-group col-md-4">
                <label>Acento</label>
                <div class="color-row">
                  <input type="color" [ngModel]="hexOr(form.colorAcento, '#1565c0')" (ngModelChange)="form.colorAcento = $event" name="c3" />
                  <input class="form-control" [(ngModel)]="form.colorAcento" name="c3t" />
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="card card-outline card-info">
          <div class="card-header"><h3 class="card-title"><i class="fas fa-share-alt mr-1"></i> Redes sociales</h3></div>
          <div class="card-body">
            <p class="text-muted small mb-3">URLs completas. Se muestran como íconos en el footer del storefront.</p>
            <div class="form-row">
              <div class="form-group col-md-6">
                <label><i class="fab fa-facebook text-primary mr-1"></i> Facebook</label>
                <input class="form-control" [(ngModel)]="form.direccionURLFacebook" name="fb" placeholder="https://facebook.com/…" />
              </div>
              <div class="form-group col-md-6">
                <label><i class="fab fa-instagram text-danger mr-1"></i> Instagram</label>
                <input class="form-control" [(ngModel)]="form.direccionURLInstagram" name="ig" placeholder="https://instagram.com/…" />
              </div>
              <div class="form-group col-md-6">
                <label><i class="fab fa-whatsapp text-success mr-1"></i> WhatsApp</label>
                <input class="form-control" [(ngModel)]="form.direccionURLWhatsapp" name="wa" placeholder="https://wa.me/595…" />
              </div>
              <div class="form-group col-md-6">
                <label><i class="fab fa-youtube text-danger mr-1"></i> YouTube</label>
                <input class="form-control" [(ngModel)]="form.direccionURLYoutube" name="yt" />
              </div>
              <div class="form-group col-md-6">
                <label><i class="fab fa-twitter mr-1"></i> Twitter / X</label>
                <input class="form-control" [(ngModel)]="form.direccionURLTwitter" name="tw" />
              </div>
              <div class="form-group col-md-6">
                <label><i class="fab fa-tiktok mr-1"></i> TikTok</label>
                <input class="form-control" [(ngModel)]="form.direccionURLTikTok" name="tt" placeholder="https://tiktok.com/@…" />
              </div>
            </div>
          </div>
        </div>

        <div class="card card-outline card-success">
          <div class="card-header"><h3 class="card-title"><i class="fas fa-map-marker-alt mr-1"></i> Footer · contacto y ubicación</h3></div>
          <div class="card-body">
            <div class="form-group">
              <label>Texto del footer</label>
              <textarea class="form-control" rows="2" [(ngModel)]="form.footerDescripcion" name="fdesc"
                        placeholder="Experiencia de compra clara: stock, precios y entrega…"></textarea>
              <small class="text-muted">Si vacío, se usa el tagline.</small>
            </div>
            <div class="form-row">
              <div class="form-group col-md-8">
                <label>Dirección del local</label>
                <input class="form-control" [(ngModel)]="form.direccionLocal" name="dir" placeholder="Av. España 1234" />
              </div>
              <div class="form-group col-md-4">
                <label>Ciudad</label>
                <input class="form-control" [(ngModel)]="form.ciudadLocal" name="ciu" placeholder="Asunción" />
              </div>
            </div>
            <div class="form-row">
              <div class="form-group col-md-4">
                <label>Teléfono</label>
                <input class="form-control" [(ngModel)]="form.telefonoContacto" name="tel" placeholder="021 000 000" />
              </div>
              <div class="form-group col-md-4">
                <label>Email</label>
                <input class="form-control" type="email" [(ngModel)]="form.emailContacto" name="em" placeholder="hola@tienda.com" />
              </div>
              <div class="form-group col-md-4">
                <label>Horario</label>
                <input class="form-control" [(ngModel)]="form.horarioAtencionTexto" name="hor" placeholder="Lun–Vie 8:00–18:00" />
              </div>
            </div>

            <hr />
            <h5 class="mb-2">Mapa del local</h5>
            <p class="text-muted small">Coordenadas exactas (WGS84). Podés copiarlas desde Google Maps → clic derecho → coordenadas.</p>
            <div class="form-row">
              <div class="form-group col-md-4">
                <label>Latitud</label>
                <input class="form-control" type="number" step="0.000001" [(ngModel)]="form.latitudLocal" name="lat" placeholder="-25.286664" />
              </div>
              <div class="form-group col-md-4">
                <label>Longitud</label>
                <input class="form-control" type="number" step="0.000001" [(ngModel)]="form.longitudLocal" name="lng" placeholder="-57.647162" />
              </div>
              <div class="form-group col-md-4 d-flex align-items-end">
                <div class="custom-control custom-switch mb-3">
                  <input type="checkbox" class="custom-control-input" id="mapa" [(ngModel)]="form.mostrarMapaFooter" name="mapa" />
                  <label class="custom-control-label" for="mapa">Mostrar mapa en footer</label>
                </div>
              </div>
            </div>
            <div class="dx-form-actions mb-2">
              <button type="button" class="btn btn-outline-secondary btn-sm" (click)="usarAsuncion()">
                <i class="fas fa-crosshairs mr-1"></i> Ejemplo Asunción centro
              </button>
              <button type="button" class="btn btn-outline-danger btn-sm" (click)="limpiarCoords()">
                Quitar coordenadas
              </button>
              @if (mapsLink()) {
                <a class="btn btn-outline-primary btn-sm" [href]="mapsLink()" target="_blank" rel="noopener">
                  <i class="fas fa-external-link-alt mr-1"></i> Abrir en Google Maps
                </a>
              }
            </div>
            @if (mapPreviewUrl(); as src) {
              <div class="map-preview">
                <iframe [src]="src" title="Vista previa mapa" loading="lazy" referrerpolicy="no-referrer-when-downgrade"></iframe>
              </div>
            } @else {
              <div class="dx-empty py-3"><p class="mb-0">Cargá latitud y longitud para previsualizar el mapa.</p></div>
            }
          </div>
          <div class="card-footer">
            <button type="button" class="btn btn-primary" [disabled]="busy()" (click)="guardar()">
              <i class="fas fa-save mr-1"></i> Guardar diseño y footer
            </button>
          </div>
        </div>

        <div class="card card-outline card-warning">
          <div class="card-header"><h3 class="card-title"><i class="fas fa-file-alt mr-1"></i> Contenido legal y crédito</h3></div>
          <div class="card-body">
            <div class="form-group">
              <label>FAQ (HTML)</label>
              <textarea class="form-control" rows="4" [(ngModel)]="form.contenidoFaqHtml" name="faq"
                        placeholder="<h2>…</h2><p>…</p>"></textarea>
            </div>
            <div class="form-group">
              <label>Términos y condiciones (HTML)</label>
              <textarea class="form-control" rows="4" [(ngModel)]="form.contenidoTerminosHtml" name="terminos"></textarea>
            </div>
            <div class="form-group">
              <label>Cómo comprar (HTML)</label>
              <textarea class="form-control" rows="4" [(ngModel)]="form.contenidoComoComprarHtml" name="como"></textarea>
            </div>
            <div class="form-row">
              <div class="form-group col-md-6">
                <label>Crédito “Desarrollado por”</label>
                <input class="form-control" [(ngModel)]="form.creditoDesarrolladoPor" name="credito" placeholder="Datamex" />
              </div>
              <div class="form-group col-md-6">
                <label>URL del crédito</label>
                <input class="form-control" [(ngModel)]="form.creditoDesarrolladoPorUrl" name="creditoUrl" placeholder="https://…" />
              </div>
            </div>
          </div>
        </div>

        <div class="card card-outline card-secondary">
          <div class="card-header"><h3 class="card-title">Imágenes</h3></div>
          <div class="card-body assets">
            <div class="asset">
              <h5>Logo</h5>
              @if (branding()?.logoUrl) {
                <img [src]="media(branding()!.logoUrl)" alt="Logo" class="preview logo" />
                <button type="button" class="btn btn-xs btn-outline-danger" (click)="clear('logo')">Quitar</button>
              } @else {
                <p class="text-muted mb-1">Sin logo — se usa la marca textual.</p>
              }
              <input type="file" accept="image/*" (change)="onFile($event, 'logo')" />
            </div>
            <div class="asset">
              <h5>Favicon</h5>
              @if (branding()?.faviconUrl) {
                <img [src]="media(branding()!.faviconUrl)" alt="Favicon" class="preview fav" />
                <button type="button" class="btn btn-xs btn-outline-danger" (click)="clear('favicon')">Quitar</button>
              } @else {
                <p class="text-muted mb-1">Sin favicon.</p>
              }
              <input type="file" accept="image/*,.ico" (change)="onFile($event, 'favicon')" />
            </div>
            <div class="asset">
              <h5>Banner</h5>
              @if (branding()?.bannerUrl) {
                <img [src]="media(branding()!.bannerUrl)" alt="Banner" class="preview banner" />
                <button type="button" class="btn btn-xs btn-outline-danger" (click)="clear('banner')">Quitar</button>
              } @else {
                <p class="text-muted mb-1">Opcional — franja superior de la tienda.</p>
              }
              <input type="file" accept="image/*" (change)="onFile($event, 'banner')" />
            </div>
          </div>
        </div>
      </div>

      <div class="col-lg-5">
        <div class="card preview-card sticky-top" style="top:1rem">
          <div class="card-header"><h3 class="card-title">Vista previa</h3></div>
          <div class="card-body">
            <div class="sf-preview" [style.--p]="hexOr(form.colorPrimario, '#c62828')" [style.--s]="hexOr(form.colorSecundario, '#8e0000')">
              @if (branding()?.bannerUrl) {
                <div class="sf-banner">
                  <img [src]="media(branding()!.bannerUrl)" alt="" />
                </div>
              }
              <div class="sf-header">
                @if (branding()?.logoUrl) {
                  <img [src]="media(branding()!.logoUrl)" alt="" class="sf-logo" />
                } @else {
                  <span class="sf-mark">{{ (form.logoMark || branding()?.logoMark || 'D').slice(0, 2) }}</span>
                }
                <div>
                  <strong>{{ form.nombreTiendaDisplay || branding()?.nombreTienda || 'Tienda' }}</strong>
                  <small>{{ form.taglineTienda || branding()?.tagline || 'Tu tienda online' }}</small>
                </div>
              </div>
              <div class="sf-foot">
                <strong>{{ form.nombreTiendaDisplay || 'Tienda' }}</strong>
                <p>{{ form.footerDescripcion || form.taglineTienda || 'Texto del footer…' }}</p>
                @if (form.direccionLocal) {
                  <small><i class="fas fa-map-marker-alt"></i> {{ form.direccionLocal }}@if (form.ciudadLocal) {, {{ form.ciudadLocal }}}</small>
                }
                @if (form.telefonoContacto) {
                  <small class="d-block"><i class="fas fa-phone"></i> {{ form.telefonoContacto }}</small>
                }
                <div class="sf-social">
                  @if (form.direccionURLFacebook) { <span>FB</span> }
                  @if (form.direccionURLInstagram) { <span>IG</span> }
                  @if (form.direccionURLWhatsapp) { <span>WA</span> }
                  @if (form.direccionURLTikTok) { <span>TT</span> }
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .color-row { display: flex; gap: 0.5rem; align-items: center; }
      .color-row input[type='color'] { width: 42px; height: 38px; padding: 0; border: 1px solid #ced4da; border-radius: 4px; }
      .assets { display: grid; gap: 1.25rem; }
      .asset h5 { margin-bottom: 0.4rem; }
      .preview { display: block; margin: 0.4rem 0; border: 1px solid #eee; border-radius: 8px; background: #fafafa; }
      .preview.logo { max-height: 72px; width: auto; }
      .preview.fav { width: 32px; height: 32px; object-fit: contain; }
      .preview.banner { width: 100%; max-height: 120px; object-fit: cover; }
      .map-preview {
        border: 1px solid #e5e5e5; border-radius: 10px; overflow: hidden; background: #f4f4f5;
        aspect-ratio: 16 / 10;
      }
      .map-preview iframe { width: 100%; height: 100%; border: 0; display: block; min-height: 220px; }
      .sf-preview {
        border: 1px solid #e5e5e5; border-radius: 12px; overflow: hidden; background: #f4f4f5;
        --p: #c62828; --s: #8e0000;
      }
      .sf-banner { height: 72px; overflow: hidden; background: #111; }
      .sf-banner img { width: 100%; height: 100%; object-fit: cover; display: block; }
      .sf-header {
        display: flex; gap: 0.75rem; align-items: center; padding: 0.85rem 1rem; background: #fff;
        border-bottom: 1px solid #eee;
      }
      .sf-logo { height: 40px; width: auto; }
      .sf-mark {
        width: 40px; height: 40px; border-radius: 10px; background: var(--p); color: #fff;
        display: grid; place-items: center; font-weight: 800;
      }
      .sf-header strong { display: block; font-size: 1rem; }
      .sf-header small { color: #777; }
      .sf-foot {
        padding: 1rem; background: #1a1a1a; color: #ddd; font-size: 0.85rem;
      }
      .sf-foot strong { color: #fff; display: block; margin-bottom: 0.35rem; }
      .sf-foot p { margin: 0 0 0.5rem; color: #bbb; }
      .sf-foot small { color: #999; }
      .sf-social { display: flex; gap: 0.4rem; margin-top: 0.65rem; flex-wrap: wrap; }
      .sf-social span {
        background: #333; color: #fff; border-radius: 6px; padding: 0.2rem 0.45rem; font-size: 0.7rem; font-weight: 700;
      }
    `,
  ],
})
export class BrandingPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly branding = signal<StorefrontBranding | null>(null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  private clearCoordsFlag = false;

  form = this.emptyForm();

  ngOnInit(): void {
    this.reload();
  }

  hexOr(v: string | null | undefined, fallback: string): string {
    const x = (v || '').trim();
    return /^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$/.test(x) ? x : fallback;
  }

  media(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  mapPreviewUrl(): SafeResourceUrl | null {
    const lat = Number(this.form.latitudLocal);
    const lng = Number(this.form.longitudLocal);
    if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;
    if (!this.form.mostrarMapaFooter) return null;
    const d = 0.012;
    const url =
      `https://www.openstreetmap.org/export/embed.html?bbox=${lng - d}%2C${lat - d}%2C${lng + d}%2C${lat + d}` +
      `&layer=mapnik&marker=${lat}%2C${lng}`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  mapsLink(): string | null {
    const lat = Number(this.form.latitudLocal);
    const lng = Number(this.form.longitudLocal);
    if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;
    return `https://www.google.com/maps?q=${lat},${lng}`;
  }

  usarAsuncion(): void {
    this.form.latitudLocal = -25.286664;
    this.form.longitudLocal = -57.647162;
    this.form.ciudadLocal = this.form.ciudadLocal || 'Asunción';
    this.form.mostrarMapaFooter = true;
    this.clearCoordsFlag = false;
  }

  limpiarCoords(): void {
    this.form.latitudLocal = null;
    this.form.longitudLocal = null;
    this.clearCoordsFlag = true;
  }

  reload(): void {
    this.error.set(null);
    this.api.getBranding().subscribe({
      next: (b) => {
        this.branding.set(b);
        this.clearCoordsFlag = false;
        this.form = {
          nombreTiendaDisplay: b.nombreTienda || '',
          taglineTienda: b.tagline || '',
          logoMark: b.logoMark || '',
          colorPrimario: b.colorPrimario || '#C62828',
          colorSecundario: b.colorSecundario || '#8E0000',
          colorAcento: b.colorAcento || '#1565C0',
          direccionURLFacebook: b.facebook || '',
          direccionURLTwitter: b.twitter || '',
          direccionURLInstagram: b.instagram || '',
          direccionURLYoutube: b.youtube || '',
          direccionURLWhatsapp: b.whatsapp || '',
          direccionURLTikTok: b.tiktok || '',
          footerDescripcion: b.footerDescripcion || '',
          direccionLocal: b.direccionLocal || '',
          ciudadLocal: b.ciudadLocal || '',
          telefonoContacto: b.telefonoContacto || '',
          emailContacto: b.emailContacto || '',
          horarioAtencionTexto: b.horarioAtencion || '',
          latitudLocal: b.latitud ?? null,
          longitudLocal: b.longitud ?? null,
          mostrarMapaFooter: b.mostrarMapa !== false,
          contenidoFaqHtml: b.contenidoFaqHtml || '',
          contenidoTerminosHtml: b.contenidoTerminosHtml || '',
          contenidoComoComprarHtml: b.contenidoComoComprarHtml || '',
          creditoDesarrolladoPor: b.creditoDesarrolladoPor || '',
          creditoDesarrolladoPorUrl: b.creditoDesarrolladoPorUrl || '',
        };
      },
      error: (e) => this.error.set(e?.error?.error || 'No se pudo cargar el branding'),
    });
  }

  guardar(): void {
    this.busy.set(true);
    this.error.set(null);
    this.ok.set(null);
    const body: Record<string, unknown> = {
      nombreTiendaDisplay: this.form.nombreTiendaDisplay,
      taglineTienda: this.form.taglineTienda,
      logoMark: this.form.logoMark,
      colorPrimario: this.form.colorPrimario,
      colorSecundario: this.form.colorSecundario,
      colorAcento: this.form.colorAcento,
      direccionURLFacebook: this.form.direccionURLFacebook,
      direccionURLTwitter: this.form.direccionURLTwitter,
      direccionURLInstagram: this.form.direccionURLInstagram,
      direccionURLYoutube: this.form.direccionURLYoutube,
      direccionURLWhatsapp: this.form.direccionURLWhatsapp,
      direccionURLTikTok: this.form.direccionURLTikTok,
      footerDescripcion: this.form.footerDescripcion,
      direccionLocal: this.form.direccionLocal,
      ciudadLocal: this.form.ciudadLocal,
      telefonoContacto: this.form.telefonoContacto,
      emailContacto: this.form.emailContacto,
      horarioAtencionTexto: this.form.horarioAtencionTexto,
      mostrarMapaFooter: this.form.mostrarMapaFooter,
      contenidoFaqHtml: this.form.contenidoFaqHtml,
      contenidoTerminosHtml: this.form.contenidoTerminosHtml,
      contenidoComoComprarHtml: this.form.contenidoComoComprarHtml,
      creditoDesarrolladoPor: this.form.creditoDesarrolladoPor,
      creditoDesarrolladoPorUrl: this.form.creditoDesarrolladoPorUrl,
    };
    if (this.clearCoordsFlag) {
      body['clearCoords'] = true;
    } else {
      if (this.form.latitudLocal != null && this.form.latitudLocal !== ('' as unknown)) {
        body['latitudLocal'] = Number(this.form.latitudLocal);
      }
      if (this.form.longitudLocal != null && this.form.longitudLocal !== ('' as unknown)) {
        body['longitudLocal'] = Number(this.form.longitudLocal);
      }
    }
    this.api.updateBranding(body).subscribe({
      next: (b) => {
        this.branding.set(b);
        this.clearCoordsFlag = false;
        this.ok.set('Diseño y footer guardados. Refrescá el storefront.');
        this.busy.set(false);
        this.reload();
      },
      error: (e) => {
        this.error.set(e?.error?.error || 'Error al guardar');
        this.busy.set(false);
      },
    });
  }

  onFile(ev: Event, kind: 'logo' | 'favicon' | 'banner'): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.busy.set(true);
    this.error.set(null);
    this.ok.set(null);
    this.api.uploadBrandingAsset(kind, file).subscribe({
      next: (b) => {
        this.branding.set(b);
        this.ok.set(`${kind} actualizado.`);
        this.busy.set(false);
        input.value = '';
      },
      error: (e) => {
        this.error.set(e?.error?.error || `No se pudo subir ${kind}`);
        this.busy.set(false);
        input.value = '';
      },
    });
  }

  clear(kind: 'logo' | 'favicon' | 'banner'): void {
    const body =
      kind === 'logo'
        ? { clearLogo: true }
        : kind === 'favicon'
          ? { clearFavicon: true }
          : { clearBanner: true };
    this.busy.set(true);
    this.api.updateBranding(body).subscribe({
      next: (b) => {
        this.branding.set(b);
        this.ok.set(`${kind} quitado.`);
        this.busy.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.error || 'No se pudo quitar');
        this.busy.set(false);
      },
    });
  }

  private emptyForm() {
    return {
      nombreTiendaDisplay: '',
      taglineTienda: '',
      logoMark: '',
      colorPrimario: '#C62828',
      colorSecundario: '#8E0000',
      colorAcento: '#1565C0',
      direccionURLFacebook: '',
      direccionURLTwitter: '',
      direccionURLInstagram: '',
      direccionURLYoutube: '',
      direccionURLWhatsapp: '',
      direccionURLTikTok: '',
      footerDescripcion: '',
      direccionLocal: '',
      ciudadLocal: '',
      telefonoContacto: '',
      emailContacto: '',
      horarioAtencionTexto: '',
      latitudLocal: null as number | null,
      longitudLocal: null as number | null,
      mostrarMapaFooter: true,
      contenidoFaqHtml: '',
      contenidoTerminosHtml: '',
      contenidoComoComprarHtml: '',
      creditoDesarrolladoPor: '',
      creditoDesarrolladoPorUrl: '',
    };
  }
}
