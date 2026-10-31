import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccessTokenDto, LoginRequest } from '../models/auth.model';
import { SuccessMessage } from '../models/success-message.model';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  public login(body: LoginRequest): Observable<AccessTokenDto> {
    return this.http.post<AccessTokenDto>(`${this.base}/login`, body);
  }

  public refresh(): Observable<AccessTokenDto> {
    return this.http.post<AccessTokenDto>(`${this.base}/refresh`, null);
  }

  public logout(): Observable<SuccessMessage> {
    return this.http.post<SuccessMessage>(`${this.base}/logout`, null);
  }
}
