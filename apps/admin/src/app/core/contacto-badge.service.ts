import { Injectable, inject, signal } from '@angular/core';
import { AdminApiService } from './admin-api.service';
import { AdminSessionService } from './admin-session.service';

/** Badge de mensajes de contacto no leídos. */
@Injectable({ providedIn: 'root' })
export class ContactoBadgeService {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private timer?: ReturnType<typeof setInterval>;

  readonly unread = signal(0);

  start(): void {
    this.refresh();
    if (this.timer) clearInterval(this.timer);
    this.timer = setInterval(() => this.refresh(), 20_000);
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
    if (!this.session.can('Site_App_Ecommerce', 'manager:all')) {
      this.unread.set(0);
      return;
    }
    this.api.listContacto(false).subscribe({
      next: (rows) => this.unread.set(rows.length),
      error: () => undefined,
    });
  }
}
