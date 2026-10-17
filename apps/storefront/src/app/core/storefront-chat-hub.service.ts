import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

const TOKEN_KEY = 'ecom_access_token';

/** Cliente SignalR storefront → /hubs/chat (chat + tracking) */
@Injectable({ providedIn: 'root' })
export class StorefrontChatHubService {
  private connection: signalR.HubConnection | null = null;
  private joinedConv: number | null = null;
  private joinedPedido: number | null = null;

  private token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  async ensureConnected(): Promise<signalR.HubConnection | null> {
    const token = this.token();
    if (!token) return null;

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    if (!this.connection) {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${environment.apiBaseUrl}/hubs/chat?access_token=${encodeURIComponent(token)}`, {
          accessTokenFactory: () => this.token() || '',
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();
    }

    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.connection.start();
      } catch {
        return null;
      }
    }
    return this.connection;
  }

  async joinConversacion(id: number): Promise<void> {
    const c = await this.ensureConnected();
    if (!c) return;
    try {
      if (this.joinedConv != null && this.joinedConv !== id) {
        await c.invoke('LeaveConversacion', this.joinedConv);
      }
      await c.invoke('JoinConversacion', id);
      this.joinedConv = id;
    } catch {
      /* ignore */
    }
  }

  async joinPedido(idPedidoMovil: number): Promise<void> {
    const c = await this.ensureConnected();
    if (!c) return;
    try {
      if (this.joinedPedido != null && this.joinedPedido !== idPedidoMovil) {
        await c.invoke('LeavePedido', this.joinedPedido);
      }
      await c.invoke('JoinPedido', idPedidoMovil);
      this.joinedPedido = idPedidoMovil;
    } catch {
      /* ignore */
    }
  }

  onMensaje(handler: (msg: unknown) => void): void {
    void this.ensureConnected().then((c) => {
      c?.off('mensaje');
      c?.on('mensaje', handler);
    });
  }

  onTrackingUpdated(handler: (evt: unknown) => void): void {
    void this.ensureConnected().then((c) => {
      c?.off('trackingUpdated');
      c?.on('trackingUpdated', handler);
    });
  }

  async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch {
        /* ignore */
      }
      this.connection = null;
      this.joinedConv = null;
      this.joinedPedido = null;
    }
  }
}
