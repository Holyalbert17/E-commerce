import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AccountOrder, ApiService, Devolucion, isLoggedIn, openPdfBlob, readPdfError } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from '../shared/dx-icon.component';
import { AccountOrdersTabsComponent } from '../shared/account-orders-tabs.component';

@Component({
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe, RouterLink, DxIconComponent, AccountOrdersTabsComponent],
  template: `
    <section class="dev-page" aria-labelledby="dev-title">
      <header class="toolbar">
        <div>
          <h1 id="dev-title"><dx-icon name="truck" [size]="26" /> Devoluciones</h1>
          <p class="sub">
            Si el pedido ya está facturado, se genera la nota de crédito en el ERP (mismo proceso que Stock → Notas de crédito).
            Si todavía no hay factura, se anula el pedido.
          </p>
        </div>
        <div class="toolbar-links">
          <a routerLink="/mi-cuenta"><dx-icon name="user" [size]="14" /> Mi cuenta</a>
        </div>
      </header>

      <app-account-orders-tabs />

      @if (!logged) {
        <div class="empty-box">
          <dx-icon name="login" [size]="40" />
          <p>Iniciá sesión para solicitar una devolución.</p>
          <button type="button" class="dx-btn" (click)="auth.showLogin()">
            <dx-icon name="login" [size]="16" /> Iniciar sesión
          </button>
        </div>
      } @else {
        @if (error()) {
          <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
        }
        @if (ok()) {
          <p class="dx-alert dx-alert--ok" role="status">{{ ok() }}</p>
        }

        <div class="layout">
          <form class="panel form" (ngSubmit)="enviar()" novalidate>
            <h2>Nueva devolución</h2>
            <ol class="steps" aria-hidden="true">
              <li [class.on]="!!devPedido">1. Pedido</li>
              <li [class.on]="!!motivoLabel()">2. Motivo</li>
              <li [class.on]="!!fotoPreview() || !!devDetalle.trim()">3. Foto / detalle</li>
            </ol>

            <label class="lbl" for="dev-pedido">¿Qué pedido querés devolver?</label>
            <select
              id="dev-pedido"
              class="dx-input"
              [(ngModel)]="devPedido"
              name="devPedido"
              [disabled]="pedidosDev().length === 0"
              required
            >
              <option [ngValue]="null">
                {{ pedidosDev().length === 0 ? 'No hay pedidos elegibles' : 'Seleccioná un pedido…' }}
              </option>
              @for (o of pedidosDev(); track o.idPedidoMovil) {
                <option [ngValue]="o.idPedidoMovil">
                  Pedido N° {{ o.nroPedido }} · {{ o.fechaPedido | date: 'dd/MM/yyyy' }} ·
                  {{ o.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}
                </option>
              }
            </select>
            @if (pedidoSel(); as sel) {
              <p class="meta">
                {{ sel.razonSocial || 'Tu pedido' }}
                @if (sel.direccion) {
                  · {{ sel.direccion }}
                }
                <a [routerLink]="['/pedido', sel.nroPedido]">Ver detalle</a>
              </p>
            }

            <label class="lbl" for="dev-motivo">Motivo</label>
            <div class="motivos" role="radiogroup" aria-label="Motivo de devolución">
              @for (m of motivos; track m.key) {
                <label class="motivo" [class.on]="devMotivoKey === m.key">
                  <input type="radio" name="motivo" [value]="m.key" [(ngModel)]="devMotivoKey" />
                  <span>{{ m.label }}</span>
                </label>
              }
            </div>
            @if (devMotivoKey === 'otro') {
              <input
                class="dx-input"
                [(ngModel)]="devMotivoOtro"
                name="devMotivoOtro"
                placeholder="Contanos el motivo…"
                maxlength="200"
              />
            }

            <label class="lbl" for="dev-foto">Foto del producto (opcional)</label>
            <div class="foto-box" [class.has]="!!fotoPreview()">
              @if (fotoPreview(); as prev) {
                <img [src]="prev" alt="Vista previa de la foto" />
                <button type="button" class="dx-btn dx-btn--ghost clear" (click)="clearFoto()">Quitar foto</button>
              } @else {
                <dx-icon name="camera" [size]="28" />
                <p>Sacá o subí una foto clara del producto o del empaque.</p>
                <label class="dx-btn pick" for="dev-foto">Elegir foto</label>
              }
              <input
                id="dev-foto"
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                (change)="onFoto($event)"
                hidden
              />
            </div>
            @if (fotoName()) {
              <small class="file-name">{{ fotoName() }}</small>
            }

            <label class="lbl" for="dev-detalle">Detalle (opcional)</label>
            <textarea
              id="dev-detalle"
              class="dx-input"
              [(ngModel)]="devDetalle"
              name="devDetalle"
              rows="3"
              maxlength="1000"
              placeholder="Qué pasó, número de lote, etc."
            ></textarea>

            <button type="submit" class="dx-btn submit" [disabled]="enviando() || !devPedido">
              {{ enviando() ? 'Procesando…' : 'Confirmar devolución' }}
            </button>
          </form>

          <aside class="panel list-panel">
            <h2>Mis solicitudes</h2>
            @if (loading()) {
              <p class="muted">Cargando…</p>
            } @else if (devoluciones().length === 0) {
              <div class="empty-mini">
                <dx-icon name="truck" [size]="28" />
                <p>Todavía no pediste ninguna devolución.</p>
              </div>
            } @else {
              <ul class="dev-list">
                @for (d of devoluciones(); track d.id) {
                  <li>
                    <div class="dev-top">
                      <strong>Pedido N° {{ d.nroPedido || d.idPedidoMovil }}</strong>
                      <span class="soft-badge" [class]="estadoClass(d.estado)">{{ estadoLabel(d.estado) }}</span>
                    </div>
                    <p class="motivo-txt">{{ d.motivo }}</p>
                    @if (d.detalle) {
                      <small>{{ d.detalle }}</small>
                    }
                    @if (d.estado === 'completada' && d.idNotaCreditoErp) {
                      <small>Nota de crédito ERP #{{ d.nroNotaCredito || d.idNotaCreditoErp }}</small>
                    }
                    @if (d.urlAdjunto) {
                      <a class="foto-link" [href]="mediaUrl(d.urlAdjunto)" target="_blank" rel="noopener">
                        Ver foto adjunta
                      </a>
                    }
                    <small class="date">{{ d.fechaSolicitud | date: 'dd/MM/yyyy HH:mm' }}</small>
                    @if (puedeImprimirNc(d)) {
                      <button
                        type="button"
                        class="dx-btn dx-btn--ghost nc-btn"
                        (click)="descargarNotaCredito(d)"
                        [disabled]="printing() === d.id"
                      >
                        <dx-icon name="file-text" [size]="14" />
                        {{ printing() === d.id ? 'Imprimiendo…' : 'Imprimir nota de crédito' }}
                      </button>
                    }
                  </li>
                }
              </ul>
            }
          </aside>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .dev-page {
        max-width: 1100px;
        margin: 0 auto;
        padding: 0 0 2.5rem;
      }
      .toolbar {
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1.25rem;
      }
      .toolbar h1 {
        margin: 0;
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        font-size: 1.55rem;
      }
      .sub {
        margin: 0.35rem 0 0;
        color: var(--dx-mute);
        max-width: 40rem;
      }
      .toolbar-links {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        align-items: flex-start;
      }
      .toolbar-links a {
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        font-weight: 600;
        color: var(--dx-ink);
      }
      .layout {
        display: grid;
        grid-template-columns: minmax(0, 1.15fr) minmax(0, 0.85fr);
        gap: 1.1rem;
        align-items: start;
      }
      @media (max-width: 860px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
      .panel {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 16px;
        padding: 1.15rem 1.2rem 1.35rem;
        box-shadow: var(--dx-shadow);
      }
      .panel h2 {
        margin: 0 0 0.85rem;
        font-size: 1.1rem;
      }
      .steps {
        list-style: none;
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
        padding: 0;
        margin: 0 0 1rem;
      }
      .steps li {
        font-size: 0.78rem;
        font-weight: 700;
        color: var(--dx-mute);
        background: #f8fafc;
        border: 1px solid var(--dx-line);
        border-radius: 999px;
        padding: 0.25rem 0.65rem;
      }
      .steps li.on {
        color: #fff;
        background: var(--dx-red);
        border-color: transparent;
      }
      .form {
        display: grid;
        gap: 0.55rem;
      }
      .lbl {
        font-size: 0.88rem;
        font-weight: 700;
        margin-top: 0.35rem;
      }
      .meta {
        margin: -0.15rem 0 0.2rem;
        font-size: 0.86rem;
        color: var(--dx-mute);
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem 0.65rem;
      }
      .meta a {
        color: var(--dx-red);
        font-weight: 600;
      }
      .motivos {
        display: grid;
        gap: 0.4rem;
      }
      .motivo {
        display: flex;
        align-items: center;
        gap: 0.55rem;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        padding: 0.65rem 0.8rem;
        cursor: pointer;
        background: #fff;
      }
      .motivo.on {
        border-color: var(--dx-red);
        background: color-mix(in srgb, var(--dx-red) 8%, #fff);
      }
      .motivo input {
        accent-color: var(--dx-red);
      }
      .foto-box {
        display: grid;
        justify-items: center;
        text-align: center;
        gap: 0.45rem;
        padding: 1.1rem 0.85rem;
        border: 1.5px dashed var(--dx-line);
        border-radius: 14px;
        background: #f8fafc;
        color: var(--dx-mute);
      }
      .foto-box.has {
        background: #fff;
        border-style: solid;
      }
      .foto-box img {
        max-width: 100%;
        max-height: 220px;
        border-radius: 10px;
        object-fit: contain;
      }
      .foto-box .pick {
        cursor: pointer;
      }
      .foto-box .clear {
        margin-top: 0.25rem;
      }
      .file-name {
        color: var(--dx-mute);
        margin-top: -0.2rem;
      }
      .submit {
        margin-top: 0.55rem;
        justify-self: start;
      }
      .dev-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.65rem;
      }
      .dev-list li {
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        padding: 0.75rem 0.85rem;
        display: grid;
        gap: 0.25rem;
      }
      .dev-top {
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 0.4rem;
        align-items: center;
      }
      .motivo-txt {
        margin: 0;
        font-size: 0.92rem;
      }
      .dev-list small,
      .date {
        color: var(--dx-mute);
      }
      .nc-btn {
        margin-top: 0.5rem;
      }
      .foto-link {
        font-weight: 600;
        color: var(--dx-red);
        font-size: 0.88rem;
      }
      .soft-badge {
        display: inline-flex;
        align-items: center;
        border-radius: 999px;
        padding: 0.15rem 0.55rem;
        font-size: 0.75rem;
        font-weight: 700;
      }
      .soft-badge--ok {
        background: #dcfce7;
        color: #166534;
      }
      .soft-badge--warn {
        background: #fef3c7;
        color: #92400e;
      }
      .soft-badge--danger {
        background: #fee2e2;
        color: #991b1b;
      }
      .soft-badge--info {
        background: #e0f2fe;
        color: #075985;
      }
      .empty-box,
      .empty-mini {
        display: grid;
        justify-items: center;
        text-align: center;
        gap: 0.65rem;
        padding: 2rem 1rem;
        border: 1px dashed var(--dx-line);
        border-radius: 14px;
        background: #fff;
        color: var(--dx-mute);
      }
      .empty-mini {
        padding: 1.25rem 0.75rem;
      }
      .muted {
        color: var(--dx-mute);
      }
    `,
  ],
})
export class ReturnsPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthModalService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  logged = isLoggedIn();
  readonly pedidos = signal<AccountOrder[]>([]);
  readonly devoluciones = signal<Devolucion[]>([]);
  readonly loading = signal(true);
  readonly enviando = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly fotoPreview = signal<string | null>(null);
  readonly fotoName = signal<string | null>(null);
  readonly printing = signal<number | null>(null);

  private fotoFile: File | null = null;
  private previewUrl: string | null = null;

  devPedido: number | null = null;
  devMotivoKey = 'defectuoso';
  devMotivoOtro = '';
  devDetalle = '';

  readonly motivos = [
    { key: 'defectuoso', label: 'Producto defectuoso o no funciona' },
    { key: 'danado', label: 'Llegó dañado / mal embalado' },
    { key: 'equivocado', label: 'No es lo que pedí' },
    { key: 'tardio', label: 'Llegó demasiado tarde' },
    { key: 'opinion', label: 'Cambié de opinión' },
    { key: 'otro', label: 'Otro motivo' },
  ] as const;

  readonly pedidosDev = computed(() =>
    this.pedidos().filter((o) => !!o.idPedidoMovil && !o.cancelado),
  );

  pedidoSel(): AccountOrder | null {
    const id = this.devPedido;
    if (id == null) return null;
    return this.pedidosDev().find((o) => o.idPedidoMovil === id) ?? null;
  }

  motivoLabel(): string {
    if (this.devMotivoKey === 'otro') return this.devMotivoOtro.trim();
    return this.motivos.find((m) => m.key === this.devMotivoKey)?.label ?? '';
  }

  nroPedidoPrint(d: Devolucion): number | null {
    const o = this.pedidos().find((x) => x.idPedidoMovil === d.idPedidoMovil);
    return o?.nroPedido ?? d.nroPedido ?? null;
  }

  puedeImprimirNc(d: Devolucion): boolean {
    if (!this.nroPedidoPrint(d)) return false;
    return !!d.puedeDescargarNotaCredito || d.estado === 'completada' || d.estado === 'aprobada';
  }

  descargarNotaCredito(d: Devolucion): void {
    const nro = this.nroPedidoPrint(d);
    if (!nro) {
      this.toast.error('No se encontró el número de pedido para imprimir.');
      return;
    }
    this.printing.set(d.id);
    this.api.downloadCreditNotePdf(nro).subscribe({
      next: (blob) => {
        this.printing.set(null);
        const name = d.nroNotaCredito ? `nota-credito-${d.nroNotaCredito}.pdf` : `nota-credito-pedido-${nro}.pdf`;
        if (!openPdfBlob(blob, name)) {
          void readPdfError(blob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.success('Nota de crédito lista para imprimir');
      },
      error: (e) => {
        this.printing.set(null);
        const errBlob = e?.error;
        if (errBlob instanceof Blob) {
          void readPdfError(errBlob).then((msg) => this.toast.error(msg));
          return;
        }
        this.toast.error(e?.error?.error || e.message || 'No se pudo cargar la nota de crédito');
      },
    });
  }

  ngOnInit(): void {
    if (!this.logged) return;
    const qp = this.route.snapshot.queryParamMap;
    this.api.getDevoluciones().subscribe({
      next: (x) => {
        this.devoluciones.set(x);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        if (e?.status === 401) this.logged = false;
        else this.error.set(e?.error?.error || e.message);
      },
    });
    this.api.getMyOrders().subscribe({
      next: (orders) => {
        this.pedidos.set(orders);
        const pedidoQp = qp.get('pedido') || qp.get('idPedidoMovil');
        const nroQp = qp.get('nro');
        if (pedidoQp) {
          const id = Number(pedidoQp);
          if (Number.isFinite(id) && orders.some((o) => o.idPedidoMovil === id && !o.cancelado)) {
            this.devPedido = id;
          }
        } else if (nroQp) {
          const nro = Number(nroQp);
          const match = orders.find((o) => o.nroPedido === nro && !!o.idPedidoMovil && !o.cancelado);
          if (match?.idPedidoMovil) this.devPedido = match.idPedidoMovil;
        }
      },
    });
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  mediaUrl(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  onFoto(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.flashError('Solo se admiten imágenes (JPEG, PNG, WebP…).');
      input.value = '';
      return;
    }
    if (file.size > 12 * 1024 * 1024) {
      this.flashError('La foto no puede superar 12 MB.');
      input.value = '';
      return;
    }
    this.revokePreview();
    this.fotoFile = file;
    this.fotoName.set(file.name);
    this.previewUrl = URL.createObjectURL(file);
    this.fotoPreview.set(this.previewUrl);
  }

  clearFoto(): void {
    this.fotoFile = null;
    this.fotoName.set(null);
    this.revokePreview();
    this.fotoPreview.set(null);
  }

  private revokePreview(): void {
    if (this.previewUrl) {
      URL.revokeObjectURL(this.previewUrl);
      this.previewUrl = null;
    }
  }

  enviar(): void {
    if (!this.devPedido) {
      this.flashError('Seleccioná el pedido a devolver.');
      return;
    }
    const motivo = this.motivoLabel();
    if (!motivo) {
      this.flashError(this.devMotivoKey === 'otro' ? 'Escribí el motivo.' : 'Seleccioná un motivo.');
      return;
    }
    this.enviando.set(true);
    this.error.set(null);
    this.api
      .crearDevolucion({
        idPedidoMovil: this.devPedido,
        motivo,
        detalle: this.devDetalle.trim() || undefined,
        file: this.fotoFile ?? undefined,
      })
      .subscribe({
        next: (created) => {
          this.enviando.set(false);
          if (created.estado === 'completada') {
            this.flashOk(
              created.idNotaCreditoErp
                ? 'Devolución procesada. Ya podés imprimir la nota de crédito.'
                : 'Devolución procesada en el ERP (pedido anulado o NC generada).',
            );
          } else {
            this.flashOk('Devolución registrada. Quedó pendiente de procesar en el ERP.');
          }
          this.devMotivoKey = 'defectuoso';
          this.devMotivoOtro = '';
          this.devDetalle = '';
          this.clearFoto();
          this.api.getDevoluciones().subscribe({ next: (x) => this.devoluciones.set(x) });
        },
        error: (e) => {
          this.enviando.set(false);
          this.flashError(e?.error?.error || e.message || 'No se pudo enviar la solicitud');
        },
      });
  }

  estadoLabel(estado: string): string {
    switch ((estado || '').toLowerCase()) {
      case 'solicitada':
        return 'Pendiente ERP';
      case 'aprobada':
        return 'Aprobada';
      case 'rechazada':
        return 'Rechazada';
      case 'completada':
        return 'Procesada';
      default:
        return estado || '—';
    }
  }

  estadoClass(estado: string): string {
    switch ((estado || '').toLowerCase()) {
      case 'solicitada':
        return 'soft-badge soft-badge--warn';
      case 'aprobada':
      case 'completada':
        return 'soft-badge soft-badge--ok';
      case 'rechazada':
        return 'soft-badge soft-badge--danger';
      default:
        return 'soft-badge soft-badge--info';
    }
  }

  private flashOk(message: string): void {
    this.error.set(null);
    this.ok.set(message);
    this.toast.success(message);
  }

  private flashError(message: string): void {
    this.ok.set(null);
    this.error.set(message);
    this.toast.error(message);
  }
}
