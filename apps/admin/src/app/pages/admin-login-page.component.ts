import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminApiService } from '../core/admin-api.service';
import { firstAllowedAdminPath } from '../core/admin-nav';
import { AdminSessionService } from '../core/admin-session.service';

@Component({
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="login-box">
      <div class="login-logo text-white text-center">
        <div class="brand-mark mx-auto mb-2" style="width:48px;height:48px;font-size:1.15rem">{{ brandMark() }}</div>
        {{ brandTitle() }}
        <div class="h6 font-weight-normal text-white-50 mt-1 mb-0">Panel de operación</div>
      </div>

      <div class="card adm-login-card">
        <div class="card-header">
          <span class="h5 mb-0 font-weight-bold">Ingresá a tu tienda</span>
          <div class="small mt-1" style="opacity:.9">Gestioná catálogo, pedidos y la vitrina en vivo</div>
        </div>
        <div class="card-body login-card-body">
          <form (ngSubmit)="submit()">
            <label class="small font-weight-bold text-muted mb-1">Tienda (tenant)</label>
            <div class="input-group mb-3">
              <input
                class="form-control"
                name="slug"
                [(ngModel)]="slug"
                placeholder="ej. casa-imperial"
                required
                autocomplete="organization"
              />
              <div class="input-group-append">
                <div class="input-group-text"><span class="fas fa-store"></span></div>
              </div>
            </div>

            <label class="small font-weight-bold text-muted mb-1">Usuario</label>
            <div class="input-group mb-3">
              <input
                class="form-control"
                name="usuario"
                [(ngModel)]="usuario"
                placeholder="Usuario"
                required
                autocomplete="username"
              />
              <div class="input-group-append">
                <div class="input-group-text"><span class="fas fa-user"></span></div>
              </div>
            </div>

            <label class="small font-weight-bold text-muted mb-1">Contraseña</label>
            <div class="input-group mb-3">
              <input
                class="form-control"
                [type]="showPass() ? 'text' : 'password'"
                name="password"
                [(ngModel)]="password"
                placeholder="Password"
                required
                autocomplete="current-password"
              />
              <div class="input-group-append">
                <button
                  type="button"
                  class="input-group-text btn"
                  (click)="showPass.set(!showPass())"
                  [attr.aria-label]="showPass() ? 'Ocultar' : 'Mostrar'"
                >
                  <span class="fas" [class.fa-eye]="!showPass()" [class.fa-eye-slash]="showPass()"></span>
                </button>
              </div>
            </div>

            @if (message()) {
              <div class="alert alert-danger py-2">{{ message() }}</div>
            }

            <button class="btn btn-primary btn-block btn-lg" type="submit" [disabled]="busy()">
              @if (busy()) {
                <i class="fas fa-spinner fa-spin mr-1"></i> Entrando…
              } @else {
                Entrar al panel
              }
            </button>
          </form>

          <p class="adm-login-hint mb-0">
            Tenant <code>{{ slug || 'casa-imperial' }}</code>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class AdminLoginPageComponent {
  private readonly api = inject(AdminApiService);
  private readonly session = inject(AdminSessionService);
  private readonly router = inject(Router);

  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  readonly showPass = signal(false);

  slug = this.session.tenantSlug() || 'casa-imperial';
  usuario = 'admin';
  password = '';

  brandTitle(): string {
    const s = (this.slug || 'e-commerce').trim();
    return s
      .split('-')
      .filter(Boolean)
      .map((p) => p.charAt(0).toUpperCase() + p.slice(1))
      .join(' ') || 'E-commerce';
  }

  brandMark(): string {
    const t = this.brandTitle();
    const parts = t.split(' ').filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return t.slice(0, 2).toUpperCase() || 'DX';
  }

  submit(): void {
    this.busy.set(true);
    this.message.set(null);
    const slug = this.slug.trim();
    this.session.setSlug(slug);

    this.api.adminLogin(this.usuario.trim(), this.password).subscribe({
      next: (token) => {
        this.session.setSession({
          token: token.accessToken,
          slug,
          nombre: token.nombre,
          expiresAtUtc: token.expiresAtUtc,
          idUsuarioEcommerce: token.idUsuarioEcommerce,
          idEmpresa: token.idEmpresa,
          permissions: token.permissions ?? [],
          isSuper: token.isSuper,
        });
        this.busy.set(false);
        void this.router.navigateByUrl(firstAllowedAdminPath(this.session));
      },
      error: (e) => {
        this.busy.set(false);
        this.message.set(e?.error?.error || e.message || 'No se pudo iniciar sesión');
      },
    });
  }
}
