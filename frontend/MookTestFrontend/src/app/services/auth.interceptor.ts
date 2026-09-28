import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token;
  const protectedApi = request.url.startsWith('/api/') && !['/api/auth/login', '/api/auth/register'].includes(request.url);
  const sent = protectedApi && token
    ? request.clone({ setHeaders: { Authorization: 'Bearer ' + token } }) : request;
  return next(sent).pipe(catchError((error: HttpErrorResponse) => {
    if (protectedApi && error.status === 401) {
      auth.logout();
      void router.navigate(['/login']);
    }
    return throwError(() => error);
  }));
};


