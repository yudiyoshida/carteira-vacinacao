import {
  AfterContentInit,
  ChangeDetectionStrategy,
  Component,
  contentChildren,
  input,
  viewChild,
} from '@angular/core';
import { MatColumnDef, MatTable, MatTableModule } from '@angular/material/table';

@Component({
  selector: 'app-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
  imports: [MatTableModule],
})
export class DataTable<T> implements AfterContentInit {
  public readonly data = input.required<readonly T[]>();
  public readonly columns = input.required<string[]>();
  public readonly ariaLabel = input.required<string>();

  private readonly columnDefs = contentChildren(MatColumnDef);
  private readonly table = viewChild.required(MatTable);

  // The columns live in the template of the page, where the table does not find them on its own.
  public ngAfterContentInit(): void {
    this.columnDefs().forEach((columnDef) => this.table().addColumnDef(columnDef));
  }
}
