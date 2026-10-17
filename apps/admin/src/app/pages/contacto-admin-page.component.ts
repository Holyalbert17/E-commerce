import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApiService, AdminContactoMensaje } from '../core/admin-api.service';
import { AdminSessionService } from '../core/admin-session.service';
import { ContactoBadgeService } from '../core/contacto-badge.service';

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Contacto</h1>
        <p class="dx-page-sub">
          Mensajes del formulario de la tienda (incluye suscripciones a newsletter marcadas como «Newsletter»).
        </p>
      </div>
      <div class="dx-filters">
        <select class="custom-select custom-select-sm" [(ngModel)]="filtro" (ngModelChange)="load()">
          <option value="todos">Todos</option>
          <option value="no_leidos">No leídos</option>
          <option value="leidos">Leídos</option>
        </select>
        <button type="button" class="btn btn-outline-secondary btn-sm" (click)="load()" title="Actualizar">
          <i class="fas fa-sync-alt"></i>
        </button>
        @if (unreadCount() > 0) {
          <span class="badge badge-soft-warning">{{ unreadCount() }} sin leer</span>
        }
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
        {{ error() }}
      </div>
    }
    @if (ok()) {
      <div class="alert alert-success alert-dismissible">
        <button type="button" class="close" (click)="ok.set(null)"><span>&times;</span></button>
        {{ ok() }}
      </div>
    }

    <div class="row">
      <div class="col-lg-5">
        <div class="card">
          <div class="list-group list-group-flush">
            @for (m of rows(); track m.id) {
              <button
                type="button"
                class="list-group-item list-group-item-action"
                [class.active]="selected()?.id === m.id"
                [class.font-weight-bold]="!m.leido"
                (click)="select(m)"
              >
                <div class="d-flex justify-content-between">
                  <span>{{ m.nombre }}</span>
                  <small class="text-muted">{{ m.fechaUtc | date: 'short' }}</small>
                </div>
                <div class="small text-muted text-truncate">{{ m.email }} · {{ preview(m.mensaje) }}</div>
                @if (!m.leido) {
                  <span class="badge badge-danger mt-1">Nuevo</span>
                }
              </button>
            } @empty {
              <div class="p-4 text-center text-muted">
                {{ loading() ? 'Cargando…' : 'Sin mensajes con ese filtro.' }}
              </div>
            }
          </div>
        </div>
      </div>

      <div class="col-lg-7">
        @if (selected(); as m) {
          <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap">
              <div>
                <h3 class="card-title mb-0">{{ m.nombre }}</h3>
                <small class="text-muted">{{ m.fechaUtc | date: 'medium' }}</small>
              </div>
              <div class="btn-group btn-group-sm">
                @if (!m.leido) {
                  <button type="button" class="btn btn-primary" (click)="setLeido(m, true)" [disabled]="busy()">
                    Marcar leído
                  </button>
                } @else {
                  <button type="button" class="btn btn-outline-secondary" (click)="setLeido(m, false)" [disabled]="busy()">
                    Marcar no leído
                  </button>
                }
                <a class="btn btn-outline-secondary" [href]="'mailto:' + m.email">Responder email</a>
              </div>
            </div>
            <div class="card-body">
              <dl class="row mb-3">
                <dt class="col-sm-3">Email</dt>
                <dd class="col-sm-9"><a [href]="'mailto:' + m.email">{{ m.email }}</a></dd>
                <dt class="col-sm-3">Teléfono</dt>
                <dd class="col-sm-9">{{ m.telefono || '—' }}</dd>
              </dl>
              <div class="border rounded p-3 bg-light" style="white-space: pre-wrap">{{ m.mensaje }}</div>
            </div>
          </div>
        } @else {
          <div class="card">
            <div class="card-body text-muted">Seleccioná un mensaje a la izquierda.</div>
          </div>
        }
      </div>
    </div>
  `,
})
export class ContactoAdminPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private readonly badge = inject(ContactoBadgeService);
  readonly session = inject(AdminSessionService);

  readonly rows = signal<AdminContactoMensaje[]>([]);
  readonly selected = signal<AdminContactoMensaje | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);

  filtro: 'todos' | 'no_leidos' | 'leidos' = 'no_leidos';

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.badge.refresh();
  }

  unreadCount(): number {
    return this.rows().filter((r) => !r.leido).length;
  }

  preview(msg: string): string {
    const t = (msg || '').replace(/\s+/g, ' ').trim();
    return t.length > 80 ? t.slice(0, 80) + '…' : t;
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const leido = this.filtro === 'todos' ? undefined : this.filtro === 'leidos';
    this.api.listContacto(leido).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
        const cur = this.selected();
        if (cur) {
          const fresh = rows.find((r) => r.id === cur.id);
          this.selected.set(fresh ?? null);
        }
        this.badge.refresh();
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.error || e.message || 'No se pudo cargar');
      },
    });
  }

  select(m: AdminContactoMensaje): void {
    this.selected.set(m);
    this.ok.set(null);
    if (!m.leido) this.setLeido(m, true, true);
  }

  setLeido(m: AdminContactoMensaje, leido: boolean, silent = false): void {
    this.busy.set(true);
    this.api.marcarContactoLeido(m.id, leido).subscribe({
      next: () => {
        this.busy.set(false);
        this.rows.update((list) => list.map((x) => (x.id === m.id ? { ...x, leido } : x)));
        this.selected.update((s) => (s && s.id === m.id ? { ...s, leido } : s));
        if (!silent) this.ok.set(leido ? 'Marcado como leído' : 'Marcado como no leído');
        this.badge.refresh();
        if (this.filtro !== 'todos' && ((this.filtro === 'leidos' && !leido) || (this.filtro === 'no_leidos' && leido))) {
          this.load();
        }
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error || e.message);
      },
    });
  }
}
