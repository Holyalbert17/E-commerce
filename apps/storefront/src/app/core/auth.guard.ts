import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { isLoggedIn } from './api.service';
import { AuthModalService } from './auth-modal.service';
import { SessionService } from './session.service';

/** Rutas de cuenta: exige sesión; si no, abre login y manda al catálogo. */
export const authGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const auth = inject(AuthModalService);
  const router = inject(Router);

  if (session.loggedIn() || isLoggedIn()) return true;

  auth.showLogin();
  return router.createUrlTree(['/catalogo']);
};
