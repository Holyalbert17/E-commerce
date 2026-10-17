import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SupportChatFacade {
  readonly openRequest = signal<{ idPedido?: number | null } | null>(null);

  open(opts?: { idPedido?: number | null }): void {
    this.openRequest.set(opts ?? {});
  }

  consume(): { idPedido?: number | null } | null {
    const v = this.openRequest();
    this.openRequest.set(null);
    return v;
  }
}
