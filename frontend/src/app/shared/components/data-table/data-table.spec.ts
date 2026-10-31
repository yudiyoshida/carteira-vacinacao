import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatTableModule } from '@angular/material/table';
import { DataTable } from './data-table';

interface Row {
  name: string;
  doses: number;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, DataTable],
  template: `
    <app-data-table ariaLabel="Vacinas" [data]="rows()" [columns]="columns">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>Nome</th>
        <td mat-cell *matCellDef="let row">{{ row.name }}</td>
      </ng-container>
      <ng-container matColumnDef="doses">
        <th mat-header-cell *matHeaderCellDef>Doses</th>
        <td mat-cell *matCellDef="let row">{{ row.doses }}</td>
      </ng-container>
    </app-data-table>
  `,
})
class Host {
  public readonly columns = ['name', 'doses'];
  public readonly rows = signal<Row[]>([{ name: 'BCG', doses: 1 }]);
}

describe('DataTable', () => {
  let fixture: ComponentFixture<Host>;

  function table(): HTMLTableElement {
    return fixture.nativeElement.querySelector('table');
  }

  function cells(selector: string): (string | undefined)[] {
    return Array.from(table().querySelectorAll(selector)).map((cell) => cell.textContent?.trim());
  }

  beforeEach(async () => {
    fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
  });

  it('names the table for assistive technologies', () => {
    expect(table().getAttribute('aria-label')).toBe('Vacinas');
  });

  it('renders the columns the page projects, in the given order', () => {
    expect(cells('th')).toEqual(['Nome', 'Doses']);
    expect(cells('td')).toEqual(['BCG', '1']);
  });

  it('renders the new rows when the data changes', async () => {
    fixture.componentInstance.rows.set([
      { name: 'DTP', doses: 5 },
      { name: 'Hepatite B', doses: 3 },
    ]);
    await fixture.whenStable();

    expect(cells('td')).toEqual(['DTP', '5', 'Hepatite B', '3']);
  });
});
