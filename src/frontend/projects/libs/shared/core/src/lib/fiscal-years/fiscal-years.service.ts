import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@bpf-builder/shared/environment';
import { Observable } from 'rxjs';
import { CreateFiscalYearRequest, FiscalYear } from './fiscal-years.models';

@Injectable({ providedIn: 'root' })
export class FiscalYearsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_ENVIRONMENT).apiBaseUrl}/fiscal-years`;

  /** Latest first; empty while the user has no organization. */
  list(): Observable<FiscalYear[]> {
    return this.http.get<FiscalYear[]>(this.baseUrl);
  }

  get(id: string): Observable<FiscalYear> {
    return this.http.get<FiscalYear>(`${this.baseUrl}/${id}`);
  }

  /** Fails with 404 while the user has no organization. */
  create(request: CreateFiscalYearRequest): Observable<FiscalYear> {
    return this.http.post<FiscalYear>(this.baseUrl, request);
  }
}
