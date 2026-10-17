import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface ToastItem {
  id: number;
  kind: ToastKind;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  readonly items = signal<ToastItem[]>([]);

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  dismiss(id: number): void {
    this.items.update((rows) => rows.filter((t) => t.id !== id));
  }

  private push(kind: ToastKind, message: string): void {
    const id = ++this.seq;
    this.items.update((rows) => [...rows, { id, kind, message }]);
    window.setTimeout(() => this.dismiss(id), 4200);
  }
}
