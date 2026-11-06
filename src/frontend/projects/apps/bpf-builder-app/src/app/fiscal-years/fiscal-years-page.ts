import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  FiscalYear,
  FiscalYearsService,
  FormSubmission,
  LoadState,
  withLoadState,
} from '@bpf-builder/shared/core';
import { FieldErrors } from '@bpf-builder/shared/ui';
import { switchMap } from 'rxjs';

@Component({
  selector: 'bpf-fiscal-years-page',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="space-y-8">
      <h1 class="text-2xl font-bold tracking-tight">Fiscal years</h1>

      <form class="panel" [formGroup]="form" (ngSubmit)="create()" novalidate>
        <h2 class="text-lg font-semibold">Add a fiscal year</h2>
        <p class="text-sm text-slate-600">
          Leave both dates empty for the last calendar year, from January 1 to December 31.
        </p>

        @if (created(); as year) {
          <p class="alert-success" role="status">
            The fiscal year {{ year.startDate | date: 'mediumDate' }} –
            {{ year.endDate | date: 'mediumDate' }} was added.
          </p>
        }
        @if (submission.errors().form; as message) {
          <p class="alert-error" role="alert">{{ message }}</p>
        }

        <div class="grid gap-4 sm:grid-cols-3">
          <div>
            <label class="form-label" for="startDate">Start date</label>
            <input id="startDate" class="form-input" type="date" formControlName="startDate" />
            <bpf-field-errors [messages]="submission.messagesFor('startDate')" />
          </div>
          <div>
            <label class="form-label" for="endDate">End date</label>
            <input id="endDate" class="form-input" type="date" formControlName="endDate" />
            <bpf-field-errors [messages]="submission.messagesFor('endDate')" />
          </div>
          <div>
            <label class="form-label" for="totalTurnover">Total turnover excl. VAT (EUR)</label>
            <input
              id="totalTurnover"
              class="form-input"
              type="number"
              min="0"
              step="0.01"
              formControlName="totalTurnover"
            />
            <bpf-field-errors [messages]="submission.messagesFor('totalTurnover')" />
          </div>
        </div>
        <p class="text-xs text-slate-500">
          The turnover of the whole organization, training or not.
        </p>

        <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
          Add fiscal year
        </button>
      </form>

      <section class="space-y-4" aria-labelledby="list-title">
        <h2 id="list-title" class="text-lg font-semibold">Your fiscal years</h2>
        @let current = state();
        @if (current.status === 'loading') {
          <p class="text-slate-600" role="status">Loading the fiscal years…</p>
        } @else if (current.status === 'ready') {
          <ul class="space-y-3" data-testid="fiscal-years">
            @for (year of current.value; track year.id) {
              <li
                class="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
              >
                <div>
                  <p class="font-medium" data-testid="period">
                    {{ year.startDate | date: 'mediumDate' }} –
                    {{ year.endDate | date: 'mediumDate' }}
                  </p>
                  <p class="text-sm text-slate-500" data-testid="details">
                    Turnover {{ year.totalTurnover | currency: 'EUR' }} ·
                    @if (year.importedAtUtc) {
                      Activity imported on {{ year.importedAtUtc | date: 'mediumDate' }}
                    } @else {
                      No activity imported yet
                    }
                  </p>
                </div>
                <a [routerLink]="['/fiscal-years', year.id, 'import']" class="link text-sm">
                  {{ year.importedAtUtc ? 'View import' : 'Import activity' }}
                </a>
              </li>
            } @empty {
              <li class="text-slate-600">
                No fiscal years yet. Fill in your
                <a routerLink="/organization" class="link">organization</a> first, then add a fiscal
                year above.
              </li>
            }
          </ul>
        } @else {
          <p class="alert-error" role="alert">
            The fiscal years could not be loaded. Please try again later.
          </p>
        }
      </section>
    </section>
  `,
})
export class FiscalYearsPage {
  private readonly fiscalYears = inject(FiscalYearsService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly version = signal(0);

  protected readonly created = signal<FiscalYear | null>(null);
  protected readonly form = this.formBuilder.group({
    startDate: [''],
    endDate: [''],
    totalTurnover: this.formBuilder.control<number | null>(null, Validators.required),
  });
  protected readonly submission = new FormSubmission(this.form, {
    totalTurnover: 'Enter the total turnover, 0 or more.',
  });
  protected readonly state = toSignal(
    toObservable(this.version).pipe(switchMap(() => withLoadState(this.fiscalYears.list()))),
    { initialValue: { status: 'loading' } as LoadState<FiscalYear[]> },
  );

  protected create(): void {
    const { startDate, endDate, totalTurnover } = this.form.getRawValue();
    this.created.set(null);
    this.submission.submit(
      () =>
        this.fiscalYears.create({
          startDate: startDate || null,
          endDate: endDate || null,
          totalTurnover: totalTurnover!,
        }),
      (year) => {
        this.created.set(year);
        this.form.reset();
        this.version.update((version) => version + 1);
      },
    );
  }
}
