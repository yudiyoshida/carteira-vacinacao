export interface PersonSummaryDto {
  id: string;
  name: string;
  // Masked by the API: ***.456.789-** (RN26).
  cpf: string;
}

export interface PersonDto {
  id: string;
  name: string;
  // Formatted by the API: 123.456.789-09 (RN26).
  cpf: string;
}

export interface CreatePersonRequest {
  name: string;
  cpf: string;
}

export interface FindAllPeopleParams {
  search: string;
  page: number;
  size: number;
}
