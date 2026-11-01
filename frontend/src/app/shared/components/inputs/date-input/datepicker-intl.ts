import { Injectable } from '@angular/core';
import { MatDatepickerIntl } from '@angular/material/datepicker';

@Injectable()
export class DatepickerIntl extends MatDatepickerIntl {
  public override calendarLabel = 'Calendário';
  public override openCalendarLabel = 'Abrir o calendário';
  public override closeCalendarLabel = 'Fechar o calendário';
  public override prevMonthLabel = 'Mês anterior';
  public override nextMonthLabel = 'Próximo mês';
  public override prevYearLabel = 'Ano anterior';
  public override nextYearLabel = 'Próximo ano';
  public override prevMultiYearLabel = '24 anos anteriores';
  public override nextMultiYearLabel = 'Próximos 24 anos';
  public override switchToMonthViewLabel = 'Escolher a data';
  public override switchToMultiYearViewLabel = 'Escolher o mês e o ano';

  public override formatYearRangeLabel(start: string, end: string): string {
    return `${start} a ${end}`;
  }
}
