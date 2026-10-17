import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApiService, ProvisionResult, TenantSummary } from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Empresas</h1>
        <p class="dx-page-sub">Provisioning multi-tenant · alta manual o desde ERP.</p>
      </div>
      <button type="button" class="btn btn-outline-secondary btn-sm" (click)="reload()">
        <i class="fas fa-sync-alt mr-1"></i> Recargar
      </button>
      <button type="button" class="btn btn-outline-info btn-sm" (click)="runSync()"
              title="Inbound: ecom-etl">
        <i class="fas fa-info-circle mr-1"></i> Cómo sync
      </button>
      <button type="button" class="btn btn-outline-info btn-sm" (click)="showSyncStatus()">
        Estado sync
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
      <div class="col-lg-6">
        <form class="card card-primary card-outline" (ngSubmit)="createManual()">
          <div class="card-header"><h3 class="card-title">1) Alta manual (Standalone)</h3></div>
          <div class="card-body">
            <p class="text-muted small">Genera IDEmpresa local. ProcesaPedidosERP=false.</p>
            <div class="form-group">
              <label>Razón social</label>
              <input class="form-control" [(ngModel)]="manual.razonSocial" name="rs" required />
            </div>
            <div class="form-group">
              <label>Nombre fantasía</label>
              <input class="form-control" [(ngModel)]="manual.nombreFantasia" name="nf" />
            </div>
            <div class="form-group">
              <label>RUC / documento</label>
              <input class="form-control" [(ngModel)]="manual.numeroDocumento" name="doc" required />
            </div>
            <div class="form-group">
              <label>Slug tienda</label>
              <input class="form-control" [(ngModel)]="manual.slugTienda" name="slug" required placeholder="mitienda" />
            </div>
            <div class="form-group">
              <label>Dominio custom</label>
              <input class="form-control" [(ngModel)]="manual.dominioCustom" name="dom" />
            </div>
            <div class="form-row">
              <div class="form-group col-6">
                <div class="custom-control custom-checkbox">
                  <input type="checkbox" class="custom-control-input" id="del" [(ngModel)]="manual.realizaDelivery" name="del" />
                  <label class="custom-control-label" for="del">Delivery</label>
                </div>
              </div>
              <div class="form-group col-6">
                <div class="custom-control custom-checkbox">
                  <input type="checkbox" class="custom-control-input" id="pick" [(ngModel)]="manual.realizaPickup" name="pick" />
                  <label class="custom-control-label" for="pick">Pickup</label>
                </div>
              </div>
            </div>
            <div class="form-row">
              <div class="form-group col-6">
                <label>Admin usuario</label>
                <input class="form-control" [(ngModel)]="manual.adminUsuario" name="au" />
              </div>
              <div class="form-group col-6">
                <label>Admin password</label>
                <input class="form-control" [(ngModel)]="manual.adminPassword" name="ap" />
              </div>
            </div>
          </div>
          <div class="card-footer">
            <button class="btn btn-primary" type="submit" [disabled]="busy()">Crear empresa</button>
          </div>
        </form>
      </div>

      <div class="col-lg-6">
        <form class="card card-success card-outline" (ngSubmit)="createFromErp()">
          <div class="card-header"><h3 class="card-title">2) Desde ERP (Integrated)</h3></div>
          <div class="card-body">
            <p class="text-muted small">Usa IDEmpresa del ERP. ProcesaPedidosERP=true.</p>
            <div class="form-group">
              <label>IDEmpresa ERP</label>
              <input class="form-control" type="number" [(ngModel)]="erp.idEmpresa" name="ide" required />
            </div>
            <div class="form-group">
              <label>Razón social</label>
              <input class="form-control" [(ngModel)]="erp.razonSocial" name="ers" required />
            </div>
            <div class="form-group">
              <label>Nombre fantasía</label>
              <input class="form-control" [(ngModel)]="erp.nombreFantasia" name="enf" />
            </div>
            <div class="form-group">
              <label>Documento</label>
              <input class="form-control" [(ngModel)]="erp.numeroDocumento" name="edoc" />
            </div>
            <div class="form-group">
              <label>Slug tienda</label>
              <input class="form-control" [(ngModel)]="erp.slugTienda" name="eslug" required />
            </div>
            <div class="form-row">
              <div class="form-group col-6">
                <label>IDDeposito ERP</label>
                <input class="form-control" type="number" [(ngModel)]="erp.idDepositoErp" name="idep" />
              </div>
              <div class="form-group col-6">
                <label>IDArticuloDelivery</label>
                <input class="form-control" type="number" [(ngModel)]="erp.idArticuloDeliveryErp" name="idel" />
              </div>
            </div>
            <div class="custom-control custom-checkbox">
              <input type="checkbox" class="custom-control-input" id="defs"
                     [(ngModel)]="erp.createLocalDefaultsIfMissing" name="defs" />
              <label class="custom-control-label" for="defs">Crear defaults locales si faltan</label>
            </div>
          </div>
          <div class="card-footer">
            <button class="btn btn-success" type="submit" [disabled]="busy()">Provisionar desde ERP</button>
          </div>
        </form>
      </div>
    </div>

    <div class="card card-outline card-secondary mt-3">
      <div class="card-header">
        <h3 class="card-title">Tiendas existentes</h3>
        <div class="card-tools">
          <span class="badge badge-soft-info">{{ tenants().length }}</span>
        </div>
      </div>
      <div class="card-body p-0 table-responsive">
        <table class="table table-striped table-hover table-bordered mb-0">
          <thead>
            <tr>
              <th style="width:70px">ID</th>
              <th>Razón social</th>
              <th>Slug</th>
              <th style="width:110px">Origen</th>
              <th style="width:70px">ERP</th>
              <th style="width:80px">Activo</th>
              <th style="width:90px">Depósito</th>
            </tr>
          </thead>
          <tbody>
            @for (t of tenants(); track t.idEmpresa) {
              <tr [class.table-secondary]="!t.activo">
                <td>{{ t.idEmpresa }}</td>
                <td>
                  <strong>{{ t.razonSocial }}</strong>
                  <small>{{ t.numeroDocumento || '—' }}</small>
                </td>
                <td><code>{{ t.slugTienda }}</code></td>
                <td>
                  @if (t.sourceHint === 1) {
                    <span class="badge badge-soft-success">Desde ERP</span>
                  } @else {
                    <span class="badge badge-soft-info">Manual</span>
                  }
                </td>
                <td>
                  @if (t.procesaPedidosERP) {
                    <span class="badge badge-soft-success">Sí</span>
                  } @else {
                    <span class="badge badge-soft-warning">No</span>
                  }
                </td>
                <td class="text-center">
                  @if (t.activo) {
                    <span class="badge badge-soft-success">Sí</span>
                  } @else {
                    <span class="badge badge-soft-danger">No</span>
                  }
                </td>
                <td>{{ t.idDeposito }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7">
                  <div class="dx-empty py-4">
                    <i class="fas fa-building"></i>
                    <p>Sin empresas. Usá el alta manual o el provisioning desde ERP.</p>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class TenantsPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly tenants = signal<TenantSummary[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly busy = signal(false);

  manual = {
    razonSocial: '',
    nombreFantasia: '',
    numeroDocumento: '',
    slugTienda: '',
    dominioCustom: '',
    realizaDelivery: true,
    realizaPickup: true,
    adminUsuario: 'admin',
    adminPassword: 'Admin12!',
  };

  erp = {
    idEmpresa: 100,
    razonSocial: '',
    nombreFantasia: '',
    numeroDocumento: '',
    slugTienda: '',
    idDepositoErp: null as number | null,
    idArticuloDeliveryErp: null as number | null,
    createLocalDefaultsIfMissing: true,
  };

  ngOnInit(): void {
    this.reload();
  }

  createManual(): void {
    this.busy.set(true);
    this.error.set(null);
    this.ok.set(null);
    this.api
      .provisionManual({
        razonSocial: this.manual.razonSocial,
        nombreFantasia: this.manual.nombreFantasia || null,
        numeroDocumento: this.manual.numeroDocumento,
        slugTienda: this.manual.slugTienda,
        dominioCustom: this.manual.dominioCustom || null,
        realizaDelivery: this.manual.realizaDelivery,
        realizaPickup: this.manual.realizaPickup,
        controlaStockEnCarrito: true,
        montoPedidoMinimo: 0,
        adminUsuario: this.manual.adminUsuario,
        adminPassword: this.manual.adminPassword,
      })
      .subscribe({
        next: (r) => {
          this.busy.set(false);
          this.ok.set(this.formatResult(r));
          this.reload();
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set(e?.error?.error || e.message || 'Error alta manual');
        },
      });
  }

  createFromErp(): void {
    this.busy.set(true);
    this.error.set(null);
    this.ok.set(null);
    this.api
      .provisionFromErp({
        idEmpresa: this.erp.idEmpresa,
        razonSocial: this.erp.razonSocial,
        nombreFantasia: this.erp.nombreFantasia || null,
        numeroDocumento: this.erp.numeroDocumento || null,
        slugTienda: this.erp.slugTienda,
        idDepositoErp: this.erp.idDepositoErp,
        idArticuloDeliveryErp: this.erp.idArticuloDeliveryErp,
        realizaDelivery: true,
        realizaPickup: true,
        controlaStockEnCarrito: true,
        procesaPedidosERP: true,
        montoPedidoMinimo: 0,
        createLocalDefaultsIfMissing: this.erp.createLocalDefaultsIfMissing,
      })
      .subscribe({
        next: (r) => {
          this.busy.set(false);
          this.ok.set(this.formatResult(r));
          this.reload();
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set(e?.error?.error || e.message || 'Error provision ERP');
        },
      });
  }

  private formatResult(r: ProvisionResult): string {
    let msg = r.mensaje;
    if (r.adminUsuario) {
      msg += ` Admin: ${r.adminUsuario} / ${r.adminPasswordPlainDev}`;
    }
    msg += ` → slug: ${r.slugTienda}`;
    return msg;
  }

  reload(): void {
    this.api.listTenants().subscribe({
      next: (rows) => this.tenants.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message || 'Error listando'),
    });
  }

  runSync(): void {
    this.busy.set(false);
    this.error.set(null);
    this.ok.set(
      'Inbound ERP→ecom: usá ecom-etl (python). Esta API ya no hace pull. Ver /sync para cursors.',
    );
  }

  showSyncStatus(): void {
    this.api.syncStatus().subscribe({
      next: (r) => this.ok.set(`Estado sync: ${JSON.stringify(r).slice(0, 240)}`),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
