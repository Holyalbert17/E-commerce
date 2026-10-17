import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApiService, AdminErpHealth } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { openStorefront } from '../core/storefront-url';

interface SyncCursorRow {
  entityKey: string;
  lastWatermarkUtc?: string | null;
  lastRunUtc?: string | null;
  lastStatus?: string | null;
  lastError?: string | null;
  rowsProcessed?: number;
  rowsRead?: number;
  rowsInserted?: number;
  rowsUpdated?: number;
  durationMs?: number;
}

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Estado sync (ecom-etl)</h1>
        <p class="dx-page-sub">
          El espejo ERP→e-com lo mantiene la app <strong>ecom-etl</strong> (1 ERP → N tiendas).
          Esta pantalla solo muestra cursors y write-back de pedidos.
          Tenant <code>{{ session.tenantSlug() }}</code>
          @if (session.idEmpresa(); as id) { · empresa #{{ id }} }.
        </p>
      </div>
      <div class="dx-filters">
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="refreshStatus()" [disabled]="busy()">
          <i class="fas fa-sync-alt mr-1" [class.fa-spin]="busy()"></i> Actualizar estado
        </button>
        <a routerLink="/" class="btn btn-outline-secondary btn-sm">Dashboard</a>
        <button type="button" class="btn btn-success btn-sm" (click)="openShop()">
          <i class="fas fa-external-link-alt mr-1"></i> Ver tienda
        </button>
      </div>
    </div>

    <div class="alert alert-info">
      Inbound: panel ecom-etl en
      <a href="http://10.99.99.205:8787/" target="_blank" rel="noopener">http://10.99.99.205:8787/</a>
      · CLI <code>ecom-etl -c config/panel.yaml continuous --interval 30 --tenants multi</code>.
      Esta API ya no ejecuta pull. Write-back de pedidos sigue disponible abajo.
    </div>

    @if (msg()) {
      <div class="alert" [class.alert-danger]="msgKind()==='err'" [class.alert-success]="msgKind()==='ok'"
           [class.alert-info]="msgKind()==='info'">
        {{ msg() }}
      </div>
    }

    @if (erpHealth(); as h) {
      <div class="callout mb-3"
           [class.callout-success]="h.hasConnection && h.writeBackEffective"
           [class.callout-warning]="h.hasConnection && !h.writeBackEffective"
           [class.callout-danger]="!h.hasConnection">
        <div class="d-flex flex-wrap align-items-center justify-content-between">
          <div>
            <strong>Salud ERP</strong>
            <span class="badge ml-1"
                  [class.badge-success]="h.connectionSource==='Tenant'"
                  [class.badge-info]="h.connectionSource==='Global'"
                  [class.badge-secondary]="h.connectionSource==='None'">CS {{ h.connectionSource }}</span>
            <span class="badge ml-1" [class.badge-success]="h.writeBackEffective" [class.badge-secondary]="!h.writeBackEffective">
              Write-back {{ h.writeBackEffective ? 'ON' : 'OFF' }}
            </span>
            <span class="badge badge-light ml-1">Depósito #{{ h.idDeposito }}</span>
            <span class="badge ml-1" [class.badge-warning]="h.pendingOrdersWriteBack>0" [class.badge-light]="h.pendingOrdersWriteBack===0">
              Pendientes: {{ h.pendingOrdersWriteBack }}
            </span>
            <div class="small text-muted mt-1">{{ h.hint }}</div>
          </div>
          <div class="btn-group btn-group-sm mt-2 mt-md-0">
            <button type="button" class="btn btn-outline-primary"
                    [disabled]="busy() || h.pendingOrdersWriteBack===0" (click)="flushPending()">
              Reintentar pendientes
            </button>
            <a routerLink="/configuracion" class="btn btn-outline-secondary">Config ERP</a>
          </div>
        </div>
      </div>
    }

    <div class="row">
      <div class="col-lg-4">
        <div class="callout callout-info mb-3">
          <h5><i class="fas fa-warehouse mr-1"></i> Stock / catálogo</h5>
          <p class="mb-2 small">
            Depósito espejo <strong>#{{ erpHealth()?.idDeposito || '…' }}</strong>.
            El pull continuo lo hace <strong>ecom-etl</strong>, no esta API.
          </p>
        </div>

        <div class="card card-outline card-info">
          <div class="card-header"><h3 class="card-title">¿Qué hace ecom-etl?</h3></div>
          <div class="card-body p-0">
            <ul class="list-group list-group-flush">
              <li class="list-group-item">Copia catálogo, precios, stock, clientes, tarifas y promos.</li>
              <li class="list-group-item">Conserva los mismos IDs del ERP.</li>
              <li class="list-group-item text-muted">Solo lectura hacia ERP — nunca escribe en su base.</li>
            </ul>
          </div>
          <div class="card-footer">
            <button type="button" class="btn btn-outline-warning btn-sm btn-block mb-2"
                    [disabled]="busy()" (click)="publishOutsideVitrina()">
              <i class="fas fa-store mr-1"></i> Publicar todo lo fuera de vitrina
            </button>
            <a routerLink="/catalogo/activar" class="btn btn-outline-secondary btn-sm btn-block">
              Ir a Activar / inactivar (manual)
            </a>
          </div>
        </div>

        <div class="card card-outline card-secondary mt-3">
          <div class="card-header"><h3 class="card-title">Dominios ecom-etl</h3></div>
          <div class="card-body">
            <ul class="mb-0 pl-3 small">
              @for (k of quickKeys; track k.key) {
                <li><code>{{ k.key }}</code> — {{ k.label }}</li>
              }
            </ul>
          </div>
        </div>
      </div>

      <div class="col-lg-8">
        <div class="card card-outline card-primary">
          <div class="card-header">
            <h3 class="card-title">Último estado por entidad</h3>
            <div class="card-tools">
              @if (erpConfigured() === true) {
                <span class="badge badge-success mr-1">ERP conectado</span>
              } @else if (erpConfigured() === false) {
                <span class="badge badge-warning mr-1">Sin CS ERP</span>
              }
              @if (connectionSource(); as src) {
                <span class="badge badge-info mr-1">Origen {{ src }}</span>
              }
              @if (writeBackEffective() === true) {
                <span class="badge badge-success">Write-back ON</span>
              } @else if (writeBackEffective() === false) {
                <span class="badge badge-secondary">Write-back OFF</span>
              }
              <a routerLink="/configuracion" class="btn btn-tool btn-sm" title="Config ERP por empresa">
                <i class="fas fa-cog"></i>
              </a>
            </div>
          </div>
          <div class="card-body p-0 table-responsive">
            <table class="table table-sm table-hover mb-0">
              <thead>
                <tr>
                  <th>Entidad</th>
                  <th>Estado</th>
                  <th>Leídas</th>
                  <th>Ins.</th>
                  <th>Upd.</th>
                  <th>ms</th>
                  <th>Última corrida</th>
                  <th>Detalle</th>
                </tr>
              </thead>
              <tbody>
                @for (c of cursors(); track c.entityKey) {
                  <tr>
                    <td><code>{{ c.entityKey }}</code></td>
                    <td>
                      @switch (c.lastStatus) {
                        @case ('ok') { <span class="badge badge-success">ok</span> }
                        @case ('error') { <span class="badge badge-danger">error</span> }
                        @case ('skipped') { <span class="badge badge-secondary">omitido</span> }
                        @default { <span class="badge badge-light text-muted">—</span> }
                      }
                    </td>
                    <td>{{ c.rowsRead ?? c.rowsProcessed ?? 0 }}</td>
                    <td>{{ c.rowsInserted ?? 0 }}</td>
                    <td>{{ c.rowsUpdated ?? 0 }}</td>
                    <td class="text-muted">{{ c.durationMs ?? '—' }}</td>
                    <td class="text-muted small">
                      {{ c.lastRunUtc ? (c.lastRunUtc | date:'short') : '—' }}
                    </td>
                    <td class="small text-danger">{{ c.lastError || '' }}</td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="8" class="text-center text-muted py-4">
                      Sin cursors aún. Corré ecom-etl (<code>once --mode full</code>).
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
})
export class SyncOpsPageComponent implements OnInit {
  readonly session = inject(AdminSessionService);
  private readonly api = inject(AdminApiService);

