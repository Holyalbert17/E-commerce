import { Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  template: `
    <div class="toasts" aria-live="polite" aria-relevant="additions">
      @for (t of toast.items(); track t.id) {
        <div class="toast" [class]="t.kind" role="status">
          <span>{{ t.message }}</span>
          <button type="button" (click)="toast.dismiss(t.id)" aria-label="Cerrar">×</button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .toasts {
        position: fixed;
        top: 1rem;
        right: 1rem;
        z-index: 1400;
        display: grid;
        gap: 0.5rem;
        width: min(360px, calc(100vw - 2rem));
        pointer-events: none;
      }
      .toast {
        pointer-events: auto;
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.75rem;
        padding: 0.75rem 0.9rem;
        border-radius: 12px;
        box-shadow: 0 10px 30px rgba(15, 23, 42, 0.18);
        font-weight: 600;
        font-size: 0.92rem;
        animation: in 0.2s ease;
      }
      .toast.success {
        background: #ecfdf5;
        color: #065f46;
        border: 1px solid #a7f3d0;
      }
      .toast.error {
        background: #fef2f2;
        color: #991b1b;
        border: 1px solid #fecaca;
      }
      .toast.info {
        background: #eff6ff;
        color: #1e40af;
        border: 1px solid #bfdbfe;
      }
      .toast button {
        border: none;
        background: transparent;
        font-size: 1.2rem;
        line-height: 1;
        cursor: pointer;
        color: inherit;
        opacity: 0.7;
      }
      @keyframes in {
        from {
          opacity: 0;
          transform: translateY(-8px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }
    `,
  ],
})
export class ToastHostComponent {
  readonly toast = inject(ToastService);
}
