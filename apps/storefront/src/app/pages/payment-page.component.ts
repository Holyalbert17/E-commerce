import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, PaymentSession } from '../core/api.service';
import { DxIconComponent } from '../shared/dx-icon.component';
import { environment } from '../../environments/environment';

@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DxIconComponent],
  template: `
    <section class="pago">
      <header>
        <div>
          <p class="kicker">Checkout</p>
          <h1>Pago online</h1>
        </div>
        <a routerLink="/catalogo" class="back"><dx-icon name="grid" [size]="14" /> Seguir comprando</a>
      </header>

      @if (error()) {
        <div class="dx-alert dx-alert--error fail-box" role="alert">
          <strong>No se pudo completar el pago</strong>
          <p>{{ error() }}</p>
        </div>
      }

      @if (session(); as s) {
        <div class="card">
          <p class="meta">Pedido N° <strong>{{ s.nroPedido }}</strong></p>
          <p class="total">{{ s.montoTotal | currency: 'PYG':'symbol-narrow':'1.0-0' }}</p>
          <p class="msg">{{ s.mensaje }}</p>

          @if (s.pagoCompletado) {
            <a class="dx-btn dx-btn--lg" [routerLink]="['/pedido', s.nroPedido]">
              <dx-icon name="check" [size]="16" /> Ver confirmación
            </a>
          } @else if (s.requierePagoOnline) {
            @if (!s.processId && s.bancardMode !== 'Stub') {
              <div class="fail-inline" role="status">
                <p>Todavía no hay process_id de Bancard. Podés reintentar o volver al carrito.</p>
              </div>
            }

            @if (canSimulate(s)) {
              <p class="hint">Modo sandbox Bancard — sin conexión a vpos.</p>
              <button type="button" class="dx-btn dx-btn--lg" [disabled]="busy()" (click)="simulate()">
                {{ busy() ? 'Procesando…' : 'Simular pago exitoso' }}
              </button>
            } @else if (s.checkoutScriptUrl && s.processId) {
              <div id="iframe-container"></div>
              <p class="hint">Completá el pago en el formulario Bancard.</p>
            } @else if (s.bancardMode !== 'Stub') {
              <p class="hint">Esperando process_id de Bancard…</p>
            }

            <div class="retry-actions">
              <button type="button" class="dx-btn" [disabled]="busy()" (click)="retryPayment()">
                Reintentar pago
              </button>
              <a class="dx-btn dx-btn--ghost" routerLink="/carrito">Volver al carrito</a>
              @if (s.shopProcessId) {
                <button type="button" class="dx-btn dx-btn--ghost danger" [disabled]="busy()" (click)="cancelPayment()">
                  Cancelar pago
                </button>
              }
            </div>
          }
        </div>
      } @else if (!error()) {
        <p class="loading">Cargando sesión de pago…</p>
      }
    </section>
  `,
  styles: [
    `
      .pago {
        max-width: 520px;
        margin: 0 auto;
        padding: 1.25rem 1rem 2.5rem;
        color: var(--dx-ink);
      }
      header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        margin-bottom: 1.25rem;
      }
      .kicker {
        margin: 0 0 0.25rem;
        color: var(--dx-red);
        font-weight: 700;
        font-size: 0.8rem;
        letter-spacing: 0.04em;
        text-transform: uppercase;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 1.85rem);
      }
      .back {
        color: var(--dx-ink-soft);
        text-decoration: none;
        font-weight: 600;
        font-size: 0.9rem;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
      .back:hover {
        color: var(--dx-red);
      }
      .card {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.35rem 1.4rem;
        box-shadow: var(--dx-shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.06));
        display: grid;
        gap: 0.5rem;
      }
      .meta {
        margin: 0;
        color: var(--dx-mute, #64748b);
      }
      .total {
        margin: 0;
        font-size: 1.85rem;
        font-weight: 800;
        color: var(--dx-red);
      }
      .msg {
        margin: 0 0 0.5rem;
        color: var(--dx-ink-soft);
      }
      .hint {
        margin: 0.5rem 0 0;
        font-size: 0.9rem;
        color: var(--dx-mute, #64748b);
      }
      .fail-box {
        margin-bottom: 1rem;
      }
      .fail-box p {
        margin: 0.35rem 0 0;
      }
      .fail-inline {
        background: #fff7ed;
        border: 1px solid #fed7aa;
        border-radius: 10px;
        padding: 0.75rem 0.9rem;
        color: #9a3412;
        margin: 0.25rem 0 0.5rem;
      }
      .fail-inline p {
        margin: 0;
        font-size: 0.92rem;
      }
      .retry-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-top: 0.85rem;
      }
      .retry-actions a.dx-btn {
        text-decoration: none;
        margin-top: 0;
      }
      .danger {
        color: var(--dx-red) !important;
        border-color: #f0b4b4 !important;
      }
      .loading {
        color: var(--dx-ink-soft);
      }
      a.dx-btn {
        text-decoration: none;
        justify-self: start;
        margin-top: 0.5rem;
      }
      button.dx-btn {
        justify-self: start;
        margin-top: 0.5rem;
      }
      .retry-actions button.dx-btn,
      .retry-actions a.dx-btn {
        margin-top: 0;
        justify-self: auto;
      }
      #iframe-container {
        min-height: 120px;
        margin-top: 0.5rem;
      }
    `,
  ],
})
export class PaymentPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly session = signal<PaymentSession | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  private nroPedido = 0;

  ngOnInit(): void {
    this.nroPedido = Number(this.route.snapshot.paramMap.get('nro'));
    if (!this.nroPedido) {
      this.error.set('Pedido inválido');
      return;
    }
    this.loadSession();
  }

  canSimulate(s: PaymentSession): boolean {
    return s.bancardMode === 'Stub' && (environment.demoPrefill || !environment.production);
  }

  loadSession(): void {
    this.error.set(null);
    this.api.getPaymentSession(this.nroPedido).subscribe({
      next: (s) => {
        this.session.set(s);
        if (s.pagoCompletado) return;
        if (!this.canSimulate(s) && s.checkoutScriptUrl && s.processId) {
          this.loadBancardScript(s.checkoutScriptUrl, s.processId);
        }
      },
      error: (e) => this.error.set(e?.error?.error || e.message || 'Sesión no encontrada'),
    });
  }

  retryPayment(): void {
    this.busy.set(true);
    this.error.set(null);
    this.api.getPaymentSession(this.nroPedido).subscribe({
      next: (s) => {
        this.busy.set(false);
        this.session.set(s);
        if (s.pagoCompletado) {
          void this.router.navigate(['/pedido', s.nroPedido]);
          return;
        }
        if (!this.canSimulate(s) && s.checkoutScriptUrl && s.processId) {
          this.loadBancardScript(s.checkoutScriptUrl, s.processId);
        }
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo reintentar el pago');
      },
    });
  }

  cancelPayment(): void {
    const s = this.session();
    if (!s?.shopProcessId) return;
    this.busy.set(true);
    this.error.set(null);
    this.api.rollbackPayment(s.shopProcessId).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigate(['/carrito']);
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo cancelar el pago');
      },
    });
  }

  simulate(): void {
    const s = this.session();
    if (!s?.shopProcessId || !this.canSimulate(s)) return;
    this.busy.set(true);
    this.api.simulateBancardSuccess(s.shopProcessId).subscribe({
      next: (r) => {
        this.busy.set(false);
        void this.router.navigate(['/pedido', r.nroPedido]);
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo simular el pago');
      },
    });
  }

  private loadBancardScript(scriptUrl: string, processId: string): void {
    const existing = document.querySelector(`script[src="${scriptUrl}"]`);
    const run = () => {
      const w = window as unknown as { Bancard?: { Checkout: { createForm: (id: string, pid: string) => void } } };
      w.Bancard?.Checkout.createForm('iframe-container', processId);
    };
    if (existing) {
      run();
      return;
    }
    const script = document.createElement('script');
    script.src = scriptUrl;
    script.onload = () => run();
    document.body.appendChild(script);
  }
}
