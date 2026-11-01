import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  EMPTY,
  filter,
  finalize,
  Observable,
  switchMap,
  tap,
} from 'rxjs';
import { VaccinationApiService } from '../../../../core/api/vaccination-api.service';
import {
  DoseDto,
  VaccinationCardDto,
  VaccinationCardEntryDto,
} from '../../../../core/models/vaccination.model';
import { RoutesEnum } from '../../../../core/routes/routes.enum';
import { routePath } from '../../../../core/routes/routes.util';
import { formatApiDate } from '../../../../core/utils/api-date';
import { toApiError } from '../../../../core/utils/api-error';
import { orderedDoses, sameDose } from '../../../../core/utils/dose';
import { openConfirmModal } from '../../../../shared/components/confirm-modal/confirm-modal';
import { EmptyState } from '../../../../shared/components/empty-state/empty-state';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { Skeleton } from '../../../../shared/components/skeleton/skeleton';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { doseLabel, DoseLabelPipe } from '../../../../shared/pipes/dose-label.pipe';
import { AppliedOnForm, AppliedOnFormData } from './components/applied-on-form/applied-on-form';
import { DoseBlock, DoseBlockKind } from './components/dose-block/dose-block';
import {
  VaccinationForm,
  VaccinationFormData,
} from './components/vaccination-form/vaccination-form';

interface DoseBlockView {
  dose: DoseDto;
  kind: DoseBlockKind;
  // Both empty until the dose has a vaccination.
  vaccinationId: string;
  appliedOn: string;
  isLast: boolean;
}

// One block per dose of the scheme: the card of the API has only the doses already applied.
function toBlocks({ scheme, doses, nextExpectedDose }: VaccinationCardEntryDto): DoseBlockView[] {
  const lastVaccination = doses.at(-1);

  return orderedDoses(scheme).map((dose): DoseBlockView => {
    const vaccination = doses.find((applied) => sameDose(applied.dose, dose));
    const nextKind: DoseBlockKind = sameDose(dose, nextExpectedDose) ? 'next' : 'pending';

    return {
      dose,
      kind: vaccination ? 'applied' : nextKind,
      vaccinationId: vaccination?.vaccinationId ?? '',
      appliedOn: vaccination?.appliedOn ?? '',
      isLast: vaccination !== undefined && vaccination === lastVaccination,
    };
  });
}

@Component({
  selector: 'app-vaccination-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vaccination-card.html',
  styleUrl: './vaccination-card.scss',
  imports: [
    RouterLink,
    MatButton,
    MatIcon,
    DoseBlock,
    DoseLabelPipe,
    EmptyState,
    PageHeader,
    Skeleton,
  ],
})
export class VaccinationCard {
  private readonly vaccinationApi = inject(VaccinationApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  // The id of the person, from the route.
  public readonly id = input.required<string>();

  protected readonly peoplePath = routePath(RoutesEnum.PEOPLE);
  protected readonly card = signal<VaccinationCardDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly rows = computed(() =>
    (this.card()?.vaccines ?? []).map((vaccine) => ({ vaccine, blocks: toBlocks(vaccine) })),
  );

  private readonly reload$ = new BehaviorSubject<void>(undefined);

  constructor() {
    combineLatest([toObservable(this.id), this.reload$])
      .pipe(
        // Cancels the previous request, whose response could arrive after the one of a newer change.
        switchMap(([id]) => this.load(id)),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected reload(): void {
    this.reload$.next();
  }

  protected openRegistration(vaccineId: string | null = null): void {
    this.dialog
      .open<VaccinationForm, VaccinationFormData, boolean>(VaccinationForm, {
        data: {
          personId: this.id(),
          cardVaccines: this.card()?.vaccines ?? [],
          vaccineId,
        },
      })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.reload());
  }

  protected openCorrection(vaccine: VaccinationCardEntryDto, block: DoseBlockView): void {
    this.dialog
      .open<AppliedOnForm, AppliedOnFormData, boolean>(AppliedOnForm, {
        data: {
          personId: this.id(),
          vaccinationId: block.vaccinationId,
          vaccineName: vaccine.vaccineName,
          doseLabel: doseLabel(block.dose, vaccine.scheme),
          appliedOn: block.appliedOn,
        },
      })
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.reload());
  }

  protected confirmDeletion(vaccine: VaccinationCardEntryDto, block: DoseBlockView): void {
    const label = doseLabel(block.dose, vaccine.scheme);
    const question = `Excluir a vacinação de ${vaccine.vaccineName} (${label}), aplicada em ${formatApiDate(block.appliedOn)}?`;
    // F10: without another vaccination, the vaccine leaves the card.
    const warning = vaccine.doses.length === 1 ? ' A vacina sairá do cartão.' : '';

    openConfirmModal(this.dialog, {
      heading: 'Excluir vacinação',
      message: question + warning,
      confirmLabel: 'Excluir',
    })
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => this.vaccinationApi.delete(this.id(), block.vaccinationId)),
      )
      .subscribe({
        next: ({ message }) => {
          this.toast.success(message);
          this.reload();
        },
        error: (error: unknown) => this.toast.error(toApiError(error).message),
      });
  }

  private load(id: string): Observable<VaccinationCardDto> {
    this.loading.set(true);

    return this.vaccinationApi.findCard(id).pipe(
      tap((card) => this.card.set(card)),
      // An inner catch keeps the page alive for the next attempt.
      catchError((error: unknown) => {
        this.toast.error(toApiError(error).message);
        return EMPTY;
      }),
      finalize(() => this.loading.set(false)),
    );
  }
}