  readonly busy = signal(false);
  readonly msg = signal<string | null>(null);
  readonly msgKind = signal<'ok' | 'err' | 'info'>('info');
  readonly cursors = signal<SyncCursorRow[]>([]);
  readonly erpConfigured = signal<boolean | null>(null);
  readonly connectionSource = signal<string | null>(null);
  readonly writeBackEffective = signal<boolean | null>(null);
  readonly erpHealth = signal<AdminErpHealth | null>(null);

  readonly quickKeys = [
    { key: 'empresa', label: 'Configuración empresa' },
    { key: 'catalog', label: 'Catálogo / embalajes' },
    { key: 'prices', label: 'Listas y precios' },
    { key: 'stock', label: 'Stock depósito' },
    { key: 'customers', label: 'Clientes e-com' },
    { key: 'shipping', label: 'Agencias y tarifas' },
    { key: 'promotions', label: 'Grupos / promos' },
  ];

  ngOnInit(): void {
    this.refreshStatus();
  }

  openShop(): void {
    openStorefront(this.session.tenantSlug());
  }

  refreshStatus(): void {
    this.busy.set(true);
    this.api.getErpHealth().subscribe({
      next: (h) => this.erpHealth.set(h),
      error: () => this.erpHealth.set(null),
    });
    this.api.syncStatus().subscribe({
      next: (s) => {
        this.erpConfigured.set(!!s?.erpConnectionConfigured);
        this.connectionSource.set(s?.connectionSource ?? null);
        this.writeBackEffective.set(
          typeof s?.writeBackEffective === 'boolean' ? s.writeBackEffective : null,
        );
        this.cursors.set((s?.cursors ?? []) as SyncCursorRow[]);
        this.busy.set(false);
      },
      error: (e) => {
        this.msgKind.set('err');
        this.msg.set(e?.error?.error || e.message);
        this.busy.set(false);
      },
    });
  }

