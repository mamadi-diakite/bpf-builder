export interface FiscalYear {
  readonly id: string;
  /** `YYYY-MM-DD` */
  readonly startDate: string;
  /** `YYYY-MM-DD` */
  readonly endDate: string;
  /** Turnover of the whole organization excluding VAT, training or not. */
  readonly totalTurnover: number;
  readonly createdAtUtc: string;
  /** `null` while the year has no imported activity. */
  readonly importedAtUtc: string | null;
}

/** Leave both dates `null` for the last full calendar year. */
export interface CreateFiscalYearRequest {
  readonly startDate: string | null;
  readonly endDate: string | null;
  readonly totalTurnover: number;
}
