import { DoseDto, DoseSchemeDto } from '../../core/models/vaccination.model';
import { doseLabel, DoseLabelPipe } from './dose-label.pipe';

const SCHEME: DoseSchemeDto = { primaryDoses: 10, boosterDoses: 5 };

describe('DoseLabelPipe', () => {
  const pipe = new DoseLabelPipe();

  it('RN18: labels the dose of a 1 + 0 scheme as "Dose única"', () => {
    expect(
      pipe.transform({ type: 'Primary', number: 1 }, { primaryDoses: 1, boosterDoses: 0 }),
    ).toBe('Dose única');
  });

  it.each<[string, DoseSchemeDto]>([
    ['a booster', { primaryDoses: 1, boosterDoses: 1 }],
    ['a second primary dose', { primaryDoses: 2, boosterDoses: 0 }],
  ])('RN18: keeps the number when the scheme also has %s', (_, scheme) => {
    expect(pipe.transform({ type: 'Primary', number: 1 }, scheme)).toBe('1ª dose');
  });

  it.each<[DoseDto, string]>([
    [{ type: 'Primary', number: 1 }, '1ª dose'],
    [{ type: 'Primary', number: 2 }, '2ª dose'],
    [{ type: 'Primary', number: 10 }, '10ª dose'],
  ])('RN18: labels the primary dose %o as "%s"', (dose, label) => {
    expect(pipe.transform(dose, SCHEME)).toBe(label);
  });

  it.each<[DoseDto, string]>([
    [{ type: 'Booster', number: 1 }, '1º reforço'],
    [{ type: 'Booster', number: 5 }, '5º reforço'],
  ])('RN18: labels the booster %o as "%s"', (dose, label) => {
    expect(pipe.transform(dose, SCHEME)).toBe(label);
  });

  it('RN18: gives TypeScript code the same label as the HTML', () => {
    const dose: DoseDto = { type: 'Booster', number: 1 };

    expect(doseLabel(dose, SCHEME)).toBe(pipe.transform(dose, SCHEME));
  });
});
