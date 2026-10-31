import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { COMPLETE_CPF_PATTERN } from '../../../../core/utils/cpf-mask';
import { CpfInput } from './cpf-input';

describe('CpfInput', () => {
  let control: FormControl<string>;
  let fixture: ComponentFixture<CpfInput>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  function type(value: string, caret = value.length): void {
    input().value = value;
    input().setSelectionRange(caret, caret);
    input().dispatchEvent(new Event('input'));
  }

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  beforeEach(async () => {
    control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(COMPLETE_CPF_PATTERN)],
    });
    fixture = TestBed.createComponent(CpfInput);
    fixture.componentRef.setInput('label', 'CPF');
    fixture.componentRef.setInput('control', control);
    await fixture.whenStable();
  });

  it('labels the field and opens the numeric keyboard, without browser suggestions', () => {
    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain('CPF');
    expect(input().type).toBe('text');
    expect(input().getAttribute('inputmode')).toBe('numeric');
    expect(input().getAttribute('autocomplete')).toBe('off');
  });

  it('RN02: applies the mask to the field and to the control while the operator types', () => {
    type('1234');

    expect(input().value).toBe('123.4');
    expect(control.value).toBe('123.4');
  });

  it('RN02: accepts a pasted CPF with spaces around it', () => {
    type(' 123.456.789-09 ');

    expect(input().value).toBe('123.456.789-09');
    expect(control.value).toBe('123.456.789-09');
  });

  it('keeps the caret on the digit being edited in the middle of the value', () => {
    type('123.456.789-09');

    type('1293.456.789-09', 3);

    expect(input().value).toBe('129.345.678-90');
    expect(input().selectionStart).toBe(3);
    expect(input().selectionEnd).toBe(3);
  });

  it('shows no error before the field is touched', () => {
    expect(errorMessage()).toBeUndefined();
  });

  it('RN02: asks for the CPF when the form marks the empty field as touched', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('O CPF é obrigatório.');
  });

  it('RN02: refuses an incomplete CPF', async () => {
    type('123456');
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('O CPF informado é inválido.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    type('123.456.789-00');
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });

  it('removes the error of the API when the operator edits the field', async () => {
    type('123.456.789-00');
    control.markAllAsTouched();
    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    type('123.456.789-09');
    await fixture.whenStable();

    expect(errorMessage()).toBeUndefined();
  });
});
