import {
  AfterContentInit,
  ChangeDetectionStrategy,
  Component,
  contentChildren,
  input,
  output,
  viewChild,
} from '@angular/core';
import { MatPaginator, MatPaginatorIntl, PageEvent } from '@angular/material/paginator';
import { MatColumnDef, MatTable, MatTableModule } from '@angular/material/table';
import { PaginatorIntl } from './paginator-intl';

@Component({
  selector: 'app-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
  imports: [MatPaginator, MatTableModule],
  providers: [{ provide: MatPaginatorIntl, useClass: PaginatorIntl }],
})
export class DataTable<T> implements AfterContentInit {
  public readonly data = input.required<readonly T[]>();
  public readonly columns = input.required<string[]>();
  public readonly ariaLabel = input.required<string>();
  // Without a total, the list has no pages and the paginator stays out.
  public readonly totalItems = input<number | null>(null);
  public readonly pageIndex = input(0);
  public readonly pageSize = input(20);

  public readonly pageChange = output<PageEvent>();

  // The API accepts at most 100 items per page (TechSpec §7.1).
  protected readonly pageSizeOptions = [10, 20, 50, 100];

  private readonly columnDefs = contentChildren(MatColumnDef);
  private readonly table = viewChild.required(MatTable);

  // The columns live in the template of the page, where the table does not find them on its own.
  public ngAfterContentInit(): void {
    this.columnDefs().forEach((columnDef) => this.table().addColumnDef(columnDef));
  }
}
