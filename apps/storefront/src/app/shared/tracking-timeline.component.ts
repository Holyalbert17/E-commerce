import { Component, input, computed } from '@angular/core';
import { DatePipe } from '@angular/common';

export interface TrackingEventLike {
  id: number;
  estado: string;
  mensaje?: string | null;
  fechaEstado: string;
  codigoTracking?: string | null;
}

/** Pipeline Amazon-style: barra resumen + timeline detallada. */
const STEPS = [
  { id: 'compra_realizada', label: 'Pedido' },
  { id: 'preparando', label: 'Preparación' },
  { id: 'en_transito', label: 'En camino' },
  { id: 'entregado', label: 'Entregado' },
] as const;

const ESTADO_ALIASES: Record<string, string> = {
  compra_realizada: 'compra_realizada',
  preparando: 'preparando',
  preparado: 'preparando',
  facturado: 'preparando',
  en_transito: 'en_transito',
  en_camino: 'en_transito',
  en_reparto: 'en_transito',
  entregado: 'entregado',
  cancelado: 'cancelado',
  demorado: 'demorado',
  excepcion: 'excepcion',
};

@Component({
  selector: 'dx-tracking-timeline',
  standalone: true,
  imports: [DatePipe],
  template: `
    @if (loading()) {
      <p class="mute">Cargando seguimiento…</p>
    } @else if (events().length === 0) {
      <p class="mute">Aún no hay eventos de seguimiento.</p>
    } @else {
      @if (showProgress()) {
        <div class="progress" role="status" [attr.aria-label]="'Estado: ' + currentLabel()">
          @for (s of steps; track s.id; let i = $index) {
            <div class="step" [class.done]="i <= activeIndex()" [class.current]="i === activeIndex()">
              <span class="dot"></span>
              <span class="lbl">{{ s.label }}</span>
            </div>
            @if (i < steps.length - 1) {
              <div class="bar" [class.done]="i < activeIndex()"></div>
            }
          }
        </div>
        @if (isException()) {
          <p class="exc" role="alert">{{ exceptionLabel() }}</p>
        }
      }

      @if (mode() !== 'summary') {
        <ol class="tl">
          @for (t of sorted(); track t.id) {
            <li [class.exc-item]="isExcEstado(t.estado)">
              <span class="ico" [attr.data-estado]="canon(t.estado)"></span>
              <div>
                <strong>{{ labelEstado(t.estado) }}</strong>
                <span class="when">{{ t.fechaEstado | date: 'short' }}</span>
                @if (t.mensaje) {
                  <p>{{ t.mensaje }}</p>
                }
                @if (t.codigoTracking) {
                  <small>Código: {{ t.codigoTracking }}</small>
                }
              </div>
            </li>
          }
        </ol>
      }
    }
  `,
  styles: [
    `
      .mute {
        color: var(--dx-mute, #78716c);
        margin: 0;
      }
      .progress {
        display: flex;
        align-items: flex-start;
        gap: 0;
        margin-bottom: 1rem;
        padding: 0.35rem 0;
      }
      .step {
        display: grid;
        justify-items: center;
        gap: 0.35rem;
        min-width: 3.5rem;
        flex: 0 0 auto;
      }
      .dot {
        width: 12px;
        height: 12px;
        border-radius: 50%;
        background: #d6d3d1;
        border: 2px solid #fff;
        box-shadow: 0 0 0 1px #d6d3d1;
      }
      .step.done .dot {
        background: var(--dx-red, #0f766e);
        box-shadow: 0 0 0 1px var(--dx-red, #0f766e);
      }
      .step.current .dot {
        transform: scale(1.25);
      }
      .lbl {
        font-size: 0.68rem;
        color: var(--dx-mute, #78716c);
        text-align: center;
        line-height: 1.2;
      }
      .step.done .lbl {
        color: var(--dx-ink, #1c1917);
        font-weight: 600;
      }
      .bar {
        flex: 1;
        height: 3px;
        margin-top: 5px;
        background: #e7e5e4;
        border-radius: 2px;
        min-width: 12px;
      }
      .bar.done {
        background: var(--dx-red, #0f766e);
      }
      .exc {
        margin: 0 0 0.75rem;
        padding: 0.5rem 0.75rem;
        background: #fff7ed;
        color: #9a3412;
        border-radius: 8px;
        font-size: 0.9rem;
      }
      .tl {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0;
        border-left: 2px solid var(--dx-line, #e7e5e4);
        margin-left: 0.55rem;
      }
      .tl li {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 0.65rem;
        padding: 0 0 0.9rem 0.85rem;
        position: relative;
      }
      .ico {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: var(--dx-red, #0f766e);
        margin-left: -1.15rem;
        margin-top: 0.35rem;
        box-shadow: 0 0 0 3px #fff;
      }
      .exc-item .ico {
        background: #ea580c;
      }
      .tl strong {
        display: block;
        font-size: 0.95rem;
      }
      .when {
        color: var(--dx-mute, #78716c);
        font-size: 0.8rem;
        margin-left: 0.35rem;
      }
      .tl p {
        margin: 0.2rem 0 0;
        color: var(--dx-ink-soft, #44403c);
        font-size: 0.9rem;
      }
      .tl small {
        color: var(--dx-mute, #78716c);
      }
    `,
  ],
})
export class TrackingTimelineComponent {
  readonly events = input<TrackingEventLike[]>([]);
  readonly loading = input(false);
  /** summary = solo barra; detailed = barra + timeline; full = igual detailed */
  readonly mode = input<'summary' | 'detailed' | 'full'>('detailed');
  readonly showProgress = input(true);

  readonly steps = STEPS;

  readonly sorted = computed(() =>
    [...this.events()].sort(
      (a, b) => new Date(b.fechaEstado).getTime() - new Date(a.fechaEstado).getTime(),
    ),
  );

  readonly latest = computed(() => this.sorted()[0] ?? null);

  readonly activeIndex = computed(() => {
    const c = this.canon(this.latest()?.estado || '');
    if (c === 'cancelado' || c === 'demorado' || c === 'excepcion') {
      const idx = STEPS.findIndex((s) => s.id === 'en_transito');
      return Math.max(0, idx);
    }
    const idx = STEPS.findIndex((s) => s.id === c);
    return idx >= 0 ? idx : 0;
  });

  readonly currentLabel = computed(() => this.labelEstado(this.latest()?.estado || ''));

  readonly isException = computed(() => {
    const c = this.canon(this.latest()?.estado || '');
    return c === 'cancelado' || c === 'demorado' || c === 'excepcion';
  });

  exceptionLabel(): string {
    const t = this.latest();
    if (!t) return '';
    const base = this.labelEstado(t.estado);
    return t.mensaje ? `${base}: ${t.mensaje}` : base;
  }

  canon(estado: string): string {
    return ESTADO_ALIASES[(estado || '').toLowerCase()] || (estado || '').toLowerCase();
  }

  isExcEstado(estado: string): boolean {
    const c = this.canon(estado);
    return c === 'cancelado' || c === 'demorado' || c === 'excepcion';
  }

  labelEstado(estado: string): string {
    const map: Record<string, string> = {
      compra_realizada: 'Compra realizada',
      preparando: 'En preparación',
      preparado: 'Preparado',
      facturado: 'Facturado',
      en_transito: 'En tránsito',
      en_camino: 'En tránsito',
      en_reparto: 'En reparto',
      entregado: 'Entregado',
      cancelado: 'Cancelado',
      demorado: 'Demorado',
      excepcion: 'Excepción',
    };
    return map[(estado || '').toLowerCase()] ?? estado;
  }
}
