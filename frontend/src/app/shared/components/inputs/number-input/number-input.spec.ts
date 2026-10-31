import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { NumberInput } from './number-input';

const ERROR_MESSAGES = {
  required: 'A quantidade é obrigatória.',
  min: 'A quantidade é baixa.',
  max: 'A quantidade é alta.',
};

describe('NumberInput', () => {
  let control: FormControl<number>;
  let fixture: ComponentFixture<NumberInput>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  function type(value: string): void {
    input().value = value;
    input().dispatchEvent(new Event('input'));
  }

  beforeEach(async () => {
    control = new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(10)],
    });
    fixture = TestBed.createComponent(NumberInput);
    fixture.componentRef.setInput('label', 'Doses');
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errorMessages', ERROR_MESSAGES);
    await fixture.whenStable();
  });

  it('labels the field and shows the value of the control', () => {
    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain('Doses');
    expect(input().type).toBe('number');
    expect(input().value).toBe('1');
  });

  it('gives the limits to the browser, without adding validators to the control', async () => {
    const free = new FormControl(99, { nonNullable: true });
    fixture.componentRef.setInput('control', free);
    fixture.componentRef.setInput('min', 1);
    fixture.componentRef.setInput('max', 10);
    await fixture.whenStable();

    expect(input().getAttribute('min')).toBe('1');
    expect(input().getAttribute('max')).toBe('10');
    expect(free.valid).toBe(true);
  });

  it('updates the control with a number when the operator types', () => {
    type('3');

    expect(control.value).toBe(3);
  });

  it('shows no error before the field is touched', () => {
    expect(errorMessage()).toBeUndefined();
  });

  it('shows the message the page gave for each failed validator', async () => {
    control.markAllAsTouched();

    type('');
    await fixture.whenStable();
    expect(errorMessage()).toBe('A quantidade é obrigatória.');

    type('0');
    await fixture.whenStable();
    expect(errorMessage()).toBe('A quantidade é baixa.');

    type('11');
    await fixture.whenStable();
    expect(errorMessage()).toBe('A quantidade é alta.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });
});
