import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AdminSessionService } from './admin-session.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(AdminSessionService);
  const router = inject(Router);

  const slug = session.tenantSlug() || environment.defaultTenantSlug || 'casa-imperial';
  const token = session.getToken();
  let headers = req.headers.set('X-Tenant-Slug', slug);
  if (token) headers = headers.set('Authorization', `Bearer ${token}`);

  return next(req.clone({ headers })).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        const isLogin = req.url.includes('/api/admin/auth/login');
        if (!isLogin) {
          session.logout();
          void router.navigateByUrl('/login');
        }
      }
      return throwError(() => err);
    }),
  );
};
