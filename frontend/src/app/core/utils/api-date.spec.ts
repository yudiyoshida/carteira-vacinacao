import { formatApiDate, fromApiDate, toApiDate } from './api-date';

describe('api-date', () => {
  it('RN12: sends a civil date, without time', () => {
    expect(toApiDate(new Date(2023, 2, 10, 23, 30))).toBe('2023-03-10');
  });

  it('reads the date of the API on the same local day', () => {
    const date = fromApiDate('2023-03-10');

    expect(date.getFullYear()).toBe(2023);
    expect(date.getMonth()).toBe(2);
    expect(date.getDate()).toBe(10);
  });

  it('shows the date of the API in dd/MM/yyyy', () => {
    expect(formatApiDate('2023-03-10')).toBe('10/03/2023');
  });
});
