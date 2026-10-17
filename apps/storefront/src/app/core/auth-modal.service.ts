import { Injectable, signal } from '@angular/core';

export type AuthModalMode = 'login' | 'register' | 'forgot' | 'reset';

@Injectable({ providedIn: 'root' })
export class AuthModalService {
  readonly open = signal(false);
  readonly mode = signal<AuthModalMode>('login');
  /** Si true, el FAB de chat se reabre al cerrar el login con éxito. */
  readonly reopenSupportChat = signal(false);
  /** Payload opcional: abrir chat ligado a un pedido. */
  readonly pendingChatPedido = signal<number | null>(null);

  resetCodigo = '';
  resetChecksum = '';
  resetTenant = '';

  showLogin(opts?: { reopenChat?: boolean; idPedidoMovil?: number | null }): void {
    if (opts?.reopenChat) this.reopenSupportChat.set(true);
    if (opts?.idPedidoMovil != null) this.pendingChatPedido.set(opts.idPedidoMovil);
    this.mode.set('login');
    this.open.set(true);
  }

  showRegister(): void {
    this.mode.set('register');
    this.open.set(true);
  }

  showForgot(): void {
    this.mode.set('forgot');
    this.open.set(true);
  }

  showReset(codigo: string, checksum: string, tenant?: string): void {
    this.resetCodigo = codigo;
    this.resetChecksum = checksum;
    this.resetTenant = tenant ?? '';
    this.mode.set('reset');
    this.open.set(true);
  }

  switchTo(mode: AuthModalMode): void {
    this.mode.set(mode);
  }

  notifyLoginSuccess(): void {
    // El widget observa reopenSupportChat / session.loggedIn
  }

  consumeReopenChat(): boolean {
    const v = this.reopenSupportChat();
    this.reopenSupportChat.set(false);
    return v;
  }

  consumePendingPedido(): number | null {
    const v = this.pendingChatPedido();
    this.pendingChatPedido.set(null);
    return v;
  }

  close(): void {
    this.open.set(false);
  }
}
