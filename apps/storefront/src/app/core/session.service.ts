import { Injectable, signal } from '@angular/core';
import { setAccessToken } from './tenant.interceptor';

const USER_NAME_KEY = 'ecom_user_nombre';
const USER_ID_KEY = 'ecom_user_id';
const CANAL_KEY = 'ecom_canal';
const MAYORISTA_KEY = 'ecom_mayorista';

export type Canal = 'B2C' | 'B2B';

export interface SessionToken {
  accessToken: string;
  idUsuarioEcommerce: number;
  nombre: string;
  canalPreferido: string;
  expiresAtUtc?: string;
}

function readCanal(): Canal {
  if (localStorage.getItem(MAYORISTA_KEY) !== '1') return 'B2C';
  return localStorage.getItem(CANAL_KEY) === 'B2B' ? 'B2B' : 'B2C';
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  readonly userName = signal<string | null>(localStorage.getItem(USER_NAME_KEY));
  readonly welcomeMessage = signal<string | null>(null);
  readonly loggedIn = signal(!!localStorage.getItem('ecom_access_token'));
  /** Solo true si admin aprobó o umbral de compras activó mayorista. */
  readonly mayoristaAprobado = signal(localStorage.getItem(MAYORISTA_KEY) === '1');
  /** Canal reactivo (única fuente de verdad para la UI). */
  readonly canal = signal<Canal>(readCanal());
  /** Badge del carrito. */
  readonly cartCount = signal(0);
  /** Monto total del carrito (Gs). */
  readonly cartTotal = signal(0);

  displayName(): string {
    return this.userName() || 'Cliente';
  }

  setMayoristaAprobado(ok: boolean): void {
    localStorage.setItem(MAYORISTA_KEY, ok ? '1' : '0');
    this.mayoristaAprobado.set(ok);
    if (!ok && this.canal() === 'B2B') this.setCanal('B2C');
  }

  setCanal(canal: Canal): void {
    if (canal === 'B2B' && !this.mayoristaAprobado()) {
      canal = 'B2C';
    }
    localStorage.setItem(CANAL_KEY, canal);
    this.canal.set(canal);
  }

  setCartCount(n: number): void {
    this.cartCount.set(Math.max(0, n | 0));
  }

  setCartTotal(n: number): void {
    this.cartTotal.set(Number.isFinite(n) ? Math.max(0, n) : 0);
  }

  persist(token: SessionToken, opts?: { welcome?: boolean; from?: 'login' | 'activate' | 'register' }): void {
    setAccessToken(token.accessToken, token.expiresAtUtc ?? null);
    this.setCanal('B2C');
    this.setMayoristaAprobado(false);
    const nombre = (token.nombre || '').trim() || 'Cliente';
    localStorage.setItem(USER_NAME_KEY, nombre);
    localStorage.setItem(USER_ID_KEY, String(token.idUsuarioEcommerce));
    this.userName.set(nombre);
    this.loggedIn.set(true);

    if (opts?.welcome !== false) {
      const from = opts?.from ?? 'login';
      const msg =
        from === 'activate'
          ? `¡Bienvenido/a, ${nombre}! Tu cuenta ya está activa.`
          : from === 'register'
            ? `¡Bienvenido/a, ${nombre}! Ya podés comprar.`
            : `¡Bienvenido/a, ${nombre}!`;
      this.showWelcome(msg);
    }
  }

  applyProfileCanal(estadoMayorista: string, canalPreferido: string): void {
    const aprobado = (estadoMayorista || '').toLowerCase() === 'aprobado';
    this.setMayoristaAprobado(aprobado);
    if (aprobado && canalPreferido === 'B2B') this.setCanal('B2B');
    else this.setCanal('B2C');
  }

  showWelcome(message: string): void {
    this.welcomeMessage.set(message);
    window.setTimeout(() => {
      if (this.welcomeMessage() === message) this.welcomeMessage.set(null);
    }, 6000);
  }

  dismissWelcome(): void {
    this.welcomeMessage.set(null);
  }

  clear(): void {
    setAccessToken(null);
    localStorage.removeItem(USER_NAME_KEY);
    localStorage.removeItem(USER_ID_KEY);
    localStorage.removeItem(MAYORISTA_KEY);
    localStorage.removeItem('ecom_expires_at');
    localStorage.setItem(CANAL_KEY, 'B2C');
    this.userName.set(null);
    this.loggedIn.set(false);
    this.mayoristaAprobado.set(false);
    this.canal.set('B2C');
    this.cartCount.set(0);
    this.cartTotal.set(0);
    this.welcomeMessage.set(null);
  }
}
