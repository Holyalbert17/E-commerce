import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { BrandingService } from '../core/branding.service';
import { ToastService } from '../core/toast.service';
import { DxIconComponent } from '../shared/dx-icon.component';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, DxIconComponent],
  template: `
    <section class="contact" aria-labelledby="ct-title">
      <header>
        <div>
          <h1 id="ct-title"><dx-icon name="chat" [size]="28" /> Contacto</h1>
          <p class="lead">Escribinos y te respondemos a la brevedad.</p>
        </div>
        <a routerLink="/catalogo"><dx-icon name="grid" [size]="14" /> Catálogo</a>
      </header>

      <div class="grid">
        <form class="form" (ngSubmit)="enviar()">
          <label>
            Nombre
            <input class="dx-input" [(ngModel)]="nombre" name="nombre" required maxlength="120" />
          </label>
          <label>
            Email
            <input class="dx-input" type="email" [(ngModel)]="email" name="email" required maxlength="160" />
          </label>
          <label>
            Teléfono <span class="opt">(opcional)</span>
            <input class="dx-input" [(ngModel)]="telefono" name="telefono" maxlength="40" />
          </label>
          <label>
            Mensaje
            <textarea class="dx-input" rows="5" [(ngModel)]="mensaje" name="mensaje" required maxlength="4000"></textarea>
          </label>
          @if (error()) {
            <p class="dx-alert dx-alert--error" role="alert">{{ error() }}</p>
          }
          <button type="submit" class="dx-btn dx-btn--lg" [disabled]="busy()">
            {{ busy() ? 'Enviando…' : 'Enviar mensaje' }}
          </button>
        </form>

        <aside class="side">
          <h2>Datos de la tienda</h2>
          @if (brand.branding()?.direccionLocal; as dir) {
            <p>
              <dx-icon name="store" [size]="16" />
              {{ dir }}
              @if (brand.branding()?.ciudadLocal; as ciu) {
                <span class="mute"> · {{ ciu }}</span>
              }
            </p>
          }
          @if (brand.branding()?.telefonoContacto; as tel) {
            <p>
              <a [href]="'tel:' + tel">{{ tel }}</a>
            </p>
          }
          @if (brand.branding()?.emailContacto; as mail) {
            <p>
              <a [href]="'mailto:' + mail">{{ mail }}</a>
            </p>
          }
          @if (brand.branding()?.horarioAtencion; as hor) {
            <p class="mute">{{ hor }}</p>
          }
          @if (!hasContact()) {
            <p class="mute">Pronto publicaremos más datos de contacto.</p>
          }
          <a class="dx-btn dx-btn--ghost" routerLink="/sucursales">Ver sucursales</a>
        </aside>
      </div>
    </section>
  `,
  styles: [
    `
      header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 1.25rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: clamp(1.5rem, 3vw, 2rem);
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
      }
      .lead {
        margin: 0.35rem 0 0;
        color: var(--dx-mute);
      }
      a:not(.dx-btn) {
        color: var(--dx-red);
        font-weight: 700;
        text-decoration: none;
      }
      .grid {
        display: grid;
        grid-template-columns: 1.4fr 1fr;
        gap: 1.25rem;
      }
      @media (max-width: 800px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
      .form,
      .side {
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.2rem;
        box-shadow: var(--dx-shadow);
      }
      label {
        display: grid;
        gap: 0.35rem;
        margin-bottom: 0.75rem;
        font-weight: 650;
        font-size: 0.9rem;
      }
      .opt {
        font-weight: 500;
        color: var(--dx-mute);
      }
      .side h2 {
        margin: 0 0 0.75rem;
        font-family: var(--dx-display);
        font-size: 1.1rem;
      }
      .side p {
        margin: 0 0 0.55rem;
        display: flex;
        align-items: flex-start;
        gap: 0.4rem;
        color: var(--dx-ink-soft);
      }
      .mute {
        color: var(--dx-mute);
      }
      .side .dx-btn {
        margin-top: 0.75rem;
      }
    `,
  ],
})
export class ContactPageComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly brand = inject(BrandingService);

  nombre = '';
  email = '';
  telefono = '';
  mensaje = '';
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  hasContact(): boolean {
    const b = this.brand.branding();
    return !!(b?.direccionLocal || b?.telefonoContacto || b?.emailContacto || b?.horarioAtencion);
  }

  enviar(): void {
    const nombre = this.nombre.trim();
    const email = this.email.trim();
    const mensaje = this.mensaje.trim();
    if (!nombre || !email || !mensaje) {
      this.error.set('Completá nombre, email y mensaje.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.api
      .enviarContacto({
        nombre,
        email,
        telefono: this.telefono.trim() || undefined,
        mensaje,
      })
      .subscribe({
        next: (r) => {
          this.busy.set(false);
          this.toast.success(r.mensaje || 'Mensaje enviado');
          this.nombre = '';
          this.email = '';
          this.telefono = '';
          this.mensaje = '';
        },
        error: (e) => {
          this.busy.set(false);
          const msg = e?.error?.error || e.message || 'No se pudo enviar';
          this.error.set(msg);
          this.toast.error(msg);
        },
      });
  }
}
