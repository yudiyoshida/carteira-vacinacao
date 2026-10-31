import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';

@Injectable()
export class PaginatorIntl extends MatPaginatorIntl {
  public override itemsPerPageLabel = 'Itens por página:';
  public override nextPageLabel = 'Próxima página';
  public override previousPageLabel = 'Página anterior';
  public override firstPageLabel = 'Primeira página';
  public override lastPageLabel = 'Última página';

  public override getRangeLabel = (page: number, pageSize: number, length: number): string => {
    if (length === 0) {
      return '0 de 0';
    }

    const firstItem = page * pageSize + 1;
    const lastItem = Math.min(firstItem + pageSize - 1, length);
    return `${firstItem} – ${lastItem} de ${length}`;
  };
}