  flushPending(): void {
    if (!confirm('¿Reintentar write-back de pedidos pendientes hacia el ERP?')) return;
    this.busy.set(true);
    this.api.flushPendingErpOrders().subscribe({
      next: (r) => {
        this.busy.set(false);
        this.msgKind.set(r.errores > 0 ? 'err' : 'ok');
        this.msg.set(`Write-back: ${r.exitosos} ok · ${r.errores} error · ${r.procesados} procesados`);
        this.refreshStatus();
      },
      error: (e) => {
        this.busy.set(false);
        this.msgKind.set('err');
        this.msg.set(e?.error?.error || e.message);
      },
    });
  }

  // Pull inbound eliminado — usar ecom-etl.
  runAll(): void {
    this.msgKind.set('info');
    this.msg.set('El pull lo hace ecom-etl, no la API. Ver alerta arriba.');
  }

  runKeys(_keys: string[] | undefined): void {
    this.runAll();
  }

  /** Publica en vitrina todos los productos locales aún OFF (páginas sucesivas). */
  publishOutsideVitrina(): void {
    if (
      !confirm(
        '¿Publicar TODOS los productos que están fuera de vitrina?\n\nEsto activa activoEcommerce en masa. Preferí “Activar / inactivar” si querés elegir uno a uno.',
      )
    ) {
      return;
    }
    this.busy.set(true);
    this.msg.set(null);
    const ids: number[] = [];
    const pageSize = 100;

    const loadPage = (page: number): void => {
      this.api.products({ activoEcommerce: false, page, pageSize }).subscribe({
        next: (r) => {
          for (const p of r.items ?? []) ids.push(p.id);
          if (page < (r.totalPages || 1)) {
            loadPage(page + 1);
            return;
          }
          if (ids.length === 0) {
            this.msgKind.set('info');
            this.msg.set('No hay productos fuera de vitrina. Si el catálogo está vacío, corré ecom-etl full.');
            this.busy.set(false);
            return;
          }
          if (!confirm(`Se van a publicar ${ids.length} producto(s). ¿Continuar?`)) {
            this.busy.set(false);
            this.msgKind.set('info');
            this.msg.set('Publicación cancelada.');
            return;
          }
          this.api
            .bulkActivoEcommerce({
              ids,
              activoEcommerce: true,
              estadoEcommerce: 'activo',
            })
            .subscribe({
              next: (res) => {
                this.msgKind.set('ok');
                this.msg.set(
                  `Vitrina actualizada: ${res.updatedCount} productos publicados. Abrí la tienda para verificar.`,
                );
                this.busy.set(false);
              },
              error: (e) => {
                this.msgKind.set('err');
                this.msg.set(e?.error?.error || e.message);
                this.busy.set(false);
              },
            });
        },
        error: (e) => {
          this.msgKind.set('err');
          this.msg.set(e?.error?.error || e.message);
          this.busy.set(false);
        },
      });
    };

    loadPage(1);
  }
}
