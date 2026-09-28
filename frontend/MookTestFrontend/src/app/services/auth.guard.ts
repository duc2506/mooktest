import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
export const authGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.token) return router.createUrlTree(['/login']);
  return auth.user()?.role === route.data['role'] ? true : router.parseUrl(auth.landingPage);
};
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.token ? inject(Router).parseUrl(auth.landingPage) : true;
};

