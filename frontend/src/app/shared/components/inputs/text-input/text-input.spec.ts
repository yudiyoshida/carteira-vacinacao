import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { API_ERROR_KEY } from '../../../../core/utils/api-error';
import { TextInput } from './text-input';

const ERROR_MESSAGES = {
  required: 'O nome é obrigatório.',
  minlength: 'O nome é curto.',
};

describe('TextInput', () => {
  let control: FormControl<string>;
  let fixture: ComponentFixture<TextInput>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  function errorMessage(): string | undefined {
    return fixture.nativeElement.querySelector('mat-error')?.textContent?.trim();
  }

  beforeEach(async () => {
    control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2)],
    });
    fixture = TestBed.createComponent(TextInput);
    fixture.componentRef.setInput('label', 'Nome');
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errorMessages', ERROR_MESSAGES);
    await fixture.whenStable();
  });

  it('labels the field as text, without the suggestions of the browser', () => {
    expect(fixture.nativeElement.querySelector('mat-label')?.textContent).toContain('Nome');
    expect(input().type).toBe('text');
    expect(input().getAttribute('autocomplete')).toBe('off');
  });

  it('becomes a search field when the page asks for it', async () => {
    fixture.componentRef.setInput('type', 'search');
    await fixture.whenStable();

    expect(input().type).toBe('search');
  });

  it('limits the typing only when the page sets a maximum length', async () => {
    expect(input().hasAttribute('maxlength')).toBe(false);

    fixture.componentRef.setInput('maxLength', 100);
    await fixture.whenStable();

    expect(input().getAttribute('maxlength')).toBe('100');
  });

  it('updates the control when the operator types', () => {
    input().value = 'BCG';
    input().dispatchEvent(new Event('input'));

    expect(control.value).toBe('BCG');
  });

  it('shows no error before the field is touched', () => {
    expect(errorMessage()).toBeUndefined();
  });

  it('shows the message the page gave for the failed validator', async () => {
    control.markAllAsTouched();
    await fixture.whenStable();

    expect(errorMessage()).toBe('O nome é obrigatório.');

    input().value = 'B';
    input().dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(errorMessage()).toBe('O nome é curto.');
  });

  it('shows the error of the API, which reaches the field from outside', async () => {
    control.setValue('BCG');
    control.markAllAsTouched();
    await fixture.whenStable();

    control.setErrors({ [API_ERROR_KEY]: 'Mensagem enviada pela API.' });
    await fixture.whenStable();

    expect(errorMessage()).toBe('Mensagem enviada pela API.');
  });
});
