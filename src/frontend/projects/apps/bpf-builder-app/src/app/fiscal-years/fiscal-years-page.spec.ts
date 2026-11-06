import { FiscalYear } from '@bpf-builder/shared/core';
import { AppHarness } from '../../testing/app-harness';

const imported: FiscalYear = {
  id: 'fy-2025',
  startDate: '2025-01-01',
  endDate: '2025-12-31',
  totalTurnover: 468900,
  createdAtUtc: '2026-01-10T09:00:00Z',
  importedAtUtc: '2026-02-03T10:30:00Z',
};

const pending: FiscalYear = {
  id: 'fy-2024',
  startDate: '2024-01-01',
  endDate: '2024-12-31',
  totalTurnover: 412500,
  createdAtUtc: '2026-01-10T09:00:00Z',
  importedAtUtc: null,
};

describe('Fiscal years page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openPage(years: FiscalYear[]): Promise<void> {
    app.signIn();
    await app.open('/fiscal-years');
    expect(app.text('[role="status"]')).toBe('Loading the fiscal years…');
    app.http.expectOne('/api/fiscal-years').flush(years);
    await app.settle();
  }

  it('is only for signed-in users', async () => {
    await app.open('/fiscal-years');

    expect(app.router.url).toBe('/login?returnUrl=%2Ffiscal-years');
  });

  it('lists the fiscal years with their import status', async () => {
    await openPage([imported, pending]);

    expect(app.texts('[data-testid="period"]').map(collapse)).toEqual([
      'Jan 1, 2025 – Dec 31, 2025',
      'Jan 1, 2024 – Dec 31, 2024',
    ]);
    expect(app.texts('[data-testid="details"]').map(collapse)).toEqual([
      'Turnover €468,900.00 · Activity imported on Feb 3, 2026',
      'Turnover €412,500.00 · No activity imported yet',
    ]);
    const links = [...app.element.querySelectorAll('[data-testid="fiscal-years"] a')];
    expect(links.map((link) => link.textContent!.trim())).toEqual([
      'View import',
      'Import activity',
    ]);
    expect(links[1].getAttribute('href')).toBe('/fiscal-years/fy-2024/import');
  });

  it('points to the organization when there is no fiscal year', async () => {
    await openPage([]);

    expect(collapse(app.text('[data-testid="fiscal-years"] li')!)).toBe(
      'No fiscal years yet. Fill in your organization first, then add a fiscal year above.',
    );
    expect(app.element.querySelector('[data-testid="fiscal-years"] a')!.getAttribute('href')).toBe(
      '/organization',
    );
  });

  it('requires the turnover, then adds the last calendar year when no date is given', async () => {
    await openPage([]);

    await app.submit();
    expect(app.texts('bpf-field-errors p')).toEqual(['Enter the total turnover, 0 or more.']);

    await app.type('totalTurnover', '468900');
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/fiscal-years' });
    expect(request.request.body).toEqual({ startDate: null, endDate: null, totalTurnover: 468900 });
    request.flush(imported);
    await app.settle();

    expect(collapse(app.text('form [role="status"]')!)).toBe(
      'The fiscal year Jan 1, 2025 – Dec 31, 2025 was added.',
    );
    expect(app.element.querySelector<HTMLInputElement>('#totalTurnover')!.value).toBe('');
    app.http.expectOne('/api/fiscal-years').flush([imported]);
    await app.settle();
    expect(app.texts('[data-testid="period"]')).toHaveLength(1);
  });

  it('sends the chosen dates and shows the server messages', async () => {
    await openPage([pending]);

    await app.type('startDate', '2025-07-01');
    await app.type('endDate', '2025-06-30');
    await app.type('totalTurnover', '1000');
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/fiscal-years' });
    expect(request.request.body).toEqual({
      startDate: '2025-07-01',
      endDate: '2025-06-30',
      totalTurnover: 1000,
    });
    request.flush(
      { errors: { endDate: ['End date must be after the start date.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await app.settle();

    expect(app.texts('bpf-field-errors p')).toEqual(['End date must be after the start date.']);
  });

  it('asks to save the organization first', async () => {
    await openPage([]);

    await app.type('totalTurnover', '0');
    await app.submit();
    app.http
      .expectOne({ method: 'POST', url: '/api/fiscal-years' })
      .flush(
        { title: "Save the organization's identification first." },
        { status: 404, statusText: 'Not Found' },
      );
    await app.settle();

    expect(app.text('form [role="alert"]')).toBe("Save the organization's identification first.");
  });

  it('explains when the fiscal years cannot be loaded', async () => {
    app.signIn();
    await app.open('/fiscal-years');
    app.http.expectOne('/api/fiscal-years').flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('section [role="alert"]')).toBe(
      'The fiscal years could not be loaded. Please try again later.',
    );
  });
});

function collapse(text: string): string {
  return text.replace(/\s+/g, ' ');
}
