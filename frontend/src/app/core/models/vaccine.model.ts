export interface VaccineDto {
  id: string;
  name: string;
  primaryDoses: number;
  boosterDoses: number;
}

export interface CreateVaccineRequest {
  name: string;
  primaryDoses: number;
  boosterDoses: number;
}
