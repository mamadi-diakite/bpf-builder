import { ActivityImport, FiscalYear, ImportReport } from '@bpf-builder/shared/core';
import { vi } from 'vitest';
import { AppHarness } from '../../testing/app-harness';

const year: FiscalYear = {
  id: 'fy-2025',
  startDate: '2025-01-01',
  endDate: '2025-12-31',
  totalTurnover: 468900,
  createdAtUtc: '2026-01-10T09:00:00Z',
  importedAtUtc: null,
};

const importedYear: FiscalYear = { ...year, importedAtUtc: '2026-02-03T10:30:00Z' };

const saved: ActivityImport = {
  fileName: 'activity-2025.xlsx',
  importedAtUtc: '2026-02-03T10:30:00Z',
  counts: { sessions: 12, learners: 84, trainers: 5, revenueLines: 14, costLines: 9 },
};

const rejected: ImportReport = {
  imported: false,
  counts: { sessions: 11, learners: 83, trainers: 5, revenueLines: 14, costLines: 9 },
  issues: [
    {
      sheet: 'Learners',
      row: 7,
      column: 'Session code',
      message: 'Unknown session S-99.',
      severity: 'Error',
    },
    { sheet: 'Sessions', row: null, column: null, message: 'Sheet is missing.', severity: 'Error' },
    {
      sheet: 'Trainers',
      row: 3,
      column: 'Hours',
      message: 'Hours look high.',
      severity: 'Warning',
    },
  ],
};

const accepted: ImportReport = {
  imported: true,
  counts: saved.counts,
  issues: [rejected.issues[2]],
};

const url = '/api/fiscal-years/fy-2025/import';

