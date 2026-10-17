import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, concatMap, from, of, toArray } from 'rxjs';
import { AdminApiService, AdminProduct, AdminProductImage } from '../core/admin-api.service';

const ACCEPT = 'image/jpeg,image/png,image/webp,image/gif,image/avif';
const MAX_MB = 20;

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Galería del producto</h1>
        <p class="dx-page-sub">
          @if (product(); as p) {
            <strong>{{ p.nombre }}</strong>
            <code class="ml-2">{{ p.sku || '#' + p.id }}</code>
            <span class="text-muted ml-2">· calidad original · JPEG/PNG/WebP/AVIF hasta {{ maxMb }} MB</span>
          } @else {
            Gestión profesional de imágenes (upload + URL)
          }
        </p>
      </div>
      <a routerLink="/catalogo" class="btn btn-outline-secondary btn-sm">
        <i class="fas fa-arrow-left mr-1"></i> Catálogo
      </a>
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
      <div class="col-lg-5 mb-3">
        <div
          class="img-dropzone"
          [class.img-dropzone--active]="dragging()"
          [class.img-dropzone--busy]="uploading()"
          (dragover)="onDragOver($event)"
          (dragleave)="dragging.set(false)"
          (drop)="onDrop($event)"
          (click)="fileInput.click()"
        >
          <input
            #fileInput
            type="file"
            class="d-none"
            [accept]="accept"
            multiple
            (change)="onFilesSelected($event)"
          />
          <div class="img-dropzone__icon"><i class="fas fa-cloud-upload-alt"></i></div>
          <h3 class="img-dropzone__title">
            {{ uploading() ? 'Subiendo…' : 'Arrastrá imágenes o hacé clic' }}
          </h3>
          <p class="img-dropzone__hint">
            Se guarda el archivo <strong>sin recomprimir</strong> (máxima calidad).
            Varias a la vez.
          </p>
          @if (uploadProgress()) {
            <div class="progress mt-2" style="height:6px;max-width:280px;margin:0 auto">
              <div class="progress-bar bg-teal" [style.width.%]="uploadProgress()"></div>
            </div>
          }
        </div>

        <div class="card card-outline card-secondary mt-3">
          <div class="card-header py-2">
            <h3 class="card-title mb-0" style="font-size:0.95rem">Agregar por URL externa</h3>
          </div>
          <div class="card-body py-2">
            <div class="form-row">
              <div class="col-12 form-group mb-2">
                <input class="form-control form-control-sm" [(ngModel)]="urlForm.url"
                       placeholder="https://cdn…/foto-hires.jpg" />
              </div>
              <div class="col-8 form-group mb-2">
                <input class="form-control form-control-sm" [(ngModel)]="urlForm.titulo" placeholder="Título (opcional)" />
              </div>
              <div class="col-4 form-group mb-2">
                <div class="custom-control custom-checkbox mt-1">
                  <input type="checkbox" class="custom-control-input" id="urlPrincipal"
                         [(ngModel)]="urlForm.esPrincipal" />
                  <label class="custom-control-label" for="urlPrincipal">Principal</label>
                </div>
              </div>
              <div class="col-12">
                <button type="button" class="btn btn-sm btn-outline-primary" (click)="addUrl()" [disabled]="busy()">
                  <i class="fas fa-link mr-1"></i> Agregar URL
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="col-lg-7 mb-3">
        <div class="d-flex justify-content-between align-items-center mb-2">
          <h3 class="h6 mb-0 text-muted">
            {{ images().length }} imagen{{ images().length === 1 ? '' : 'es' }}
            · la primera del orden es la vitrina si no hay principal
          </h3>
          @if (images().length > 1) {
            <span class="small text-muted">Usá ↑ ↓ para reordenar</span>
          }
        </div>

        @if (previewUrl(); as prev) {
          <div class="img-lightbox mb-3" (click)="previewUrl.set(null)">
            <img [src]="prev" alt="Vista previa" />
            <button type="button" class="btn btn-sm btn-light img-lightbox__close"
                    (click)="previewUrl.set(null); $event.stopPropagation()">
              <i class="fas fa-times"></i>
            </button>
          </div>
        }

        <div class="img-gallery">
          @for (img of images(); track img.id; let i = $index) {
            <article class="img-tile" [class.img-tile--principal]="img.esPrincipal">
              <button type="button" class="img-tile__thumb" (click)="previewUrl.set(mediaUrl(img.url))">
                <img [src]="mediaUrl(img.url)" alt="" loading="lazy"
                     (error)="$any($event.target).classList.add('is-broken')" />
                @if (img.esPrincipal) {
                  <span class="img-tile__badge"><i class="fas fa-star"></i> Principal</span>
                }
              </button>
              <div class="img-tile__meta">
                <input class="form-control form-control-sm" [(ngModel)]="draft[img.id].titulo"
                       placeholder="Título / alt" />
                <div class="img-tile__actions">
                  <button type="button" class="btn btn-xs btn-outline-secondary" title="Subir"
                          [disabled]="i === 0" (click)="move(i, -1)">
                    <i class="fas fa-arrow-up"></i>
                  </button>
                  <button type="button" class="btn btn-xs btn-outline-secondary" title="Bajar"
                          [disabled]="i === images().length - 1" (click)="move(i, 1)">
                    <i class="fas fa-arrow-down"></i>
                  </button>
                  <button type="button" class="btn btn-xs btn-outline-warning" title="Marcar principal"
                          [disabled]="img.esPrincipal" (click)="setPrincipal(img)">
                    <i class="fas fa-star"></i>
                  </button>
                  <button type="button" class="btn btn-xs btn-primary" title="Guardar título"
                          (click)="saveMeta(img)">
                    <i class="fas fa-save"></i>
                  </button>
                  <button type="button" class="btn btn-xs btn-outline-danger" title="Eliminar"
                          (click)="remove(img)">
                    <i class="fas fa-trash"></i>
                  </button>
                </div>
                <a class="small text-muted text-truncate d-block" [href]="mediaUrl(img.url)" target="_blank" rel="noopener">
                  {{ img.url }}
                </a>
              </div>
            </article>
          } @empty {
            <div class="dx-empty py-4">
              <i class="fas fa-images"></i>
              <p>Todavía no hay imágenes. Subí fotos de alta resolución o pegá una URL de CDN.</p>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class ProductImagesPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  readonly accept = ACCEPT;
  readonly maxMb = MAX_MB;

  productId = 0;
  readonly product = signal<AdminProduct | null>(null);
  readonly images = signal<AdminProductImage[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly dragging = signal(false);
  readonly uploading = signal(false);
  readonly uploadProgress = signal(0);
  readonly busy = signal(false);
  readonly previewUrl = signal<string | null>(null);

  urlForm = { url: '', titulo: '', esPrincipal: false };
  draft: Record<number, { titulo: string }> = {};

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || url || '';
  }

  ngOnInit(): void {
    this.productId = Number(this.route.snapshot.paramMap.get('id'));
    this.reload();
  }

  reload(): void {
    this.error.set(null);
    this.api.product(this.productId).subscribe({
      next: (p) => this.product.set(p),
      error: () => this.product.set(null),
    });
    this.api.listProductImages(this.productId).subscribe({
      next: (rows) => {
        this.images.set(rows);
        this.draft = {};
        for (const img of rows) this.draft[img.id] = { titulo: img.titulo ?? '' };
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  onDragOver(ev: DragEvent): void {
    ev.preventDefault();
    this.dragging.set(true);
  }

  onDrop(ev: DragEvent): void {
    ev.preventDefault();
    this.dragging.set(false);
    const files = ev.dataTransfer?.files;
    if (files?.length) this.uploadFiles([...files]);
  }

  onFilesSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const files = input.files ? [...input.files] : [];
    input.value = '';
    if (files.length) this.uploadFiles(files);
  }

  uploadFiles(files: File[]): void {
    const valid: File[] = [];
    for (const f of files) {
      if (!f.type.startsWith('image/')) {
        this.error.set(`"${f.name}" no es una imagen.`);
        continue;
      }
      if (f.size > MAX_MB * 1024 * 1024) {
        this.error.set(`"${f.name}" supera ${MAX_MB} MB.`);
        continue;
      }
      valid.push(f);
    }
    if (!valid.length) return;

    this.uploading.set(true);
    this.uploadProgress.set(0);
    this.error.set(null);
    let done = 0;

    from(valid)
      .pipe(
        concatMap((file, idx) =>
          this.api.uploadProductImage(this.productId, file, {
            esPrincipal: this.images().length === 0 && idx === 0,
          }).pipe(
            catchError((e) => {
              this.error.set(e?.error?.error || e.message || `Error en ${file.name}`);
              return of(null);
            }),
          ),
        ),
        toArray(),
      )
      .subscribe({
        next: (results) => {
          done = results.filter(Boolean).length;
          this.uploadProgress.set(100);
          this.uploading.set(false);
          if (done > 0) {
            this.ok.set(`${done} imagen${done === 1 ? '' : 'es'} subida${done === 1 ? '' : 's'} en calidad original`);
            this.reload();
          }
        },
        error: (e) => {
          this.uploading.set(false);
          this.error.set(e?.error?.error || e.message);
        },
      });
  }

  addUrl(): void {
    if (!this.urlForm.url.trim()) {
      this.error.set('URL requerida');
      return;
    }
    this.busy.set(true);
    this.api
      .createProductImage(this.productId, {
        url: this.urlForm.url.trim(),
        titulo: this.urlForm.titulo || null,
        orden: 0,
        esPrincipal: this.urlForm.esPrincipal,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.ok.set('Imagen por URL agregada');
          this.urlForm = { url: '', titulo: '', esPrincipal: false };
          this.reload();
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set(e?.error?.error || e.message);
        },
      });
  }

  setPrincipal(img: AdminProductImage): void {
    this.api.setPrincipalProductImage(this.productId, img.id).subscribe({
      next: () => {
        this.ok.set('Imagen principal actualizada');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  saveMeta(img: AdminProductImage): void {
    const titulo = this.draft[img.id]?.titulo ?? '';
    this.api
      .updateProductImage(this.productId, img.id, {
        url: img.url,
        titulo: titulo || null,
        orden: img.orden,
        esPrincipal: img.esPrincipal,
        activo: true,
      })
      .subscribe({
        next: () => {
          this.ok.set('Metadatos guardados');
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  move(index: number, delta: number): void {
    const list = [...this.images()];
    const j = index + delta;
    if (j < 0 || j >= list.length) return;
    [list[index], list[j]] = [list[j], list[index]];
    const ids = list.map((x) => x.id);
    this.images.set(list);
    this.api.reorderProductImages(this.productId, ids).subscribe({
      next: () => this.ok.set('Orden actualizado'),
      error: (e) => {
        this.error.set(e?.error?.error || e.message);
        this.reload();
      },
    });
  }

  remove(img: AdminProductImage): void {
    if (!confirm('¿Eliminar esta imagen? Si es archivo local, también se borra del disco.')) return;
    this.api.deleteProductImage(this.productId, img.id).subscribe({
      next: () => {
        this.ok.set('Imagen eliminada');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
