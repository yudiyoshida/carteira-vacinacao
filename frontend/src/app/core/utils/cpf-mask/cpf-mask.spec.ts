import { applyCpfMask, caretAfterCpfMask, COMPLETE_CPF_PATTERN } from './cpf-mask';

describe('applyCpfMask', () => {
  it.each([
    ['', ''],
    ['1', '1'],
    ['123', '123'],
    ['1234', '123.4'],
    ['123456', '123.456'],
    ['1234567', '123.456.7'],
    ['123456789', '123.456.789'],
    ['1234567890', '123.456.789-0'],
    ['12345678909', '123.456.789-09'],
  ])('RN02: shows the digits of "%s" as "%s"', (typed, masked) => {
    expect(applyCpfMask(typed)).toBe(masked);
  });

  it('RN02: keeps a CPF that already has the mask', () => {
    expect(applyCpfMask('123.456.789-09')).toBe('123.456.789-09');
  });

  it('drops every character that is not a digit', () => {
    expect(applyCpfMask(' 123a456.789/09 ')).toBe('123.456.789-09');
  });

  it('RN02: stops at the 11 digits of a CPF', () => {
    expect(applyCpfMask('123456789091')).toBe('123.456.789-09');
  });
});

describe('caretAfterCpfMask', () => {
  it('moves past a separator that the mask adds', () => {
    expect(caretAfterCpfMask('1234', 4)).toBe(5);
  });

  it('stays after the digit typed in the middle of the value', () => {
    expect(caretAfterCpfMask('1239.456.789-09', 4)).toBe(5);
  });

  it('stays before the separator after a removed digit', () => {
    expect(caretAfterCpfMask('123.56.789-09', 4)).toBe(3);
  });

  it('stays before a separator removed with backspace', () => {
    expect(caretAfterCpfMask('123456.789-09', 3)).toBe(3);
  });

  it('does not count a character that the mask drops', () => {
    expect(caretAfterCpfMask('123.4x56.789-09', 6)).toBe(5);
  });

  it('stays at the start of the value', () => {
    expect(caretAfterCpfMask('123.456.789-09', 0)).toBe(0);
  });

  it('stays at the end when a digit past the 11th is typed', () => {
    expect(caretAfterCpfMask('123.456.789-091', 15)).toBe(14);
  });
});

describe('COMPLETE_CPF_PATTERN', () => {
  it('RN02: accepts the 11 digits with the mask', () => {
    expect(COMPLETE_CPF_PATTERN.test('123.456.789-09')).toBe(true);
  });

  it.each(['123.456.789-0', '12345678909', '123.456.789-0a'])('RN02: refuses "%s"', (value) => {
    expect(COMPLETE_CPF_PATTERN.test(value)).toBe(false);
  });
});
