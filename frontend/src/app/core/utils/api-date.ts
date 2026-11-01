import { format, parseISO } from 'date-fns';

const API_DATE_FORMAT = 'yyyy-MM-dd';
const SCREEN_DATE_FORMAT = 'dd/MM/yyyy';

export function toApiDate(date: Date): string {
  return format(date, API_DATE_FORMAT);
}

// parseISO reads a date without time as local. new Date() reads it as UTC, one day earlier in Brasília.
export function fromApiDate(value: string): Date {
  return parseISO(value);
}

export function formatApiDate(value: string): string {
  return format(fromApiDate(value), SCREEN_DATE_FORMAT);
}
