import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { AdminApiService, AdminBarrioDelivery, AdminCiudadDelivery, AdminTarifaDelivery } from '../core/admin-api.service';

const ESTADO_LABEL: Record<number, string> = {
  0: 'Pendiente',
  1: 'Autorizada',
  2: 'Anulada',
  3: 'Cancelada',
};

@Component({
  standalone: true,
  imports: [FormsModule, CurrencyPipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Tarifas delivery</h1>
        <p class="dx-page-sub">Autorización / anulación / cancelación · espejo ERP TarifaDelivery.</p>
      </div>
      <div class="dx-filters">
        <select class="custom-select custom-select-sm" [(ngModel)]="estadoFilter">
          <option value="">Todos los estados</option>
          <option value="0">Pendientes</option>
          <option value="1">Autorizadas</option>
          <option value="2">Anuladas</option>
          <option value="3">Canceladas</option>
        </select>
        <input class="form-control form-control-sm" placeholder="Filtrar ciudad/barrio…" [(ngModel)]="filter" />
        <button class="btn btn-outline-secondary btn-sm" type="button" (click)="reload()">
          <i class="fas fa-sync-alt"></i>
        </button>
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
      </div>
    }

    <div class="card card-outline card-primary mb-3">
      <div class="card-header"><h3 class="card-title">Alta de tarifa</h3></div>
      <div class="card-body">
        <div class="form-row">
          <div class="form-group col-md-4">
            <label>Ciudad</label>
            <select class="custom-select custom-select-sm" [(ngModel)]="createForm.idCiudad"
                    (ngModelChange)="onCreateCiudadChange($event)">
              <option [ngValue]="0">Seleccioná…</option>
              @for (c of ciudades(); track c.id) {
                <option [ngValue]="c.id">
                  {{ c.nombre }}@if (c.divisionPolitica) { / {{ c.divisionPolitica }} }
                </option>
              }
            </select>
          </div>
          <div class="form-group col-md-4">
            <label>Barrio</label>
            <select class="custom-select custom-select-sm" [(ngModel)]="createForm.idBarrio"
                    [disabled]="!createForm.idCiudad">
              <option [ngValue]="0">Seleccioná…</option>
              @for (b of barrios(); track b.id) {
                <option [ngValue]="b.id">{{ b.nombre }}</option>
              }
            </select>
          </div>
          <div class="form-group col-md-2">
            <label>Importe (Gs)</label>
            <input class="form-control form-control-sm" type="number" min="0" step="500" [(ngModel)]="createForm.importeDelivery" />
          </div>
          <div class="form-group col-md-2 d-flex align-items-end">
            <button type="button" class="btn btn-primary btn-sm btn-block" (click)="create()">
              <i class="fas fa-plus mr-1"></i> Crear tarifa
            </button>
          </div>
        </div>
        <small class="text-muted">Queda en estado Pendiente (ABIERTO) hasta autorizar.</small>
      </div>
    </div>

    <div class="card card-outline card-warning mb-3">
      <div class="card-body d-flex flex-wrap align-items-center" style="gap:0.5rem">
        <button type="button" class="btn btn-sm btn-default" (click)="toggleAll()">
          {{ allSelected() ? 'Quitar selección' : 'Seleccionar visibles' }}
        </button>
        <button type="button" class="btn btn-sm btn-success"
                [disabled]="selected.size === 0" (click)="bulk('autorizar')">
          <i class="fas fa-check mr-1"></i> Autorizar ({{ selected.size }})
        </button>
        <button type="button" class="btn btn-sm btn-outline-danger"
                [disabled]="selected.size === 0" (click)="bulk('anular')">
          <i class="fas fa-ban mr-1"></i> Anular
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                [disabled]="selected.size === 0" (click)="bulk('cancelar')">
          <i class="fas fa-times mr-1"></i> Cancelar
        </button>
      </div>
    </div>

    <div class="card card-outline card-info">
      <div class="card-header">
        <h3 class="card-title">Tarifas</h3>
        <div class="card-tools">
          <span class="badge badge-info">{{ filtered().length }}</span>
        </div>
      </div>
      <div class="card-body p-0 table-responsive">
        <table class="table table-hover table-bordered mb-0">
          <thead>
            <tr>
              <th style="width:36px"></th>
              <th>Ciudad</th>
              <th>Barrio</th>
              <th style="width:180px">Importe</th>
              <th style="width:110px">Estado</th>
              <th style="width:140px"></th>
            </tr>
          </thead>
          <tbody>
            @for (t of filtered(); track t.id) {
              <tr [class.table-secondary]="!t.activo || t.estado === 2 || t.estado === 3">
                <td>
                  <input type="checkbox" [checked]="selected.has(t.id)" (change)="toggle(t.id, $event)" />
                </td>
                <td>{{ t.nombreCiudad }}</td>
                <td>{{ t.nombreBarrio }}</td>
                <td>
                  @if (t.estado === 0) {
                    <div class="input-group input-group-sm">
                      <input class="form-control" type="number" min="0" step="500"
                             [ngModel]="draft[t.id] !== undefined ? draft[t.id] : t.importeDelivery"
                             (ngModelChange)="draft[t.id] = $event" />
                      <div class="input-group-append"><span class="input-group-text">Gs</span></div>
                    </div>
                  } @else {
                    {{ t.importeDelivery | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                  }
                </td>
                <td>
                  <span class="badge" [class]="estadoBadge(t.estado)">{{ estadoLabel(t.estado) }}</span>
                </td>
                <td class="table-actions text-nowrap">
                  @if (t.estado === 0) {
                    <button class="btn btn-xs btn-primary" type="button" (click)="save(t)" title="Guardar importe">
                      <i class="fas fa-save"></i>
                    </button>
                    <button class="btn btn-xs btn-success" type="button" (click)="one(t, 'autorizar')" title="Autorizar">
                      <i class="fas fa-check"></i>
                    </button>
                    <button class="btn btn-xs btn-danger" type="button" (click)="one(t, 'anular')" title="Anular">
                      <i class="fas fa-ban"></i>
                    </button>
                  }
                  @if (t.estado === 1) {
                    <button class="btn btn-xs btn-outline-secondary" type="button" (click)="one(t, 'cancelar')" title="Cancelar">
                      <i class="fas fa-times"></i>
                    </button>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <div class="dx-empty py-4">
                    <i class="fas fa-truck"></i>
                    <p>Sin tarifas con esos filtros.</p>
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
export class ShippingAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  readonly tarifas = signal<AdminTarifaDelivery[]>([]);
  readonly ciudades = signal<AdminCiudadDelivery[]>([]);
  readonly barrios = signal<AdminBarrioDelivery[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  draft: Record<number, number> = {};
  filter = '';
  estadoFilter = '';
  selected = new Set<number>();
  createForm = { idCiudad: 0, idBarrio: 0, importeDelivery: 15000 };

  ngOnInit(): void {
    this.api.getCiudadesDelivery().subscribe({
      next: (rows) => this.ciudades.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.reload();
  }

  onCreateCiudadChange(id: number): void {
    this.createForm.idBarrio = 0;
    this.barrios.set([]);
    if (!id) return;
    this.api.getBarriosDelivery(id).subscribe({
      next: (rows) => this.barrios.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  create(): void {
    if (!this.createForm.idCiudad || !this.createForm.idBarrio) {
      this.error.set('Seleccioná ciudad y barrio.');
      return;
    }
    this.api.createTarifa({
      idCiudad: Number(this.createForm.idCiudad),
      idBarrio: Number(this.createForm.idBarrio),
      importeDelivery: Number(this.createForm.importeDelivery) || 0,
    }).subscribe({
      next: () => {
        this.ok.set('Tarifa creada (pendiente de autorización)');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  estadoLabel(e: number): string {
    return ESTADO_LABEL[e] ?? String(e);
  }

  estadoBadge(e: number): string {
    return (
      {
        0: 'badge-soft-warning',
        1: 'badge-soft-success',
        2: 'badge-soft-danger',
        3: 'badge-soft-danger',
      }[e] ?? 'badge-soft-warning'
    );
  }

  filtered(): AdminTarifaDelivery[] {
    let rows = this.tarifas();
    if (this.estadoFilter !== '') {
      const e = Number(this.estadoFilter);
      rows = rows.filter((t) => t.estado === e);
    }
    const q = this.filter.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter(
      (t) => t.nombreCiudad.toLowerCase().includes(q) || t.nombreBarrio.toLowerCase().includes(q),
    );
  }

  toggle(id: number, ev: Event): void {
    if ((ev.target as HTMLInputElement).checked) this.selected.add(id);
    else this.selected.delete(id);
  }

  allSelected(): boolean {
    const rows = this.filtered();
    return rows.length > 0 && rows.every((t) => this.selected.has(t.id));
  }

  toggleAll(): void {
    const rows = this.filtered();
    if (this.allSelected()) {
      for (const t of rows) this.selected.delete(t.id);
    } else {
      for (const t of rows) this.selected.add(t.id);
    }
  }

  save(t: AdminTarifaDelivery): void {
    const importe = Number(this.draft[t.id] ?? t.importeDelivery);
    this.api.updateTarifa(t.id, { importeDelivery: importe }).subscribe({
      next: () => {
        this.ok.set(`Tarifa ${t.nombreBarrio} actualizada`);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  one(t: AdminTarifaDelivery, op: 'autorizar' | 'anular' | 'cancelar'): void {
    this.bulk(op, [t.id]);
  }

  bulk(op: 'autorizar' | 'anular' | 'cancelar', ids?: number[]): void {
    const list = ids ?? [...this.selected];
    if (!list.length) return;
    const call =
      op === 'autorizar'
        ? this.api.autorizarTarifas(list)
        : op === 'anular'
          ? this.api.anularTarifas(list)
          : this.api.cancelarTarifas(list);
    call.subscribe({
      next: (r) => {
        this.ok.set(`${op}: ${r.updatedCount} tarifa(s)`);
        this.selected.clear();
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  reload(): void {
    this.api.listTarifas().subscribe({
      next: (rows) => {
        this.tarifas.set(rows);
        this.draft = {};
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