describe('Import page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => {
    app.http.verify();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  async function openPage(fiscalYear: FiscalYear = year): Promise<void> {
    app.signIn();
    await app.open('/fiscal-years/fy-2025/import');
    expect(app.text('[role="status"]')).toBe('Loading the fiscal year…');
    app.http.expectOne('/api/fiscal-years/fy-2025').flush(fiscalYear);
    await app.settle();
  }

  function workbook(name = 'activity-2025.xlsx', size = 2048): File {
    const file = new File(['xlsx'], name);
    Object.defineProperty(file, 'size', { value: size });
    return file;
  }

  it('is only for signed-in users', async () => {
    await app.open('/fiscal-years/fy-2025/import');

    expect(app.router.url).toBe('/login?returnUrl=%2Ffiscal-years%2Ffy-2025%2Fimport');
  });

  it('shows the fiscal year and the upload form while nothing is imported', async () => {
    await openPage();

    expect(app.text('h1')).toBe('Import activity');
    expect(app.text('[data-testid="period"]')!.replace(/\s+/g, ' ')).toBe(
      'Fiscal year Jan 1, 2025 – Dec 31, 2025',
    );
    expect(app.element.querySelector('#file')).not.toBeNull();
    expect(app.element.querySelector('[data-testid="report"]')).toBeNull();
  });

  it('downloads the template and the sample with the access token', async () => {
    const createObjectURL = vi.fn(() => 'blob:template');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }));
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    await openPage();

    await app.click('section button:first-of-type');
    const template = app.http.expectOne('/api/template');
    expect(template.request.headers.get('Authorization')).toBe('Bearer token-1');
    template.flush(new Blob(['xlsx']), {
      headers: { 'Content-Disposition': 'attachment; filename=bpf-activity-template-v1.xlsx' },
    });
    await app.settle();

    expect((click.mock.contexts[0] as HTMLAnchorElement).download).toBe(
      'bpf-activity-template-v1.xlsx',
    );
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:template');

    await app.click('section button:last-of-type');
    app.http.expectOne('/api/template/sample').flush(new Blob(['xlsx']));
    await app.settle();

    expect((click.mock.contexts[1] as HTMLAnchorElement).download).toBe('bpf-activity-sample.xlsx');
  });

  it('explains when a download fails', async () => {
    await openPage();

    await app.click('section button:last-of-type');
    app.http
      .expectOne('/api/template/sample')
      .flush(new Blob(), { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('section [role="alert"]')).toBe('Something went wrong. Please try again.');
  });

  it('checks the file before sending it', async () => {
    await openPage();

    await app.submit();
    expect(app.text('bpf-field-errors')).toBe('Choose a filled activity template.');

    await app.choose('file', workbook('activity.csv'));
    expect(app.text('bpf-field-errors')).toBe('Only Excel .xlsx files can be imported.');

    await app.choose('file', workbook('activity.XLSX', 10 * 1024 * 1024 + 1));
    expect(app.text('bpf-field-errors')).toBe('The file is larger than 10 MB.');

    await app.choose('file');
    expect(app.text('bpf-field-errors')).toBe('Choose a filled activity template.');
  });

  it('shows the validation report when the file has errors, without saving', async () => {
    await openPage();

    await app.choose('file', workbook());
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url });
    expect((request.request.body as FormData).get('file')).toBeInstanceOf(File);
    request.flush(rejected, { status: 422, statusText: 'Unprocessable Entity' });
    await app.settle();

    expect(app.text('[data-testid="report"] [role="alert"]')).toBe(
      'Nothing was saved. Fix the errors in your file, then import it again.',
    );
    expect(app.text('[data-testid="summary"]')!.replace(/\s+/g, ' ')).toBe('2 errors, 1 warning.');
    const rows = [...app.element.querySelectorAll('[data-testid="report"] tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent!.trim()),
    );
    expect(rows).toEqual([
      ['Error', 'Learners', '7', 'Session code', 'Unknown session S-99.'],
      ['Error', 'Sessions', '—', '—', 'Sheet is missing.'],
      ['Warning', 'Trainers', '3', 'Hours', 'Hours look high.'],
    ]);
    expect(app.element.querySelector('tbody td')!.className).toContain('text-rose-700');
    expect(app.element.querySelector('tbody tr:last-child td')!.className).toContain(
      'text-amber-700',
    );

    await app.submit();
    expect(app.text('bpf-field-errors')).toBe('Choose a filled activity template.');
  });

  it('imports the activity, then shows what was saved', async () => {
    await openPage();

    await app.choose('file', workbook());
    await app.submit();
    app.http
      .expectOne({ method: 'POST', url })
      .flush(accepted, { status: 201, statusText: 'Created' });
    await app.settle();
    app.http.expectOne('/api/fiscal-years/fy-2025').flush(importedYear);
    await app.settle();
    expect(app.text('[role="status"]')).toBe('Loading the imported activity…');
    app.http.expectOne(url).flush(saved);
    await app.settle();

    expect(app.text('[data-testid="report"] [role="status"]')).toBe('The activity was imported.');
    expect(app.text('[data-testid="summary"]')!.replace(/\s+/g, ' ')).toBe('0 errors, 1 warning.');
    expect(app.element.querySelector('#file')).toBeNull();
    expect(app.text('[data-testid="imported"]')!.replace(/\s+/g, ' ')).toBe(
      'Imported on Feb 3, 2026 from activity-2025.xlsx.',
    );
    expect(app.texts('dl dt')).toEqual([
      'Sessions',
      'Learners',
      'Trainers',
      'Revenue lines',
      'Cost lines',
    ]);
    expect(app.texts('dl dd')).toEqual(['12', '84', '5', '14', '9']);
  });

  it('reports a clean import and server file errors', async () => {
    await openPage();

    await app.choose('file', workbook());
    await app.submit();
    app.http
      .expectOne({ method: 'POST', url })
      .flush(
        { errors: { file: ['This file is not a readable Excel workbook.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();
    expect(app.text('bpf-field-errors')).toBe('This file is not a readable Excel workbook.');

    await app.submit();
    app.http
      .expectOne({ method: 'POST', url })
      .flush({ ...accepted, issues: [] }, { status: 201, statusText: 'Created' });
    await app.settle();
    app.http.expectOne('/api/fiscal-years/fy-2025').flush(importedYear);
    await app.settle();
    app.http.expectOne(url).flush(saved);
    await app.settle();

    expect(app.text('[data-testid="summary"]')!.replace(/\s+/g, ' ')).toBe('0 errors, 0 warnings.');
    expect(app.element.querySelector('[data-testid="report"] table')).toBeNull();
  });

  it('shows a single error and a conflict message', async () => {
    await openPage();

    await app.choose('file', workbook());
    await app.submit();
    app.http
      .expectOne({ method: 'POST', url })
      .flush(
        { ...rejected, issues: [rejected.issues[0]] },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
    await app.settle();
    expect(app.text('[data-testid="summary"]')!.replace(/\s+/g, ' ')).toBe('1 error, 0 warnings.');

    await app.choose('file', workbook());
    await app.submit();
    app.http
      .expectOne({ method: 'POST', url })
      .flush(
        { title: 'This fiscal year already has imported activity.' },
        { status: 409, statusText: 'Conflict' },
      );
    await app.settle();
    expect(app.text('form [role="alert"]')).toBe('This fiscal year already has imported activity.');
    expect(app.element.querySelector('[data-testid="report"]')).toBeNull();
  });

  it('explains when the imported activity cannot be loaded', async () => {
    await openPage(importedYear);
    app.http.expectOne(url).flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'The imported activity could not be loaded. Please try again later.',
    );
  });

  it('says when the fiscal year does not exist or cannot be loaded', async () => {
    app.signIn();
    await app.open('/fiscal-years/fy-2025/import');
    app.http
      .expectOne('/api/fiscal-years/fy-2025')
      .flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();
    expect(app.text('.alert-info')).toBe('This fiscal year does not exist.');
  });

  it('explains when the fiscal year cannot be loaded', async () => {
    app.signIn();
    await app.open('/fiscal-years/fy-2025/import');
    app.http
      .expectOne('/api/fiscal-years/fy-2025')
      .flush(null, { status: 500, statusText: 'Error' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe(
      'The fiscal year could not be loaded. Please try again later.',
    );
  });
});
