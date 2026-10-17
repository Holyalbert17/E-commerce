import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApiService, AdminProduct, PagedResult } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { openStorefront } from '../core/storefront-url';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Activar / inactivar</h1>
        <p class="dx-page-sub">
          Alta en el ERP → ecom-etl sincroniza → acá publicás en la tienda.
          Filtrá “Fuera de vitrina”, seleccioná la página y publicá.
        </p>
      </div>
      <div class="dx-filters">
        <input class="form-control form-control-sm" type="search" placeholder="Buscar…"
               [(ngModel)]="search" (keyup.enter)="load(1)" />
        <select class="custom-select custom-select-sm" [(ngModel)]="vitrina" (change)="load(1)">
          <option value="">Vitrina: todos</option>
          <option value="off">Fuera de vitrina</option>
          <option value="on">En vitrina</option>
        </select>
        <select class="custom-select custom-select-sm" [(ngModel)]="estado" (change)="load(1)">
          <option value="">Estado: todos</option>
          <option value="activo">activo</option>
          <option value="oculto">oculto</option>
          <option value="agotado">agotado</option>
          <option value="borrador">borrador</option>
        </select>
        <button type="button" class="btn btn-default btn-sm" (click)="load(1)">
          <i class="fas fa-search"></i>
        </button>
        <button type="button" class="btn btn-success btn-sm"
                [disabled]="selected.size === 0" (click)="bulk(true)">
          <i class="fas fa-check mr-1"></i> Publicar ({{ selected.size }})
        </button>
        <button type="button" class="btn btn-outline-danger btn-sm"
                [disabled]="selected.size === 0" (click)="bulk(false)">
          <i class="fas fa-ban mr-1"></i> Ocultar
        </button>
        <button type="button" class="btn btn-outline-success btn-sm" (click)="openShop()">
          <i class="fas fa-store"></i>
        </button>
        <a routerLink="/catalogo" class="btn btn-outline-secondary btn-sm">Catálogo</a>
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
        <button type="button" class="btn btn-sm btn-success ml-2" (click)="openShop()">Ver tienda</button>
      </div>
    }

    <div class="card card-outline card-primary">
      <div class="card-header">
        <h3 class="card-title">Productos</h3>
        <div class="card-tools">
          <button type="button" class="btn btn-tool" (click)="toggleAll()">
            {{ allSelected() ? 'Quitar página' : 'Seleccionar página' }}
          </button>
        </div>
      </div>
      <div class="card-body p-0 table-responsive">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th style="width:40px"></th>
              <th>SKU</th>
              <th>Producto</th>
              <th>Estado</th>
              <th>Vitrina</th>
            </tr>
          </thead>
          <tbody>
            @for (p of page()?.items ?? []; track p.id) {
              <tr>
                <td>
                  <input type="checkbox" [checked]="selected.has(p.id)" (change)="toggle(p.id, $event)" />
                </td>
                <td><code>{{ p.sku || '—' }}</code></td>
                <td>{{ p.nombre }}</td>
                <td><span class="badge badge-soft-info">{{ p.estadoEcommerce }}</span></td>
                <td>
                  @if (p.activoEcommerce) {
                    <span class="badge badge-soft-success">ON</span>
                  } @else {
                    <span class="badge badge-soft-danger">OFF</span>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5">
                  <div class="dx-empty py-4">
                    <i class="fas fa-toggle-on"></i>
                    <p>Sin productos con esos filtros.</p>
                    <a routerLink="/sync" class="btn btn-sm btn-outline-primary mt-2">Ver estado ETL</a>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      @if (page(); as pg) {
        <div class="card-footer clearfix">
          <div class="float-left text-muted">Pág. {{ pg.page }} / {{ pg.totalPages || 1 }} · sel. {{ selected.size }}</div>
          <div class="float-right">
            <button type="button" class="btn btn-default btn-sm mr-1"
                    [disabled]="pg.page <= 1" (click)="load(pg.page - 1)">Anterior</button>
            <button type="button" class="btn btn-default btn-sm"
                    [disabled]="pg.page >= pg.totalPages" (click)="load(pg.page + 1)">Siguiente</button>
          </div>
        </div>
      }
    </div>
  `,
})
export class ProductActivatePageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);

  search = '';
  estado = '';
  vitrina: '' | 'on' | 'off' = 'off';
  selected = new Set<number>();
  readonly page = signal<PagedResult<AdminProduct> | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);

  ngOnInit(): void {
    this.load(1);
  }

  openShop(): void {
    openStorefront(this.session.tenantSlug());
  }

  allSelected(): boolean {
    const items = this.page()?.items ?? [];
    return items.length > 0 && items.every((p) => this.selected.has(p.id));
  }

  toggle(id: number, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) this.selected.add(id);
    else this.selected.delete(id);
  }

  toggleAll(): void {
    const items = this.page()?.items ?? [];
    if (this.allSelected()) {
      for (const p of items) this.selected.delete(p.id);
    } else {
      for (const p of items) this.selected.add(p.id);
    }
  }

  load(pageNum: number): void {
    this.error.set(null);
    const activoEcommerce =
      this.vitrina === 'on' ? true : this.vitrina === 'off' ? false : undefined;
    this.api
      .products({
        search: this.search || undefined,
        estado: this.estado || undefined,
        activoEcommerce,
        page: pageNum,
        pageSize: 25,
      })
      .subscribe({
        next: (r) => this.page.set(r),
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  bulk(activo: boolean): void {
    const ids = [...this.selected];
    if (ids.length === 0) return;
    this.api.bulkActivoEcommerce({
      ids,
      activoEcommerce: activo,
      estadoEcommerce: activo ? 'activo' : 'oculto',
    }).subscribe({
      next: (r) => {
        this.ok.set(
          `${activo ? 'Publicados' : 'Ocultados'}: ${r.updatedCount} productos. Abrí la tienda para verificar.`,
        );
        this.selected.clear();
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
