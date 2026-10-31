import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { IPagination } from '../models/pagination.model';
import { PersonSummaryDto } from '../models/person.model';
import { SuccessMessage } from '../models/success-message.model';
import { PersonApiService } from './person-api.service';

const PEOPLE_URL = '/api/v1/people';

const MARIA: PersonSummaryDto = { id: 'maria-id', name: 'Maria', cpf: '***.456.789-**' };
const FIRST_PAGE: IPagination<PersonSummaryDto> = {
  currentPage: 1,
  itemsPerPage: 20,
  totalItems: 1,
  totalPages: 1,
  data: [MARIA],
};

describe('PersonApiService', () => {
  let service: PersonApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PersonApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('RN19: asks for the page and the size, without an empty search', () => {
    let page: IPagination<PersonSummaryDto> | undefined;

    service.findAll({ search: '', page: 2, size: 50 }).subscribe((response) => (page = response));

    const request = http.expectOne((r) => r.url === PEOPLE_URL);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys()).toEqual(['page', 'size']);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('50');
    request.flush(FIRST_PAGE);
    expect(page).toEqual(FIRST_PAGE);
  });

  it('RN19: sends the search term as a query parameter', () => {
    service.findAll({ search: 'maria', page: 1, size: 20 }).subscribe();

    const request = http.expectOne((r) => r.url === PEOPLE_URL);
    expect(request.request.params.get('search')).toBe('maria');
    request.flush({ ...FIRST_PAGE, data: [] });
  });

  it('registers a person and returns the id', () => {
    const body = { name: 'Maria', cpf: '123.456.789-09' };
    let id: string | undefined;

    service.create(body).subscribe((response) => (id = response.id));

    const request = http.expectOne(PEOPLE_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ id: 'maria-id' }, { status: 201, statusText: 'Created' });
    expect(id).toBe('maria-id');
  });

  it('removes a person and returns the message of the API', () => {
    const response: SuccessMessage = { message: 'Pessoa removida com sucesso.' };
    let message: string | undefined;

    service.delete('maria-id').subscribe((result) => (message = result.message));

    const request = http.expectOne(`${PEOPLE_URL}/maria-id`);
    expect(request.request.method).toBe('DELETE');
    request.flush(response);
    expect(message).toBe('Pessoa removida com sucesso.');
  });
});
