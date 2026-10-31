import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { IPagination } from '../models/pagination.model';
import { CreateVaccineRequest, VaccineDto } from '../models/vaccine.model';

@Injectable({ providedIn: 'root' })
export class VaccineApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/vaccines`;

  // Without page and size, the API returns every vaccine on a single page (RN20).
  public findAll(search = ''): Observable<IPagination<VaccineDto>> {
    const params = search ? new HttpParams().set('search', search) : undefined;
    return this.http.get<IPagination<VaccineDto>>(this.base, { params });
  }

  public create(body: CreateVaccineRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, body);
  }
}
