import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminApiService, VentasResumenDto } from '../core/admin-api.service';

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
  imports: [FormsModule, CurrencyPipe, DatePipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Reportes · Ventas</h1>
        <p class="dx-page-sub">Pedidos, ticket promedio, conversión, top SKUs y write-back ERP.</p>
      </div>
      <div class="dx-filters">
        <input class="form-control form-control-sm" type="date" [(ngModel)]="from" />
        <input class="form-control form-control-sm" type="date" [(ngModel)]="to" />
        <button type="button" class="btn btn-primary btn-sm" (click)="load()" [disabled]="loading()">
          <i class="fas fa-chart-bar mr-1"></i> Generar
        </button>
        <button type="button" class="btn btn-outline-secondary btn-sm"
                [disabled]="!report()" (click)="exportCsv()">
          <i class="fas fa-file-csv mr-1"></i> Export CSV
        </button>
        <a routerLink="/pedidos" [queryParams]="{ migrado: 'false' }" class="btn btn-outline-warning btn-sm">
          Pedidos pendientes ERP
        </a>
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }

    @if (migErrors().length) {
      <div class="card card-outline card-danger mb-3">
        <div class="card-header">
          <h3 class="card-title">Write-back ERP con error ({{ migErrors().length }})</h3>
          <div class="card-tools">
            <button type="button" class="btn btn-tool" (click)="loadMigLogs()">
              <i class="fas fa-sync-alt"></i>
            </button>
          </div>
        </div>
        <div class="card-body p-0 table-responsive" style="max-height:260px;overflow:auto">
          <table class="table table-sm mb-0">
            <thead>
              <tr><th>Pedido</th><th>Intentos</th><th>Fecha</th><th>Error</th><th></th></tr>
            </thead>
            <tbody>
              @for (l of migErrors(); track l.id) {
                <tr>
                  <td><strong>#{{ l.nroPedido }}</strong></td>
                  <td>{{ l.intentos }}</td>
                  <td class="small text-muted">{{ l.fechaIntento | date:'short' }}</td>
                  <td class="small">{{ l.mensajeError || l.detalleTecnico || '—' }}</td>
                  <td>
                    <a class="btn btn-xs btn-outline-primary" [routerLink]="['/pedidos', l.idPedidoMovil]">Ver</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    } @else if (migLoaded()) {
      <div class="alert alert-success py-2">Sin errores recientes de write-back ERP.</div>
    }

    @if (report(); as r) {
      <div class="row mb-3">
        <div class="col-md-3">
          <div class="info-box bg-info">
            <span class="info-box-icon"><i class="fas fa-shopping-bag"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Pedidos</span>
              <span class="info-box-number">{{ r.totalPedidos }}</span>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="info-box bg-success">
            <span class="info-box-icon"><i class="fas fa-coins"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Monto total</span>
              <span class="info-box-number">{{ r.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</span>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="info-box bg-warning">
            <span class="info-box-icon"><i class="fas fa-receipt"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Ticket promedio</span>
              <span class="info-box-number">{{ r.ticketPromedio | currency: 'PYG':'symbol-narrow':'1.0-0' }}</span>
            </div>
          </div>
        </div>
        <div class="col-md-3">
          <div class="info-box bg-secondary">
            <span class="info-box-icon"><i class="fas fa-percentage"></i></span>
            <div class="info-box-content">
              <span class="info-box-text">Conversión</span>
              <span class="info-box-number">{{ r.conversionPct }}%</span>
              <span class="progress-description">
                {{ r.carritosCerrados }} cerrados / {{ r.carritosAbiertos + r.carritosCerrados }} carritos
              </span>
            </div>
          </div>
        </div>
      </div>

      <div class="row">
        <div class="col-lg-7">
          <div class="card card-outline card-primary">
            <div class="card-header"><h3 class="card-title">Ventas por día</h3></div>
            <div class="card-body p-0 table-responsive">
              <table class="table table-striped table-bordered mb-0">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th style="width:120px">Pedidos</th>
                    <th style="width:180px">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  @for (d of r.porDia; track d.fecha) {
                    <tr>
                      <td>{{ d.fecha | date: 'mediumDate' }}</td>
                      <td class="text-center">{{ d.pedidosCount }}</td>
                      <td class="text-right">{{ d.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="3">
                        <div class="dx-empty py-4">
                          <i class="fas fa-chart-line"></i>
                          <p>Sin ventas en el período.</p>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
        <div class="col-lg-5">
          <div class="card card-outline card-info">
            <div class="card-header"><h3 class="card-title">Top SKUs</h3></div>
            <div class="card-body p-0 table-responsive">
              <table class="table table-sm table-striped mb-0">
                <thead>
                  <tr><th>Producto</th><th>Cant.</th><th>Monto</th></tr>
                </thead>
                <tbody>
                  @for (s of r.topSkus; track s.idArticulo) {
                    <tr>
                      <td>
                        <strong>{{ s.nombre }}</strong>
                        <div class="text-muted small">{{ s.sku || ('#' + s.idArticulo) }}</div>
                      </td>
                      <td class="text-center">{{ s.cantidadVendida }}</td>
                      <td class="text-right">{{ s.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="3">
                        <div class="dx-empty py-3">
                          <i class="fas fa-box"></i>
                          <p>Sin datos de top SKUs.</p>
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
    } @else if (loading()) {
      <div class="dx-empty py-5">
        <i class="fas fa-spinner fa-spin"></i>
        <p>Cargando reporte…</p>
      </div>
    } @else {
      <div class="dx-empty py-5">
        <i class="fas fa-chart-bar"></i>
        <p>Elegí un rango de fechas y generá el reporte.</p>
      </div>
    }
  `,
})
export class ReportsPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly report = signal<VentasResumenDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly migErrors = signal<MigracionLogRow[]>([]);
  readonly migLoaded = signal(false);

  from = '';
  to = '';

  ngOnInit(): void {
    const today = new Date();
    const desde = new Date(today);
    desde.setDate(desde.getDate() - 30);
    this.to = today.toISOString().slice(0, 10);
    this.from = desde.toISOString().slice(0, 10);
    this.load();
    this.loadMigLogs();
  }

  loadMigLogs(): void {
    this.api.listMigracionLogs().subscribe({
      next: (rows) => {
        const errs = ((rows || []) as MigracionLogRow[])
          .filter((x) => x.estado === 'error')
          .slice(0, 30);
        this.migErrors.set(errs);
        this.migLoaded.set(true);
      },
      error: () => {
        this.migErrors.set([]);
        this.migLoaded.set(true);
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.reportVentas(this.from, this.to).subscribe({
      next: (r) => {
        this.report.set(r);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || e.message || 'Error al generar reporte');
      },
    });
  }

  exportCsv(): void {
    const r = this.report();
    if (!r) return;
    const lines: string[] = [
      'seccion,clave,valor',
      `resumen,totalPedidos,${r.totalPedidos}`,
      `resumen,montoTotal,${r.montoTotal}`,
      `resumen,ticketPromedio,${r.ticketPromedio}`,
      `resumen,conversionPct,${r.conversionPct}`,
      `resumen,carritosAbiertos,${r.carritosAbiertos}`,
      `resumen,carritosCerrados,${r.carritosCerrados}`,
      '',
      'fecha,pedidosCount,montoTotal',
      ...(r.porDia || []).map((d) => `${d.fecha},${d.pedidosCount},${d.montoTotal}`),
      '',
      'idArticulo,sku,nombre,cantidadVendida,montoTotal',
      ...(r.topSkus || []).map(
        (s) => `${s.idArticulo},"${(s.sku || '').replace(/"/g, '""')}","${(s.nombre || '').replace(/"/g, '""')}",${s.cantidadVendida},${s.montoTotal}`,
      ),
    ];
    const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `ventas-${this.from}_${this.to}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
