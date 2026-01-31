
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, } from '@angular/router';
import { catchError, map, of, Observable } from 'rxjs';
import { TokenResponse } from '../responses/token-response';
import { TokenService } from '../services/token.service';
import { UserService } from '../services/user.service';
import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree | Observable<boolean | UrlTree> => {
  const router = inject(Router);
  const tokenService = inject(TokenService);
  const userService = inject(UserService);

  const session = tokenService.getSession();
  if (session == null) {
    return router.createUrlTree(['/login']);
  }

  try {
    if (!tokenService.isLoggedIn()) {
      return userService.refreshToken().pipe(
        map((data: TokenResponse) => {
          tokenService.saveSession(data);
          return true as boolean;
        }),
        catchError(() => of(router.createUrlTree(['/login'])))
      );
    }
  } catch (err) {
    return router.createUrlTree(['/login']);
  }

  return true;
};