import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { ptBR } from 'date-fns/locale/pt-BR';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { DateInput } from './date-input';

describe('DateInput', () => {
  let control: FormControl<Date | null>;
  let fixture: ComponentFixture<DateInput>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  function type(value: string): void {
    input().value = value;
    input().dispatchEvent(new Event('input'));
  }

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideDateFnsAdapter(), { provide: MAT_DATE_LOCALE, useValue: ptBR }],
    });
    control = new FormControl<Date | null>(null, Validators.required);
    fixture = TestBed.createComponent(DateInput);
    fixture.componentRef.setInput('label', 'Data de aplicação');
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errorMessages', {
      required: 'A data de aplicação é obrigatória.',
    });
    await fixture.whenStable();
  });

  it('labels the field, without the suggestions of the browser', () => {
    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain(
      'Data de aplicação',
    );
    expect(input().getAttribute('autocomplete')).toBe('off');
  });

  it('names the button of the calendar in Portuguese', () => {
    const toggle = fixture.nativeElement.querySelector('mat-datepicker-toggle button');

    expect(toggle.getAttribute('aria-label')).toBe('Abrir o calendário');
  });

  it('reads the date the operator types in dd/MM/yyyy', () => {
    type('10/03/2023');

    expect(control.value).toEqual(new Date(2023, 2, 10));
  });

  it('shows a date set from outside in dd/MM/yyyy', async () => {
    control.setValue(new Date(2023, 2, 10));
    await fixture.whenStable();

    expect(input().value).toBe('10/03/2023');
  });

  it('shows no error before the field is touched', () => {
    expect(errorMessage()).toBeUndefined();
  });

  it('shows the message the page gave for the failed validator', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('A data de aplicação é obrigatória.');
  });

  it('tells the format when the text is not a date', async () => {
    type('ontem');
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('Informe a data no formato dd/mm/aaaa.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    type('10/03/2023');
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });
});
