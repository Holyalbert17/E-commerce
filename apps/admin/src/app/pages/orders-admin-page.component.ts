import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { AdminApiService, AdminOrderListItem, PagedResult } from '../core/admin-api.service';

interface MigracionLogRow {
  id: number;
  idPedidoMovil: number;
  nroPedido: number;
  estado: string;
  mensajeError?: string | null;
  detalleTecnico?: string | null;
  intentos: number;
  fechaIntento: string;
}

@Component({
  standalone: true,
  imports: [RouterLink, FormsModule, CurrencyPipe, DatePipe, NgClass],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Pedidos</h1>
        <p class="dx-page-sub">
          Fulfillment, factura fiscal y write-back ERP.
          <span class="text-muted">«Enviar a ERP» escribe en Postgres; «Marcar local» solo cambia el flag.</span>
        </p>
      </div>
      <div class="dx-filters">
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="exportCsv()">
          <i class="fas fa-file-csv mr-1"></i> CSV
        </button>
        <button type="button" class="btn btn-outline-info btn-sm" (click)="toggleLogs()">
          <i class="fas fa-history mr-1"></i> Logs ERP
        </button>
      </div>
    </div>

    <div class="dx-filterbar">
      <input class="form-control form-control-sm" type="search" placeholder="N° / cliente…"
             [(ngModel)]="search" (ngModelChange)="onSearchChange()" (keyup.enter)="applyFilters(1)" />
      <select class="custom-select custom-select-sm" [(ngModel)]="migradoFilter" (change)="applyFilters(1)">
        <option value="">ERP: todos</option>
        <option value="false">Pendientes ERP</option>
        <option value="true">Ya en ERP</option>
      </select>
      <select class="custom-select custom-select-sm" [(ngModel)]="fulfillmentFilter" (change)="applyFilters(1)">
        <option value="">Fulfillment: todos</option>
        <option value="compra_realizada">Compra</option>
        <option value="preparando">Preparando</option>
        <option value="preparado">Preparado</option>
        <option value="facturado">Facturado</option>
        <option value="en_transito">En tránsito</option>
        <option value="demorado">Demorado</option>
        <option value="excepcion">Excepción</option>
        <option value="entregado">Entregado</option>
        <option value="cancelado">Cancelado</option>
      </select>
      <button type="button" class="btn btn-primary btn-sm" (click)="applyFilters(1)">
        <i class="fas fa-search mr-1"></i> Filtrar
      </button>
      @if (search || migradoFilter || fulfillmentFilter) {
        <button type="button" class="btn btn-link btn-sm" (click)="clearFilters()">Limpiar</button>
      }
    </div>

    @if (selectedCount() > 0) {
      <div class="dx-bulkbar">
        <strong>{{ selectedCount() }}</strong> seleccionado(s)
        <select class="custom-select custom-select-sm" style="max-width:180px" [(ngModel)]="bulkEstado">
          <option value="">Cambiar seguimiento…</option>
          <option value="preparando">→ Preparando</option>
          <option value="preparado">→ Preparado</option>
          <option value="facturado">→ Marcado facturado</option>
          <option value="en_transito">→ En tránsito</option>
          <option value="demorado">→ Demorado</option>
          <option value="entregado">→ Entregado</option>
          <option value="cancelado">→ Cancelado</option>
        </select>
        <button type="button" class="btn btn-outline-primary btn-sm"
                [disabled]="!bulkEstado || bulkBusy()" (click)="aplicarBulkFulfillment()">
          Aplicar
        </button>
        <button type="button" class="btn btn-success btn-sm"
                [disabled]="bulkBusy()" (click)="facturarSelected()">
          <i class="fas fa-file-invoice mr-1"></i> Facturar
        </button>
        <button type="button" class="btn btn-primary btn-sm"
                [disabled]="bulkBusy()" (click)="migrarErp()">
          <i class="fas fa-database mr-1"></i> Enviar a ERP
        </button>
        <button type="button" class="btn btn-outline-warning btn-sm" (click)="migrarSelected()"
                title="Solo marca Migrado=true en e-com. No INSERT en ERP.">
          <i class="fas fa-flag mr-1"></i> Solo marcar local
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

    @if (showLogs()) {
      <div class="card card-outline card-info mb-3">
        <div class="card-header">
          <h3 class="card-title">Logs write-back ERP</h3>
          <div class="card-tools">
            <button type="button" class="btn btn-tool" (click)="loadLogs()" [disabled]="logsBusy()">
              <i class="fas fa-sync-alt" [class.fa-spin]="logsBusy()"></i>
            </button>
            <button type="button" class="btn btn-tool" (click)="showLogs.set(false)"><i class="fas fa-times"></i></button>
          </div>
        </div>
        <div class="card-body p-0 table-responsive" style="max-height:320px;overflow:auto">
          <table class="table table-sm table-hover mb-0">
            <thead>
              <tr>
                <th>Pedido</th>
                <th>Estado</th>
                <th>Intentos</th>
                <th>Fecha</th>
                <th>Detalle</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (l of logs(); track l.id) {
                <tr [class.table-danger]="l.estado==='error'">
                  <td><strong>#{{ l.nroPedido }}</strong></td>
                  <td>
                    <span class="badge"
                          [class.badge-success]="l.estado==='ok'"
                          [class.badge-danger]="l.estado==='error'"
                          [class.badge-secondary]="l.estado!=='ok' && l.estado!=='error'">
                      {{ l.estado }}
                    </span>
                  </td>
                  <td>{{ l.intentos }}</td>
                  <td class="small text-muted">{{ l.fechaIntento | date:'short' }}</td>
                  <td class="small">{{ l.mensajeError || l.detalleTecnico || '—' }}</td>
                  <td>
                    @if (l.estado==='error') {
                      <button type="button" class="btn btn-xs btn-outline-primary"
                              (click)="reintentarUno(l.idPedidoMovil)">Reintentar</button>
                    }
                    <a class="btn btn-xs btn-link" [routerLink]="['/pedidos', l.idPedidoMovil]">Ver</a>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6" class="text-center text-muted py-3">Sin logs aún.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }

    <div class="card card-outline card-success">
      <div class="card-body p-0 table-responsive dx-table-sticky">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th style="width:40px" class="text-center">
                <input type="checkbox" [checked]="allPageSelected()" (change)="toggleAll($event)" />
              </th>
              <th>N°</th>
              <th>Fecha</th>
              <th>Cliente</th>
              <th>Entrega</th>
              <th>Estado</th>
              <th>Factura</th>
              <th>Total</th>
              <th>ERP</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (o of page()?.items ?? []; track o.idPedidoMovil) {
              <tr>
                <td class="text-center">
                  <input type="checkbox" [checked]="selected.has(o.idPedidoMovil)"
                         (change)="toggle(o.idPedidoMovil, $event)" />
                </td>
                <td><strong>{{ o.nroPedido }}</strong></td>
                <td>{{ o.fechaPedido | date: 'short' }}</td>
                <td>
                  {{ o.razonSocial }}
                  <small>{{ o.numeroDocumento }} · {{ o.itemsCount }} ítems</small>
                </td>
                <td>
                  @if (o.tipoEntrega === 0) {
                    <span class="badge badge-soft-info">Delivery</span>
                  } @else {
                    <span class="badge badge-soft-warning">Pickup</span>
                  }
                </td>
                <td>
                  <span class="badge" [ngClass]="fulfillmentBadge(o.estadoFulfillment)">
                    {{ labelFulfillment(o.estadoFulfillment) }}
                  </span>
                </td>
                <td>
                  @if (o.puedeDescargarFactura && o.nroFactura) {
                    <span class="badge badge-soft-success">N° {{ o.nroFactura }}</span>
                    <button type="button" class="btn btn-link btn-xs p-0 d-block" (click)="descargarFactura(o)">
                      Imprimir
                    </button>
                  } @else {
                    <span class="text-muted">—</span>
                  }
                </td>
                <td>{{ o.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                <td>
                  @if (o.migrado) {
                    <span class="badge badge-soft-success">En ERP</span>
                  } @else {
                    <span class="badge badge-soft-warning">Pendiente</span>
                  }
                </td>
                <td>
                  <a class="btn btn-xs btn-outline-primary" [routerLink]="['/pedidos', o.idPedidoMovil]">
                    Ver
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="10">
                  <div class="dx-empty py-4">
                    @if (loading()) {
                      <i class="fas fa-spinner fa-spin"></i>
                      <p>Cargando pedidos…</p>
                    } @else {
                      <i class="fas fa-receipt"></i>
                      <p>Sin pedidos con esos filtros.</p>
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
          <div class="float-left text-muted">{{ pg.totalCount }} pedidos · sel. {{ selectedCount() }}</div>
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
export class OrdersAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;
  search = '';
  migradoFilter = '';
  fulfillmentFilter = '';
  bulkEstado = '';
  readonly selected = new Set<number>();
  readonly selectedCount = signal(0);
  readonly page = signal<PagedResult<AdminOrderListItem> | null>(null);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly loading = signal(false);
  readonly bulkBusy = signal(false);
  readonly showLogs = signal(false);
  readonly logsBusy = signal(false);
  readonly logs = signal<MigracionLogRow[]>([]);

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((q) => {
      this.search = q.get('q') || '';
      const migrado = q.get('migrado');
      this.migradoFilter = migrado === 'false' || migrado === 'true' ? migrado : '';
      this.fulfillmentFilter = q.get('ff') || '';
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
        migrado: this.migradoFilter || null,
        ff: this.fulfillmentFilter || null,
        page: pageNum === 1 ? null : pageNum,
      },
      replaceUrl: true,
    });
  }

  clearFilters(): void {
    this.search = '';
    this.migradoFilter = '';
    this.fulfillmentFilter = '';
    this.applyFilters(1);
  }

  allPageSelected(): boolean {
    const items = this.page()?.items ?? [];
    return items.length > 0 && items.every((o) => this.selected.has(o.idPedidoMovil));
  }

  toggle(id: number, ev: Event): void {
    if ((ev.target as HTMLInputElement).checked) this.selected.add(id);
    else this.selected.delete(id);
    this.selectedCount.set(this.selected.size);
  }

  toggleAll(ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    for (const o of this.page()?.items ?? []) {
      if (checked) this.selected.add(o.idPedidoMovil);
      else this.selected.delete(o.idPedidoMovil);
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
    const migrado =
      this.migradoFilter === '' ? undefined : this.migradoFilter === 'true';
    this.api
      .orders({
        search: this.search || undefined,
        migrado,
        estadoFulfillment: this.fulfillmentFilter || undefined,
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

  aplicarBulkFulfillment(): void {
    const ids = [...this.selected];
    const estado = this.bulkEstado;
    if (!ids.length || !estado) return;
    if (!confirm(`Aplicar estado «${this.labelFulfillment(estado)}» a ${ids.length} pedido(s)?`)) return;
    this.bulkBusy.set(true);
    this.error.set(null);
    let ok = 0;
    let fail = 0;
    const run = (i: number): void => {
      if (i >= ids.length) {
        this.bulkBusy.set(false);
        this.ok.set(`Fulfillment: ${ok} ok` + (fail ? ` · ${fail} error(es)` : ''));
        this.bulkEstado = '';
        this.clearSelection();
        this.load(this.page()?.page ?? 1);
        return;
      }
      this.api.addPedidoTracking(ids[i], { estado, mensaje: 'Actualización masiva desde admin' }).subscribe({
        next: () => {
          ok++;
          run(i + 1);
        },
        error: () => {
          fail++;
          run(i + 1);
        },
      });
    };
    run(0);
  }

  facturarSelected(): void {
    const ids = [...this.selected].filter((id) => {
      const o = (this.page()?.items ?? []).find((x) => x.idPedidoMovil === id);
      return o ? !o.facturado : true;
    });
    if (!ids.length) {
      this.error.set('Seleccioná pedidos sin facturar.');
      return;
    }
    if (!confirm(`¿Facturar ${ids.length} pedido(s) con N° automático?`)) return;
    this.bulkBusy.set(true);
    this.error.set(null);
    this.api.facturarPedidos(ids).subscribe({
      next: (r) => {
        this.bulkBusy.set(false);
        this.ok.set(`Facturación: ${r.exitosos} ok` + (r.errores ? ` · ${r.errores} error(es)` : ''));
        this.clearSelection();
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => {
        this.bulkBusy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  descargarFactura(o: AdminOrderListItem): void {
    this.api.downloadOrderFacturaPdf(o.idPedidoMovil).subscribe({
      next: (blob) => {
        const name = o.nroFactura ? `factura-${o.nroFactura}.pdf` : `pedido-${o.nroPedido}.pdf`;
        if (!this.api.openPdfBlob(blob, name)) {
          this.error.set('No se pudo cargar la factura');
        }
      },
      error: (e) => {
        this.error.set(e?.error?.error || e.message || 'Error al descargar');
      },
    });
  }

  migrarSelected(): void {
    const ids = [...this.selected].filter((id) => {
      const o = (this.page()?.items ?? []).find((x) => x.idPedidoMovil === id);
      return o ? !o.migrado : true;
    });
    if (!ids.length) {
      this.error.set('Seleccioná pedidos pendientes.');
      return;
    }
    if (!confirm(
      `¿Marcar ${ids.length} pedido(s) como migrados SOLO en e-com?\n` +
      `Esto NO escribe en el ERP. Preferí “Enviar a ERP” si querés el INSERT real.`,
    )) return;
    this.api.migrarPedidos(ids).subscribe({
      next: (r) => {
        this.ok.set(`Marcados localmente: ${r.updatedCount}`);
        this.clearSelection();
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  migrarErp(): void {
    const ids = [...this.selected].filter((id) => {
      const o = (this.page()?.items ?? []).find((x) => x.idPedidoMovil === id);
      return o ? !o.migrado : true;
    });
    if (!ids.length) {
      this.error.set('Seleccioná pedidos pendientes de ERP.');
      return;
    }
    this.bulkBusy.set(true);
    this.api.migrarPedidosErp(ids).subscribe({
      next: (r) => {
        this.bulkBusy.set(false);
        this.ok.set(`ERP: ${r.exitosos} ok · ${r.errores} error(es) · ${r.procesados} procesados`);
        this.clearSelection();
        this.load(this.page()?.page ?? 1);
        if (this.showLogs()) this.loadLogs();
      },
      error: (e) => {
        this.bulkBusy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  toggleLogs(): void {
    const next = !this.showLogs();
    this.showLogs.set(next);
    if (next) this.loadLogs();
  }

  loadLogs(): void {
    this.logsBusy.set(true);
    this.api.listMigracionLogs().subscribe({
      next: (rows) => {
        this.logsBusy.set(false);
        this.logs.set((rows || []) as MigracionLogRow[]);
      },
      error: (e) => {
        this.logsBusy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  reintentarUno(idPedidoMovil: number): void {
    this.api.reintentarMigracionPedido(idPedidoMovil).subscribe({
      next: (r) => {
        this.ok.set(`Reintento #${idPedidoMovil}: ${r.exitosos} ok · ${r.errores} err`);
        this.load(this.page()?.page ?? 1);
        this.loadLogs();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  exportCsv(): void {
    this.error.set(null);
    this.api.exportOrders().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `pedidos-export-${Date.now()}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Exportación CSV de pedidos lista.');
      },
      error: (e) => this.error.set(e?.error?.error || e.message || 'Error al exportar'),
    });
  }

  labelFulfillment(e?: string | null): string {
    const map: Record<string, string> = {
      compra_realizada: 'Compra',
      preparando: 'Preparando',
      preparado: 'Preparado',
      facturado: 'Facturado',
      en_transito: 'En tránsito',
      demorado: 'Demorado',
      excepcion: 'Excepción',
      entregado: 'Entregado',
      cancelado: 'Cancelado',
    };
    return e ? map[e] || e : '—';
  }

  fulfillmentBadge(e?: string | null): string {
    if (!e) return 'badge-soft-info';
    return 'badge-ff-' + e;
  }
}
