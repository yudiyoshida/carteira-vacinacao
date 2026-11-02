const CPF_DIGITS = 11;

// The whole CPF, as the typing mask leaves it. The check digits stay with the API (RN02).
export const COMPLETE_CPF_PATTERN = /^\d{3}\.\d{3}\.\d{3}-\d{2}$/;

// The typing mask of the CPF field: 123.456.789-09.
// Not the masked CPF of the lists (RN26), which comes from the API.
export function applyCpfMask(value: string): string {
  const digits = onlyDigits(value).slice(0, CPF_DIGITS);
  const groups = [digits.slice(0, 3), digits.slice(3, 6), digits.slice(6, 9)];
  const checkDigits = digits.slice(9);
  const masked = groups.filter(Boolean).join('.');
  return checkDigits ? `${masked}-${checkDigits}` : masked;
}

// Where the caret goes after the mask: right after the same digit it followed before.
export function caretAfterCpfMask(value: string, caret: number): number {
  const digitsBeforeCaret = onlyDigits(value.slice(0, caret)).length;
  const masked = applyCpfMask(value);
  const untilCaret = new RegExp(`^(?:\\D*\\d){${digitsBeforeCaret}}`).exec(masked);
  return untilCaret ? untilCaret[0].length : masked.length;
}

function onlyDigits(value: string): string {
  return value.replace(/\D/g, '');
}
