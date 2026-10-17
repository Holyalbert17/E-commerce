import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { getTenantSlug } from '../core/tenant.interceptor';
import { DxIconComponent } from '../shared/dx-icon.component';

@Component({
  standalone: true,
  imports: [DxIconComponent, RouterLink, FormsModule],
  template: `
    <section class="act" aria-labelledby="act-title">
      <div class="panel">
        <h1 id="act-title">Activar cuenta</h1>
        <p class="msg" role="status">{{ message() }}</p>

        @if (showResend()) {
          <label class="resend">
            <span>Reenviar enlace de activación</span>
            <input class="dx-input" type="email" [(ngModel)]="resendEmail" placeholder="tu@email.com" />
          </label>
          <button type="button" class="dx-btn" (click)="resend()" [disabled]="resending() || !resendEmail.trim()">
            {{ resending() ? 'Enviando…' : 'Reenviar activación' }}
          </button>
        }

        <div class="actions">
          <a class="dx-btn" routerLink="/catalogo">Ir al catálogo</a>
          <button type="button" class="dx-btn dx-btn--ghost" (click)="auth.showLogin()">
            <dx-icon name="login" [size]="16" /> Ingresar
          </button>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .act {
        display: grid;
        place-items: center;
        min-height: 40vh;
      }
      .panel {
        width: min(100%, 480px);
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 14px;
        padding: 1.75rem;
        box-shadow: var(--dx-shadow);
        display: grid;
        gap: 1rem;
      }
      h1 {
        margin: 0;
        font-family: var(--dx-display);
      }
      .msg {
        margin: 0;
        color: var(--dx-ink-soft);
        font-size: 1.05rem;
      }
      .resend {
        display: grid;
        gap: 0.35rem;
        font-size: 0.9rem;
        font-weight: 650;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }
      a.dx-btn {
        text-decoration: none;
      }
    `,
  ],
})
export class ActivatePageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);
  readonly auth = inject(AuthModalService);

  readonly message = signal('Activando…');
  readonly showResend = signal(false);
  readonly resending = signal(false);
  resendEmail = '';

  ngOnInit(): void {
    const c = this.route.snapshot.queryParamMap.get('c');
    const k = this.route.snapshot.queryParamMap.get('k');
    const tenant = this.route.snapshot.queryParamMap.get('tenant') || getTenantSlug();
    if (!c || !k) {
      this.message.set('Falta el código de activación. Podés solicitar un reenvío abajo.');
      this.showResend.set(true);
      return;
    }
    this.api.activate(c, k, tenant || getTenantSlug() || 'casa-imperial').subscribe({
      next: (tok) => {
        this.session.persist(
          {
            accessToken: tok.accessToken,
            idUsuarioEcommerce: tok.idUsuarioEcommerce,
            nombre: tok.nombre,
            canalPreferido: tok.canalPreferido,
            expiresAtUtc: tok.expiresAtUtc,
          },
          { from: 'activate' },
        );
        this.message.set('Cuenta activada. Ya podés comprar.');
        this.showResend.set(false);
        // Autologin ya persistido; llevar al catálogo tras un momento.
        window.setTimeout(() => {
          void this.router.navigateByUrl('/catalogo');
        }, 1200);
      },
      error: (e) => {
        this.message.set(e?.error?.error || 'No se pudo activar. Probá reenviar el enlace.');
        this.showResend.set(true);
      },
    });
  }

  resend(): void {
    const email = this.resendEmail.trim();
    if (!email) return;
    this.resending.set(true);
    this.api.resendActivation(email, getTenantSlug() || undefined).subscribe({
      next: (r) => {
        this.resending.set(false);
        this.message.set(r.mensaje + (r.activationUrlDev ? ` (dev: ${r.activationUrlDev})` : ''));
      },
      error: (e) => {
        this.resending.set(false);
        this.message.set(e?.error?.error || 'No se pudo reenviar');
      },
    });
  }
}
