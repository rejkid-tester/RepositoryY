import { UserService } from '../services/user.service';
import { inject } from '@angular/core';
import { TokenService } from '../services/token.service';
//import { AccountService } from '../_services';


export function appInitializer() {
  const accountService = inject(UserService);
  const tokenService = inject(TokenService);

  return new Promise<void>((resolve) => {
    // attempt to refresh token on app start up to auto authenticate
    accountService.refreshToken()
      .subscribe({
        next: (value: any) => {
          console.log("appInitializer successful: " + value.firstName, value.lastName, value.email);
          try { tokenService.saveSession(value); } catch {}
          resolve();
        },
        error: (error: string) => {
          console.log("Error in appInitializer");
          resolve();
        }
      });
  }).then(() => {
    console.log("appInitializer in then");
  }).catch(() => {
    console.log("Error in appInitializer in catch");
  });
}

/* export function appInitializer(accountService: AccountService) {
      // attempt to refresh token on app start up to auto authenticate
      return () => accountService.refreshToken();
} */