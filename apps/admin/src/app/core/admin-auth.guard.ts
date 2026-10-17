import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminSessionService } from './admin-session.service';

export const adminAuthGuard: CanActivateFn = () => {
  const session = inject(AdminSessionService);
  const router = inject(Router);
  if (session.isLoggedIn()) return true;
  session.logout();
  return router.createUrlTree(['/login']);
};
