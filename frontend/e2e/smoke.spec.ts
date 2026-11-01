import { expect, test } from '@playwright/test';

// The demo data of the seed. The account is a public credential, not a secret (TechSpec §6.4).
const DEMO_EMAIL = 'operador@demo.local';
const DEMO_PASSWORD = 'Demo@2026!';
const DEMO_PERSON = 'Maria Demonstração';

// After the 2nd dose of the seed (RN13) and not in the future (RN12), without the real clock.
const APPLIED_ON = '10/07/2023';

test('registers the next expected dose on the card of the demo person', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('E-mail').fill(DEMO_EMAIL);
  await page.getByLabel('Senha').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();

  await expect(page.getByRole('heading', { name: 'Pessoas' })).toBeVisible();
  await expect(page.getByRole('row', { name: DEMO_PERSON })).toContainText('***.456.789-**');
  await page.getByRole('link', { name: `Abrir o cartão de ${DEMO_PERSON}` }).click();

  await expect(page.getByRole('heading', { name: DEMO_PERSON })).toBeVisible();
  await expect(page.getByText('CPF 123.456.789-09')).toBeVisible();

  const hepatitisB = page.getByRole('region', { name: 'Hepatite B' });
  const nextDose = hepatitisB.getByRole('button', { name: 'Registrar 3ª dose de Hepatite B' });
  const appliedDose = hepatitisB.getByRole('button', {
    name: `3ª dose de Hepatite B, aplicada em ${APPLIED_ON}`,
  });

  await nextDose.click();

  const form = page.getByRole('dialog', { name: 'Registrar dose' });
  await expect(form.getByRole('combobox', { name: 'Vacina' })).toHaveText('Hepatite B');
  await expect(form.getByRole('combobox', { name: 'Dose' })).toHaveText('3ª dose');
  await form.getByRole('textbox', { name: 'Data de aplicação' }).fill(APPLIED_ON);
  await form.getByRole('button', { name: 'Registrar', exact: true }).click();

  await expect(appliedDose).toBeVisible();
  await expect(hepatitisB).toContainText('Série completa');

  // Back to the state of the seed, so the next run finds the same next dose on the same volume.
  await appliedDose.click();
  await page.getByRole('menuitem', { name: 'Excluir dose' }).click();
  await page
    .getByRole('alertdialog', { name: 'Excluir vacinação' })
    .getByRole('button', { name: 'Excluir' })
    .click();

  await expect(nextDose).toBeVisible();
});
