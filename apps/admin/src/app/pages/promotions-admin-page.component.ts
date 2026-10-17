import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import {
  AdminApiService,
  AdminPromotionListItem,
  AdminProduct,
} from '../core/admin-api.service';

const TIPO_GRUPO = [
  { value: 0, label: 'Combos / sugerencias' },
  { value: 1, label: 'Destacados' },
  { value: 2, label: 'Publicidad (home)' },
  { value: 3, label: 'Oferta / promoción (home)' },
] as const;

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Promociones / vitrinas</h1>
        <p class="dx-page-sub">
          Vitrinas ERP. <strong>Publicidad (2)</strong> = banners del home ·
          <strong>Oferta (3)</strong> = flyers + productos. Si la miniatura sale rota, el archivo no está en el media:
          editá y <strong>subí el flyer de nuevo</strong>. Layout: Personalización → Layout home.
        </p>
      </div>
      <button type="button" class="btn btn-info btn-sm" (click)="startCreate()">
        <i class="fas fa-plus mr-1"></i> Nuevo
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
      <div class="col-lg-5">
        <div class="card card-primary card-outline">
          <div class="card-header">
            <h3 class="card-title">{{ editId ? 'Editar #' + editId : 'Alta rápida' }}</h3>
          </div>
          <div class="card-body">
            <div class="form-group">
              <label>Nombre</label>
              <input class="form-control" [(ngModel)]="form.nombre" placeholder="Ej: Promo verano" />
            </div>
            <div class="form-group">
              <label>Tipo</label>
              <select class="custom-select" [(ngModel)]="form.tipoGrupo">
                @for (t of tipos; track t.value) {
                  <option [ngValue]="t.value">{{ t.label }}</option>
                }
              </select>
            </div>
            <div class="form-group">
              <label>Imagen del flyer / publicidad</label>
              <div class="upload-row">
                <input class="form-control" [(ngModel)]="form.imagen" name="imagen" placeholder="URL o subí un archivo…" />
                <label class="btn btn-outline-secondary mb-0">
                  <i class="fas fa-upload"></i> Subir
                  <input type="file" accept="image/*" hidden (change)="onImageSelected($event)" [disabled]="uploading()" />
                </label>
              </div>
              @if (uploading()) {
                <small class="text-muted">Subiendo imagen…</small>
              }
              @if (form.imagen) {
                <div class="preview mt-2">
                  <img [src]="mediaUrl(form.imagen)" alt="Vista previa flyer"
                       (error)="$any($event.target).classList.add('is-broken')" />
                  @if (editId && brokenIds.has(editId)) {
                    <small class="text-danger d-block mt-1">El archivo no está en el media local. Subí el flyer de nuevo.</small>
                  }
                </div>
              }
            </div>
            <div class="form-group">
              <label>Subtítulo flyer</label>
              <input class="form-control" [(ngModel)]="form.subtitulo" placeholder="Texto corto bajo el título" />
            </div>
            <div class="form-group">
              <label>Link destino</label>
              <input class="form-control" [(ngModel)]="form.urlDestino" placeholder="/catalogo o URL" />
            </div>
            <div class="form-row">
              <div class="form-group col-md-6">
                <label>Inicio</label>
                <input class="form-control" type="date" [(ngModel)]="form.inicio" />
              </div>
              <div class="form-group col-md-6">
                <label>Fin</label>
                <input class="form-control" type="date" [(ngModel)]="form.fin" />
              </div>
            </div>
            <div class="form-group">
              <label>Orden</label>
              <input class="form-control" type="number" [(ngModel)]="form.orden" />
            </div>
            <div class="form-group">
              <label>Artículos de la vitrina</label>
              <input
                class="form-control mb-2"
                [(ngModel)]="productSearch"
                name="psearch"
                placeholder="Buscar por SKU o nombre…"
                (ngModelChange)="searchProducts()"
              />
              @if (productSearching()) {
                <small class="text-muted d-block mb-1">Buscando…</small>
              }
              <div class="product-pick border rounded p-2 mb-2" style="max-height: 180px; overflow: auto">
                @for (p of productHits(); track p.id) {
                  <label class="d-flex align-items-center mb-1" style="gap: 0.4rem; cursor: pointer">
                    <input
                      type="checkbox"
                      [checked]="form.articuloIds.includes(p.id)"
                      (change)="toggleArticulo(p.id, $event)"
                    />
                    <span class="small">#{{ p.id }} · {{ p.sku || '—' }} · {{ p.nombre }}</span>
                  </label>
                } @empty {
                  <span class="text-muted small">Escribí para buscar productos del catálogo.</span>
                }
              </div>
              @if (form.articuloIds.length) {
                <div class="selected-chips mb-1">
                  @for (id of form.articuloIds; track id) {
                    <span class="badge badge-info mr-1 mb-1">
                      #{{ id }}
                      <button type="button" class="btn btn-link btn-xs text-white p-0 ml-1" (click)="removeArticulo(id)">×</button>
                    </span>
                  }
                </div>
                <small class="form-text text-muted">{{ form.articuloIds.length }} artículo(s) seleccionado(s).</small>
              }
            </div>
          </div>
          <div class="card-footer">
            <button class="btn btn-primary" (click)="save()" [disabled]="busy()">
              <i class="fas fa-save mr-1"></i> Guardar
            </button>
            @if (editId) {
              <button class="btn btn-default ml-2" (click)="resetForm()">Cancelar</button>
            }
          </div>
        </div>
      </div>

      <div class="col-lg-7">
        <div class="card">
          <div class="card-header">
            <h3 class="card-title">Listado</h3>
            <div class="card-tools">
              <input class="form-control form-control-sm" style="width:180px"
                     placeholder="Filtrar…" [(ngModel)]="filter" />
            </div>
          </div>
          <div class="card-body pt-2 pb-0">
            <div class="dx-chip-row mb-2 px-1">
              <button type="button" class="dx-chip" [class.on]="tipoFilter === null" (click)="tipoFilter = null">Todas</button>
              @for (t of tipos; track t.value) {
                <button type="button" class="dx-chip" [class.on]="tipoFilter === t.value" (click)="tipoFilter = t.value">
                  {{ t.label }}
                </button>
              }
            </div>
          </div>
          <div class="card-body p-0 table-responsive dx-table-sticky">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th></th>
                  <th>Cod</th>
                  <th>Nombre</th>
                  <th>Tipo</th>
                  <th>Vigencia</th>
                  <th>Arts</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                @for (p of filtered(); track p.id) {
                  <tr class="promo-row" [class.table-secondary]="!p.activo" (click)="edit(p)">
                    <td class="promo-thumb-cell">
                      @if (p.imagen) {
                        <img class="promo-thumb" [src]="mediaUrl(p.imagen)" [alt]="p.nombre"
                             (error)="markBroken($event, p.id)" />
                      } @else {
                        <span class="promo-thumb promo-thumb--empty"><i class="fas fa-image"></i></span>
                      }
                    </td>
                    <td>{{ p.codGrupoArticulo }}</td>
                    <td>
                      <strong>{{ p.nombre }}</strong>
                      @if (!p.activo) {
                        <span class="badge badge-soft-danger ml-1">off</span>
                      }
                      @if (brokenIds.has(p.id)) {
                        <small class="text-danger">Imagen no encontrada — volvé a subir el flyer</small>
                      }
                    </td>
                    <td><span class="badge badge-soft-info">{{ tipoLabel(p.tipoGrupo) }}</span></td>
                    <td>
                      {{ p.fechaInicioVigencia | date: 'dd/MM/yy' }}
                      –
                      {{ p.fechaFinVigencia | date: 'dd/MM/yy' }}
                    </td>
                    <td>{{ p.articulosCount }}</td>
                    <td class="table-actions text-nowrap" (click)="$event.stopPropagation()">
                      <button class="btn btn-xs btn-info" (click)="edit(p)" title="Editar">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button class="btn btn-xs btn-danger" (click)="remove(p)" [disabled]="!p.activo" title="Desactivar">
                        <i class="fas fa-ban"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7">
                      <div class="dx-empty py-4">
                        <i class="fas fa-tags"></i>
                        <p>Sin promociones. Creá una con el formulario.</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .upload-row {
        display: grid;
        grid-template-columns: 1fr auto;
        gap: 0.5rem;
        align-items: center;
      }
      .preview img {
        max-width: 100%;
        max-height: 160px;
        border-radius: 8px;
        border: 1px solid #dee2e6;
        object-fit: cover;
        background: #f8f9fa;
      }
      .promo-thumb-cell {
        width: 72px;
      }
      .promo-thumb {
        width: 64px;
        height: 40px;
        object-fit: cover;
        border-radius: 6px;
        border: 1px solid #e5e7eb;
        background: #f8fafc;
        display: block;
      }
      .promo-thumb.is-broken {
        opacity: 0.3;
        filter: grayscale(1);
      }
      .promo-thumb--empty {
        display: grid;
        place-items: center;
        color: #94a3b8;
        font-size: 0.85rem;
      }
      .preview img.is-broken {
        opacity: 0.35;
        filter: grayscale(1);
      }
    `,
  ],
})
export class PromotionsAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private productSearchTimer?: ReturnType<typeof setTimeout>;

  readonly tipos = TIPO_GRUPO;
  readonly promotions = signal<AdminPromotionListItem[]>([]);
  readonly productHits = signal<AdminProduct[]>([]);
  readonly productSearching = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly busy = signal(false);
  readonly uploading = signal(false);

  filter = '';
  tipoFilter: number | null = null;
  productSearch = '';
  editId: number | null = null;
  form = this.emptyForm();
  readonly brokenIds = new Set<number>();

  ngOnInit(): void {
    this.reload();
  }

  searchProducts(): void {
    if (this.productSearchTimer) clearTimeout(this.productSearchTimer);
    const q = this.productSearch.trim();
    if (q.length < 2) {
      this.productHits.set([]);
      this.productSearching.set(false);
      return;
    }
    this.productSearching.set(true);
    this.productSearchTimer = setTimeout(() => {
      this.api.products({ search: q, pageSize: 40, activoEcommerce: true }).subscribe({
        next: (r) => {
          this.productHits.set(r.items);
          this.productSearching.set(false);
        },
        error: () => {
          this.productHits.set([]);
          this.productSearching.set(false);
        },
      });
    }, 280);
  }

  toggleArticulo(id: number, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) {
      if (!this.form.articuloIds.includes(id)) this.form.articuloIds = [...this.form.articuloIds, id];
    } else {
      this.form.articuloIds = this.form.articuloIds.filter((x) => x !== id);
    }
  }

  removeArticulo(id: number): void {
    this.form.articuloIds = this.form.articuloIds.filter((x) => x !== id);
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || url || '';
  }

  onImageSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.uploading.set(true);
    this.error.set(null);
    this.api.uploadPromotionImage(file, this.editId).subscribe({
      next: (res) => {
        this.form.imagen = res.url;
        this.ok.set('Imagen subida. Guardá la promoción para confirmar el resto de datos.');
        this.uploading.set(false);
        input.value = '';
        if (this.editId) this.reload();
      },
      error: (e) => {
        this.error.set(e?.error?.error || 'No se pudo subir la imagen');
        this.uploading.set(false);
        input.value = '';
      },
    });
  }

  filtered(): AdminPromotionListItem[] {
    const q = this.filter.trim().toLowerCase();
    return this.promotions().filter((p) => {
      if (this.tipoFilter !== null && p.tipoGrupo !== this.tipoFilter) return false;
      if (!q) return true;
      return p.nombre.toLowerCase().includes(q) || this.tipoLabel(p.tipoGrupo).toLowerCase().includes(q);
    });
  }

  markBroken(ev: Event, id: number): void {
    (ev.target as HTMLImageElement).classList.add('is-broken');
    this.brokenIds.add(id);
  }

  tipoLabel(tipo: number): string {
    return TIPO_GRUPO.find((t) => t.value === tipo)?.label ?? String(tipo);
  }

  startCreate(): void {
    this.resetForm();
  }

  edit(p: AdminPromotionListItem): void {
    this.editId = p.id;
    this.api.getPromotion(p.id).subscribe({
      next: (d) => {
        this.form = {
          nombre: d.nombre,
          imagen: d.imagen || '',
          subtitulo: d.subtitulo || '',
          urlDestino: d.urlDestino || '',
          inicio: d.fechaInicioVigencia.slice(0, 10),
          fin: d.fechaFinVigencia.slice(0, 10),
          orden: d.orden,
          tipoGrupo: d.tipoGrupo,
          articuloIds: d.articulos.map((a) => a.idArticulo),
        };
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  resetForm(): void {
    this.editId = null;
    this.form = this.emptyForm();
    this.productSearch = '';
    this.productHits.set([]);
  }

  save(): void {
    this.busy.set(true);
    this.error.set(null);
    this.ok.set(null);
    const body = {
      nombre: this.form.nombre,
      fechaInicioVigencia: this.form.inicio,
      fechaFinVigencia: this.form.fin,
      orden: this.form.orden,
      imagen: this.form.imagen || null,
      subtitulo: this.form.subtitulo || null,
      urlDestino: this.form.urlDestino || null,
      activo: true,
      estado: 1,
      tipoGrupo: this.form.tipoGrupo,
      articuloIds: this.form.articuloIds,
    };
    const req = this.editId
      ? this.api.updatePromotion(this.editId, body)
      : this.api.createPromotion(body);
    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.ok.set('Promoción guardada');
        this.resetForm();
        this.reload();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  remove(p: AdminPromotionListItem): void {
    if (!confirm(`Desactivar "${p.nombre}"?`)) return;
    this.api.deletePromotion(p.id).subscribe({
      next: () => {
        this.ok.set('Promoción desactivada');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  private emptyForm() {
    return {
      nombre: '',
      imagen: '',
      subtitulo: '',
      urlDestino: '/catalogo',
      inicio: new Date().toISOString().slice(0, 10),
      fin: new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10),
      orden: 1,
      tipoGrupo: 3,
      articuloIds: [] as number[],
    };
  }

  private reload(): void {
    this.api.listPromotions().subscribe({
      next: (rows) => this.promotions.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
