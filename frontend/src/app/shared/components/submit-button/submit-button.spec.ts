import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SubmitButton } from './submit-button';

describe('SubmitButton', () => {
  let fixture: ComponentFixture<SubmitButton>;

  function button(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button');
  }

  function indicator(): Element | null {
    return button().querySelector('[role="progressbar"]');
  }

  beforeEach(async () => {
    fixture = TestBed.createComponent(SubmitButton);
    fixture.componentRef.setInput('label', 'Entrar');
    await fixture.whenStable();
  });

  it('submits the form with the label', () => {
    expect(button().type).toBe('submit');
    expect(button().textContent).toContain('Entrar');
    expect(button().disabled).toBe(false);
    expect(indicator()).toBeNull();
  });

  it('blocks a second submission and shows the indicator while loading', async () => {
    fixture.componentRef.setInput('loading', true);
    await fixture.whenStable();

    expect(button().disabled).toBe(true);
    expect(button().getAttribute('aria-busy')).toBe('true');
    expect(button().textContent).toContain('Entrar');
    expect(indicator()).not.toBeNull();
  });

  it('releases the button when the loading ends', async () => {
    fixture.componentRef.setInput('loading', true);
    await fixture.whenStable();

    fixture.componentRef.setInput('loading', false);
    await fixture.whenStable();

    expect(button().disabled).toBe(false);
    expect(indicator()).toBeNull();
  });
});
