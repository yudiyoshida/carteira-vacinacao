import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { IPagination } from '../models/pagination.model';
import { CreatePersonRequest, FindAllPeopleParams, PersonSummaryDto } from '../models/person.model';
import { SuccessMessage } from '../models/success-message.model';

@Injectable({ providedIn: 'root' })
export class PersonApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/people`;

  public findAll({
    search,
    page,
    size,
  }: FindAllPeopleParams): Observable<IPagination<PersonSummaryDto>> {
    const pageParams = new HttpParams().set('page', page).set('size', size);
    const params = search ? pageParams.set('search', search) : pageParams;
    return this.http.get<IPagination<PersonSummaryDto>>(this.base, { params });
  }

  public create(body: CreatePersonRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, body);
  }

  public delete(id: string): Observable<SuccessMessage> {
    return this.http.delete<SuccessMessage>(`${this.base}/${id}`);
  }
}
