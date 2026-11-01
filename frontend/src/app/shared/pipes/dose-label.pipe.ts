import { Pipe, PipeTransform } from '@angular/core';
import { DoseDto, DoseSchemeDto } from '../../core/models/vaccination.model';

// Also a function: a message built in TypeScript needs the label, and a pipe only runs in the HTML.
export function doseLabel(
  { type, number }: DoseDto,
  { primaryDoses, boosterDoses }: DoseSchemeDto,
): string {
  if (primaryDoses === 1 && boosterDoses === 0) {
    return 'Dose única';
  }

  return type === 'Primary' ? `${number}ª dose` : `${number}º reforço`;
}

@Pipe({ name: 'doseLabel' })
export class DoseLabelPipe implements PipeTransform {
  public transform(dose: DoseDto, scheme: DoseSchemeDto): string {
    return doseLabel(dose, scheme);
  }
}
