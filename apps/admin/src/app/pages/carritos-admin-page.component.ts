import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminApiService, AdminCarritoListItem, PagedResult } from '../core/admin-api.service';

/** Estados PedidoCarritoCompra (paridad ERP EstadoCarrito). */
const ESTADO_CARRITO: Record<number, string> = {
  0: 'Abierto',
  1: 'Cerrado',
  2: 'Anulado',
};

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyPipe, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Carritos de compra</h1>
        <p class="dx-page-sub">Consulta, trazabilidad y exportación · espejo ERP.</p>
      </div>
      <div class="dx-filters">
        <button type="button" class="btn btn-outline-success btn-sm" (click)="exportCsv()"
                title="Exportar pendientes (CSV) y marcar migrado">
          <i class="fas fa-file-csv mr-1"></i> Exportar
        </button>
        <button type="button" class="btn btn-warning btn-sm" (click)="exportAbandonados()"
                title="Carritos abiertos sin actividad 24h+">
          <i class="fas fa-user-clock mr-1"></i> Abandonados
        </button>
      </div>
    </div>

    <div class="dx-filterbar">
      <input class="form-control form-control-sm" type="search" placeholder="Nro / cliente / doc…"
             [(ngModel)]="search" (ngModelChange)="onSearchChange()" (keyup.enter)="applyFilters(1)" />
      <select class="custom-select custom-select-sm" [(ngModel)]="estado" (change)="applyFilters(1)">
        <option value="">Estado: todos</option>
        <option value="0">Abierto</option>
        <option value="1">Cerrado</option>
        <option value="2">Anulado</option>
      </select>
      <select class="custom-select custom-select-sm" [(ngModel)]="migrado" (change)="applyFilters(1)">
        <option value="">Migración: todos</option>
        <option value="false">Pendiente</option>
        <option value="true">Migrado</option>
      </select>
      <input class="form-control form-control-sm" type="date" [(ngModel)]="fechaDesde" (change)="applyFilters(1)" />
      <input class="form-control form-control-sm" type="date" [(ngModel)]="fechaHasta" (change)="applyFilters(1)" />
      <button type="button" class="btn btn-primary btn-sm" (click)="applyFilters(1)">
        <i class="fas fa-search mr-1"></i> Filtrar
      </button>
      @if (search || migrado || estado || fechaDesde || fechaHasta) {
        <button type="button" class="btn btn-link btn-sm" (click)="clearFilters()">Limpiar</button>
      }
    </div>

    @if (selectedCount() > 0) {
      <div class="dx-bulkbar">
        <strong>{{ selectedCount() }}</strong> seleccionado(s)
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="migrarSelected()">
          <i class="fas fa-check mr-1"></i> Marcar migrado
        </button>
        <button type="button" class="btn btn-outline-success btn-sm" (click)="exportCsv()">
          <i class="fas fa-file-csv mr-1"></i> Exportar selección
        </button>
        <button type="button" class="btn btn-link btn-sm ml-auto" (click)="clearSelection()">Quitar selección</button>
      </div>
    }

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

    <div class="card card-outline card-primary">
      <div class="card-header">
        <h3 class="card-title">Carritos</h3>
        @if (page(); as pg) {
          <div class="card-tools"><span class="badge badge-primary">{{ pg.totalCount }}</span></div>
        }
      </div>
      <div class="card-body p-0 table-responsive dx-table-sticky">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th style="width:40px" class="text-center">
                <input type="checkbox" [checked]="allPageSelected()" (change)="toggleAll($event)" />
              </th>
              <th style="width:90px">Nro</th>
              <th style="width:130px">Fecha</th>
              <th>Cliente</th>
              <th style="width:120px">Total</th>
              <th style="width:80px">Ítems</th>
              <th style="width:90px">Estado</th>
              <th style="width:90px">Migrado</th>
              <th style="width:70px"></th>
            </tr>
          </thead>
          <tbody>
            @for (c of page()?.items ?? []; track c.id) {
              <tr [class.is-agotado]="c.estado === 0">
                <td class="text-center">
                  <input type="checkbox" [checked]="selected.has(c.id)"
                         [disabled]="c.migrado || c.estado !== 1"
                         (change)="toggle(c.id, $event)" />
                </td>
                <td><strong>#{{ c.nroPedidoCarritoCompra }}</strong></td>
                <td class="small">{{ c.fechaPedido | date: 'short' }}</td>
                <td>
                  <strong>{{ c.razonSocial }}</strong>
                  <small>{{ c.numeroDocumento }}</small>
                </td>
                <td>{{ c.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                <td class="text-center">{{ c.itemsCount }}</td>
                <td class="text-center">
                  <span
                    class="badge"
                    [class.badge-soft-info]="c.estado === 0"
                    [class.badge-soft-success]="c.estado === 1"
                    [class.badge-soft-danger]="c.estado === 2"
                  >{{ estadoLabel(c.estado) }}</span>
                </td>
                <td class="text-center">
                  @if (c.migrado) {
                    <span class="badge badge-soft-success">Sí</span>
                  } @else {
                    <span class="badge badge-soft-warning">No</span>
                  }
                </td>
                <td class="text-center">
                  <a class="btn btn-xs btn-primary" [routerLink]="['/carritos', c.id]" title="Trazabilidad">
                    <i class="fas fa-route"></i>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="9">
                  <div class="dx-empty py-4">
                    @if (loading()) {
                      <i class="fas fa-spinner fa-spin"></i>
                      <p>Cargando carritos…</p>
                    } @else {
                      <i class="fas fa-shopping-cart"></i>
                      <p>Sin carritos con esos filtros.</p>
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
          <div class="float-left text-muted">{{ pg.totalCount }} carritos · pág. {{ pg.page }} / {{ pg.totalPages || 1 }} · sel. {{ selectedCount() }}</div>
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
export class CarritosAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;

  search = '';
  migrado = '';
  estado = '';
  fechaDesde = '';
  fechaHasta = '';
  readonly selected = new Set<number>();
  readonly selectedCount = signal(0);
  readonly page = signal<PagedResult<AdminCarritoListItem> | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((q) => {
      this.search = q.get('q') || '';
      const migrado = q.get('migrado');
      this.migrado = migrado === 'false' || migrado === 'true' ? migrado : '';
      this.estado = q.get('estado') ?? '';
      this.fechaDesde = q.get('desde') || '';
      this.fechaHasta = q.get('hasta') || '';
      this.load(Math.max(1, Number(q.get('page')) || 1));
    });
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
        migrado: this.migrado || null,
        estado: this.estado === '' ? null : this.estado,
        desde: this.fechaDesde || null,
        hasta: this.fechaHasta || null,
        page: pageNum === 1 ? null : pageNum,
      },
      replaceUrl: true,
    });
  }

  clearFilters(): void {
    this.search = '';
    this.migrado = '';
    this.estado = '';
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.applyFilters(1);
  }

  estadoLabel(estado: number): string {
    return ESTADO_CARRITO[estado] ?? `Estado ${estado}`;
  }

  allPageSelected(): boolean {
    const items = (this.page()?.items ?? []).filter((c) => !c.migrado && c.estado === 1);
    return items.length > 0 && items.every((c) => this.selected.has(c.id));
  }

  toggle(id: number, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) this.selected.add(id);
    else this.selected.delete(id);
    this.selectedCount.set(this.selected.size);
  }

  toggleAll(ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    for (const c of this.page()?.items ?? []) {
      if (c.migrado || c.estado !== 1) continue;
      if (checked) this.selected.add(c.id);
      else this.selected.delete(c.id);
    }
    this.selectedCount.set(this.selected.size);
  }

  clearSelection(): void {
    this.selected.clear();
    this.selectedCount.set(0);
  }

  load(pageNum: number): void {
    this.error.set(null);
    this.loading.set(true);
    const estadoNum = this.estado === '' ? undefined : Number(this.estado);
    this.api
      .listCarritos({
        search: this.search || undefined,
        estado: Number.isFinite(estadoNum as number) ? estadoNum : undefined,
        migrado: this.migrado === '' ? undefined : this.migrado === 'true',
        fechaDesde: this.fechaDesde || undefined,
        fechaHasta: this.fechaHasta || undefined,
        page: pageNum,
        pageSize: 20,
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

  migrarSelected(): void {
    const ids = [...this.selected];
    if (!ids.length) return;
    this.api.migrarCarritos(ids).subscribe({
      next: (r) => {
        this.ok.set(`Migrados: ${r.updatedCount}`);
        this.clearSelection();
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  exportCsv(): void {
    this.error.set(null);
    this.ok.set(null);
    const ids = this.selected.size ? [...this.selected] : undefined;
    this.api.exportarCarritos({ ids, format: 'csv', marcarMigrado: true }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `carritos-export-${Date.now()}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Exportación CSV generada (pendientes marcados migrado).');
        this.clearSelection();
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message || 'Error al exportar'),
    });
  }

  exportAbandonados(): void {
    this.api.exportAbandonados(24).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `carritos-abandonados-${Date.now()}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Export de carritos abandonados (24h+) listo.');
      },
      error: (e) => this.error.set(e?.error?.error || e.message || 'Error abandonados'),
    });
  }
}
