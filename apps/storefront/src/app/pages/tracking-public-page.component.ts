import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ApiService, TrackingPublico } from '../core/api.service';
import { ToastService } from '../core/toast.service';
import { TrackingTimelineComponent } from '../shared/tracking-timeline.component';
import { DxIconComponent } from '../shared/dx-icon.component';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, TrackingTimelineComponent, DxIconComponent],
  template: `
    <section class="pub">
      <header>
        <div>
          <h1><dx-icon name="package" [size]="24" /> Seguimiento de pedido</h1>
          @if (lastRefresh(); as t) {
            <p class="live">Actualizado {{ t | date: 'shortTime' }} · auto cada 20 s</p>
          }
        </div>
        <div class="head-actions">
          <button type="button" class="dx-btn dx-btn--ghost" (click)="reload(false)" [disabled]="loading() || !tokenOk()">
            {{ loading() ? 'Actualizando…' : 'Actualizar' }}
          </button>
          <a routerLink="/catalogo">Ir a la tienda</a>
        </div>
      </header>

      @if (error()) {
        <div class="err-box" role="alert">
          <p>{{ error() }}</p>
          <a class="dx-btn" routerLink="/catalogo">Volver al catálogo</a>
        </div>
      } @else if (loading() && !data()) {
        <p class="mute" role="status" aria-busy="true">Cargando seguimiento…</p>
      } @else {
        @if (data(); as d) {
          <div class="card">
            <p class="nro">Pedido N° <strong>{{ d.nroPedido }}</strong></p>
            <p class="estado">
              Estado actual: <strong>{{ d.estadoActual }}</strong>
              @if (d.fechaEstadoActual) {
                <span>· {{ d.fechaEstadoActual | date: 'short' }}</span>
              }
            </p>
            @if (d.mensajeActual) {
              <p class="msg">{{ d.mensajeActual }}</p>
            }
            <dx-tracking-timeline [events]="d.eventos" [loading]="loading()" mode="detailed" />
          </div>
        }
      }
    </section>
  `,
  styles: [
    `
      .pub {
        max-width: 560px;
        margin: 0 auto;
        padding: 1.25rem 1rem 2.5rem;
      }
      header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        margin-bottom: 1rem;
        flex-wrap: wrap;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: 1.45rem;
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
      }
      .live {
        margin: 0.35rem 0 0;
        font-size: 0.82rem;
        color: var(--dx-mute);
      }
      .head-actions {
        display: flex;
        gap: 0.65rem;
        align-items: center;
        flex-wrap: wrap;
      }
      .head-actions a {
        font-weight: 700;
        text-decoration: none;
        color: var(--dx-red);
      }
      .card {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.1rem 1.2rem;
        display: grid;
        gap: 0.55rem;
      }
      .nro,
      .estado,
      .msg,
      .mute {
        margin: 0;
      }
      .msg {
        color: var(--dx-ink-soft);
      }
      .err-box {
        display: grid;
        gap: 0.75rem;
        justify-items: start;
        padding: 1.25rem;
        background: #fff5f5;
        border: 1px solid #f5c2c0;
        border-radius: 12px;
        color: #b91c1c;
      }
      .err-box p {
        margin: 0;
      }
      .mute {
        color: var(--dx-mute);
      }
    `,
  ],
})
export class TrackingPublicPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private token = '';
  private timer: number | null = null;
  private staleWarned = false;

  readonly data = signal<TrackingPublico | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly lastRefresh = signal<Date | null>(null);

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token') || '';
    if (!this.token) {
      this.error.set('Enlace inválido');
      this.loading.set(false);
      return;
    }
    this.reload(false);
    this.timer = window.setInterval(() => this.reload(true), 20_000);
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  tokenOk(): boolean {
    return !!this.token && !this.error();
  }

  reload(silent: boolean): void {
    if (!this.token) return;
    if (!silent || !this.data()) this.loading.set(true);

    this.api.getTrackingPublico(this.token).subscribe({
      next: (d) => {
        this.data.set(d);
        this.error.set(null);
        this.loading.set(false);
        this.lastRefresh.set(new Date());
        this.staleWarned = false;
      },
      error: (e) => {
        this.loading.set(false);
        if (!this.data()) {
          this.error.set(e?.error?.error || e.message || 'Enlace no válido');
          this.clearTimer();
        } else if (!this.staleWarned) {
          this.staleWarned = true;
          this.toast.info('No se pudo actualizar el seguimiento');
        }
      },
    });
  }

  private clearTimer(): void {
    if (this.timer != null) {
      window.clearInterval(this.timer);
      this.timer = null;
    }
  }
}
