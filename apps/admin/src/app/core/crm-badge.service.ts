import { Injectable, inject, signal } from '@angular/core';
import { AdminApiService } from './admin-api.service';
import { AdminSessionService } from './admin-session.service';

/** Badge de no leídos CRM compartido entre shell y /crm. */
@Injectable({ providedIn: 'root' })
export class CrmBadgeService {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private timer?: ReturnType<typeof setInterval>;

  readonly unread = signal(0);

  start(): void {
    this.refresh();
    if (this.timer) clearInterval(this.timer);
    this.timer = setInterval(() => this.refresh(), 15_000);
  }

  stop(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = undefined;
    }
    this.unread.set(0);
  }

  refresh(): void {
    if (!this.session.isLoggedIn()) {
      this.unread.set(0);
      return;
    }
    this.api.listChatConversaciones().subscribe({
      next: (rows) => {
        const total = rows.reduce((s, c) => s + (c.noLeidosSoporte ?? 0), 0);
        this.unread.set(total);
      },
      error: () => undefined,
    });
  }
}
