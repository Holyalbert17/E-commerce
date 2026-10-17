import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { SessionService } from './session.service';
import { newUuid } from './uuid';

const TENANT_SLUG_KEY = 'ecom_tenant_slug';
const CART_SESSION_KEY = 'ecom_cart_session';
const TOKEN_KEY = 'ecom_access_token';
const EXPIRES_KEY = 'ecom_expires_at';

function defaultSlug(): string {
  return environment.defaultTenantSlug || 'casa-imperial';
}

/** Slugs legacy → tenant operativo (Casa Imperial). */
function resolveSlug(raw: string | null | undefined): string {
  const s = (raw || '').trim().toLowerCase();
  if (!s || s === 'demo') return defaultSlug();
  return (raw || defaultSlug()).trim();
}

function clearExpiredToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRES_KEY);
}

export function isTokenExpired(): boolean {
  const raw = localStorage.getItem(EXPIRES_KEY);
  if (!raw) return false;
  const exp = Date.parse(raw);
  if (Number.isNaN(exp)) return false;
  return Date.now() >= exp - 30_000;
}

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionService);
  const slug = resolveSlug(localStorage.getItem(TENANT_SLUG_KEY));
  if (localStorage.getItem(TENANT_SLUG_KEY) !== slug) {
    localStorage.setItem(TENANT_SLUG_KEY, slug);
  }

  if (isTokenExpired()) {
    clearExpiredToken();
    if (session.loggedIn()) session.clear();
  }

  const token = localStorage.getItem(TOKEN_KEY);

  let headers = req.headers.set('X-Tenant-Slug', slug);
  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  let cartSession = localStorage.getItem(CART_SESSION_KEY);
  if (!cartSession) {
    cartSession = newUuid();
    localStorage.setItem(CART_SESSION_KEY, cartSession);
  }
  headers = headers.set('X-Cart-Session', cartSession);

  return next(req.clone({ headers })).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        const isAuthPublic =
          /\/api\/auth\/(login|register|activate|forgot-password|reset-password)/i.test(req.url);
        if (!isAuthPublic && token) {
          clearExpiredToken();
          session.clear();
        }
      }
      return throwError(() => err);
    }),
  );
};

export function setTenantSlug(slug: string): void {
  localStorage.setItem(TENANT_SLUG_KEY, resolveSlug(slug));
}

export function getTenantSlug(): string {
  const resolved = resolveSlug(localStorage.getItem(TENANT_SLUG_KEY));
  if (localStorage.getItem(TENANT_SLUG_KEY) !== resolved) {
    localStorage.setItem(TENANT_SLUG_KEY, resolved);
  }
  return resolved;
}

export function setAccessToken(token: string | null, expiresAtUtc?: string | null): void {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
    if (expiresAtUtc) localStorage.setItem(EXPIRES_KEY, expiresAtUtc);
  } else {
    clearExpiredToken();
  }
}

export function getAccessToken(): string | null {
  if (isTokenExpired()) {
    clearExpiredToken();
    return null;
  }
  return localStorage.getItem(TOKEN_KEY);
}

export function getCartSession(): string {
  let session = localStorage.getItem(CART_SESSION_KEY);
  if (!session) {
    session = newUuid();
    localStorage.setItem(CART_SESSION_KEY, session);
  }
  return session;
}
