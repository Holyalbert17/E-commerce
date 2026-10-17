import { DecimalPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AdminApiService,
  AdminAgencia,
  AdminCliente,
  AdminListaPrecio,
  AdminListaPrecioCuenta,
  AdminMayoristaSolicitud,
  AdminTipoDocumento,
  PagedResult,
  UpsertClienteRequest,
} from '../core/admin-api.service';

type Panel = 'list' | 'create' | 'edit' | 'asignar' | 'historial';

@Component({
  standalone: true,
  imports: [FormsModule, DecimalPipe, DatePipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Clientes</h1>
        <p class="dx-page-sub">Cuentas e-commerce · lista de precios y solicitudes mayorista.</p>
      </div>
      <div class="dx-filters">
        @if (panel === 'list') {
          <button type="button" class="btn btn-primary btn-sm" (click)="openCreate()">
            <i class="fas fa-plus mr-1"></i> Nuevo
          </button>
          <button type="button" class="btn btn-outline-success btn-sm" (click)="exportClientes()">
            <i class="fas fa-file-csv mr-1"></i> CSV
          </button>
          <button type="button" class="btn btn-outline-success btn-sm" (click)="exportClientesXlsx()">
            <i class="fas fa-file-excel mr-1"></i> Excel
          </button>
          <label class="btn btn-outline-secondary btn-sm mb-0">
            <i class="fas fa-file-import mr-1"></i> Import
            <input type="file" accept=".csv,text/csv" hidden (change)="importClientes($event)" />
          </label>
        } @else {
          <button type="button" class="btn btn-outline-secondary btn-sm" (click)="back()">
            <i class="fas fa-arrow-left mr-1"></i> Volver al listado
          </button>
        }
      </div>
    </div>

    @if (panel === 'list') {
      <div class="dx-filterbar">
        <input class="form-control form-control-sm" type="search" placeholder="Buscar razón social, doc, email…"
               [(ngModel)]="search" (ngModelChange)="onSearchChange()" (keyup.enter)="applyFilters(1)" />
        <select class="custom-select custom-select-sm" style="max-width:110px" [(ngModel)]="pageSize" (change)="applyFilters(1)">
          <option [ngValue]="20">20 / pág</option>
          <option [ngValue]="50">50 / pág</option>
        </select>
        <button type="button" class="btn btn-primary btn-sm" (click)="applyFilters(1)">
          <i class="fas fa-search mr-1"></i> Filtrar
        </button>
        @if (search) {
          <button type="button" class="btn btn-link btn-sm" (click)="clearFilters()">Limpiar</button>
        }
      </div>
    }

    @if (panel === 'list' && solicitudes().length) {
      <div class="dx-attention">
        <a class="dx-attention-item" href="#clientes-mayorista">
          <i class="fas fa-user-tie"></i>
          <div>
            <strong>{{ solicitudes().length }}</strong>
            <span>solicitudes mayorista para revisar</span>
          </div>
        </a>
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

    @if (panel === 'list') {
      <div class="card card-outline card-secondary mb-3" id="clientes-mayorista">
        <div class="card-header">
          <h3 class="card-title">Solicitudes mayorista</h3>
          <div class="card-tools">
            <button type="button" class="btn btn-tool" (click)="loadMayorista()">
              <i class="fas fa-sync-alt"></i>
            </button>
          </div>
        </div>
        <div class="card-body p-0 table-responsive dx-table-sticky" style="max-height:240px">
          <table class="table table-sm table-hover mb-0">
            <thead>
              <tr>
                <th>Usuario</th>
                <th>Estado</th>
                <th>Compras</th>
                <th>Cuenta</th>
                <th style="width:140px"></th>
              </tr>
            </thead>
            <tbody>
              @for (s of solicitudes(); track s.idUsuarioEcommerce) {
                <tr>
                  <td>
                    <strong>{{ s.nombre }}</strong>
                    <small class="d-block text-muted">{{ s.email || '—' }}</small>
                  </td>
                  <td><span class="badge badge-soft-warning">{{ s.estadoMayorista }}</span></td>
                  <td>{{ s.montoComprasAcumulado | number:'1.0-0' }}</td>
                  <td>{{ s.razonSocial || '—' }}</td>
                  <td class="text-nowrap">
                    @if (s.estadoMayorista === 'Solicitado') {
                      <select class="custom-select custom-select-sm mb-1"
                              [(ngModel)]="listaAprobar[s.idUsuarioEcommerce]">
                        <option [ngValue]="null">Lista default</option>
                        @for (l of listas(); track l.id) {
                          <option [ngValue]="l.id">{{ l.descripcion }}</option>
                        }
                      </select>
                      <button type="button" class="btn btn-xs btn-success mr-1"
                              (click)="aprobar(s)" title="Aprobar + lista ERP">
                        <i class="fas fa-check"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-danger"
                              (click)="rechazar(s)" title="Rechazar">
                        <i class="fas fa-times"></i>
                      </button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="5">
                    <div class="dx-empty py-3">
                      <i class="fas fa-user-tie"></i>
                      <p>Sin solicitudes mayorista pendientes.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <div class="card card-outline card-warning">
        <div class="card-header">
          <h3 class="card-title">Clientes / Cuentas</h3>
          @if (page(); as pg) {
            <div class="card-tools"><span class="badge badge-soft-warning">{{ pg.totalCount }}</span></div>
          }
        </div>
        <div class="card-body p-0 table-responsive dx-table-sticky">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th style="width:80px">Nro</th>
                <th>Razón social</th>
                <th>Documento</th>
                <th>Teléfono</th>
                <th>Email</th>
                <th>Lista precio</th>
                <th style="width:80px">Activo</th>
                <th style="width:140px" class="text-center">Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (c of page()?.items ?? []; track c.id) {
                <tr [class.table-secondary]="!c.activo">
                  <td>{{ c.nroCuenta }}</td>
                  <td>
                    <strong>{{ c.razonSocial }}</strong>
                    <small>{{ c.nombreCuenta || '' }} {{ c.apellidoCuenta || '' }}</small>
                  </td>
                  <td>{{ c.numeroDocumento || '—' }}</td>
                  <td>{{ c.telefono || '—' }}</td>
                  <td>{{ c.email || '—' }}</td>
                  <td>
                    @if (c.listaDescripcion) {
                      <span class="badge badge-soft-info">{{ c.listaDescripcion }}</span>
                    } @else {
                      <span class="text-muted">Sin asignar</span>
                    }
                  </td>
                  <td class="text-center">
                    @if (c.activo) {
                      <span class="badge badge-soft-success">Sí</span>
                    } @else {
                      <span class="badge badge-soft-danger">No</span>
                    }
                  </td>
                  <td class="table-actions text-center text-nowrap">
                    <a class="btn btn-xs btn-outline-primary" title="Ver pedidos"
                       [routerLink]="['/pedidos']"
                       [queryParams]="{ q: c.numeroDocumento || c.razonSocial }">
                      <i class="fas fa-shopping-bag"></i>
                    </a>
                    <a class="btn btn-xs btn-outline-secondary" title="Ver carritos"
                       [routerLink]="['/carritos']"
                       [queryParams]="{ q: c.numeroDocumento || c.razonSocial }">
                      <i class="fas fa-shopping-cart"></i>
                    </a>
                    <button type="button" class="btn btn-xs btn-default" title="Editar" (click)="openEdit(c)">
                      <i class="fas fa-edit"></i>
                    </button>
                    <button type="button" class="btn btn-xs btn-warning" title="Asignar lista"
                            (click)="openAsignar(c)" [disabled]="!c.activo">
                      <i class="fas fa-tags"></i>
                    </button>
                    <button type="button" class="btn btn-xs btn-info" title="Historial listas"
                            (click)="openHistorial(c)">
                      <i class="fas fa-history"></i>
                    </button>
                    <button type="button" class="btn btn-xs btn-danger" title="Eliminar"
                            (click)="remove(c)" [disabled]="!c.activo">
                      <i class="fas fa-trash"></i>
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="8">
                    <div class="dx-empty py-4">
                      @if (loading()) {
                        <i class="fas fa-spinner fa-spin"></i>
                        <p>Cargando clientes…</p>
                      } @else {
                        <i class="fas fa-users"></i>
                        <p>Sin clientes con esos filtros. Creá uno o importá CSV.</p>
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
            <div class="float-left text-muted">{{ pg.totalCount }} clientes · pág. {{ pg.page }} / {{ pg.totalPages || 1 }}</div>
            <div class="float-right">
              <button type="button" class="btn btn-default btn-sm mr-1"
                      [disabled]="pg.page <= 1" (click)="applyFilters(pg.page - 1)">Anterior</button>
              <button type="button" class="btn btn-default btn-sm"
                      [disabled]="pg.page >= pg.totalPages" (click)="applyFilters(pg.page + 1)">Siguiente</button>
            </div>
          </div>
        }
      </div>
    }

    @if (panel === 'create' || panel === 'edit') {
      <div class="card card-outline card-info">
        <div class="card-header">
          <h3 class="card-title">{{ panel === 'create' ? 'Nuevo cliente' : 'Editar cliente' }}</h3>
        </div>
        <div class="card-body">
          <form class="form-horizontal dx-erp-form" (ngSubmit)="save()">
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Razón social</label>
              <div class="col-sm-6">
                <input class="form-control" [(ngModel)]="form.razonSocial" name="rs" required />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Nombre / Apellido</label>
              <div class="col-sm-3">
                <input class="form-control" [(ngModel)]="form.nombreCuenta" name="nom" />
              </div>
              <div class="col-sm-3">
                <input class="form-control" [(ngModel)]="form.apellidoCuenta" name="ape" />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Email</label>
              <div class="col-sm-6">
                <input class="form-control" type="email" [(ngModel)]="form.email" name="email" />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Documento</label>
              <div class="col-sm-3">
                <select class="custom-select" [(ngModel)]="form.idTipoDocumento" name="tdoc">
                  <option [ngValue]="null">Tipo…</option>
                  @for (t of tiposDoc(); track t.id) {
                    <option [ngValue]="t.id">{{ t.nombre }}</option>
                  }
                </select>
              </div>
              <div class="col-sm-3">
                <input class="form-control" [(ngModel)]="form.numeroDocumento" name="ndoc" placeholder="N° documento" />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Teléfono</label>
              <div class="col-sm-6">
                <input class="form-control" [(ngModel)]="form.telefono" name="tel" placeholder="0981…" />
                <small class="form-text text-muted">
                  Se guarda en el usuario e-com vinculado (o crea un contacto bloqueado si no hay).
                </small>
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Dirección</label>
              <div class="col-sm-6">
                <input class="form-control" [(ngModel)]="form.direccion" name="dir" />
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Lista precio</label>
              <div class="col-sm-6">
                <select class="custom-select" [(ngModel)]="form.idListaPrecio" name="lista">
                  <option [ngValue]="null">— Sin asignar —</option>
                  @for (l of listas(); track l.id) {
                    <option [ngValue]="l.id">{{ l.nroListaPrecio }} — {{ l.descripcion }} ({{ l.canalEcommerce }})</option>
                  }
                </select>
              </div>
            </div>
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Agencia / sucursal</label>
              <div class="col-sm-6">
                <select class="custom-select" [(ngModel)]="form.idAgencia" name="agencia">
                  <option [ngValue]="null">— Sin asignar —</option>
                  @for (a of agencias(); track a.id) {
                    <option [ngValue]="a.id">{{ a.codAgencia }} — {{ a.descripcionAgencia }}</option>
                  }
                </select>
              </div>
            </div>
            @if (panel === 'edit') {
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Activo</label>
                <div class="col-sm-6">
                  <div class="custom-control custom-switch mt-2">
                    <input type="checkbox" class="custom-control-input" id="cliAct"
                           [(ngModel)]="form.activo" name="activo" />
                    <label class="custom-control-label" for="cliAct"></label>
                  </div>
                </div>
              </div>
            }
            <div class="form-group row mb-0">
              <div class="offset-sm-3 col-sm-9">
                <button type="button" class="btn btn-info btn-sm mr-2" (click)="back()">
                  <i class="fas fa-arrow-left mr-1"></i> Volver
                </button>
                <button type="submit" class="btn btn-primary btn-sm">
                  <i class="fas fa-save mr-1"></i> Guardar
                </button>
              </div>
            </div>
          </form>
        </div>
      </div>
    }

    @if (panel === 'asignar' && selected) {
      <div class="card card-outline card-warning">
        <div class="card-header">
          <h3 class="card-title">
            Asignar lista de precio
            <small class="text-muted ml-2">{{ selected.razonSocial }}</small>
          </h3>
        </div>
        <div class="card-body">
          <p class="text-muted">
            Actual: <strong>{{ selected.listaDescripcion || 'Sin asignar' }}</strong>
          </p>
          <div class="form-group row">
            <label class="col-sm-3 col-form-label">Lista vigente</label>
            <div class="col-sm-6">
              <select class="custom-select" [(ngModel)]="asignarListaId">
                @for (l of listas(); track l.id) {
                  <option [ngValue]="l.id">{{ l.nroListaPrecio }} — {{ l.descripcion }} ({{ l.canalEcommerce }})</option>
                }
              </select>
            </div>
          </div>
        </div>
        <div class="card-footer">
          <button type="button" class="btn btn-info btn-sm mr-2" (click)="back()">Volver</button>
          <button type="button" class="btn btn-warning btn-sm" (click)="asignar()"
                  [disabled]="!asignarListaId">
            <i class="fas fa-tags mr-1"></i> Asignar
          </button>
        </div>
      </div>
    }

    @if (panel === 'historial' && selected) {
      <div class="card card-outline card-info">
        <div class="card-header">
          <h3 class="card-title">
            Historial listas de precio
            <small class="text-muted ml-2">{{ selected.razonSocial }}</small>
          </h3>
          <div class="card-tools">
            <button type="button" class="btn btn-tool" (click)="openHistorial(selected!)">
              <i class="fas fa-sync-alt"></i>
            </button>
          </div>
        </div>
        <div class="card-body p-0 table-responsive">
          <table class="table table-sm table-striped mb-0">
            <thead>
              <tr>
                <th>Lista</th>
                <th>Estado</th>
                <th>Fecha</th>
                <th>Usuario</th>
              </tr>
            </thead>
            <tbody>
              @for (h of historial(); track h.id) {
                <tr [class.table-success]="h.activo">
                  <td>
                    <strong>{{ h.listaDescripcion || ('#' + h.idListaPrecio) }}</strong>
                    @if (h.nroListaPrecio != null) {
                      <small class="d-block text-muted">N° {{ h.nroListaPrecio }}</small>
                    }
                  </td>
                  <td>
                    @if (h.activo) {
                      <span class="badge badge-soft-success">Vigente</span>
                    } @else {
                      <span class="badge badge-soft-secondary">Histórico</span>
                    }
                  </td>
                  <td>{{ h.ultModificacion | date:'short' }}</td>
                  <td>{{ h.userName || '—' }}</td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="4">
                    <div class="dx-empty py-3">
                      <i class="fas fa-history"></i>
                      <p>Sin asignaciones registradas. Asigná una lista para iniciar el historial.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <div class="card-footer">
          <button type="button" class="btn btn-info btn-sm mr-2" (click)="back()">Volver</button>
          <button type="button" class="btn btn-warning btn-sm" (click)="openAsignar(selected!)">
            <i class="fas fa-tags mr-1"></i> Asignar lista
          </button>
        </div>
      </div>
    }
  `,
})
export class ClientesPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;

  panel: Panel = 'list';
  search = '';
  pageSize = 20;
  selected: AdminCliente | null = null;
  asignarListaId: number | null = null;
  editId = 0;

  readonly page = signal<PagedResult<AdminCliente> | null>(null);
  readonly listas = signal<AdminListaPrecio[]>([]);
  readonly agencias = signal<AdminAgencia[]>([]);
  readonly tiposDoc = signal<AdminTipoDocumento[]>([]);
  readonly solicitudes = signal<AdminMayoristaSolicitud[]>([]);
  readonly historial = signal<AdminListaPrecioCuenta[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly loading = signal(false);

  form: UpsertClienteRequest = this.emptyForm();
  listaAprobar: Record<number, number | null> = {};

  ngOnInit(): void {
    this.loadMayorista();
    this.api.listasVigentesCliente().subscribe({
      next: (rows) => this.listas.set(rows),
      error: () =>
        this.api.listListasPrecio().subscribe({
          next: (rows) => this.listas.set(rows.filter((l) => l.activo && l.esListaPrecioEcommerce)),
        }),
    });
    this.api.listAgencias().subscribe({
      next: (rows) => this.agencias.set(rows.filter((a) => a.activo !== false)),
      error: () => this.agencias.set([]),
    });
    this.api.listTiposDocumento().subscribe({
      next: (rows) => this.tiposDoc.set(rows),
      error: () => this.tiposDoc.set([]),
    });
    this.route.queryParamMap.subscribe((q) => {
      this.search = q.get('q') || '';
      const ps = Number(q.get('ps'));
      this.pageSize = ps === 50 ? 50 : 20;
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
        ps: this.pageSize === 20 ? null : this.pageSize,
        page: pageNum === 1 ? null : pageNum,
      },
      replaceUrl: true,
    });
  }

  clearFilters(): void {
    this.search = '';
    this.applyFilters(1);
  }

  loadMayorista(): void {
    this.api.listSolicitudesMayorista().subscribe({
      next: (rows) => this.solicitudes.set(rows),
      error: () => this.solicitudes.set([]),
    });
  }

  aprobar(s: AdminMayoristaSolicitud): void {
    const listaId = this.listaAprobar[s.idUsuarioEcommerce] ?? s.idListaPrecio ?? null;
    this.api.aprobarMayorista(s.idUsuarioEcommerce, listaId).subscribe({
      next: () => {
        this.ok.set(`Mayorista aprobado: ${s.nombre}` + (listaId ? ` · lista #${listaId}` : ''));
        this.loadMayorista();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  rechazar(s: AdminMayoristaSolicitud): void {
    this.api.rechazarMayorista(s.idUsuarioEcommerce).subscribe({
      next: () => {
        this.ok.set(`Solicitud rechazada: ${s.nombre}`);
        this.loadMayorista();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  emptyForm(): UpsertClienteRequest {
    return {
      razonSocial: '',
      nombreCuenta: '',
      apellidoCuenta: '',
      email: '',
      direccion: '',
      idListaPrecio: null,
      idAgencia: null,
      idTipoDocumento: null,
      numeroDocumento: '',
      telefono: '',
      activo: true,
    };
  }

  load(pageNum: number): void {
    this.error.set(null);
    this.loading.set(true);
    this.api.listClientes({ search: this.search || undefined, page: pageNum, pageSize: this.pageSize }).subscribe({
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

  back(): void {
    this.panel = 'list';
    this.selected = null;
    this.load(this.page()?.page ?? 1);
  }

  openCreate(): void {
    this.form = this.emptyForm();
    this.panel = 'create';
  }

  openEdit(c: AdminCliente): void {
    this.editId = c.id;
    this.selected = c;
    this.form = {
      razonSocial: c.razonSocial,
      nombreCuenta: c.nombreCuenta,
      apellidoCuenta: c.apellidoCuenta,
      email: c.email,
      direccion: c.direccion,
      idListaPrecio: c.idListaPrecio,
      idAgencia: c.idAgencia,
      idTipoDocumento: c.idTipoDocumento ?? null,
      numeroDocumento: c.numeroDocumento || '',
      telefono: c.telefono || '',
      activo: c.activo,
    };
    this.panel = 'edit';
  }

  openAsignar(c: AdminCliente): void {
    this.selected = c;
    this.asignarListaId = c.idListaPrecio ?? this.listas()[0]?.id ?? null;
    this.panel = 'asignar';
  }

  openHistorial(c: AdminCliente): void {
    this.selected = c;
    this.panel = 'historial';
    this.historial.set([]);
    this.api.historialListasCliente(c.id).subscribe({
      next: (rows) => this.historial.set(rows),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  save(): void {
    if (!this.form.razonSocial?.trim()) {
      this.error.set('Razón social requerida');
      return;
    }
    if (this.panel === 'create') {
      this.api.createCliente(this.form).subscribe({
        next: () => {
          this.ok.set('Cliente creado');
          this.back();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
      return;
    }
    this.api.updateCliente(this.editId, this.form).subscribe({
      next: () => {
        this.ok.set('Cliente actualizado');
        this.back();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  asignar(): void {
    if (!this.selected || !this.asignarListaId) return;
    this.api.asignarListaCliente(this.selected.id, this.asignarListaId).subscribe({
      next: () => {
        this.ok.set('Lista asignada');
        this.back();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  remove(c: AdminCliente): void {
    if (!confirm(`¿Desactivar cliente ${c.razonSocial}?`)) return;
    this.api.deleteCliente(c.id).subscribe({
      next: () => {
        this.ok.set('Cliente desactivado');
        this.load(this.page()?.page ?? 1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  exportClientes(): void {
    this.api.exportPlanilla('clientes', undefined, 'csv').subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `clientes-${Date.now()}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Planilla clientes exportada (CSV)');
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  exportClientesXlsx(): void {
    this.api.exportPlanilla('clientes', undefined, 'xlsx').subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `clientes-${Date.now()}.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
        this.ok.set('Planilla clientes exportada (Excel)');
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  importClientes(ev: Event): void {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.api.importPlanilla('clientes', file).subscribe({
      next: (r) => {
        this.ok.set(`Import clientes: ${JSON.stringify(r)}`);
        this.load(1);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
