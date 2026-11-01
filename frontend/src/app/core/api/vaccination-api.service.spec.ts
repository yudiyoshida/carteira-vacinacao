import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { VaccinationCardDto } from '../models/vaccination.model';
import { VaccinationApiService } from './vaccination-api.service';

const PERSON_URL = '/api/v1/people/maria-id';
const VACCINATION_URL = `${PERSON_URL}/vaccinations/vaccination-id`;

const CARD: VaccinationCardDto = {
  person: { id: 'maria-id', name: 'Maria', cpf: '123.456.789-09' },
  vaccines: [],
};

describe('VaccinationApiService', () => {
  let service: VaccinationApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VaccinationApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('RN17: asks for the vaccination card of the person', () => {
    let card: VaccinationCardDto | undefined;

    service.findCard('maria-id').subscribe((response) => (card = response));

    const request = http.expectOne(`${PERSON_URL}/vaccination-card`);
    expect(request.request.method).toBe('GET');
    request.flush(CARD);
    expect(card).toEqual(CARD);
  });

  it('registers a vaccination and returns the id', () => {
    const body = {
      vaccineId: 'bcg-id',
      dose: { type: 'Primary' as const, number: 1 },
      appliedOn: '2023-03-10',
    };
    let id: string | undefined;

    service.register('maria-id', body).subscribe((response) => (id = response.id));

    const request = http.expectOne(`${PERSON_URL}/vaccinations`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ id: 'vaccination-id' }, { status: 201, statusText: 'Created' });
    expect(id).toBe('vaccination-id');
  });

  it('corrects the applied-on date and returns the message of the API', () => {
    let message: string | undefined;

    service
      .correctDate('maria-id', 'vaccination-id', { appliedOn: '2023-03-11' })
      .subscribe((response) => (message = response.message));

    const request = http.expectOne(VACCINATION_URL);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ appliedOn: '2023-03-11' });
    request.flush({ message: 'Data de aplicação corrigida com sucesso.' });
    expect(message).toBe('Data de aplicação corrigida com sucesso.');
  });

  it('deletes a vaccination and returns the message of the API', () => {
    let message: string | undefined;

    service
      .delete('maria-id', 'vaccination-id')
      .subscribe((response) => (message = response.message));

    const request = http.expectOne(VACCINATION_URL);
    expect(request.request.method).toBe('DELETE');
    request.flush({ message: 'Vacinação excluída com sucesso.' });
    expect(message).toBe('Vacinação excluída com sucesso.');
  });
});
