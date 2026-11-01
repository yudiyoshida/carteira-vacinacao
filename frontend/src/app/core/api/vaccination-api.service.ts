import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SuccessMessage } from '../models/success-message.model';
import {
  CorrectVaccinationDateRequest,
  RegisterVaccinationRequest,
  VaccinationCardDto,
} from '../models/vaccination.model';

@Injectable({ providedIn: 'root' })
export class VaccinationApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/people`;

  public findCard(personId: string): Observable<VaccinationCardDto> {
    return this.http.get<VaccinationCardDto>(`${this.base}/${personId}/vaccination-card`);
  }

  public register(personId: string, body: RegisterVaccinationRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/${personId}/vaccinations`, body);
  }

  public correctDate(
    personId: string,
    vaccinationId: string,
    body: CorrectVaccinationDateRequest,
  ): Observable<SuccessMessage> {
    return this.http.patch<SuccessMessage>(
      `${this.base}/${personId}/vaccinations/${vaccinationId}`,
      body,
    );
  }

  public delete(personId: string, vaccinationId: string): Observable<SuccessMessage> {
    return this.http.delete<SuccessMessage>(
      `${this.base}/${personId}/vaccinations/${vaccinationId}`,
    );
  }
}
