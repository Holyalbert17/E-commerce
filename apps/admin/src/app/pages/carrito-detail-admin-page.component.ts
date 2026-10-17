import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminApiService, AdminCarritoDetail } from '../core/admin-api.service';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Trazabilidad carrito</h1>
        <p class="dx-page-sub">ConsultaTrazabilidad ERP · timeline de estados.</p>
      </div>
      <a routerLink="/carritos" class="btn btn-info btn-sm">
        <i class="fas fa-arrow-left mr-1"></i> Volver
      </a>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }

    @if (detail(); as d) {
      <div class="row">
        <div class="col-lg-4">
          <div class="card card-outline card-primary">
            <div class="card-header"><h3 class="card-title">Cabecera #{{ d.nroPedidoCarritoCompra }}</h3></div>
            <div class="card-body">
              <dl class="row mb-0 dx-erp-form">
                <dt class="col-5">Cliente</dt>
                <dd class="col-7">{{ d.razonSocial }}</dd>
                <dt class="col-5">Documento</dt>
                <dd class="col-7">{{ d.numeroDocumento }}</dd>
                <dt class="col-5">Fecha</dt>
                <dd class="col-7">{{ d.fechaPedido | date: 'short' }}</dd>
                <dt class="col-5">Entrega</dt>
                <dd class="col-7">{{ d.fechaPrevistaEntrega | date: 'shortDate' }}</dd>
                <dt class="col-5">Lugar</dt>
                <dd class="col-7">{{ d.lugarEntrega || '—' }}</dd>
                <dt class="col-5">Tipo entrega</dt>
                <dd class="col-7">{{ d.tipoEntrega }}</dd>
                <dt class="col-5">Tipo pago</dt>
                <dd class="col-7">{{ d.tipoPago }}</dd>
                <dt class="col-5">Estado</dt>
                <dd class="col-7">
                  <span
                    class="badge"
                    [class.badge-soft-info]="d.estado === 0"
                    [class.badge-soft-success]="d.estado === 1"
                    [class.badge-soft-danger]="d.estado === 2"
                  >{{ estadoLabel(d.estado) }}</span>
                </dd>
                <dt class="col-5">Pedido móvil</dt>
                <dd class="col-7">
                  @if (d.idPedidoMovil) {
                    <a [routerLink]="['/pedidos', d.idPedidoMovil]">#{{ d.idPedidoMovil }}</a>
                  } @else {
                    —
                  }
                </dd>
                <dt class="col-5">Total</dt>
                <dd class="col-7"><strong>{{ d.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong></dd>
              </dl>
              @if (d.observacion) {
                <hr />
                <p class="mb-0 small text-muted">{{ d.observacion }}</p>
              }
            </div>
          </div>
        </div>

        <div class="col-lg-8">
          <div class="card card-outline card-info mb-3">
            <div class="card-header"><h3 class="card-title">Timeline trazabilidad</h3></div>
            <div class="card-body">
              <div class="dx-timeline">
                @for (s of d.trazabilidad; track s.codigo) {
                  <div class="dx-timeline-step" [class.done]="s.completado">
                    <div class="dx-timeline-dot">
                      @if (s.completado) {
                        <i class="fas fa-check"></i>
                      } @else {
                        <i class="far fa-circle"></i>
                      }
                    </div>
                    <div class="dx-timeline-body">
                      <strong>{{ s.etiqueta }}</strong>
                      <div class="text-muted small">
                        @if (s.fecha) {
                          {{ s.fecha | date: 'short' }}
                        } @else {
                          Pendiente
                        }
                        @if (s.detalle) {
                          · {{ s.detalle }}
                        }
                      </div>
                    </div>
                  </div>
                }
              </div>
            </div>
          </div>

          <div class="card card-outline card-secondary">
            <div class="card-header"><h3 class="card-title">Ítems</h3></div>
            <div class="card-body p-0 table-responsive">
              <table class="table table-sm table-striped mb-0">
                <thead>
                  <tr>
                    <th>SKU</th>
                    <th>Producto</th>
                    <th class="text-right">Cant.</th>
                    <th class="text-right">P. unit.</th>
                    <th class="text-right">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  @for (i of d.items; track i.id) {
                    <tr>
                      <td><code>{{ i.sku || '—' }}</code></td>
                      <td>{{ i.nombre }}</td>
                      <td class="text-right">{{ i.cantidadEmbalada }}</td>
                      <td class="text-right">{{ i.precioUnitario | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                      <td class="text-right">{{ i.montoLinea | currency: 'PYG':'symbol-narrow':'1.0-0' }}</td>
                    </tr>
                  } @empty {
                    <tr><td colspan="5" class="text-center text-muted py-3">Sin ítems</td></tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class CarritoDetailAdminPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  readonly detail = signal<AdminCarritoDetail | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getCarrito(id).subscribe({
      next: (d) => this.detail.set(d),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  estadoLabel(estado: number): string {
    return ({ 0: 'Abierto', 1: 'Cerrado', 2: 'Anulado' } as Record<number, string>)[estado]
      ?? `Estado ${estado}`;
  }
}
