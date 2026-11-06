export const MAX_IMPORT_BYTES = 10 * 1024 * 1024;

export interface ImportCounts {
  readonly sessions: number;
  readonly learners: number;
  readonly trainers: number;
  readonly revenueLines: number;
  readonly costLines: number;
}

export interface ImportIssue {
  readonly sheet: string;
  /** Excel row number; `null` for a problem with the whole sheet. */
  readonly row: number | null;
  /** Column header; `null` for a problem with the whole sheet or row. */
  readonly column: string | null;
  readonly message: string;
  /** Errors block the import, warnings do not. */
  readonly severity: 'Error' | 'Warning';
}

export interface ImportReport {
  /** `false` when at least one issue is an error: nothing was saved. */
  readonly imported: boolean;
  /** Rows read without error. */
  readonly counts: ImportCounts;
  readonly issues: readonly ImportIssue[];
}

/** The activity saved for a fiscal year. */
export interface ActivityImport {
  readonly fileName: string;
  readonly importedAtUtc: string;
  readonly counts: ImportCounts;
}

export interface DownloadedFile {
  readonly fileName: string;
  readonly content: Blob;
}

export function countLines(counts: ImportCounts): { label: string; value: number }[] {
  return [
    { label: 'Sessions', value: counts.sessions },
    { label: 'Learners', value: counts.learners },
    { label: 'Trainers', value: counts.trainers },
    { label: 'Revenue lines', value: counts.revenueLines },
    { label: 'Cost lines', value: counts.costLines },
  ];
}
