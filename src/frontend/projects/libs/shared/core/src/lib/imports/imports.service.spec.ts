import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@bpf-builder/shared/environment';
import { firstValueFrom } from 'rxjs';
import { countLines, ImportReport } from './imports.models';
import { ImportsService } from './imports.service';

const rejected: ImportReport = {
  imported: false,
  counts: { sessions: 2, learners: 0, trainers: 1, revenueLines: 0, costLines: 0 },
  issues: [
    {
      sheet: 'Learners',
      row: 4,
      column: 'Session code',
      message: 'Unknown session.',
      severity: 'Error',
    },
  ],
};

describe('ImportsService', () => {
  let service: ImportsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(ImportsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the file and returns the report of a rejected import', async () => {
    const file = new File(['x'], 'activity.xlsx');
    const report = firstValueFrom(service.upload('fy-1', file));

    const request = http.expectOne('/api/fiscal-years/fy-1/import');
    expect(request.request.method).toBe('POST');
    expect(((request.request.body as FormData).get('file') as File).name).toBe('activity.xlsx');
    request.flush(rejected, { status: 422, statusText: 'Unprocessable Entity' });

    expect(await report).toEqual(rejected);
  });

  it('passes other upload errors on', async () => {
    const report = firstValueFrom(service.upload('fy-1', new File(['x'], 'activity.xlsx')));

    http
      .expectOne('/api/fiscal-years/fy-1/import')
      .flush({ title: 'Conflict' }, { status: 409, statusText: 'Conflict' });

    await expect(report).rejects.toBeInstanceOf(HttpErrorResponse);
  });

  it('names the downloaded template after the server file name', async () => {
    const file = firstValueFrom(service.downloadTemplate());

    http.expectOne('/api/template').flush(new Blob(['xlsx']), {
      headers: {
        'Content-Disposition':
          "attachment; filename=bpf-activity-template-v1.xlsx; filename*=UTF-8''bpf-activity-template-v1.xlsx",
      },
    });

    expect((await file).fileName).toBe('bpf-activity-template-v1.xlsx');
  });

  it('reads a quoted file name, or falls back to a default one', async () => {
    const quoted = firstValueFrom(service.downloadSample());
    http.expectOne('/api/template/sample').flush(new Blob(['xlsx']), {
      headers: { 'Content-Disposition': 'attachment; filename="sample v1.xlsx"' },
    });
    expect((await quoted).fileName).toBe('sample v1.xlsx');

    const unnamed = firstValueFrom(service.downloadSample());
    http.expectOne('/api/template/sample').flush(new Blob(['xlsx']));
    const file = await unnamed;
    expect(file.fileName).toBe('bpf-activity-sample.xlsx');
    expect(file.content).toBeInstanceOf(Blob);

    const odd = firstValueFrom(service.downloadTemplate());
    http
      .expectOne('/api/template')
      .flush(new Blob(['xlsx']), { headers: { 'Content-Disposition': 'attachment' } });
    expect((await odd).fileName).toBe('bpf-activity-template.xlsx');
  });

  it('lists the counts with their labels', () => {
    expect(countLines(rejected.counts)).toEqual([
      { label: 'Sessions', value: 2 },
      { label: 'Learners', value: 0 },
      { label: 'Trainers', value: 1 },
      { label: 'Revenue lines', value: 0 },
      { label: 'Cost lines', value: 0 },
    ]);
  });
});
