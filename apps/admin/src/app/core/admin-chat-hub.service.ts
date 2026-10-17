import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AdminSessionService } from './admin-session.service';

/** Cliente SignalR para CRM (hub /hubs/chat). */
@Injectable({ providedIn: 'root' })
export class AdminChatHubService {
  private readonly session = inject(AdminSessionService);
  private connection: signalR.HubConnection | null = null;
  private joinedConv: number | null = null;

  async ensureConnected(): Promise<signalR.HubConnection | null> {
    const token = this.session.getToken();
    if (!token) return null;

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    if (!this.connection) {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${environment.apiBaseUrl}/hubs/chat?access_token=${encodeURIComponent(token)}`, {
          accessTokenFactory: () => this.session.getToken() || '',
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

  async joinEmpresa(idEmpresa: number): Promise<void> {
    const c = await this.ensureConnected();
    if (!c) return;
    try {
      await c.invoke('JoinEmpresa', idEmpresa);
    } catch {
      /* ignore */
    }
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

  onMensaje(handler: (msg: unknown) => void): void {
    void this.ensureConnected().then((c) => {
      c?.off('mensaje');
      c?.on('mensaje', handler);
    });
  }

  onConversacionUpdated(handler: (payload: unknown) => void): void {
    void this.ensureConnected().then((c) => {
      c?.off('conversacionUpdated');
      c?.on('conversacionUpdated', handler);
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
    }
  }
}
