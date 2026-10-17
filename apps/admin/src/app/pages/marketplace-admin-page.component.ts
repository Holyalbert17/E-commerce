import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminApiService } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';

interface AdminResena {
  id: number;
  idArticulo: number;
  email: string;
  canalEcommerce: string;
  puntuacion: number;
  titulo: string | null;
  comentario: string | null;
  fechaResena: string;
}

interface AdminCupon {
  id: number;
  codigo: string;
  descripcion: string;
  tipoDescuento: string;
  valorDescuento: number;
  montoMinimo: number;
  canalEcommerce: string;
  usosActuales: number;
  usosMaximos: number | null;
  activo: boolean;
}

interface AdminDev {
  id: number;
  idPedidoMovil: number;
  idArticulo: number | null;
  motivo: string;
  detalle: string | null;
  estado: string;
  fechaSolicitud: string;
  nroPedido?: number | null;
  email?: string | null;
  articuloNombre?: string | null;
  urlAdjunto?: string | null;
  idNotaCreditoErp?: number | null;
  nroNotaCredito?: number | null;
  puedeDescargarNotaCredito?: boolean;
  erpMensaje?: string | null;
}

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe, RouterLink],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Marketplace</h1>
        <p class="dx-page-sub">
          Reseñas, cupones y devoluciones · Chat en
          <a routerLink="/crm">CRM</a>. El código del home (Contenido → cupón) debe coincidir con un cupón activo acá.
        </p>
      </div>
      <button type="button" class="btn btn-default btn-sm" (click)="reload()">
        <i class="fas fa-sync-alt mr-1"></i> Actualizar
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

    <ul class="nav nav-tabs dx-erp-tabs mb-3">
      @if (canResenas()) {
        <li class="nav-item">
          <a class="nav-link" [class.active]="tab() === 'resenas'" href="#" (click)="setTab($event, 'resenas')">
            Reseñas
            <span class="badge badge-secondary ml-1">{{ resenas().length }}</span>
          </a>
        </li>
      }
      @if (canCupones()) {
        <li class="nav-item">
          <a class="nav-link" [class.active]="tab() === 'cupones'" href="#" (click)="setTab($event, 'cupones')">
            Cupones
            <span class="badge badge-secondary ml-1">{{ cupones().length }}</span>
          </a>
        </li>
      }
      @if (canDev()) {
        <li class="nav-item">
          <a class="nav-link" [class.active]="tab() === 'dev'" href="#" (click)="setTab($event, 'dev')">
            Devoluciones
            @if (pendientesDev() > 0) {
              <span class="badge badge-warning ml-1">{{ pendientesDev() }}</span>
            } @else {
              <span class="badge badge-secondary ml-1">{{ devoluciones().length }}</span>
            }
          </a>
        </li>
      }
    </ul>

    @if (tab() === 'resenas' && canResenas()) {
      <div class="card card-outline card-primary mb-3">
        <div class="card-body py-2">
          <div class="dx-filters">
            <input class="form-control form-control-sm" [(ngModel)]="qResena" name="qr" placeholder="Buscar email / comentario…" />
            <select class="custom-select custom-select-sm" [(ngModel)]="filtroCanalResena" name="fcr" style="max-width:140px">
              <option value="">Canal: todos</option>
              <option value="B2C">B2C</option>
              <option value="B2B">B2B</option>
            </select>
          </div>
        </div>
      </div>
      <div class="card">
        <div class="card-body table-responsive p-0">
          <table class="table table-hover table-striped mb-0">
            <thead>
              <tr>
                <th style="width:70px">ID</th>
                <th>Artículo</th>
                <th>Calificación</th>
                <th>Cliente</th>
                <th>Comentario</th>
                <th style="width:140px">Fecha</th>
                <th style="width:160px"></th>
              </tr>
            </thead>
            <tbody>
              @for (r of resenasFiltradas(); track r.id) {
                <tr>
                  <td>{{ r.id }}</td>
                  <td><code>#{{ r.idArticulo }}</code></td>
                  <td>
                    <span class="dx-stars">{{ stars(r.puntuacion) }}</span>
                    <small>{{ r.puntuacion }}/5 · {{ r.canalEcommerce }}</small>
                  </td>
                  <td>{{ r.email }}</td>
                  <td>
                    @if (r.titulo) {
                      <strong>{{ r.titulo }}</strong><br />
                    }
                    {{ r.comentario || '—' }}
                  </td>
                  <td>{{ r.fechaResena | date: 'short' }}</td>
                  <td class="table-actions text-nowrap">
                    <button type="button" class="btn btn-xs btn-success" (click)="aprobar(r.id, true)" title="Aprobar">
                      <i class="fas fa-check"></i> Aprobar
                    </button>
                    <button type="button" class="btn btn-xs btn-outline-danger" (click)="aprobar(r.id, false)" title="Ocultar">
                      <i class="fas fa-eye-slash"></i>
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7">
                    <div class="dx-empty">
                      <i class="far fa-star"></i>
                      <p>No hay reseñas con ese filtro.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }

    @if (tab() === 'cupones' && canCupones()) {
      <div class="alert alert-light border mb-3">
        <strong>Equipo de marketing</strong>
        <ol class="mb-0 pl-3 mt-1 small">
          <li>Creá y activá el cupón acá (código interno, vigencia, canal).</li>
          <li>
            Publicitalo con un <a routerLink="/promociones">banner en Promociones</a> o en Contenido —
            no hace falta mostrar el código en el home.
          </li>
          <li>El cliente aplica el código en el checkout.</li>
        </ol>
      </div>
      <div class="card card-outline card-success mb-3">
        <div class="card-header"><h3 class="card-title">Alta de cupón</h3></div>
        <div class="card-body">
          <div class="form-row">
            <div class="form-group col-md-2">
              <label>Código</label>
              <input class="form-control text-uppercase" [(ngModel)]="cupon.codigo" name="cc" placeholder="VERANO10" />
            </div>
            <div class="form-group col-md-3">
              <label>Descripción</label>
              <input class="form-control" [(ngModel)]="cupon.descripcion" name="cd" placeholder="Promo visible" />
            </div>
            <div class="form-group col-md-2">
              <label>Tipo</label>
              <select class="form-control" [(ngModel)]="cupon.tipoDescuento" name="ct">
                <option value="porcentaje">Porcentaje %</option>
                <option value="monto">Monto fijo</option>
              </select>
            </div>
            <div class="form-group col-md-2">
              <label>Valor</label>
              <input type="number" class="form-control" [(ngModel)]="cupon.valorDescuento" name="cv" />
            </div>
            <div class="form-group col-md-3">
              <label>Monto mínimo</label>
              <input type="number" class="form-control" [(ngModel)]="cupon.montoMinimo" name="cm" />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group col-md-2">
              <label>Tope descuento</label>
              <input type="number" class="form-control" [(ngModel)]="cupon.montoMaximoDescuento" name="cmax"
                placeholder="Opcional" />
            </div>
            <div class="form-group col-md-2">
              <label>Canal</label>
              <select class="form-control" [(ngModel)]="cupon.canalEcommerce" name="ccan">
                <option value="ALL">Todos</option>
                <option value="WEB">Web</option>
                <option value="APP">App</option>
              </select>
            </div>
            <div class="form-group col-md-2">
              <label>Inicio</label>
              <input type="date" class="form-control" [(ngModel)]="cupon.fechaInicio" name="cfi" />
            </div>
            <div class="form-group col-md-2">
              <label>Fin</label>
              <input type="date" class="form-control" [(ngModel)]="cupon.fechaFin" name="cff" />
            </div>
            <div class="form-group col-md-2">
              <label>Usos máximos</label>
              <input type="number" class="form-control" [(ngModel)]="cupon.usosMaximos" name="cum"
                placeholder="∞" />
            </div>
            <div class="form-group col-md-1">
              <label>Por usuario</label>
              <input type="number" class="form-control" [(ngModel)]="cupon.usosPorUsuario" name="cup" min="1" />
            </div>
            <div class="form-group col-md-1">
              <label>&nbsp;</label>
              <button type="button" class="btn btn-primary btn-block" (click)="crearCupon()" [disabled]="busy()">
                Alta
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h3 class="card-title mb-0">Cupones</h3>
          <input class="form-control form-control-sm" style="max-width:220px" [(ngModel)]="qCupon" name="qc"
            placeholder="Filtrar código…" />
        </div>
        <div class="card-body table-responsive p-0">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th>Código</th>
                <th>Descripción</th>
                <th>Descuento</th>
                <th>Canal</th>
                <th>Usos</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (c of cuponesFiltrados(); track c.id) {
                <tr [class.table-secondary]="!c.activo">
                  <td><code>{{ c.codigo }}</code></td>
                  <td>{{ c.descripcion }}</td>
                  <td>
                    @if (c.tipoDescuento === 'porcentaje') {
                      {{ c.valorDescuento }}%
                    } @else {
                      {{ c.valorDescuento | number:'1.0-0' }} Gs
                    }
                    <small>mín. {{ c.montoMinimo | number:'1.0-0' }}</small>
                  </td>
                  <td>{{ c.canalEcommerce }}</td>
                  <td>{{ c.usosActuales }} / {{ c.usosMaximos ?? '∞' }}</td>
                  <td>
                    @if (c.activo) {
                      <span class="badge badge-soft-success">Activo</span>
                    } @else {
                      <span class="badge badge-soft-danger">Inactivo</span>
                    }
                  </td>
                  <td>
                    <button type="button" class="btn btn-xs btn-outline-secondary" (click)="toggleCupon(c.id, !c.activo)">
                      {{ c.activo ? 'Desactivar' : 'Activar' }}
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7">
                    <div class="dx-empty">
                      <i class="fas fa-ticket-alt"></i>
                      <p>No hay cupones. Creá el primero arriba.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }

    @if (tab() === 'dev' && canDev()) {
      <div class="card card-outline card-warning mb-3">
        <div class="card-body py-3">
          <div class="d-flex flex-wrap align-items-center justify-content-between mb-2" style="gap:0.75rem">
            <div>
              <strong>Cola de devoluciones</strong>
              <div class="text-muted small">
                Con ecommerce integrado, el cliente genera la NC en el ERP al confirmar
                (MotivoNDNC tipo DEVOLUCION, igual que Stock → Notas de crédito).
                Acá quedan las que el ERP no pudo cerrar (sin factura, sin motivo, sin write-back).
              </div>
            </div>
            @if (pendientesDev() > 0) {
              <span class="badge badge-warning badge-pill px-3 py-2">
                {{ pendientesDev() }} pendiente{{ pendientesDev() === 1 ? '' : 's' }}
              </span>
            }
          </div>
          <div class="btn-group btn-group-sm flex-wrap" role="group" aria-label="Filtro estado">
            <button type="button" class="btn" [class.btn-warning]="filtroEstadoDev() === 'solicitada'" [class.btn-outline-secondary]="filtroEstadoDev() !== 'solicitada'" (click)="filtroEstadoDev.set('solicitada')">
              Pendientes ({{ pendientesDev() }})
            </button>
            <button type="button" class="btn" [class.btn-success]="filtroEstadoDev() === 'aprobada'" [class.btn-outline-secondary]="filtroEstadoDev() !== 'aprobada'" (click)="filtroEstadoDev.set('aprobada')">
              Aprobadas
            </button>
            <button type="button" class="btn" [class.btn-primary]="filtroEstadoDev() === 'completada'" [class.btn-outline-secondary]="filtroEstadoDev() !== 'completada'" (click)="filtroEstadoDev.set('completada')">
              Completadas
            </button>
            <button type="button" class="btn" [class.btn-danger]="filtroEstadoDev() === 'rechazada'" [class.btn-outline-secondary]="filtroEstadoDev() !== 'rechazada'" (click)="filtroEstadoDev.set('rechazada')">
              Rechazadas
            </button>
            <button type="button" class="btn" [class.btn-secondary]="filtroEstadoDev() === ''" [class.btn-outline-secondary]="filtroEstadoDev() !== ''" (click)="filtroEstadoDev.set('')">
              Todas
            </button>
          </div>
          <div class="mt-2">
            <input
              class="form-control form-control-sm"
              style="max-width:320px"
              [ngModel]="qDev()"
              (ngModelChange)="qDev.set($event)"
              name="qdev"
              placeholder="Buscar N° pedido, email, motivo…"
            />
          </div>
        </div>
      </div>
      <div class="card">
        <div class="card-body table-responsive p-0">
          <table class="table table-hover mb-0">
            <thead>
              <tr>
                <th style="width:90px">Pedido</th>
                <th>Cliente</th>
                <th>Motivo</th>
                <th style="width:120px">Estado</th>
                <th style="width:130px">Fecha</th>
                <th style="width:220px">Acción</th>
              </tr>
            </thead>
            <tbody>
              @for (d of devolucionesFiltradas(); track d.id) {
                <tr [class.table-warning]="d.estado === 'solicitada'">
                  <td>
                    <a [routerLink]="['/pedidos', d.idPedidoMovil]" class="font-weight-bold">
                      N° {{ d.nroPedido || d.idPedidoMovil }}
                    </a>
                    <div class="text-muted small">#{{ d.id }}</div>
                  </td>
                  <td>
                    <div>{{ d.email || '—' }}</div>
                    @if (d.articuloNombre) {
                      <small class="text-muted">{{ d.articuloNombre }}</small>
                    } @else if (d.idArticulo) {
                      <small class="text-muted">Art. #{{ d.idArticulo }}</small>
                    }
                  </td>
                  <td>
                    <strong>{{ d.motivo }}</strong>
                    @if (d.detalle) {
                      <div class="text-muted small">{{ d.detalle }}</div>
                    }
                    @if (d.erpMensaje) {
                      <div class="text-muted small"><i class="fas fa-info-circle"></i> {{ d.erpMensaje }}</div>
                    }
                    @if (d.idNotaCreditoErp) {
                      <div class="text-muted small">NC ERP #{{ d.nroNotaCredito || d.idNotaCreditoErp }}</div>
                    }
                    @if (d.urlAdjunto) {
                      <a [href]="d.urlAdjunto" target="_blank" rel="noopener" class="btn btn-xs btn-outline-secondary mt-1">
                        <i class="fas fa-image"></i> Ver foto
                      </a>
                    }
                  </td>
                  <td>
                    <span
                      class="badge"
                      [class.badge-soft-warning]="d.estado === 'solicitada'"
                      [class.badge-soft-success]="d.estado === 'aprobada' || d.estado === 'completada'"
                      [class.badge-soft-danger]="d.estado === 'rechazada'"
                      [class.badge-soft-info]="d.estado === 'completada'"
                    >
                      {{ estadoDevLabel(d.estado) }}
                    </span>
                  </td>
                  <td>{{ d.fechaSolicitud | date: 'short' }}</td>
                  <td class="table-actions text-nowrap">
                    @if (d.estado === 'solicitada') {
                      <button type="button" class="btn btn-xs btn-success" (click)="estadoDev(d, 'aprobada')" title="Aprobar devolución">
                        <i class="fas fa-check"></i> Aprobar
                      </button>
                      <button type="button" class="btn btn-xs btn-outline-danger" (click)="estadoDev(d, 'rechazada')" title="Rechazar">
                        <i class="fas fa-times"></i> Rechazar
                      </button>
                    } @else if (d.estado === 'aprobada') {
                      <button type="button" class="btn btn-xs btn-primary" (click)="estadoDev(d, 'completada')" title="Marcar reintegro listo">
                        <i class="fas fa-flag-checkered"></i> Completar
                      </button>
                      <a class="btn btn-xs btn-outline-secondary" [routerLink]="['/pedidos', d.idPedidoMovil]">Ver pedido</a>
                    } @else {
                      <a class="btn btn-xs btn-outline-secondary" [routerLink]="['/pedidos', d.idPedidoMovil]">Ver pedido</a>
                    }
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="6">
                    <div class="dx-empty">
                      <i class="fas fa-undo"></i>
                      <p>
                        @if (filtroEstadoDev() === 'solicitada') {
                          No hay devoluciones pendientes. ¡Todo al día!
                        } @else {
                          Sin devoluciones con ese filtro.
                        }
                      </p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `,
})
export class MarketplaceAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  readonly tab = signal<'resenas' | 'cupones' | 'dev'>('resenas');
  readonly resenas = signal<AdminResena[]>([]);
  readonly cupones = signal<AdminCupon[]>([]);
  readonly devoluciones = signal<AdminDev[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly busy = signal(false);

  readonly canResenas = computed(() => this.session.can('Site_App_Ecommerce_Resenas', 'manager:all'));
  readonly canCupones = computed(() => this.session.can('Site_App_Ecommerce_Cupones', 'manager:all'));
  readonly canDev = computed(() => this.session.can('Site_App_Ecommerce_Devoluciones', 'manager:all'));

  qResena = '';
  filtroCanalResena = '';
  qCupon = '';
  readonly qDev = signal('');
  readonly filtroEstadoDev = signal('solicitada');

  cupon = {
    codigo: '',
    descripcion: '',
    tipoDescuento: 'porcentaje',
    valorDescuento: 10,
    montoMinimo: 0,
    montoMaximoDescuento: null as number | null,
    canalEcommerce: 'ALL',
    fechaInicio: '',
    fechaFin: '',
    usosMaximos: null as number | null,
    usosPorUsuario: 1,
  };

  readonly resenasFiltradas = computed(() => {
    const q = this.qResena.trim().toLowerCase();
    const canal = this.filtroCanalResena;
    return this.resenas().filter((r) => {
      if (canal && r.canalEcommerce !== canal) return false;
      if (!q) return true;
      const blob = `${r.email} ${r.titulo || ''} ${r.comentario || ''} ${r.idArticulo}`.toLowerCase();
      return blob.includes(q);
    });
  });

  readonly cuponesFiltrados = computed(() => {
    const q = this.qCupon.trim().toLowerCase();
    if (!q) return this.cupones();
    return this.cupones().filter(
      (c) => c.codigo.toLowerCase().includes(q) || (c.descripcion || '').toLowerCase().includes(q),
    );
  });

  readonly pendientesDev = computed(() => this.devoluciones().filter((d) => d.estado === 'solicitada').length);

  readonly devolucionesFiltradas = computed(() => {
    const e = this.filtroEstadoDev();
    const q = this.qDev().trim().toLowerCase();
    return this.devoluciones().filter((d) => {
      if (e && d.estado !== e) return false;
      if (!q) return true;
      const blob = `${d.nroPedido || ''} ${d.idPedidoMovil} ${d.email || ''} ${d.motivo} ${d.detalle || ''} ${d.articuloNombre || ''}`.toLowerCase();
      return blob.includes(q);
    });
  });

  ngOnInit(): void {
    if (this.canResenas()) this.tab.set('resenas');
    else if (this.canCupones()) this.tab.set('cupones');
    else if (this.canDev()) this.tab.set('dev');
    this.reload();
  }

  setTab(ev: Event, t: 'resenas' | 'cupones' | 'dev'): void {
    ev.preventDefault();
    if (t === 'resenas' && !this.canResenas()) return;
    if (t === 'cupones' && !this.canCupones()) return;
    if (t === 'dev' && !this.canDev()) return;
    this.tab.set(t);
    this.error.set(null);
    this.reload();
  }

  stars(n: number): string {
    const v = Math.max(0, Math.min(5, Math.round(n)));
    return '★'.repeat(v) + '☆'.repeat(5 - v);
  }

  reload(): void {
    const t = this.tab();
    if (t === 'resenas') {
      this.api.listResenas().subscribe({
        next: (x) => this.resenas.set(x),
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
    } else if (t === 'cupones') {
      this.api.listCupones().subscribe({
        next: (x) => this.cupones.set(x),
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
    } else {
      this.api.listDevoluciones().subscribe({
        next: (x) => this.devoluciones.set(x),
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
    }
  }

  aprobar(id: number, aprobarFlag: boolean): void {
    this.api.aprobarResena(id, aprobarFlag).subscribe({
      next: () => {
        this.ok.set(aprobarFlag ? 'Reseña aprobada' : 'Reseña ocultada');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  crearCupon(): void {
    if (!this.cupon.codigo.trim()) {
      this.error.set('El código del cupón es obligatorio.');
      return;
    }
    this.busy.set(true);
    this.api
      .crearCupon({
        codigo: this.cupon.codigo.trim().toUpperCase(),
        descripcion: this.cupon.descripcion || this.cupon.codigo,
        tipoDescuento: this.cupon.tipoDescuento,
        valorDescuento: this.cupon.valorDescuento,
        montoMinimo: this.cupon.montoMinimo,
        montoMaximoDescuento: this.cupon.montoMaximoDescuento || null,
        canalEcommerce: this.cupon.canalEcommerce || 'ALL',
        fechaInicio: this.cupon.fechaInicio || null,
        fechaFin: this.cupon.fechaFin || null,
        usosMaximos: this.cupon.usosMaximos || null,
        usosPorUsuario: this.cupon.usosPorUsuario || 1,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.ok.set('Cupón creado');
          this.cupon = {
            codigo: '',
            descripcion: '',
            tipoDescuento: 'porcentaje',
            valorDescuento: 10,
            montoMinimo: 0,
            montoMaximoDescuento: null,
            canalEcommerce: 'ALL',
            fechaInicio: '',
            fechaFin: '',
            usosMaximos: null,
            usosPorUsuario: 1,
          };
          this.reload();
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set(e?.error?.error || e.message);
        },
      });
  }

  toggleCupon(id: number, activo: boolean): void {
    this.api.toggleCupon(id, activo).subscribe({
      next: () => {
        this.ok.set(activo ? 'Cupón activado' : 'Cupón desactivado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  estadoDevLabel(estado: string): string {
    switch ((estado || '').toLowerCase()) {
      case 'solicitada':
        return 'Pendiente';
      case 'aprobada':
        return 'Aprobada';
      case 'rechazada':
        return 'Rechazada';
      case 'completada':
        return 'Completada';
      default:
        return estado || '—';
    }
  }

  estadoDev(d: AdminDev, estado: string): void {
    const nro = d.nroPedido || d.idPedidoMovil;
    const msgs: Record<string, string> = {
      aprobada: `¿Aprobar y generar la nota de crédito / anulación en el ERP del pedido N° ${nro}?`,
      rechazada: `¿Rechazar devolución del pedido N° ${nro}?`,
      completada: `¿Procesar en el ERP la devolución del pedido N° ${nro}? (NC si hay factura, o anulación)`,
    };
    const msg = msgs[estado] || `¿Cambiar estado a ${estado}?`;
    if (!window.confirm(msg)) return;
    this.api.updateDevolucion(d.id, estado).subscribe({
      next: () => {
        this.ok.set(`Devolución ${this.estadoDevLabel(estado).toLowerCase()}`);
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
