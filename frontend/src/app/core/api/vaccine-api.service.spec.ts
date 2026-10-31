import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { IPagination } from '../models/pagination.model';
import { VaccineDto } from '../models/vaccine.model';
import { VaccineApiService } from './vaccine-api.service';

const VACCINES_URL = '/api/v1/vaccines';

const BCG: VaccineDto = { id: 'bcg-id', name: 'BCG', primaryDoses: 1, boosterDoses: 0 };
const ALL_VACCINES: IPagination<VaccineDto> = {
  currentPage: 1,
  itemsPerPage: 1,
  totalItems: 1,
  totalPages: 1,
  data: [BCG],
};

describe('VaccineApiService', () => {
  let service: VaccineApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VaccineApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('RN20: asks for every vaccine, without pagination parameters', () => {
    let page: IPagination<VaccineDto> | undefined;

    service.findAll().subscribe((response) => (page = response));

    const request = http.expectOne((r) => r.url === VACCINES_URL);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys()).toEqual([]);
    request.flush(ALL_VACCINES);
    expect(page).toEqual(ALL_VACCINES);
  });

  it('RN20: sends the search term as a query parameter', () => {
    service.findAll('hepatite').subscribe();

    const request = http.expectOne((r) => r.url === VACCINES_URL);
    expect(request.request.params.keys()).toEqual(['search']);
    expect(request.request.params.get('search')).toBe('hepatite');
    request.flush({ ...ALL_VACCINES, data: [] });
  });

  it('registers a vaccine with the dose scheme and returns its id', () => {
    const body = { name: 'BCG', primaryDoses: 1, boosterDoses: 0 };
    let id: string | undefined;

    service.create(body).subscribe((response) => (id = response.id));

    const request = http.expectOne(VACCINES_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ id: 'bcg-id' }, { status: 201, statusText: 'Created' });
    expect(id).toBe('bcg-id');
  });
});
