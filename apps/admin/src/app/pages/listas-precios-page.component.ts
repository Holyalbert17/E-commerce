import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AdminApiService,
  AdminDetalleLista,
  AdminEmbalajeOption,
  AdminListaPrecio,
  UpsertListaPrecioRequest,
} from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Listas de precios</h1>
        <p class="dx-page-sub">ListaPrecio + DetalleListaPrecioCondicionVenta (espejo ERP).</p>
      </div>
      <div class="dx-filters">
        @if (!selectedId && !creating) {
          <input class="form-control form-control-sm" style="min-width:180px" placeholder="Filtrar lista / canal…"
                 [(ngModel)]="filter" />
        }
        @if (selectedId && !creating) {
          <button type="button" class="btn btn-outline-success btn-sm" (click)="exportLista()">
            <i class="fas fa-file-export mr-1"></i> Export CSV
          </button>
          <label class="btn btn-outline-info btn-sm mb-0">
            <i class="fas fa-file-import mr-1"></i> Import CSV
            <input type="file" accept=".csv,text/csv" hidden (change)="importLista($event)" />
          </label>
        }
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="reload()">
          <i class="fas fa-sync-alt"></i>
        </button>
        @if (!selectedId) {
          <button type="button" class="btn btn-info btn-sm" (click)="startCreate()">
            <i class="fas fa-plus mr-1"></i> Nuevo
          </button>
        }
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

    @if (!selectedId && !creating) {
      <div class="card card-outline card-primary">
        <div class="card-header">
          <h3 class="card-title">Listas</h3>
          <div class="card-tools">
            <span class="badge badge-soft-info">{{ filtered().length }}</span>
          </div>
        </div>
        <div class="card-body p-0 table-responsive">
          <table class="table table-striped table-hover table-bordered mb-0">
            <thead>
              <tr>
                <th style="width:70px">Nro</th>
                <th>Descripción</th>
                <th style="width:90px">Canal</th>
                <th style="width:110px">Estado</th>
                <th style="width:90px">Ítems</th>
                <th style="width:80px">Activo</th>
                <th style="width:150px" class="text-center">Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (l of filtered(); track l.id) {
                <tr [class.table-secondary]="!l.activo || l.estado === 0 || l.estado === 3">
                  <td>{{ l.nroListaPrecio }}</td>
                  <td>
                    <strong>{{ l.descripcion }}</strong>
                    <small>
                      @if (l.fechaInicioVigencia) {
                        {{ l.fechaInicioVigencia | date: 'shortDate' }}
                      }
                      –
                      @if (l.fechaFinVigencia) {
                        {{ l.fechaFinVigencia | date: 'shortDate' }}
                      } @else {
                        ∞
                      }
                    </small>
                  </td>
                  <td><span class="badge badge-soft-info">{{ l.canalEcommerce }}</span></td>
                  <td><span class="badge" [class]="listaEstadoBadge(l.estado)">{{ listaEstadoLabel(l.estado) }}</span></td>
                  <td class="text-center"><span class="badge badge-soft-info">{{ l.detalleCount }}</span></td>
                  <td class="text-center">
                    @if (l.activo) {
                      <span class="badge badge-soft-success">Sí</span>
                    } @else {
                      <span class="badge badge-soft-danger">No</span>
                    }
                  </td>
                  <td class="table-actions text-center text-nowrap">
                    <button type="button" class="btn btn-xs btn-primary" title="Detalle"
                            (click)="openLista(l.id)">
                      <i class="fas fa-list"></i>
                    </button>
                    @if (l.estado === 1) {
                      <button type="button" class="btn btn-xs btn-success" title="Autorizar"
                              (click)="authLista(l.id, 'autorizar')">
                        <i class="fas fa-check"></i>
                      </button>
                    }
                    @if (l.estado === 1 || l.estado === 2) {
                      <button type="button" class="btn btn-xs btn-danger" title="Anular"
                              (click)="authLista(l.id, 'anular')">
                        <i class="fas fa-ban"></i>
                      </button>
                    }
                    @if (l.estado === 2) {
                      <button type="button" class="btn btn-xs btn-outline-secondary" title="Cancelar"
                              (click)="authLista(l.id, 'cancelar')">
                        <i class="fas fa-times"></i>
                      </button>
                    }
                    <button type="button" class="btn btn-xs btn-outline-danger" title="Eliminar"
                            (click)="removeLista(l)" [disabled]="!l.activo">
                      <i class="fas fa-trash"></i>
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7">
                    <div class="dx-empty py-4">
                      <i class="fas fa-tags"></i>
                      <p>Sin listas. Creá una nueva o ajustá el filtro.</p>
                      <button type="button" class="btn btn-sm btn-info" (click)="startCreate()">Nueva lista</button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }

    @if (creating || (selectedId && lista())) {
      <div class="card card-outline card-info mb-3">
        <div class="card-header">
          <h3 class="card-title">{{ creating ? 'Nueva lista' : 'Editar lista #' + selectedId }}</h3>
          <div class="card-tools">
            <ol class="breadcrumb float-sm-right mb-0 bg-transparent p-0">
              <li class="breadcrumb-item"><a href="#" (click)="backList($event)">Listas</a></li>
              <li class="breadcrumb-item active">{{ creating ? 'Nuevo' : 'Editar' }}</li>
            </ol>
          </div>
        </div>
        <div class="card-body">
          <form class="form-horizontal dx-erp-form" (ngSubmit)="saveLista()">
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Descripción</label>
              <div class="col-sm-6">
                <input class="form-control" [(ngModel)]="form.descripcion" name="desc" required />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Canal</label>
              <div class="col-sm-3">
                <select class="custom-select" [(ngModel)]="form.canalEcommerce" name="canal">
                  <option value="B2C">B2C</option>
                  <option value="B2B">B2B</option>
                  <option value="BOTH">BOTH</option>
                </select>
              </div>
              <div class="col-sm-3">
                <div class="custom-control custom-switch mt-2">
                  <input type="checkbox" class="custom-control-input" id="ecomLista"
                         [(ngModel)]="form.esListaPrecioEcommerce" name="ecom" />
                  <label class="custom-control-label" for="ecomLista">Es lista e-commerce</label>
                </div>
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Vigencia</label>
              <div class="col-sm-3">
                <input class="form-control" type="date" [(ngModel)]="form.fechaInicioVigencia" name="fi" />
              </div>
              <div class="col-sm-3">
                <input class="form-control" type="date" [(ngModel)]="form.fechaFinVigencia" name="ff" />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Descuentos (ERP)</label>
              <div class="col-sm-3">
                <label class="small text-muted">% Minorista</label>
                <input class="form-control" type="number" min="0" max="100" step="0.01"
                       [(ngModel)]="form.porDescuentoMinorista" name="pdmin" />
              </div>
              <div class="col-sm-3">
                <label class="small text-muted">% Mayorista</label>
                <input class="form-control" type="number" min="0" max="100" step="0.01"
                       [(ngModel)]="form.porDescuentoMayorista" name="pdmay" />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Cant. mín. mayorista</label>
              <div class="col-sm-3">
                <input class="form-control" type="number" min="0" step="1"
                       [(ngModel)]="form.cantidadMinimaMayorista" name="cmm" />
              </div>
            </div>
            @if (!creating) {
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Activo</label>
                <div class="col-sm-3">
                  <div class="custom-control custom-switch mt-2">
                    <input type="checkbox" class="custom-control-input" id="actLista"
                           [(ngModel)]="form.activo" name="activo" />
                    <label class="custom-control-label" for="actLista"></label>
                  </div>
                </div>
              </div>
            }
            <div class="form-group row mb-0">
              <div class="offset-sm-3 col-sm-9">
                <button type="button" class="btn btn-info btn-sm mr-2" (click)="backList($event)">
                  <i class="fas fa-arrow-left mr-1"></i> Volver
                </button>
                <button type="submit" class="btn btn-primary btn-sm mr-2">
                  <i class="fas fa-save mr-1"></i> Guardar
                </button>
                @if (!creating && lista(); as L) {
                  @if (L.estado === 1) {
                    <button type="button" class="btn btn-success btn-sm mr-1"
                            (click)="authLista(L.id, 'autorizar')">Autorizar</button>
                  }
                  @if (L.estado === 1 || L.estado === 2) {
                    <button type="button" class="btn btn-outline-danger btn-sm mr-1"
                            (click)="authLista(L.id, 'anular')">Anular</button>
                  }
                  @if (L.estado === 2) {
                    <button type="button" class="btn btn-outline-secondary btn-sm"
                            (click)="authLista(L.id, 'cancelar')">Cancelar</button>
                  }
                }
              </div>
            </div>
          </form>
        </div>
      </div>
    }

    @if (selectedId && !creating) {
      <div class="card card-outline card-primary">
        <div class="card-header">
          <h3 class="card-title">Detalle condición venta</h3>
        </div>
        <div class="card-body">
          <div class="form-row align-items-end mb-3">
            <div class="col-md-5 form-group mb-0">
              <label>Buscar embalaje</label>
              <input class="form-control form-control-sm" [(ngModel)]="embSearch"
                     (keyup.enter)="searchEmb()" placeholder="SKU / nombre…" />
            </div>
            <div class="col-md-4 form-group mb-0">
              <label>Embalaje</label>
              <select class="custom-select custom-select-sm" [(ngModel)]="newEmbId">
                @for (e of embalajes(); track e.idArticuloEmbalaje) {
                  <option [ngValue]="e.idArticuloEmbalaje">
                    {{ e.sku || e.idArticulo }} — {{ e.nombreArticulo }}
                    ({{ e.descripcionEmbalaje || 'emb' }})
                  </option>
                }
              </select>
            </div>
            <div class="col-md-2 form-group mb-0">
              <label>Precio contado</label>
              <input class="form-control form-control-sm" type="number" min="0" [(ngModel)]="newPrecio" />
            </div>
            <div class="col-md-1 form-group mb-0">
              <button type="button" class="btn btn-success btn-sm btn-block" (click)="addDetalle()">
                <i class="fas fa-plus"></i>
              </button>
            </div>
          </div>

          <div class="table-responsive">
            <table class="table table-sm table-striped table-bordered mb-0">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Artículo</th>
                  <th>Embalaje</th>
                  <th style="width:120px">Contado</th>
                  <th style="width:120px">Cheque</th>
                  <th style="width:120px">Crédito</th>
                  <th style="width:70px">Activo</th>
                  <th style="width:100px"></th>
                </tr>
              </thead>
              <tbody>
                @for (d of detalles(); track d.id) {
                  <tr [class.table-secondary]="!d.activo">
                    <td><code>{{ d.sku || '—' }}</code></td>
                    <td>{{ d.nombreArticulo }}</td>
                    <td>{{ d.descripcionEmbalaje || '—' }}</td>
                    <td>
                      <input class="form-control form-control-sm" type="number" min="0"
                             [ngModel]="draft[d.id]?.contado ?? d.precioContado"
                             (ngModelChange)="setDraft(d.id, 'contado', $event)" />
                    </td>
                    <td>
                      <input class="form-control form-control-sm" type="number" min="0"
                             [ngModel]="draft[d.id]?.cheque ?? d.precioCheque"
                             (ngModelChange)="setDraft(d.id, 'cheque', $event)" />
                    </td>
                    <td>
                      <input class="form-control form-control-sm" type="number" min="0"
                             [ngModel]="draft[d.id]?.credito ?? d.precioCredito"
                             (ngModelChange)="setDraft(d.id, 'credito', $event)" />
                    </td>
                    <td class="text-center">
                      @if (d.activo) {
                        <span class="badge badge-soft-success">Sí</span>
                      } @else {
                        <span class="badge badge-soft-danger">No</span>
                      }
                    </td>
                    <td class="table-actions text-nowrap">
                      <button type="button" class="btn btn-xs btn-primary" (click)="saveDetalle(d)">
                        <i class="fas fa-save"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-danger" (click)="removeDetalle(d)"
                              [disabled]="!d.activo">
                        <i class="fas fa-trash"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="8">
                      <div class="dx-empty py-3">
                        <i class="fas fa-box-open"></i>
                        <p>Sin detalles de precio. Agregá un embalaje abajo.</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }
  `,
})
export class ListasPreciosPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly listas = signal<AdminListaPrecio[]>([]);
  readonly lista = signal<AdminListaPrecio | null>(null);
  readonly detalles = signal<AdminDetalleLista[]>([]);
  readonly embalajes = signal<AdminEmbalajeOption[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);

  filter = '';
  creating = false;
  selectedId: number | null = null;
  form: UpsertListaPrecioRequest = this.emptyForm();
  draft: Record<number, { contado: number; cheque: number; credito: number }> = {};
  embSearch = '';
  newEmbId: number | null = null;
  newPrecio = 0;

  ngOnInit(): void {
    this.route.paramMap.subscribe((pm) => {
      const id = pm.get('id');
      this.reload(() => {
        if (id) this.openLista(Number(id));
        else if (!this.creating) {
          this.selectedId = null;
          this.lista.set(null);
          this.detalles.set([]);
        }
      });
    });
  }

  emptyForm(): UpsertListaPrecioRequest {
    const today = new Date().toISOString().slice(0, 10);
    return {
      descripcion: '',
      esListaPrecioEcommerce: true,
      precioConImpuestos: true,
      canalEcommerce: 'B2C',
      fechaInicioVigencia: today,
      fechaFinVigencia: null,
      activo: true,
      cantidadMinimaMayorista: 0,
      porDescuentoMinorista: 0,
      porDescuentoMayorista: 0,
    };
  }

  filtered(): AdminListaPrecio[] {
    const q = this.filter.trim().toLowerCase();
    if (!q) return this.listas();
    return this.listas().filter(
      (l) => l.descripcion.toLowerCase().includes(q) || l.canalEcommerce.toLowerCase().includes(q),
    );
  }

  listaEstadoLabel(e: number): string {
    return ({ 0: 'Anulada', 1: 'Pendiente', 2: 'Autorizada', 3: 'Cancelada' } as Record<number, string>)[e] ?? String(e);
  }

  listaEstadoBadge(e: number): string {
    return (
      {
        0: 'badge-soft-danger',
        1: 'badge-soft-warning',
        2: 'badge-soft-success',
        3: 'badge-soft-danger',
      }[e] ?? 'badge-soft-warning'
    );
  }

  authLista(id: number, op: 'autorizar' | 'anular' | 'cancelar'): void {
    const call =
      op === 'autorizar'
        ? this.api.autorizarListaPrecio(id)
        : op === 'anular'
          ? this.api.anularListaPrecio(id)
          : this.api.cancelarListaPrecio(id);
    call.subscribe({
      next: () => {
        this.ok.set(`Lista ${op}`);
        this.reload();
        if (this.selectedId === id) this.openLista(id);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  reload(after?: () => void): void {
    this.error.set(null);
    this.api.listListasPrecio().subscribe({
      next: (rows) => {
        this.listas.set(rows);
        after?.();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  startCreate(): void {
    this.creating = true;
    this.selectedId = null;
    this.form = this.emptyForm();
    this.lista.set(null);
    this.detalles.set([]);
  }

  openLista(id: number): void {
    this.creating = false;
    this.selectedId = id;
    void this.router.navigate(['/listas-precios', id]);
    this.api.getListaPrecio(id).subscribe({
      next: (l) => {
        this.lista.set(l);
        this.form = {
          descripcion: l.descripcion,
          nroListaPrecio: l.nroListaPrecio,
          esListaPrecioEcommerce: l.esListaPrecioEcommerce,
          precioConImpuestos: l.precioConImpuestos,
          canalEcommerce: l.canalEcommerce,
          fechaInicioVigencia: l.fechaInicioVigencia?.slice(0, 10) ?? null,
          fechaFinVigencia: l.fechaFinVigencia?.slice(0, 10) ?? null,
          activo: l.activo,
          cantidadMinimaMayorista: l.cantidadMinimaMayorista ?? 0,
          porDescuentoMinorista: l.porDescuentoMinorista ?? 0,
          porDescuentoMayorista: l.porDescuentoMayorista ?? 0,
        };
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.loadDetalles(id);
    this.searchEmb();
  }

  loadDetalles(id: number): void {
    this.api.listDetallesLista(id).subscribe({
      next: (rows) => {
        this.detalles.set(rows);
        const next: Record<number, { contado: number; cheque: number; credito: number }> = {};
        for (const d of rows) {
          next[d.id] = {
            contado: d.precioContado,
            cheque: d.precioCheque,
            credito: d.precioCredito,
          };
        }
        this.draft = next;
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  backList(ev?: Event): void {
    ev?.preventDefault();
    this.creating = false;
    this.selectedId = null;
    this.lista.set(null);
    void this.router.navigateByUrl('/listas-precios');
    this.reload();
  }

  saveLista(): void {
    if (!this.form.descripcion?.trim()) {
      this.error.set('Descripción requerida');
      return;
    }
    const body = { ...this.form, descripcion: this.form.descripcion.trim() };
    if (this.creating) {
      this.api.createListaPrecio(body).subscribe({
        next: (l) => {
          this.ok.set('Lista creada');
          this.openLista(l.id);
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
      return;
    }
    if (!this.selectedId) return;
    this.api.updateListaPrecio(this.selectedId, body).subscribe({
      next: () => {
        this.ok.set('Lista actualizada');
        this.reload();
        this.openLista(this.selectedId!);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  removeLista(l: AdminListaPrecio): void {
    if (!confirm(`¿Desactivar lista ${l.descripcion}?`)) return;
    this.api.deleteListaPrecio(l.id).subscribe({
      next: () => {
        this.ok.set('Lista desactivada');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  searchEmb(): void {
    this.api.searchEmbalajes(this.embSearch || undefined).subscribe({
      next: (rows) => {
        this.embalajes.set(rows);
        if (!this.newEmbId && rows.length) this.newEmbId = rows[0].idArticuloEmbalaje;
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  addDetalle(): void {
    if (!this.selectedId || !this.newEmbId) return;
    this.api
      .upsertDetalleLista(this.selectedId, {
        idArticuloEmbalaje: this.newEmbId,
        precioContado: this.newPrecio,
      })
      .subscribe({
        next: () => {
          this.ok.set('Detalle agregado');
          this.newPrecio = 0;
          this.loadDetalles(this.selectedId!);
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  saveDetalle(d: AdminDetalleLista): void {
    if (!this.selectedId) return;
    const row = this.draft[d.id];
    this.api
      .updateDetalleLista(this.selectedId, d.id, {
        precioContado: row?.contado ?? d.precioContado,
        precioCheque: row?.cheque ?? d.precioCheque,
        precioCredito: row?.credito ?? d.precioCredito,
        activo: true,
      })
      .subscribe({
        next: () => {
          this.ok.set('Precios actualizados');
          this.loadDetalles(this.selectedId!);
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  setDraft(id: number, field: 'contado' | 'cheque' | 'credito', value: number): void {
    const d = this.detalles().find((x) => x.id === id);
    const current = this.draft[id] ?? {
      contado: d?.precioContado ?? 0,
      cheque: d?.precioCheque ?? 0,
      credito: d?.precioCredito ?? 0,
    };
    this.draft[id] = { ...current, [field]: Number(value) || 0 };
  }

  removeDetalle(d: AdminDetalleLista): void {
    if (!this.selectedId) return;
    this.api.deleteDetalleLista(this.selectedId, d.id).subscribe({
      next: () => {
        this.ok.set('Detalle desactivado');
        this.loadDetalles(this.selectedId!);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  exportLista(): void {
    if (!this.selectedId) return;
    this.api.exportPlanilla('lista', this.selectedId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `lista-${this.selectedId}-${Date.now()}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Planilla lista exportada');
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  importLista(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file || !this.selectedId) return;
    this.api.importPlanilla('lista', file, this.selectedId).subscribe({
      next: (r) => {
        this.ok.set(`Import lista: ${JSON.stringify(r)}`);
        this.loadDetalles(this.selectedId!);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
