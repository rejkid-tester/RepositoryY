import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AccountModalService {
  private _loginVisible = new BehaviorSubject<boolean>(true);
  loginVisible$ = this._loginVisible.asObservable();

  showLogin(): void { this._loginVisible.next(true); }
  hideLogin(): void { this._loginVisible.next(false); }
  toggleLogin(): void { this._loginVisible.next(!this._loginVisible.getValue()); }
}
