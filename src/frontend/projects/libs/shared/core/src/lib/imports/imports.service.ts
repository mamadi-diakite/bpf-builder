import { HttpClient, HttpErrorResponse, HttpResponse, HttpStatusCode } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@bpf-builder/shared/environment';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { ActivityImport, DownloadedFile, ImportReport } from './imports.models';

@Injectable({ providedIn: 'root' })
export class ImportsService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(APP_ENVIRONMENT).apiBaseUrl;

  /** Fails with 404 while the fiscal year has no imported activity. */
  get(fiscalYearId: string): Observable<ActivityImport> {
    return this.http.get<ActivityImport>(this.importUrl(fiscalYearId));
  }

  /** Emits the validation report, whether the activity was saved or rejected for errors. */
  upload(fiscalYearId: string, file: File): Observable<ImportReport> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http
      .post<ImportReport>(this.importUrl(fiscalYearId), body)
      .pipe(
        catchError((error: unknown) =>
          error instanceof HttpErrorResponse && error.status === HttpStatusCode.UnprocessableEntity
            ? of(error.error as ImportReport)
            : throwError(() => error),
        ),
      );
  }

  /** The empty activity template to fill. */
  downloadTemplate(): Observable<DownloadedFile> {
    return this.download('template', 'bpf-activity-template.xlsx');
  }

  /** The template filled with fictional activity, to see what is expected. */
  downloadSample(): Observable<DownloadedFile> {
    return this.download('template/sample', 'bpf-activity-sample.xlsx');
  }

  private importUrl(fiscalYearId: string): string {
    return `${this.apiBaseUrl}/fiscal-years/${fiscalYearId}/import`;
  }

  private download(path: string, fallbackName: string): Observable<DownloadedFile> {
    return this.http
      .get(`${this.apiBaseUrl}/${path}`, { observe: 'response', responseType: 'blob' })
      .pipe(
        map((response) => ({
          fileName: fileNameOf(response) ?? fallbackName,
          content: response.body!,
        })),
      );
  }
}

function fileNameOf(response: HttpResponse<Blob>): string | null {
  const header = response.headers.get('Content-Disposition') ?? '';
  return /filename="?([^";]+)"?/.exec(header)?.[1] ?? null;
}
