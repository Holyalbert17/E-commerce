import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ApiService, CheckoutResult, Seguimiento, isLoggedIn, openPdfBlob, readPdfError } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { PersonalizacionService } from '../core/personalizacion.service';
import { StorefrontChatHubService } from '../core/storefront-chat-hub.service';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from '../shared/dx-icon.component';
import { TrackingTimelineComponent } from '../shared/tracking-timeline.component';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, DxIconComponent, TrackingTimelineComponent],
  template: `
    <section class="ok">
      @if (error()) {
        <div class="panel err-panel" role="alert">
          <p class="err">{{ error() }}</p>
          <a class="dx-btn" routerLink="/catalogo">Volver al catálogo</a>
        </div>
      }

      @if (order(); as o) {
        <div class="panel">
          <span class="badge"><dx-icon name="check" [size]="18" /> Pedido confirmado</span>
          <h1>{{ exitoMsg() }}</h1>
          <p class="msg">{{ o.mensaje }}</p>

          <div class="card">
            <p><strong>N° pedido</strong> <span>{{ o.nroPedido }}</span></p>
            @if (o.idPedidoMovil) {
              <p><strong>Pedido móvil</strong> <span>{{ o.idPedidoMovil }} · ECOMMERCE</span></p>
            }
            @if (o.facturado && o.nroFactura) {
              <p>
                <strong>Factura</strong>
                <span class="fact-nro">N° {{ o.nroFactura }}</span>
              </p>
            }
            <p><strong>Cliente</strong> <span>{{ o.razonSocial }}</span></p>
            <p>
              <strong>Entrega</strong>
              <span
                >{{ o.tipoEntrega === 0 ? 'Delivery' : 'Pickup'
                }}{{ o.lugarEntrega ? ' · ' + o.lugarEntrega : '' }}</span
              >
            </p>
            <p><strong>Pago</strong> <span>{{ o.tipoPago === 0 ? 'En la entrega' : 'Al pedido' }}</span></p>
            <p><strong>Previsto</strong> <span>{{ o.fechaPrevistaEntrega | date: 'short' }}</span></p>
            <p class="total">{{ o.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</p>
            <ul>
              @for (line of o.items; track line.idArticulo) {
                <li>
                  <span>{{ line.cantidad }} × {{ line.nombre }}</span>
                  <strong>{{ line.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                </li>
              }
            </ul>
            @if (o.puedeDescargarFactura) {
              <button
                type="button"
                class="dx-btn fact-btn"
                (click)="descargarFactura(o)"
                [disabled]="downloading()"
              >
                <dx-icon name="file-text" [size]="14" />
                {{ downloading() ? 'Imprimiendo…' : 'Imprimir factura' }}
              </button>
            }
          </div>

          @if (o.idPedidoMovil) {
            <div class="card track-card">
              <h2>Seguimiento</h2>
              <dx-tracking-timeline [events]="tracking()" [loading]="trackLoading()" mode="detailed" />
              <button type="button" class="dx-btn dx-btn--ghost share" (click)="compartir(o.idPedidoMovil!)" [disabled]="sharing()">
                {{ sharing() ? 'Generando…' : 'Compartir seguimiento' }}
              </button>
              @if (shareUrl()) {
                <p class="share-url">
                  <a [href]="shareUrl()!" target="_blank" rel="noopener">{{ shareUrl() }}</a>
                </p>
              }
            </div>
          }

          <div class="actions">
            <a routerLink="/catalogo" class="dx-btn"><dx-icon name="grid" [size]="16" /> Seguir comprando</a>
            <a routerLink="/mis-pedidos" class="dx-btn dx-btn--ghost">Mis pedidos</a>
          </div>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .ok {
        display: grid;
        place-items: center;
        padding: 1.25rem 1rem 2.5rem;
      }
      .panel {
        width: min(100%, 560px);
        display: grid;
        gap: 0.85rem;
      }
      .badge {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        width: fit-content;
        padding: 0.35rem 0.75rem;
        border-radius: 999px;
        background: #ecfdf5;
        color: #15803d;
        font-weight: 700;
        font-size: 0.85rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.6rem, 3vw, 2rem);
        color: var(--dx-ink);
      }
      h2 {
        margin: 0 0 0.65rem;
        font-size: 1.05rem;
        font-family: var(--dx-display);
      }
      .msg {
        margin: 0;
        color: var(--dx-ink-soft);
      }
      .card {
        padding: 1.15rem 1.25rem;
        border-radius: 14px;
        background: #fff;
        border: 1px solid var(--dx-line);
        box-shadow: var(--dx-shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.06));
      }
      .card > p {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        margin: 0.45rem 0;
        color: var(--dx-ink);
      }
      .card > p strong {
        color: var(--dx-mute, #64748b);
        font-weight: 600;
        font-size: 0.9rem;
      }
      ul {
        list-style: none;
        padding: 0.85rem 0 0;
        margin: 0.85rem 0 0;
        border-top: 1px solid var(--dx-line);
        display: grid;
        gap: 0.45rem;
      }
      ul li {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        font-size: 0.95rem;
      }
      .total {
        font-size: 1.45rem;
        font-weight: 800;
        margin: 0.9rem 0 0 !important;
        color: var(--dx-red);
        justify-content: flex-start !important;
      }
      .fact-nro {
        color: #1565c0;
        font-weight: 700;
      }
      .fact-btn {
        margin-top: 1rem;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        width: fit-content;
      }
      .timeline {
        margin: 0;
        padding-left: 1.1rem;
        display: grid;
        gap: 0.55rem;
      }
      .timeline li {
        display: block;
      }
      .timeline strong {
        text-transform: capitalize;
      }
      .timeline span {
        margin-left: 0.45rem;
        color: var(--dx-mute);
        font-size: 0.85rem;
      }
      .timeline p {
        margin: 0.2rem 0 0;
        color: var(--dx-ink-soft);
      }
      .mute {
        margin: 0;
        color: var(--dx-mute);
      }
      .share {
        margin-top: 0.75rem;
        width: fit-content;
      }
      .share-url {
        margin: 0.4rem 0 0;
        font-size: 0.85rem;
        word-break: break-all;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }
      a.dx-btn {
        text-decoration: none;
      }
      .err {
        margin: 0 0 0.75rem;
        color: #b91c1c;
      }
      .err-panel {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.25rem;
      }
    `,
  ],
})
export class OrderPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly personalizacion = inject(PersonalizacionService);
  private readonly hub = inject(StorefrontChatHubService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthModalService);

  readonly order = signal<CheckoutResult | null>(null);
  readonly error = signal<string | null>(null);
  readonly tracking = signal<Seguimiento[]>([]);
  readonly trackLoading = signal(false);
  readonly sharing = signal(false);
  readonly downloading = signal(false);
  readonly shareUrl = signal<string | null>(null);
  private pedidoHubId: number | null = null;

  exitoMsg(): string {
    return this.personalizacion.checkoutTextos().msgExito || '¡Gracias por tu compra!';
  }

  ngOnInit(): void {
    const nro = Number(this.route.snapshot.paramMap.get('nro'));
    if (!nro) {
      this.error.set('Pedido inválido');
      return;
    }
    this.api.getOrder(nro).subscribe({
      next: (o) => {
        this.order.set(o);
        if (o.idPedidoMovil) {
          this.bindTracking(o.idPedidoMovil);
        }
      },
      error: (e) => this.error.set(e?.error?.error || e.message || 'Pedido no encontrado'),
    });
  }

  ngOnDestroy(): void {
    this.pedidoHubId = null;
  }

  compartir(idPedidoMovil: number): void {
    this.sharing.set(true);
    this.api.shareTracking(idPedidoMovil).subscribe({
      next: (r) => {
        this.sharing.set(false);
        const url = `${window.location.origin}${r.path}`;
        this.shareUrl.set(url);
        void navigator.clipboard?.writeText(url);
        this.toast.success('Link de seguimiento copiado');
      },
      error: (e) => {
        this.sharing.set(false);
        this.toast.error(e?.error?.error || 'No se pudo generar el link');
      },
    });
  }

  descargarFactura(o: CheckoutResult): void {
    if (!isLoggedIn()) {
      this.auth.showLogin();
      this.toast.error('Iniciá sesión para imprimir la factura');
      return;
    }
    this.downloading.set(true);
    this.api.downloadInvoicePdf(o.nroPedido).subscribe({
      next: (blob) => {
        this.downloading.set(false);
        const name = o.nroFactura
          ? `factura-${o.nroFactura}.pdf`
          : `factura-pedido-${o.nroPedido}.pdf`;
        if (!openPdfBlob(blob, name)) {
          void readPdfError(blob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.success(`Factura ${o.nroFactura ?? o.nroPedido} lista para imprimir`);
      },
      error: (e) => {
        this.downloading.set(false);
        if (e?.status === 401) {
          this.auth.showLogin();
          this.toast.error('Iniciá sesión para imprimir la factura');
          return;
        }
        const errBlob = e?.error;
        if (errBlob instanceof Blob) {
          void readPdfError(errBlob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.error(e?.error?.error || e.message || 'No se pudo cargar la factura');
      },
    });
  }

  private bindTracking(idPedidoMovil: number): void {
    this.trackLoading.set(true);
    this.api.getTracking(idPedidoMovil).subscribe({
      next: (rows) => {
        this.tracking.set(rows);
        this.trackLoading.set(false);
      },
      error: () => {
        this.tracking.set([]);
        this.trackLoading.set(false);
      },
    });

    this.pedidoHubId = idPedidoMovil;
    void this.hub.joinPedido(idPedidoMovil);
    this.hub.onTrackingUpdated((raw) => {
      const evt = raw as Partial<Seguimiento> & { Id?: number; Estado?: string; FechaEstado?: string };
      if (!evt || this.pedidoHubId !== idPedidoMovil) return;
      const mapped: Seguimiento = {
        id: Number(evt.id ?? evt.Id ?? Date.now()),
        idPedidoMovil,
        estado: String(evt.estado ?? evt.Estado ?? ''),
        mensaje: (evt.mensaje as string | null | undefined) ?? null,
        fechaEstado: String(evt.fechaEstado ?? evt.FechaEstado ?? new Date().toISOString()),
        codigoTracking: (evt.codigoTracking as string | null | undefined) ?? null,
      };
      if (!mapped.estado) return;
      this.tracking.update((list) => {
        if (list.some((x) => x.id === mapped.id)) return list;
        return [mapped, ...list];
      });
    });
  }
}
