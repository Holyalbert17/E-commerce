import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminApiService, AdminProduct, PagedResult } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { openStorefront } from '../core/storefront-url';

@Component({
  standalone: true,
  imports: [FormsModule, CurrencyPipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Catálogo</h1>
        <p class="dx-page-sub">
          @if (stockLocked()) {
            Alta en el ERP · ecom-etl sincroniza · acá publicás en vitrina.
          } @else {
            Artículos, vitrina y stock.
          }
        </p>
      </div>
      <div class="dx-filters">
        @if (!stockLocked()) {
          <a routerLink="/catalogo/nuevo" class="btn btn-primary btn-sm">
            <i class="fas fa-plus mr-1"></i> Nuevo
          </a>
        }
        <a routerLink="/catalogo/activar" class="btn btn-sm"
           [class.btn-primary]="stockLocked()"
           [class.btn-outline-warning]="!stockLocked()">
          <i class="fas fa-toggle-on mr-1"></i> Activar vitrina
        </a>
        <button type="button" class="btn btn-outline-success btn-sm" (click)="openShop()">
          <i class="fas fa-store mr-1"></i> Ver tienda
        </button>
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="exportArticulos()">
          <i class="fas fa-file-export mr-1"></i> CSV
        </button>
        <label class="btn btn-outline-info btn-sm mb-0">
          <i class="fas fa-file-import mr-1"></i> Import
          <input type="file" accept=".csv,text/csv" hidden (change)="importArticulos($event)" />
        </label>
      </div>
    </div>

    <div class="dx-filterbar">
      <input class="form-control form-control-sm" type="search" placeholder="Buscar SKU / nombre…"
             [(ngModel)]="search" (ngModelChange)="onSearchChange()" (keyup.enter)="applyFilters(1)" />
      <select class="custom-select custom-select-sm" [(ngModel)]="vitrina" (change)="applyFilters(1)">
        <option value="">Vitrina: todos</option>
        <option value="on">En vitrina</option>
        <option value="off">Fuera de vitrina</option>
      </select>
      <select class="custom-select custom-select-sm" [(ngModel)]="estado" (change)="applyFilters(1)">
        <option value="">Todos los estados</option>
        <option value="activo">Activo</option>
        <option value="agotado">Agotado</option>
        <option value="borrador">Borrador</option>
        <option value="oculto">Oculto</option>
      </select>
      <select class="custom-select custom-select-sm" [(ngModel)]="calidad" (change)="applyFilters(1)">
        <option value="">Calidad: todos</option>
        <option value="sin_precio">Sin precio B2C</option>
        <option value="sin_stock">Sin stock</option>
      </select>
      <select class="custom-select custom-select-sm" style="max-width:110px" [(ngModel)]="pageSize" (change)="applyFilters(1)">
        <option [ngValue]="15">15 / pág</option>
        <option [ngValue]="30">30 / pág</option>
        <option [ngValue]="50">50 / pág</option>
      </select>
      <button type="button" class="btn btn-primary btn-sm" (click)="applyFilters(1)">
        <i class="fas fa-search mr-1"></i> Filtrar
      </button>
      @if (hasFilters()) {
        <button type="button" class="btn btn-link btn-sm" (click)="clearFilters()">Limpiar</button>
      }
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        {{ error() }}
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
      </div>
    }
    @if (ok()) {
      <div class="alert alert-success alert-dismissible">
        {{ ok() }}
        <button type="button" class="close" (click)="ok.set(null)"><span>&times;</span></button>
      </div>
    }

    @if (stockLocked()) {
      <div class="callout callout-info">
        <h5><i class="fas fa-lock mr-1"></i> Catálogo espejo del ERP</h5>
        <p class="mb-1">
          El artículo se da de alta en el <strong>ERP</strong> (Stock → Artículos o Site → ECommerce).
          <strong>ecom-etl</strong> trae ficha, stock y precios. Acá solo publicás en vitrina, imágenes y SEO.
          Estado ETL: <a routerLink="/sync">Sync</a>.
        </p>
      </div>
    }
    @if (calidad) {
      <div class="callout callout-warning py-2">
        Filtro calidad <strong>{{ calidad }}</strong> en servidor
        @if (calidad === 'sin_precio') { (hasta 3000 candidatos) }.
        Totales y paginación correctos.
      </div>
    }

    <div class="card card-outline card-primary">
      <div class="card-header">
        <h3 class="card-title">Productos</h3>
        @if (page(); as pg) {
          <div class="card-tools">
            <span class="badge badge-soft-info">{{ pg.totalCount }} ítems</span>
          </div>
        }
      </div>
      <div class="card-body p-0 table-responsive dx-table-sticky">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th style="width:110px">SKU</th>
              <th>Producto</th>
              <th style="width:90px">Origen</th>
              <th style="width:120px">Precio B2C</th>
              <th style="width:130px">Stock</th>
              <th style="width:130px">Estado</th>
              <th style="width:100px">Vitrina</th>
              <th style="width:110px"></th>
              <th style="width:70px">ID</th>
            </tr>
          </thead>
          <tbody>
            @for (p of page()?.items ?? []; track p.id) {
              <tr [class.is-agotado]="p.estadoEcommerce === 'agotado' || p.stockExistencia <= 0"
                  [class.is-oculto]="p.estadoEcommerce === 'oculto'">
                <td><code>{{ p.sku || '—' }}</code></td>
                <td>
                  <a [routerLink]="['/catalogo', p.id, 'editar']"><strong>{{ p.nombre }}</strong></a>
                  <small>{{ p.marca || 'Sin marca' }}</small>
                  @if (p.slugSeo) {
                    <div><small class="text-muted">/{{ p.slugSeo }}</small></div>
                  }
                </td>
                <td>
                  @if (p.origenDatos === 'Erp') {
                    <span class="badge badge-info" [title]="p.isReadOnly ? 'Stock espejo ERP — el artículo sí se edita acá o en Site' : 'Sincronizado ERP'">
                      ERP{{ p.isReadOnly ? ' · stock 🔒' : '' }}
                    </span>
                  } @else {
                    <span class="badge badge-secondary">Local</span>
                  }
                </td>
                <td>
                  @if (p.precioDesdeB2C != null) {
                    {{ p.precioDesdeB2C | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                  } @else {
                    <span class="text-muted">—</span>
                  }
                </td>
                <td>
                  @if (p.isReadOnly) {
                    <span class="badge" [class.badge-soft-info]="p.stockExistencia > 0"
                          [class.badge-soft-danger]="p.stockExistencia <= 0"
                          title="Espejo ERP — no editable">
                      {{ p.stockExistencia }}
                    </span>
                    <a class="btn btn-xs btn-link p-0 ml-1"
                       [routerLink]="['/catalogo', p.id, 'movimientos']"
                       title="Movimientos / historial">
                      <i class="fas fa-history"></i>
                    </a>
                  } @else {
                    <div class="input-group input-group-sm input-group-stock">
                      <input
                        class="form-control"
                        type="number"
                        min="0"
                        [ngModel]="p.stockExistencia"
                        (change)="onStock(p, $event)"
                      />
                    </div>
                  }
                </td>
                <td>
                  <select class="custom-select custom-select-sm"
                          [ngModel]="p.estadoEcommerce"
                          (ngModelChange)="onEstado(p, $event)">
                    <option value="borrador">borrador</option>
                    <option value="activo">activo</option>
                    <option value="agotado">agotado</option>
                    <option value="oculto">oculto</option>
                  </select>
                </td>
                <td>
                  <div class="custom-control custom-switch">
                    <input
                      type="checkbox"
                      class="custom-control-input"
                      [id]="'ecom-' + p.id"
                      [ngModel]="p.activoEcommerce"
                      (ngModelChange)="onEcom(p, $event)"
                    />
                    <label class="custom-control-label" [attr.for]="'ecom-' + p.id"></label>
                  </div>
                </td>
                <td>
                  <a class="btn btn-xs btn-outline-primary" [routerLink]="['/catalogo', p.id, 'editar']"
                     title="Editar">
                    <i class="fas fa-edit"></i>
                  </a>
                  <a class="btn btn-xs btn-outline-primary ml-1" [routerLink]="['/catalogo', p.id, 'imagenes']"
                     title="Imágenes">
                    <i class="fas fa-images"></i>
                  </a>
                  <a class="btn btn-xs btn-outline-secondary ml-1" [routerLink]="['/catalogo', p.id, 'embalajes']"
                     title="Embalajes / UOM">
                    <i class="fas fa-ruler-combined"></i>
                  </a>
                  <a class="btn btn-xs btn-outline-info ml-1" [routerLink]="['/catalogo', p.id, 'movimientos']"
                     title="Movimientos de stock">
                    <i class="fas fa-exchange-alt"></i>
                  </a>
                </td>
                <td class="text-muted">#{{ p.id }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="9">
                  <div class="dx-empty py-4">
                    @if (loading()) {
                      <i class="fas fa-spinner fa-spin"></i>
                      <p>Cargando productos…</p>
                    } @else {
                      <i class="fas fa-box"></i>
                      <p>Sin productos con esos filtros.</p>
                      <div class="mt-2">
                        <a routerLink="/sync" class="btn btn-sm btn-outline-primary mr-1">Ver estado ETL</a>
                        <a routerLink="/catalogo/activar" class="btn btn-sm btn-primary">Activar vitrina</a>
                        @if (!stockLocked()) {
                          <a routerLink="/catalogo/nuevo" class="btn btn-sm btn-outline-secondary ml-1">Nuevo artículo</a>
                        }
                      </div>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      @if (page(); as pg) {
        <div class="card-footer clearfix">
          <div class="float-left text-muted">{{ pg.totalCount }} ítems · pág. {{ pg.page }} / {{ pg.totalPages || 1 }}</div>
          <div class="float-right">
            <button type="button" class="btn btn-default btn-sm mr-1"
                    [disabled]="pg.page <= 1" (click)="applyFilters(pg.page - 1)">Anterior</button>
            <button type="button" class="btn btn-default btn-sm"
                    [disabled]="pg.page >= pg.totalPages" (click)="applyFilters(pg.page + 1)">Siguiente</button>
          </div>
        </div>
      }
    </div>
  `,
})
export class CatalogAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;

  search = '';
  estado = '';
  calidad: '' | 'sin_precio' | 'sin_stock' = '';
  vitrina: '' | 'on' | 'off' = '';
  pageSize = 15;
  readonly page = signal<PagedResult<AdminProduct> | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly stockLocked = signal(false);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.api.getEmpresaConfig().subscribe({
      next: (c) => this.stockLocked.set(c?.modoIntegracion === 'Integrated'),
      error: () => undefined,
    });
    this.route.queryParamMap.subscribe((q) => {
      this.search = q.get('q') || '';
      this.estado = q.get('estado') || '';
      const v = q.get('vitrina');
      this.vitrina = v === 'on' || v === 'off' ? v : '';
      const c = q.get('calidad');
      this.calidad = c === 'sin_precio' || c === 'sin_stock' ? c : '';
      const ps = Number(q.get('ps'));
      this.pageSize = ps === 30 || ps === 50 ? ps : 15;
      this.load(Math.max(1, Number(q.get('page')) || 1));
    });
  }

  hasFilters(): boolean {
    return !!(this.search || this.estado || this.calidad || this.vitrina);
  }

  onSearchChange(): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.applyFilters(1), 400);
  }

  applyFilters(pageNum: number): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: this.search.trim() || null,
        estado: this.estado || null,
        vitrina: this.vitrina || null,
        calidad: this.calidad || null,
        ps: this.pageSize === 15 ? null : this.pageSize,
        page: pageNum === 1 ? null : pageNum,
      },
      replaceUrl: true,
    });
  }

  clearFilters(): void {
    this.search = '';
    this.estado = '';
    this.calidad = '';
    this.vitrina = '';
    this.applyFilters(1);
  }

  openShop(): void {
    openStorefront(this.session.tenantSlug());
  }

  load(pageNum: number): void {
    this.error.set(null);
    this.loading.set(true);
    const activoEcommerce =
      this.vitrina === 'on' ? true : this.vitrina === 'off' ? false : undefined;
    this.api
      .products({
        search: this.search || undefined,
        estado: this.estado || undefined,
        activoEcommerce,
        calidad: this.calidad || undefined,
        page: pageNum,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (r) => {
          this.loading.set(false);
          this.page.set(r);
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set(e?.error?.error || e.message);
        },
      });
  }

  onEcom(p: AdminProduct, value: boolean): void {
    this.patch(p.id, { activoEcommerce: value });
  }

  onEstado(p: AdminProduct, value: string): void {
    this.patch(p.id, { estadoEcommerce: value });
  }

  onStock(p: AdminProduct, ev: Event): void {
    if (p.isReadOnly || this.stockLocked()) {
      this.error.set('Stock solo lectura en Integrated — lo manda el depósito ERP. El artículo sí se edita acá o en Site.');
      return;
    }
    const value = Number((ev.target as HTMLInputElement).value);
    this.ok.set(null);
    this.api.updateStock(p.id, value).subscribe({
      next: () => {
        this.ok.set(`Stock actualizado: ${p.nombre}`);
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  private patch(id: number, body: { activoEcommerce?: boolean; estadoEcommerce?: string }): void {
    this.ok.set(null);
    this.api.updateProduct(id, body).subscribe({
      next: () => {
        this.ok.set('Producto actualizado');
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  exportArticulos(): void {
    this.api.exportPlanilla('articulos').subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `articulos-${Date.now()}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Planilla artículos exportada');
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  importArticulos(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.importPlanilla('articulos', file).subscribe({
      next: (r) => {
        this.ok.set(`Import artículos: ${JSON.stringify(r)}`);
        this.load(1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
