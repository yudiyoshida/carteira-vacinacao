import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { PasswordInput } from './password-input';

describe('PasswordInput', () => {
  let control: FormControl<string>;
  let fixture: ComponentFixture<PasswordInput>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  beforeEach(async () => {
    control = new FormControl('', { nonNullable: true, validators: [Validators.required] });
    fixture = TestBed.createComponent(PasswordInput);
    fixture.componentRef.setInput('label', 'Senha');
    fixture.componentRef.setInput('control', control);
    await fixture.whenStable();
  });

  it('labels the field, hides the text and identifies it for the password manager', () => {
    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain('Senha');
    expect(input().type).toBe('password');
    expect(input().getAttribute('autocomplete')).toBe('current-password');
  });

  it('asks for the password when the form marks the empty field as touched', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('A senha é obrigatória.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    control.setValue('senha-do-teste');
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });
});
