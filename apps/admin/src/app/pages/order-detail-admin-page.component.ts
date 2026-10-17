import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  AdminApiService,
  AdminChofer,
  AdminOrderDetail,
  AdminTrackingEvent,
} from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, FormsModule],
  template: `
    <div class="dx-toolbar">
      <div>
        <a routerLink="/pedidos" class="text-muted small d-inline-block mb-1">
          <i class="fas fa-arrow-left mr-1"></i> Pedidos
        </a>
        @if (order(); as o) {
          <h1 class="dx-page-title">Pedido N° {{ o.nroPedido }}</h1>
          <div class="dx-meta-row">
            <span><strong>PedidoMovil</strong> #{{ o.idPedidoMovil }}</span>
            <span><strong>Origen</strong> ECOMMERCE</span>
            <span class="badge badge-soft-info">{{ labelEstado(o.estadoFulfillment || 'compra_realizada') }}</span>
            <span>
              @if (o.migrado) {
                <span class="badge badge-soft-success">En ERP</span>
              } @else {
                <span class="badge badge-soft-warning">Pendiente ERP</span>
              }
            </span>
            @if (o.facturado && o.nroFactura) {
              <span class="badge badge-soft-success">Factura N° {{ o.nroFactura }}</span>
            }
          </div>
        } @else {
          <h1 class="dx-page-title">Detalle de pedido</h1>
          <p class="dx-page-sub">Consulta, chofer y seguimiento (paridad ERP PedidoMovil).</p>
        }
      </div>
      <button type="button" class="btn btn-default btn-sm" (click)="refresh()" [disabled]="!order()">
        <i class="fas fa-sync-alt mr-1"></i> Actualizar
      </button>
      @if (order(); as o) {
        @if (o.puedeDescargarFactura) {
          <button
            type="button"
            class="btn btn-outline-primary btn-sm"
            (click)="descargarFactura()"
            [disabled]="descargandoFactura()"
          >
            <i class="fas fa-print mr-1"></i>
            {{ descargandoFactura() ? 'Imprimiendo…' : 'Imprimir factura ERP' }}
          </button>
        }
        <button
          type="button"
          class="btn btn-outline-success btn-sm"
          (click)="syncFacturaErp()"
          [disabled]="facturando()"
          title="Consulta en ERP si ya emitieron la factura"
        >
          <i class="fas fa-sync mr-1"></i>
          {{ facturando() ? 'Consultando…' : 'Verificar factura ERP' }}
        </button>
        @if (!o.migrado) {
          <button type="button" class="btn btn-warning btn-sm" (click)="reintentarMigracion()" [disabled]="migrando()">
            <i class="fas fa-database mr-1"></i> Reintentar migración ERP
          </button>
        }
      }
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

    @if (!order() && !error()) {
      <div class="dx-empty"><i class="fas fa-spinner fa-spin"></i><p>Cargando pedido…</p></div>
    }

    @if (order(); as o) {
      <div class="row">
        <div class="col-lg-5">
          <div class="card card-outline card-primary">
            <div class="card-header"><h3 class="card-title">Cliente / entrega</h3></div>
            <div class="card-body">
              <dl class="dx-kv">
                <dt>Razón social</dt>
                <dd><strong>{{ o.razonSocial }}</strong></dd>
                <dt>Documento</dt>
                <dd>{{ o.numeroDocumento || '—' }}</dd>
                <dt>Tipo entrega</dt>
                <dd>
                  {{ o.tipoEntrega === 0 ? 'Delivery' : 'Pickup' }}
                  @if (o.direccionEntrega || o.lugarEntrega) {
                    · {{ o.direccionEntrega || o.lugarEntrega }}
                  }
                </dd>
                @if (o.latitud != null && o.longitud != null) {
                  <dt>Coordenadas</dt>
                  <dd>
                    <a
                      [href]="'https://www.openstreetmap.org/?mlat=' + o.latitud + '&mlon=' + o.longitud + '#map=17/' + o.latitud + '/' + o.longitud"
                      target="_blank"
                      rel="noopener"
                    >
                      {{ o.latitud }}, {{ o.longitud }}
                    </a>
                  </dd>
                }
                <dt>Pago</dt>
                <dd>{{ o.tipoPago === 0 ? 'En la entrega' : 'Al pedido' }}</dd>
                <dt>Factura ERP</dt>
                <dd>
                  @if (o.puedeDescargarFactura && o.nroFactura) {
                    <strong>N° {{ o.nroFactura }}</strong>
                    <button
                      type="button"
                      class="btn btn-link btn-sm p-0 d-block"
                      (click)="descargarFactura()"
                      [disabled]="descargandoFactura()"
                    >
                      {{ descargandoFactura() ? 'Imprimiendo…' : 'Imprimir factura' }}
                    </button>
                  } @else {
                    <span class="text-muted d-block mb-2">Sin factura emitida en ERP</span>
                    <button
                      type="button"
                      class="btn btn-outline-success btn-sm"
                      (click)="syncFacturaErp()"
                      [disabled]="facturando()"
                    >
                      <i class="fas fa-sync mr-1"></i>
                      {{ facturando() ? 'Consultando…' : 'Verificar en ERP' }}
                    </button>
                    <small class="form-text text-muted">
                      La factura se emite en el ERP (salón / planilla). Acá solo se imprime cuando ya tiene N° fiscal.
                    </small>
                  }
                </dd>
                <dt>Fecha</dt>
                <dd>{{ o.fechaPedido | date: 'short' }}</dd>
              </dl>
              @if (o.observacion) {
                <div class="callout callout-info mb-0">
                  <p class="mb-0">{{ o.observacion }}</p>
                </div>
              }
            </div>
          </div>

          <div class="card card-outline card-info">
            <div class="card-header"><h3 class="card-title">Chofer (IDChofer)</h3></div>
            <div class="card-body">
              <p class="mb-2">
                Actual:
                <strong>{{ o.nombreChofer || 'Sin asignar' }}</strong>
              </p>
              <div class="form-row align-items-end">
                <div class="form-group col-md-8 mb-md-0">
                  <label>Asignar chofer activo</label>
                  <select class="form-control" [(ngModel)]="choferId" name="chofer">
                    <option [ngValue]="null">— Sin chofer —</option>
                    @for (c of choferes(); track c.id) {
                      <option [ngValue]="c.id">#{{ c.nroChofer }} · {{ c.nombre }}</option>
                    }
                  </select>
                </div>
                <div class="form-group col-md-4 mb-0">
                  <button type="button" class="btn btn-primary btn-block" (click)="asignarChofer()">
                    Guardar
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-lg-7">
          <div class="card">
            <div class="card-header"><h3 class="card-title">Ítems</h3></div>
            <div class="card-body table-responsive p-0">
              <table class="table table-sm table-striped mb-0">
                <thead>
                  <tr>
                    <th>Artículo</th>
                    <th class="text-right">Cant.</th>
                    <th class="text-right">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  @for (line of o.items; track line.idArticulo) {
                    <tr>
                      <td>
                        <strong>{{ line.nombre }}</strong>
                        <small>{{ line.sku }}</small>
                      </td>
                      <td class="text-right">{{ line.cantidadEmbalada }}</td>
                      <td class="text-right">{{ line.montoLinea | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="3" class="dx-empty py-4">Sin ítems</td>
                    </tr>
                  }
                </tbody>
                <tfoot>
                  <tr>
                    <th colspan="2" class="text-right">Total</th>
                    <th class="text-right">{{ o.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</th>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </div>
      </div>

      <div class="card card-outline card-secondary">
        <div class="card-header">
          <h3 class="card-title">Seguimiento (SeguimientoPedidoEcommerce)</h3>
        </div>
        <div class="card-body">
          <div class="row">
            <div class="col-lg-6">
              <ol class="dx-track">
                @for (t of tracking(); track t.id; let first = $first) {
                  <li [class.is-latest]="first">
                    <strong>{{ labelEstado(t.estado) }}</strong>
                    <small>{{ t.fechaEstado | date: 'short' }}</small>
                    @if (t.mensaje) {
                      <p>{{ t.mensaje }}</p>
                    }
                  </li>
                } @empty {
                  <li class="text-muted" style="border:0;padding-left:0">Sin eventos aún</li>
                }
              </ol>
            </div>
            <div class="col-lg-6">
              <div class="form-group">
                <label>Nuevo estado de fulfillment</label>
                <select class="form-control" [(ngModel)]="nuevoEstado" name="est">
                  <option value="preparando">En preparación</option>
                  <option value="preparado">Preparado</option>
                  <option value="facturado">Facturado</option>
                  <option value="en_transito">En tránsito</option>
                  <option value="demorado">Demorado</option>
                  <option value="excepcion">Excepción / reintento</option>
                  <option value="entregado">Entregado</option>
                  <option value="cancelado">Cancelado</option>
                </select>
              </div>
              <div class="form-group">
                <label>Mensaje (opcional)</label>
                <input class="form-control" [(ngModel)]="nuevoMensaje" name="msg" placeholder="Ej. Demora por clima" />
              </div>
              <button type="button" class="btn btn-primary" (click)="addTracking()">
                <i class="fas fa-plus mr-1"></i> Agregar hito
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class OrderDetailAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<AdminOrderDetail | null>(null);
  readonly choferes = signal<AdminChofer[]>([]);
  readonly tracking = signal<AdminTrackingEvent[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly migrando = signal(false);
  readonly descargandoFactura = signal(false);
  readonly facturando = signal(false);
  choferId: number | null = null;
  nroFacturaManual: number | null = null;
  nuevoEstado = 'preparado';
  nuevoMensaje = '';
  private idPedido = 0;

  ngOnInit(): void {
    this.idPedido = Number(this.route.snapshot.paramMap.get('id'));
    if (!this.idPedido) {
      this.error.set('Pedido inválido');
      return;
    }
    this.load(this.idPedido);
    this.api.listChoferes().subscribe({
      next: (rows) => this.choferes.set(rows.filter((c) => c.activo)),
      error: () => undefined,
    });
  }

  refresh(): void {
    if (this.idPedido) this.load(this.idPedido);
  }

  reintentarMigracion(): void {
    const o = this.order();
    if (!o || o.migrado) return;
    this.migrando.set(true);
    this.error.set(null);
    this.api.reintentarMigracionPedido(o.idPedidoMovil).subscribe({
      next: (r) => {
        this.migrando.set(false);
        if (r.errores > 0) {
          const err = r.logs?.find((l: { estado: string }) => l.estado === 'error');
          this.error.set(err?.mensajeError || `Migración con ${r.errores} error(es). Revisá AllowWriteBack / FKs ERP.`);
        } else {
          this.ok.set(`Migración OK (${r.exitosos}). Si AllowWriteBack=false fue simulado.`);
        }
        this.load(o.idPedidoMovil);
      },
      error: (e) => {
        this.migrando.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  descargarFactura(): void {
    const o = this.order();
    if (!o?.puedeDescargarFactura) return;
    this.descargandoFactura.set(true);
    this.error.set(null);
    this.api.downloadOrderFacturaPdf(o!.idPedidoMovil).subscribe({
      next: (blob) => {
        this.descargandoFactura.set(false);
        const name = o!.nroFactura ? `factura-${o!.nroFactura}.pdf` : `pedido-${o!.nroPedido}.pdf`;
        if (!this.api.openPdfBlob(blob, name)) {
          void this.readBlobError(blob).then((msg) => this.error.set(msg));
          return;
        }
        this.ok.set('Factura lista para imprimir');
      },
      error: async (e) => {
        this.descargandoFactura.set(false);
        if (e?.error instanceof Blob) {
          this.error.set(await this.readBlobError(e.error));
        } else {
          this.error.set(e?.error?.error || e.message || 'Error al descargar');
        }
      },
    });
  }

  syncFacturaErp(): void {
    const o = this.order();
    if (!o) return;
    this.facturando.set(true);
    this.error.set(null);
    this.api.syncFacturaErp(o.idPedidoMovil).subscribe({
      next: (r) => {
        this.facturando.set(false);
        if (r.ok) {
          this.ok.set(r.mensaje || `Factura ERP N° ${r.nroFactura}`);
          this.load(o.idPedidoMovil);
        } else {
          this.error.set(r.mensaje || 'ERP aún no tiene factura para este pedido.');
        }
      },
      error: (e) => {
        this.facturando.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  facturar(): void {
    this.syncFacturaErp();
  }

  private async readBlobError(blob: Blob | null | undefined): Promise<string> {
    if (!blob) return 'No se pudo descargar la factura';
    try {
      const text = await blob.text();
      const j = JSON.parse(text) as { error?: string };
      return j.error || 'No se pudo descargar la factura';
    } catch {
      return 'No se pudo descargar la factura';
    }
  }

  labelEstado(e: string): string {
    const map: Record<string, string> = {
      compra_realizada: 'Compra realizada',
      preparando: 'En preparación',
      preparado: 'Preparado',
      facturado: 'Facturado',
      en_transito: 'En tránsito',
      en_camino: 'En tránsito',
      despachado: 'Despachado',
      demorado: 'Demorado',
      excepcion: 'Excepción',
      entregado: 'Entregado',
      cancelado: 'Cancelado',
    };
    return map[e] || e;
  }

  asignarChofer(): void {
    const o = this.order();
    if (!o) return;
    this.api.asignarChoferPedido(o.idPedidoMovil, this.choferId).subscribe({
      next: () => {
        this.ok.set('Chofer actualizado');
        this.load(o.idPedidoMovil);
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  addTracking(): void {
    const o = this.order();
    if (!o) return;
    this.api
      .addPedidoTracking(o.idPedidoMovil, {
        estado: this.nuevoEstado,
        mensaje: this.nuevoMensaje || undefined,
      })
      .subscribe({
        next: () => {
          this.ok.set('Estado de tracking agregado');
          this.nuevoMensaje = '';
          this.loadTracking(o.idPedidoMovil);
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  private load(id: number): void {
    this.api.order(id).subscribe({
      next: (o) => {
        this.order.set(o);
        this.choferId = o.idChofer ?? null;
        this.loadTracking(o.idPedidoMovil);
      },
      error: (e) => this.error.set(e?.error?.error || e.message || 'No encontrado'),
    });
  }

  private loadTracking(idPedidoMovil: number): void {
    this.api.listPedidoTracking(idPedidoMovil).subscribe({
      next: (rows) => this.tracking.set(rows),
      error: () => this.tracking.set([]),
    });
  }
}
