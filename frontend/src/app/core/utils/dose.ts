import { DoseDto, DoseSchemeDto, DoseType } from '../models/vaccination.model';

// Every dose of a scheme, in the order of the sequence: primary doses, then boosters (RN10).
export function orderedDoses({ primaryDoses, boosterDoses }: DoseSchemeDto): DoseDto[] {
  return [...dosesOf('Primary', primaryDoses), ...dosesOf('Booster', boosterDoses)];
}

export function sameDose(first: DoseDto | null, second: DoseDto | null): boolean {
  return first?.type === second?.type && first?.number === second?.number;
}

function dosesOf(type: DoseType, total: number): DoseDto[] {
  return Array.from({ length: total }, (_, index) => ({ type, number: index + 1 }));
}
