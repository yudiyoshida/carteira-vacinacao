import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatPaginatorHarness } from '@angular/material/paginator/testing';
import { MatTableModule } from '@angular/material/table';
import { By } from '@angular/platform-browser';
import { DataTable } from './data-table';
import { PaginatorIntl } from './paginator-intl';

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

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, DataTable],
  template: `
    <app-data-table
      ariaLabel="Pessoas"
      [data]="rows"
      [columns]="columns"
      [totalItems]="42"
      [pageIndex]="pageIndex()"
      [pageSize]="20"
      (pageChange)="events.push($event)"
    >
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>Nome</th>
        <td mat-cell *matCellDef="let row">{{ row.name }}</td>
      </ng-container>
    </app-data-table>
  `,
})
class PaginatedHost {
  public readonly columns = ['name'];
  public readonly rows: Row[] = [{ name: 'Maria', doses: 0 }];
  public readonly pageIndex = signal(0);
  public readonly events: PageEvent[] = [];
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

  it('shows no paginator without the total of a paginated list', () => {
    expect(fixture.nativeElement.querySelector('mat-paginator')).toBeNull();
  });
});

describe('DataTable with pages', () => {
  let fixture: ComponentFixture<PaginatedHost>;

  function paginator(): HTMLElement {
    return fixture.nativeElement.querySelector('mat-paginator');
  }

  function harness(): Promise<MatPaginatorHarness> {
    return TestbedHarnessEnvironment.loader(fixture).getHarness(MatPaginatorHarness);
  }

  beforeEach(async () => {
    fixture = TestBed.createComponent(PaginatedHost);
    await fixture.whenStable();
  });

  it('names the paginator for assistive technologies', () => {
    expect(paginator().getAttribute('aria-label')).toBe('Paginação');
  });

  it('labels the paginator in Portuguese', async () => {
    expect(paginator().textContent).toContain('Itens por página:');
    expect(paginator().querySelector('[aria-label="Próxima página"]')).not.toBeNull();
    expect(paginator().querySelector('[aria-label="Página anterior"]')).not.toBeNull();
    expect(await (await harness()).getRangeLabel()).toBe('1 – 20 de 42');
  });

  it('RN19: offers sizes up to 100 and starts from the given size', async () => {
    const instance: MatPaginator = fixture.debugElement.query(
      By.directive(MatPaginator),
    ).componentInstance;

    expect(instance.pageSizeOptions).toEqual([10, 20, 50, 100]);
    expect(await (await harness()).getPageSize()).toBe(20);
  });

  it('tells the page when the operator goes to the next page', async () => {
    await (await harness()).goToNextPage();

    expect(fixture.componentInstance.events).toEqual([
      expect.objectContaining({ pageIndex: 1, pageSize: 20 }),
    ]);
  });

  it('tells the page when the operator changes the size', async () => {
    await (await harness()).setPageSize(50);

    expect(fixture.componentInstance.events).toEqual([
      expect.objectContaining({ pageIndex: 0, pageSize: 50 }),
    ]);
  });

  it('follows the page the list asks for', async () => {
    fixture.componentInstance.pageIndex.set(2);
    await fixture.whenStable();

    expect(await (await harness()).getRangeLabel()).toBe('41 – 42 de 42');
  });
});

describe('PaginatorIntl', () => {
  const intl = new PaginatorIntl();

  it.each([
    [0, 20, 42, '1 – 20 de 42'],
    [2, 20, 42, '41 – 42 de 42'],
    [0, 20, 0, '0 de 0'],
  ])('labels page %i with %i items out of %i as "%s"', (page, size, length, label) => {
    expect(intl.getRangeLabel(page, size, length)).toBe(label);
  });
});
