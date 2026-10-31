import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { EmailInput } from './email-input';

describe('EmailInput', () => {
  let control: FormControl<string>;
  let fixture: ComponentFixture<EmailInput>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  beforeEach(async () => {
    control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    });
    fixture = TestBed.createComponent(EmailInput);
    fixture.componentRef.setInput('label', 'E-mail');
    fixture.componentRef.setInput('control', control);
    await fixture.whenStable();
  });

  it('labels the field and identifies it for the password manager', () => {
    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain('E-mail');
    expect(input().type).toBe('email');
    expect(input().getAttribute('autocomplete')).toBe('username');
  });

  it('shows no error before the field is touched', () => {
    expect(errorMessage()).toBeUndefined();
  });

  it('asks for the e-mail when the form marks the empty field as touched', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('O e-mail é obrigatório.');
  });

  it('rejects a text that is not an e-mail', async () => {
    control.setValue('operador');
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('O e-mail informado é inválido.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    control.setValue('operador@demo.local');
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });

  it('removes the error of the API when the operator edits the field', async () => {
    control.setValue('operador@demo.local');
    control.markAllAsTouched();
    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    input().value = 'outro@demo.local';
    input().dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(errorMessage()).toBeUndefined();
  });
});
