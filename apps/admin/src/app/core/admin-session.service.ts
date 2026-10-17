import { Injectable, computed, signal } from '@angular/core';
import { environment } from '../../environments/environment';

/** Separado del storefront (`ecom_access_token`) para no mezclar JWTs. */
const TOKEN_KEY = 'ecom_admin_access_token';
const LEGACY_TOKEN_KEY = 'ecom_access_token';
const EXPIRES_KEY = 'ecom_admin_expires_at';
const SLUG_KEY = 'ecom_tenant_slug';
const NAME_KEY = 'ecom_admin_name';
const USER_ID_KEY = 'ecom_admin_user_id';
const PERMS_KEY = 'ecom_admin_permissions';
const SUPER_KEY = 'ecom_admin_is_super';
const EMPRESA_KEY = 'ecom_admin_id_empresa';

function defaultSlug(): string {
  return environment.defaultTenantSlug || 'casa-imperial';
}

function resolveSlug(raw: string | null | undefined): string {
  const s = (raw || '').trim().toLowerCase();
  if (!s || s === 'demo') return defaultSlug();
  return (raw || defaultSlug()).trim();
}

@Injectable({ providedIn: 'root' })
export class AdminSessionService {
  private readonly tokenSig = signal<string | null>(this.readToken());
  private readonly slugSig = signal(resolveSlug(localStorage.getItem(SLUG_KEY)));
  private readonly nameSig = signal(localStorage.getItem(NAME_KEY) || '');
  private readonly userIdSig = signal<number | null>(this.readUserId());
  private readonly empresaSig = signal<number | null>(this.readEmpresa());
  private readonly permsSig = signal<string[]>(this.readPerms());
  private readonly superSig = signal(localStorage.getItem(SUPER_KEY) === '1');

  readonly isLoggedIn = computed(() => !!this.tokenSig() && !this.isExpired());
  readonly tenantSlug = computed(() => this.slugSig());
  readonly displayName = computed(() => this.nameSig() || 'Admin');
  readonly userId = computed(() => this.userIdSig());
  readonly idEmpresa = computed(() => this.empresaSig());
  readonly permissions = computed(() => this.permsSig());
  readonly isSuper = computed(() => this.superSig());

  constructor() {
    // Migrar localStorage legacy `demo` → Casa Imperial.
    const resolved = resolveSlug(localStorage.getItem(SLUG_KEY));
    if (localStorage.getItem(SLUG_KEY) !== resolved) {
      localStorage.setItem(SLUG_KEY, resolved);
      this.slugSig.set(resolved);
    }
  }

  getToken(): string | null {
    if (this.isExpired()) {
      this.logout();
      return null;
    }
    return this.tokenSig();
  }

  /** True si tiene el permiso, es super, o aún no hay lista (compat sesiones viejas). */
  can(...codes: string[]): boolean {
    if (this.superSig()) return true;
    const perms = this.permsSig();
    if (perms.length === 0) return true; // sesión legacy sin permisos → no ocultar nav
    if (perms.some((p) => p.toLowerCase() === 'manager:all')) return true;
    return codes.some((c) => perms.includes(c));
  }

  setSession(opts: {
    token: string;
    slug: string;
    nombre?: string;
    expiresAtUtc?: string;
    idUsuarioEcommerce?: number;
    idEmpresa?: number;
    permissions?: string[];
    isSuper?: boolean;
  }): void {
    localStorage.setItem(TOKEN_KEY, opts.token);
    localStorage.removeItem(LEGACY_TOKEN_KEY);
    localStorage.setItem(SLUG_KEY, resolveSlug(opts.slug));
    if (opts.expiresAtUtc) localStorage.setItem(EXPIRES_KEY, opts.expiresAtUtc);
    else localStorage.removeItem(EXPIRES_KEY);
    if (opts.nombre) localStorage.setItem(NAME_KEY, opts.nombre);
    if (opts.idUsuarioEcommerce != null) {
      localStorage.setItem(USER_ID_KEY, String(opts.idUsuarioEcommerce));
      this.userIdSig.set(opts.idUsuarioEcommerce);
    }
    if (opts.idEmpresa != null) {
      localStorage.setItem(EMPRESA_KEY, String(opts.idEmpresa));
      this.empresaSig.set(opts.idEmpresa);
    }
    const perms = opts.permissions ?? [];
    localStorage.setItem(PERMS_KEY, JSON.stringify(perms));
    this.permsSig.set(perms);
    const isSuper = !!opts.isSuper || perms.some((p) => p.toLowerCase() === 'manager:all');
    localStorage.setItem(SUPER_KEY, isSuper ? '1' : '0');
    this.superSig.set(isSuper);
    this.tokenSig.set(opts.token);
    this.slugSig.set(resolveSlug(opts.slug));
    if (opts.nombre) this.nameSig.set(opts.nombre);
  }

  setSlug(slug: string): void {
    const resolved = resolveSlug(slug);
    localStorage.setItem(SLUG_KEY, resolved);
    this.slugSig.set(resolved);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    localStorage.removeItem(NAME_KEY);
    localStorage.removeItem(USER_ID_KEY);
    localStorage.removeItem(EMPRESA_KEY);
    localStorage.removeItem(PERMS_KEY);
    localStorage.removeItem(SUPER_KEY);
    this.tokenSig.set(null);
    this.nameSig.set('');
    this.userIdSig.set(null);
    this.empresaSig.set(null);
    this.permsSig.set([]);
    this.superSig.set(false);
  }

  isExpired(): boolean {
    const raw = localStorage.getItem(EXPIRES_KEY);
    if (!raw) return false;
    const exp = Date.parse(raw);
    if (Number.isNaN(exp)) return false;
    return Date.now() >= exp - 30_000;
  }

  private readToken(): string | null {
    const admin = localStorage.getItem(TOKEN_KEY);
    if (admin) return admin;
    const legacy = localStorage.getItem(LEGACY_TOKEN_KEY);
    if (legacy) {
      localStorage.setItem(TOKEN_KEY, legacy);
      return legacy;
    }
    return null;
  }

  private readUserId(): number | null {
    const raw = localStorage.getItem(USER_ID_KEY);
    if (!raw) return null;
    const n = Number(raw);
    return Number.isFinite(n) && n > 0 ? n : null;
  }

  private readEmpresa(): number | null {
    const raw = localStorage.getItem(EMPRESA_KEY);
    if (!raw) return null;
    const n = Number(raw);
    return Number.isFinite(n) && n > 0 ? n : null;
  }

  private readPerms(): string[] {
    try {
      const raw = localStorage.getItem(PERMS_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string') : [];
    } catch {
      return [];
    }
  }
}
