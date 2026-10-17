import { Component, ElementRef, OnDestroy, OnInit, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, TipoDocumento, getCanal } from '../core/api.service';
import { AuthModalService } from '../core/auth-modal.service';
import { SessionService } from '../core/session.service';
import { ToastService } from '../core/toast.service';
import { getTenantSlug, setTenantSlug } from '../core/tenant.interceptor';
import { DxIconComponent } from './dx-icon.component';

@Component({
  selector: 'dx-auth-modal',
  standalone: true,
  imports: [FormsModule, DxIconComponent],
  host: {
    '(document:keydown.escape)': 'onEsc()',
    '(document:keydown)': 'onDocKey($event)',
  },
  template: `
    @if (auth.open()) {
      <div class="backdrop" (click)="auth.close()" role="presentation"></div>
      <div
        #panel
        class="modal"
        role="dialog"
        aria-modal="true"
        tabindex="-1"
        [attr.aria-labelledby]="titleId()"
        (click)="$event.stopPropagation()"
      >
        <button type="button" class="x" (click)="auth.close()" aria-label="Cerrar">
          <dx-icon name="close" [size]="18" />
        </button>

        @if (auth.mode() === 'login') {
          <div class="head">
            <span class="ico"><dx-icon name="login" [size]="26" /></span>
            <div>
              <h2 id="m-login-title">Iniciar sesión</h2>
              <p>Tu cuenta de tienda es <strong>minorista</strong>. El mayorista se habilita por solicitud o volumen.</p>
            </div>
          </div>
          <form (ngSubmit)="doLogin()">
            <label>
              Usuario o email
              <input class="dx-input" name="usuario" [(ngModel)]="login.usuario" autocomplete="username" />
            </label>
            <label>
              Contraseña
              <input
                class="dx-input"
                name="password"
                type="password"
                [(ngModel)]="login.password"
                autocomplete="current-password"
              />
            </label>
            <p class="forgot-row">
              <button type="button" class="link" (click)="goForgot()">¿Olvidaste tu contraseña?</button>
            </p>
            <button type="submit" class="dx-btn dx-btn--lg" [disabled]="busy()">
              <dx-icon name="login" [size]="18" /> Entrar
            </button>
          </form>
          @if (message()) {
            <p class="dx-alert" [class.dx-alert--ok]="ok()" [class.dx-alert--error]="!ok()" role="status">
              {{ message() }}
            </p>
          }
          @if (needsActivation()) {
            <p class="switch">
              ¿Te registraste recién?
              <button type="button" class="link" (click)="goResendActivation()">Reenviar activación</button>
            </p>
          }
          @if (showResendPanel()) {
            <div class="success" role="status">
              <label class="resend">
                <span>Correo de la cuenta</span>
                <input class="dx-input" type="email" [(ngModel)]="resendEmail" name="resendEmail" placeholder="tu@email.com" />
              </label>
              <button type="button" class="dx-btn" (click)="doResendActivation()" [disabled]="resending() || !resendEmail.trim()">
                {{ resending() ? 'Enviando…' : 'Reenviar activación' }}
              </button>
              @if (activationUrl()) {
                <a class="dx-btn" [href]="activationUrl()!" rel="noopener">
                  <dx-icon name="check" [size]="16" /> Activar cuenta ahora
                </a>
                <p class="act-link">
                  <a [href]="activationUrl()!" target="_blank" rel="noopener">{{ activationUrl() }}</a>
                </p>
              }
            </div>
          }
          <p class="switch">
            ¿No tenés cuenta?
            <button type="button" class="link" (click)="auth.switchTo('register')">Registrarse</button>
          </p>
        } @else if (auth.mode() === 'forgot') {
          <div class="head">
            <span class="ico"><dx-icon name="login" [size]="26" /></span>
            <div>
              <h2 id="m-forgot-title">Recuperar contraseña</h2>
              <p>Te enviaremos un enlace para elegir una contraseña nueva.</p>
            </div>
          </div>
          <form (ngSubmit)="doForgot()">
            <label>
              Correo
              <input
                class="dx-input"
                type="email"
                name="forgotEmail"
                [(ngModel)]="forgot.email"
                autocomplete="email"
                required
              />
            </label>
            @if (message()) {
              <p class="dx-alert" [class.dx-alert--ok]="ok()" [class.dx-alert--error]="!ok()" role="status">
                {{ message() }}
              </p>
            }
            @if (resetUrl()) {
              <p class="act-hint">En demo (sin SMTP), usá este enlace:</p>
              <a class="dx-btn" [href]="resetUrl()!" rel="noopener">
                <dx-icon name="login" [size]="16" /> Restablecer contraseña
              </a>
              <p class="act-link">
                <a [href]="resetUrl()!" target="_blank" rel="noopener">{{ resetUrl() }}</a>
              </p>
            }
            <button type="submit" class="dx-btn dx-btn--lg" [disabled]="busy() || !forgot.email.trim()">
              Enviar enlace
            </button>
          </form>
          <p class="switch">
            <button type="button" class="link" (click)="auth.switchTo('login')">Volver a iniciar sesión</button>
            <button type="button" class="link" (click)="goResendFromForgot()">¿Cuenta sin activar?</button>
          </p>
        } @else if (auth.mode() === 'reset') {
          <div class="head">
            <span class="ico"><dx-icon name="login" [size]="26" /></span>
            <div>
              <h2 id="m-reset-title">Nueva contraseña</h2>
              <p>Elegí una contraseña nueva para tu cuenta.</p>
            </div>
          </div>
          <form (ngSubmit)="doReset()">
            <label>
              Nueva contraseña
              <input
                class="dx-input"
                type="password"
                name="resetPass"
                [(ngModel)]="reset.password"
                autocomplete="new-password"
                required
              />
            </label>
            <label>
              Confirmar
              <input
                class="dx-input"
                type="password"
                name="resetPass2"
                [(ngModel)]="reset.passwordConfirmation"
                autocomplete="new-password"
                required
              />
            </label>
            @if (message()) {
              <p class="dx-alert" [class.dx-alert--ok]="ok()" [class.dx-alert--error]="!ok()" role="status">
                {{ message() }}
              </p>
            }
            <button type="submit" class="dx-btn dx-btn--lg" [disabled]="busy() || !canReset()">
              Guardar contraseña
            </button>
          </form>
          <p class="switch">
            <button type="button" class="link" (click)="auth.switchTo('login')">Ir a iniciar sesión</button>
          </p>
        } @else {
          <div class="head">
            <span class="ico"><dx-icon name="user-plus" [size]="26" /></span>
            <div>
              <h2 id="m-reg-title">Registrarse</h2>
              <p>Creá tu cuenta <strong>minorista</strong> gratis.</p>
            </div>
          </div>

          @if (!regDone()) {
            <ol class="steps" aria-label="Progreso del registro">
              <li [class.on]="regStep() >= 1" [class.now]="regStep() === 1">1 · Datos</li>
              <li [class.on]="regStep() >= 2" [class.now]="regStep() === 2">2 · Contacto</li>
              <li [class.on]="regStep() >= 3" [class.now]="regStep() === 3">3 · Acceso</li>
            </ol>
          }

          @if (regDone()) {
            <div class="success" role="status">
              <h3>¡Listo, {{ reg.nombre }}!</h3>
              <p>{{ message() }}</p>
              @if (activationUrl()) {
                <p class="act-hint">Para entrar tenés que activar la cuenta:</p>
                <a class="dx-btn" [href]="activationUrl()!" rel="noopener">
                  <dx-icon name="check" [size]="16" /> Activar cuenta ahora
                </a>
                <p class="act-link">
                  <a [href]="activationUrl()!" target="_blank" rel="noopener">{{ activationUrl() }}</a>
                </p>
              }
              <button type="button" class="dx-btn dx-btn--ghost" (click)="auth.switchTo('login')">
                <dx-icon name="login" [size]="16" /> Ir a iniciar sesión
              </button>
            </div>
          } @else {
            <form (ngSubmit)="doRegister()">
              @if (regStep() === 1) {
                <div class="row">
                  <label>
                    Nombre
                    <input class="dx-input" [(ngModel)]="reg.nombre" name="nombre" required />
                  </label>
                  <label>
                    Apellido
                    <input class="dx-input" [(ngModel)]="reg.apellido" name="apellido" required />
                  </label>
                </div>
                <label>
                  Tipo de documento
                  <select class="dx-select" [(ngModel)]="reg.idTipoDocumento" name="tipo" [disabled]="!tipos().length">
                    @if (!tipos().length) {
                      <option [ngValue]="0">Cargando tipos…</option>
                    }
                    @for (t of tipos(); track t.id) {
                      <option [ngValue]="t.id">{{ t.nombre }}</option>
                    }
                  </select>
                </label>
                <label>
                  Número de documento
                  <input class="dx-input" [(ngModel)]="reg.numeroDocumento" name="doc" required minlength="6" />
                </label>
                @if (!tipos().length) {
                  <p class="dx-alert dx-alert--error">No se pudieron cargar tipos de documento. Reintentá.</p>
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="loadTipos()">Reintentar</button>
                }
                <button type="button" class="dx-btn" (click)="regStep.set(2)" [disabled]="!canReg1()">
                  Continuar
                </button>
              }
              @if (regStep() === 2) {
                <div class="row">
                  <label>
                    Teléfono
                    <input class="dx-input" [(ngModel)]="reg.telefono" name="tel" required />
                  </label>
                  <label>
                    Móvil
                    <input class="dx-input" [(ngModel)]="reg.movil" name="movil" required />
                  </label>
                </div>
                <label>
                  Correo
                  <input class="dx-input" type="email" [(ngModel)]="reg.email" name="email" required maxlength="100" />
                </label>
                <div class="nav-btns">
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="regStep.set(1)">Atrás</button>
                  <button type="button" class="dx-btn" (click)="regStep.set(3)" [disabled]="!canReg2()">
                    Continuar
                  </button>
                </div>
              }
              @if (regStep() === 3) {
                <div class="row">
                  <label>
                    Contraseña
                    <input class="dx-input" type="password" [(ngModel)]="reg.password" name="pass" required autocomplete="new-password" />
                  </label>
                  <label>
                    Confirmar
                    <input
                      class="dx-input"
                      type="password"
                      [(ngModel)]="reg.passwordConfirmation"
                      name="pass2"
                      required
                      autocomplete="new-password"
                    />
                  </label>
                </div>
                <p class="pass-hint">
                  Mín. 5 caracteres · letra · número · símbolo (ej. <code>Demo12!</code>). Sin 3 iguales seguidos.
                </p>
                @if (reg.password && !passwordOk(reg.password)) {
                  <p class="dx-alert dx-alert--error">La contraseña no cumple los requisitos.</p>
                }
                @if (reg.passwordConfirmation && reg.password !== reg.passwordConfirmation) {
                  <p class="dx-alert dx-alert--error">Las contraseñas no coinciden.</p>
                }
                @if (message()) {
                  <p class="dx-alert dx-alert--error">{{ message() }}</p>
                }
                <div class="nav-btns">
                  <button type="button" class="dx-btn dx-btn--ghost" (click)="regStep.set(2)">Atrás</button>
                  <button type="submit" class="dx-btn" [disabled]="busy() || !canReg3()">
                    <dx-icon name="user-plus" [size]="16" /> Registrarme
                  </button>
                </div>
              }
            </form>
            <p class="switch">
              ¿Ya tenés cuenta?
              <button type="button" class="link" (click)="auth.switchTo('login')">Iniciar sesión</button>
            </p>
          }
        }
      </div>
    }
  `,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        background: rgba(18, 18, 18, 0.55);
        backdrop-filter: blur(4px);
        z-index: 90;
        animation: fade 0.2s ease;
      }
      .modal {
        position: fixed;
        z-index: 91;
        left: 50%;
        top: 50%;
        transform: translate(-50%, -50%);
        width: min(calc(100vw - 1.5rem), 460px);
        max-height: min(90vh, 720px);
        overflow: auto;
        background: #fff;
        border-radius: 18px;
        border: 1px solid var(--dx-line);
        box-shadow: 0 24px 64px rgba(0, 0, 0, 0.28);
        padding: clamp(1.2rem, 3vw, 1.75rem);
        animation: pop 0.22s ease;
        outline: none;
      }
      .x {
        position: absolute;
        top: 0.85rem;
        right: 0.85rem;
        width: 36px;
        height: 36px;
        border: none;
        border-radius: 10px;
        background: #f3f3f3;
        cursor: pointer;
        display: grid;
        place-items: center;
        color: var(--dx-ink);
      }
      .x:hover {
        background: var(--dx-red-soft);
        color: var(--dx-red);
      }
      .head {
        display: flex;
        gap: 0.85rem;
        align-items: flex-start;
        margin: 0 1.5rem 1.15rem 0;
      }
      .ico {
        width: 48px;
        height: 48px;
        border-radius: 12px;
        background: var(--dx-red-soft);
        color: var(--dx-red);
        display: grid;
        place-items: center;
        flex-shrink: 0;
      }
      h2 {
        margin: 0;
        font-family: var(--dx-display);
        font-size: 1.35rem;
      }
      .head p {
        margin: 0.3rem 0 0;
        color: var(--dx-ink-soft);
        font-size: 0.9rem;
      }
      .steps {
        list-style: none;
        margin: 0 0 1rem;
        padding: 0;
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 0.35rem;
      }
      .steps li {
        font-size: 0.72rem;
        font-weight: 700;
        text-align: center;
        padding: 0.45rem 0.25rem;
        border-radius: 999px;
        border: 1px solid var(--dx-line);
        color: var(--dx-mute, #888);
        background: #fafafa;
      }
      .steps li.on {
        color: var(--dx-red);
        border-color: #ffcdd2;
        background: var(--dx-red-soft);
      }
      .steps li.now {
        box-shadow: 0 0 0 2px rgba(198, 40, 40, 0.25);
      }
      form {
        display: grid;
        gap: 0.8rem;
      }
      label {
        display: grid;
        gap: 0.35rem;
        font-size: 0.88rem;
        font-weight: 600;
      }
      .row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.65rem;
      }
      @media (max-width: 480px) {
        .row {
          grid-template-columns: 1fr;
        }
      }
      .nav-btns {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .forgot-row {
        margin: -0.25rem 0 0;
        text-align: right;
      }
      .switch {
        margin: 1rem 0 0;
        color: var(--dx-ink-soft);
        font-size: 0.92rem;
      }
      .link {
        background: none;
        border: none;
        color: var(--dx-red);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        padding: 0;
      }
      .success {
        display: grid;
        gap: 0.75rem;
        justify-items: start;
      }
      .success h3 {
        margin: 0;
        color: var(--dx-ok);
        font-family: var(--dx-display);
      }
      .act-hint {
        margin: 0;
        font-size: 0.92rem;
        color: var(--dx-ink-soft, #475569);
      }
      .act-link {
        margin: 0;
        font-size: 0.75rem;
        word-break: break-all;
      }
      .act-link a {
        color: var(--dx-red, #c62828);
      }
      .pass-hint {
        margin: 0;
        font-size: 0.8rem;
        color: var(--dx-mute, #64748b);
      }
      .pass-hint code {
        font-size: 0.85em;
        background: #f1f5f9;
        padding: 0.1rem 0.35rem;
        border-radius: 4px;
      }
      @keyframes fade {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }
      @keyframes pop {
        from {
          opacity: 0;
          transform: translate(-50%, -46%) scale(0.97);
        }
        to {
          opacity: 1;
          transform: translate(-50%, -50%) scale(1);
        }
      }
    `,
  ],
})
export class AuthModalComponent implements OnInit, OnDestroy {
  readonly auth = inject(AuthModalService);
  private readonly api = inject(ApiService);
  private readonly session = inject(SessionService);
  private readonly toast = inject(ToastService);
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  readonly busy = signal(false);
  readonly message = signal<string | null>(null);
  readonly ok = signal(false);
  readonly tipos = signal<TipoDocumento[]>([]);
  readonly regStep = signal(1);
  readonly regDone = signal(false);
  readonly activationUrl = signal<string | null>(null);
  readonly resetUrl = signal<string | null>(null);
  readonly needsActivation = signal(false);
  /** Tras login fallido: reenvío de activación desde el modal. */
  readonly showResendPanel = signal(false);
  resendEmail = '';
  readonly resending = signal(false);

  private lastFocus: HTMLElement | null = null;

  login = { usuario: '', password: '' };
  forgot = { email: '' };
  reset = { password: '', passwordConfirmation: '' };
  reg = {
    nombre: '',
    apellido: '',
    idTipoDocumento: 0,
    numeroDocumento: '',
    telefono: '',
    movil: '',
    email: '',
    password: '',
    passwordConfirmation: '',
    tenantSlug: getTenantSlug(),
  };

  private readonly syncBody = effect(() => {
    const open = this.auth.open();
    if (open) {
      // Siempre la tienda del storefront activo (nunca pedir slug al usuario).
      const slug = getTenantSlug();
      setTenantSlug(slug);
      this.reg.tenantSlug = slug;
      this.lastFocus = document.activeElement as HTMLElement | null;
      document.body.style.overflow = 'hidden';
      queueMicrotask(() => this.focusFirst());
    } else {
      document.body.style.overflow = '';
      this.lastFocus?.focus?.();
      this.lastFocus = null;
      this.message.set(null);
      this.regStep.set(1);
      this.regDone.set(false);
      this.activationUrl.set(null);
      this.resetUrl.set(null);
      this.needsActivation.set(false);
      this.showResendPanel.set(false);
      this.reset = { password: '', passwordConfirmation: '' };
    }
  });

  ngOnInit(): void {
    this.loadTipos();
  }

  loadTipos(): void {
    this.api.getTiposDocumento().subscribe({
      next: (rows) => {
        this.tipos.set(rows);
        if (rows.length) this.reg.idTipoDocumento = rows.find((t) => t.codigo === 2)?.id ?? rows[0].id;
      },
      error: () => this.tipos.set([]),
    });
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  titleId(): string {
    switch (this.auth.mode()) {
      case 'forgot':
        return 'm-forgot-title';
      case 'reset':
        return 'm-reset-title';
      case 'register':
        return 'm-reg-title';
      default:
        return 'm-login-title';
    }
  }

  onEsc(): void {
    if (this.auth.open()) this.auth.close();
  }

  onDocKey(ev: KeyboardEvent): void {
    if (!this.auth.open() || ev.key !== 'Tab') return;
    const root = this.panel()?.nativeElement;
    if (!root) return;
    const nodes = Array.from(
      root.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ),
    ).filter((el) => !el.hasAttribute('disabled') && el.offsetParent !== null);
    if (!nodes.length) return;
    const first = nodes[0];
    const last = nodes[nodes.length - 1];
    if (ev.shiftKey && document.activeElement === first) {
      ev.preventDefault();
      last.focus();
    } else if (!ev.shiftKey && document.activeElement === last) {
      ev.preventDefault();
      first.focus();
    }
  }

  private focusFirst(): void {
    const root = this.panel()?.nativeElement;
    if (!root) return;
    const first = root.querySelector<HTMLElement>('input, select, textarea, button:not(.x)');
    (first ?? root).focus();
  }

  goForgot(): void {
    this.message.set(null);
    this.resetUrl.set(null);
    this.needsActivation.set(false);
    if (this.login.usuario.includes('@')) this.forgot.email = this.login.usuario.trim();
    this.auth.showForgot();
  }

  goResendActivation(): void {
    this.showResendPanel.set(true);
    this.resendEmail = this.login.usuario.includes('@') ? this.login.usuario.trim() : '';
    this.message.set('Ingresá el correo con el que te registraste para reenviar el enlace de activación.');
    this.ok.set(true);
  }

  goResendFromForgot(): void {
    this.showResendPanel.set(true);
    this.resendEmail = this.forgot.email.trim();
    this.auth.switchTo('login');
    this.message.set('Ingresá el correo para reenviar el enlace de activación (la recuperación solo aplica a cuentas ya activadas).');
    this.ok.set(true);
  }

  doResendActivation(): void {
    const email = this.resendEmail.trim();
    if (!email.includes('@')) {
      this.ok.set(false);
      this.message.set('Ingresá un correo válido.');
      return;
    }
    this.resending.set(true);
    this.api.resendActivation(email, getTenantSlug() || undefined).subscribe({
      next: (r) => {
        this.resending.set(false);
        this.ok.set(true);
        this.activationUrl.set(r.activationUrlDev || null);
        this.message.set(r.mensaje || 'Si la cuenta existe y no está activada, enviamos el enlace.');
        this.toast.success('Revisá tu correo / enlace de activación');
      },
      error: (e) => {
        this.resending.set(false);
        this.ok.set(false);
        this.message.set(e?.error?.error || 'No se pudo reenviar');
      },
    });
  }

  canReg1(): boolean {
    return (
      this.reg.nombre.trim().length >= 2 &&
      this.reg.apellido.trim().length >= 2 &&
      this.reg.numeroDocumento.trim().length >= 6 &&
      this.reg.idTipoDocumento > 0
    );
  }
  canReg2(): boolean {
    return (
      this.reg.telefono.trim().length >= 9 &&
      this.reg.movil.trim().length >= 9 &&
      this.reg.email.includes('@')
    );
  }
  canReg3(): boolean {
    return (
      this.passwordOk(this.reg.password) &&
      this.reg.password === this.reg.passwordConfirmation
    );
  }
  canReset(): boolean {
    return (
      this.passwordOk(this.reset.password) &&
      this.reset.password === this.reset.passwordConfirmation
    );
  }

  /** Misma regla que JwtAuthService.TestPassword en la API. */
  passwordOk(password: string): boolean {
    if (!password || password.length < 5 || password.length > 30) return false;
    if (!/[0-9]/.test(password) || !/[A-Za-z]/.test(password)) return false;
    if (/(.)\1\1/.test(password)) return false;
    if (!/[^\w]/.test(password)) return false;
    return true;
  }

  doLogin(): void {
    this.busy.set(true);
    this.message.set(null);
    const slug = getTenantSlug();
    setTenantSlug(slug);
    this.api.login(this.login.usuario, this.login.password, slug).subscribe({
      next: (token) => {
        this.api.persistSession(token, 'login');
        this.api.me().subscribe({
          next: (p) => {
            this.session.applyProfileCanal(p.estadoMayorista, p.canalPreferido);
            this.refreshCartAfterAuth();
            this.ok.set(true);
            this.busy.set(false);
            this.message.set(`¡Bienvenido/a, ${token.nombre}!`);
            this.auth.notifyLoginSuccess();
            this.toast.success(`Sesión iniciada · ${token.nombre}`);
            window.setTimeout(() => this.auth.close(), 350);
          },
          error: () => {
            this.session.setCanal('B2C');
            this.refreshCartAfterAuth();
            this.ok.set(true);
            this.busy.set(false);
            this.message.set(`¡Bienvenido/a, ${token.nombre}!`);
            this.auth.notifyLoginSuccess();
            this.toast.success(`Sesión iniciada · ${token.nombre}`);
            window.setTimeout(() => this.auth.close(), 350);
          },
        });
      },
      error: (e) => {
        this.ok.set(false);
        this.busy.set(false);
        const msg = e?.error?.error || e.message || 'No se pudo iniciar sesión';
        this.message.set(msg);
        this.needsActivation.set(/activad/i.test(msg));
        this.toast.error(msg);
      },
    });
  }

  /** El carrito guest vive en X-Cart-Session; al login se conserva y se refresca el badge. */
  private refreshCartAfterAuth(): void {
    this.api.getCart(getCanal()).subscribe({
      next: (c) => this.session.setCartCount(c.itemsCount ?? c.items?.length ?? 0),
      error: () => undefined,
    });
  }

  doForgot(): void {
    if (!this.forgot.email.trim()) {
      this.ok.set(false);
      this.message.set('Ingresá tu correo.');
      this.toast.error('Ingresá tu correo.');
      return;
    }
    this.busy.set(true);
    this.message.set(null);
    this.resetUrl.set(null);
    const slug = getTenantSlug();
    setTenantSlug(slug);
    this.api.forgotPassword(this.forgot.email.trim(), slug).subscribe({
      next: (r) => {
        this.busy.set(false);
        this.ok.set(true);
        const msg = r.mensaje || 'Si el correo existe, te enviamos un enlace de recuperación.';
        this.message.set(msg);
        this.resetUrl.set(r.resetUrlDev || null);
        this.toast.success(r.resetUrlDev ? 'Enlace de recuperación listo' : 'Revisá tu correo');
      },
      error: (e) => {
        this.busy.set(false);
        this.ok.set(false);
        const msg = e?.error?.error || e.message || 'No se pudo enviar el enlace';
        this.message.set(msg);
        this.toast.error(msg);
      },
    });
  }

  doReset(): void {
    if (!this.canReset()) {
      this.ok.set(false);
      this.message.set('Revisá la contraseña y la confirmación (mín. 6 caracteres).');
      this.toast.error('Revisá la contraseña y la confirmación.');
      return;
    }
    if (!this.auth.resetCodigo || !this.auth.resetChecksum) {
      this.ok.set(false);
      this.message.set('Link de recuperación incompleto. Pedí uno nuevo.');
      this.toast.error('Link de recuperación incompleto.');
      return;
    }
    this.busy.set(true);
    this.message.set(null);
    this.api
      .resetPassword({
        codigo: this.auth.resetCodigo,
        checksum: this.auth.resetChecksum,
        password: this.reset.password,
        passwordConfirmation: this.reset.passwordConfirmation,
        tenantSlug: this.auth.resetTenant || undefined,
      })
      .subscribe({
        next: (r) => {
          this.busy.set(false);
          this.ok.set(true);
          this.message.set(r.mensaje || 'Contraseña actualizada. Ya podés iniciar sesión.');
          this.toast.success('Contraseña actualizada');
          this.reset = { password: '', passwordConfirmation: '' };
          window.setTimeout(() => {
            this.message.set(null);
            this.auth.switchTo('login');
          }, 900);
        },
        error: (e) => {
          this.busy.set(false);
          this.ok.set(false);
          const msg = e?.error?.error || e.message || 'No se pudo actualizar la contraseña';
          this.message.set(msg);
          this.toast.error(msg);
        },
      });
  }

  doRegister(): void {
    if (!this.canReg3()) {
      this.message.set('Revisá la contraseña (letra, número y símbolo) y la confirmación.');
      this.toast.error('Revisá la contraseña y la confirmación.');
      return;
    }
    this.busy.set(true);
    this.message.set(null);
    this.activationUrl.set(null);
    const slug = getTenantSlug();
    setTenantSlug(slug);
    this.reg.tenantSlug = slug;
    this.api.register(this.reg).subscribe({
      next: (r) => {
        this.busy.set(false);
        this.regDone.set(true);
        this.activationUrl.set(r.activationUrlDev || null);
        this.message.set(
          r.mensaje ||
            'Cuenta creada. Activá tu correo para entrar (en demo usá el botón de activación).',
        );
        this.toast.success('Cuenta creada · activá tu correo');
      },
      error: (e) => {
        this.busy.set(false);
        const msg = e?.error?.error || e?.message || 'Registro fallido';
        this.message.set(msg);
        this.toast.error(msg);
      },
    });
  }
}
