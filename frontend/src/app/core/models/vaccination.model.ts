import { PersonDto } from './person.model';

export type DoseType = 'Primary' | 'Booster';

export interface DoseDto {
  type: DoseType;
  number: number;
}

export interface DoseSchemeDto {
  primaryDoses: number;
  boosterDoses: number;
}

export interface VaccinationCardDoseDto {
  vaccinationId: string;
  dose: DoseDto;
  // In yyyy-MM-dd.
  appliedOn: string;
}

export interface VaccinationCardProgressDto {
  primaryApplied: number;
  primaryTotal: number;
  primarySeriesComplete: boolean;
  boostersApplied: number;
  boostersTotal: number;
}

export interface VaccinationCardEntryDto {
  vaccineId: string;
  vaccineName: string;
  scheme: DoseSchemeDto;
  // In the order of the sequence (RN17).
  doses: VaccinationCardDoseDto[];
  progress: VaccinationCardProgressDto;
  // Null when the scheme is complete.
  nextExpectedDose: DoseDto | null;
}

export interface VaccinationCardDto {
  person: PersonDto;
  // Ordered by name, only the vaccines with a vaccination (RN17).
  vaccines: VaccinationCardEntryDto[];
}

export interface RegisterVaccinationRequest {
  vaccineId: string;
  dose: DoseDto;
  appliedOn: string;
}

export interface CorrectVaccinationDateRequest {
  appliedOn: string;
}
