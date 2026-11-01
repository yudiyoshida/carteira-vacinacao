import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { MatSelectHarness } from '@angular/material/select/testing';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { SelectInput, SelectOption } from './select-input';

const OPTIONS: SelectOption<string>[] = [
  { value: 'bcg-id', label: 'BCG' },
  { value: 'dtp-id', label: 'DTP' },
];

describe('SelectInput', () => {
  let control: FormControl<string>;
  let fixture: ComponentFixture<SelectInput<string>>;
  let loader: HarnessLoader;

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  beforeEach(async () => {
    control = new FormControl('', { nonNullable: true, validators: Validators.required });
    fixture = TestBed.createComponent<SelectInput<string>>(SelectInput);
    fixture.componentRef.setInput('label', 'Vacina');
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('options', OPTIONS);
    fixture.componentRef.setInput('errorMessages', { required: 'A vacina é obrigatória.' });
    loader = TestbedHarnessEnvironment.loader(fixture);
    await fixture.whenStable();
  });

  it('labels the field and lists the options', async () => {
    const select = await loader.getHarness(MatSelectHarness);

    await select.open();
    const options = await select.getOptions();

    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain('Vacina');
    expect(await Promise.all(options.map((option) => option.getText()))).toEqual(['BCG', 'DTP']);
  });

  it('updates the control when the operator chooses an option', async () => {
    const select = await loader.getHarness(MatSelectHarness);

    await select.open();
    await select.clickOptions({ text: 'DTP' });

    expect(control.value).toBe('dtp-id');
    expect(await select.getValueText()).toBe('DTP');
  });

  it('shows the option of a value set from outside', async () => {
    control.setValue('bcg-id');
    const select = await loader.getHarness(MatSelectHarness);

    expect(await select.getValueText()).toBe('BCG');
  });

  it('finds an object value by the comparison the page gives', async () => {
    const dose = new FormControl({ number: 2 }, { nonNullable: true });
    fixture.componentRef.setInput('control', dose);
    fixture.componentRef.setInput('options', [
      { value: { number: 1 }, label: '1ª dose' },
      { value: { number: 2 }, label: '2ª dose' },
    ]);
    fixture.componentRef.setInput(
      'compareWith',
      (first: { number: number }, second: { number: number }) => first.number === second.number,
    );
    await fixture.whenStable();

    const select = await loader.getHarness(MatSelectHarness);

    expect(await select.getValueText()).toBe('2ª dose');
  });

  it('shows no error before the field is touched', () => {
    expect(errorMessage()).toBeUndefined();
  });

  it('shows the message the page gave for the failed validator', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('A vacina é obrigatória.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    control.setValue('bcg-id');
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });
});
