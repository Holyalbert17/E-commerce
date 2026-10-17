import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminSessionService } from './admin-session.service';
import { firstAllowedAdminPath } from './admin-nav';

/** Guard por permiso (DEFI backoffice). data.permissions: string[] — OR logic. */
export const adminPermissionGuard: CanActivateFn = (route) => {
  const session = inject(AdminSessionService);
  const router = inject(Router);
  const perms = (route.data?.['permissions'] as string[] | undefined) ?? [];
  if (perms.length === 0) return true;
  if (session.can(...perms)) return true;

  const fallback = firstAllowedAdminPath(session);
  const deniedPath = '/' + (route.routeConfig?.path ?? '').replace(/\/$/, '');
  const normalizedFallback = fallback === '/' ? '/' : fallback.replace(/\/$/, '');
  const normalizedDenied = deniedPath === '/' ? '/' : deniedPath;

  if (fallback === '/login' || normalizedFallback === normalizedDenied) {
    return router.createUrlTree(['/login']);
  }
  return router.parseUrl(fallback);
};
