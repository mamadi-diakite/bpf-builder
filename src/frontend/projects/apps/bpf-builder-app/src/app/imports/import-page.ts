import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  ActivityImport,
  countLines,
  DownloadedFile,
  FiscalYear,
  FiscalYearsService,
  FormSubmission,
  ImportReport,
  ImportsService,
  LoadState,
  MAX_IMPORT_BYTES,
  toFormErrors,
  withLoadState,
} from '@bpf-builder/shared/core';
import { FieldErrors } from '@bpf-builder/shared/ui';
import { Observable } from 'rxjs';
import { saveFile } from '../shared/save-file';

/** Imports the activity of a fiscal year from the filled Excel template. */
@Component({
  selector: 'bpf-import-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="space-y-8">
      <a routerLink="/fiscal-years" class="link text-sm">← Back to the fiscal years</a>

      @let year = fiscalYear();
      @if (year.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the fiscal year…</p>
      } @else if (year.status === 'ready') {
        <div class="space-y-1">
          <h1 class="text-2xl font-bold tracking-tight">Import activity</h1>
          <p class="text-slate-600" data-testid="period">
            Fiscal year {{ year.value.startDate | date: 'mediumDate' }} –
            {{ year.value.endDate | date: 'mediumDate' }}
          </p>
        </div>

        <section class="panel" aria-labelledby="template-title">
          <h2 id="template-title" class="text-lg font-semibold">1. Get the Excel template</h2>
          <p class="text-sm text-slate-600">
            Fill one sheet each for sessions, learners, trainers, revenue and costs. The sample
            shows a filled template with fictional data.
          </p>
          @if (downloadError(); as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }
          <div class="flex flex-wrap gap-3">
            <button type="button" class="button-secondary" (click)="downloadTemplate()">
              Download the template
            </button>
            <button type="button" class="button-secondary" (click)="downloadSample()">
              Download a sample
            </button>
          </div>
        </section>

        @if (year.value.importedAtUtc) {
          <section class="panel" aria-labelledby="imported-title">
            <h2 id="imported-title" class="text-lg font-semibold">2. Imported activity</h2>
            @let saved = existing();
            @if (saved.status === 'loading') {
              <p class="text-slate-600" role="status">Loading the imported activity…</p>
            } @else if (saved.status === 'ready') {
              <p class="text-sm text-slate-600" data-testid="imported">
                Imported on {{ saved.value.importedAtUtc | date: 'mediumDate' }} from
                {{ saved.value.fileName }}.
              </p>
              <dl class="grid grid-cols-2 gap-3 sm:grid-cols-5" aria-label="Imported rows">
                @for (line of lines(saved.value.counts); track line.label) {
                  <div class="rounded-md bg-slate-50 p-3">
                    <dt class="text-xs text-slate-500">{{ line.label }}</dt>
                    <dd class="text-xl font-semibold">{{ line.value }}</dd>
                  </div>
                }
              </dl>
            } @else {
              <p class="alert-error" role="alert">
                The imported activity could not be loaded. Please try again later.
              </p>
            }
          </section>
        } @else {
          <form class="panel" [formGroup]="form" (ngSubmit)="upload(fileInput)" novalidate>
            <h2 class="text-lg font-semibold">2. Import the filled template</h2>
            <p class="text-sm text-slate-600">
              Use an Excel .xlsx file (10 MB max). Nothing is saved while the file has errors.
            </p>
            @if (submission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <div>
              <label class="form-label" for="file">Excel file</label>
              <input
                #fileInput
                id="file"
                class="mt-1 block text-sm"
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                (change)="choose(fileInput)"
              />
              <bpf-field-errors [messages]="fileMessages()" />
            </div>
            <button
              class="button-primary sm:w-auto"
              type="submit"
              [disabled]="submission.pending()"
            >
              Import activity
            </button>
          </form>
        }

        @if (report(); as result) {
          <section class="panel" aria-labelledby="report-title" data-testid="report">
            <h2 id="report-title" class="text-lg font-semibold">Validation report</h2>
            @if (result.imported) {
              <p class="alert-success" role="status">The activity was imported.</p>
            } @else {
              <p class="alert-error" role="alert">
                Nothing was saved. Fix the errors in your file, then import it again.
              </p>
            }
            <p class="text-sm text-slate-600" data-testid="summary">
              {{ errorCount(result) }} {{ errorCount(result) === 1 ? 'error' : 'errors' }},
              {{ warningCount(result) }}
              {{ warningCount(result) === 1 ? 'warning' : 'warnings' }}.
            </p>
            @if (result.issues.length > 0) {
              <div class="overflow-x-auto">
                <table class="w-full text-left text-sm">
                  <thead class="border-b border-slate-200 text-slate-500">
                    <tr>
                      <th class="py-2 pr-4 font-medium">Severity</th>
                      <th class="py-2 pr-4 font-medium">Sheet</th>
                      <th class="py-2 pr-4 font-medium">Row</th>
                      <th class="py-2 pr-4 font-medium">Column</th>
                      <th class="py-2 font-medium">Problem</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (issue of result.issues; track $index) {
                      <tr class="border-b border-slate-100 align-top">
                        <td
                          class="py-2 pr-4 font-medium"
                          [class.text-rose-700]="issue.severity === 'Error'"
                          [class.text-amber-700]="issue.severity === 'Warning'"
                        >
                          {{ issue.severity }}
                        </td>
                        <td class="py-2 pr-4">{{ issue.sheet }}</td>
                        <td class="py-2 pr-4">{{ issue.row ?? '—' }}</td>
                        <td class="py-2 pr-4">{{ issue.column ?? '—' }}</td>
                        <td class="py-2">{{ issue.message }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </section>
        }
      } @else if (year.status === 'missing') {
        <p class="alert-info">This fiscal year does not exist.</p>
      } @else {
        <p class="alert-error" role="alert">
          The fiscal year could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class ImportPage {
  private readonly fiscalYears = inject(FiscalYearsService);
  private readonly imports = inject(ImportsService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  protected readonly lines = countLines;
  protected readonly fiscalYear = signal<LoadState<FiscalYear>>({ status: 'loading' });
  protected readonly existing = signal<LoadState<ActivityImport>>({ status: 'loading' });
  protected readonly report = signal<ImportReport | null>(null);
  protected readonly downloadError = signal<string | null>(null);
  protected readonly fileProblem = signal<string | null>(null);
  protected readonly form = this.formBuilder.group({
    file: this.formBuilder.control<File | null>(null, Validators.required),
  });
  protected readonly submission = new FormSubmission(this.form, {
    file: 'Choose a filled activity template.',
  });

  constructor() {
    this.load();
  }

  protected fileMessages(): readonly string[] {
    const problem = this.fileProblem();
    return problem ? [problem] : this.submission.messagesFor('file');
  }

  protected choose(input: HTMLInputElement): void {
    const file = input.files?.[0] ?? null;
    const problem = file ? checkWorkbook(file) : null;
    this.fileProblem.set(problem);
    this.form.controls.file.setValue(problem ? null : file);
    this.form.controls.file.markAsTouched();
    if (problem) {
      input.value = '';
    }
  }

  protected upload(input: HTMLInputElement): void {
    const file = this.form.getRawValue().file;
    this.report.set(null);
    this.submission.submit(
      () => this.imports.upload(this.id, file!),
      (report) => {
        this.report.set(report);
        if (report.imported) {
          this.load();
        } else {
          this.form.reset();
          input.value = '';
        }
      },
    );
  }

  protected downloadTemplate(): void {
    this.download(this.imports.downloadTemplate());
  }

  protected downloadSample(): void {
    this.download(this.imports.downloadSample());
  }

  protected errorCount(report: ImportReport): number {
    return report.issues.filter((issue) => issue.severity === 'Error').length;
  }

  protected warningCount(report: ImportReport): number {
    return report.issues.filter((issue) => issue.severity === 'Warning').length;
  }

  private load(): void {
    withLoadState(this.fiscalYears.get(this.id)).subscribe((state) => {
      this.fiscalYear.set(state);
      if (state.status === 'ready' && state.value.importedAtUtc) {
        withLoadState(this.imports.get(this.id)).subscribe((saved) => this.existing.set(saved));
      }
    });
  }

  private download(file: Observable<DownloadedFile>): void {
    this.downloadError.set(null);
    file.subscribe({
      next: saveFile,
      error: (error: unknown) => this.downloadError.set(toFormErrors(error).form),
    });
  }
}

function checkWorkbook(file: File): string | null {
  if (!file.name.toLowerCase().endsWith('.xlsx')) {
    return 'Only Excel .xlsx files can be imported.';
  }

  return file.size > MAX_IMPORT_BYTES ? 'The file is larger than 10 MB.' : null;
}
