import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

const BASE_URL = `${environment.apiUrl}/profile`;


@Injectable({ providedIn: 'root' })
export class ProfileService {

  constructor(
    private router: Router,
    private http: HttpClient
  ) {
  }

  getProfile(): Observable<MeDto> {
    return this.http.get<MeDto>(`${BASE_URL}/me`, { withCredentials: true });
  }
}
