import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@bpf-builder/shared/environment';
import { Observable } from 'rxjs';
import {
  Organization,
  OrganizationIdentification,
  OrganizationSignatory,
} from './organization.models';

/** The organization of the signed-in user. It is created when section A is first saved. */
@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_ENVIRONMENT).apiBaseUrl}/organization`;

  /** Fails with 404 until section A has been saved. */
  get(): Observable<Organization> {
    return this.http.get<Organization>(this.baseUrl);
  }

  saveIdentification(request: OrganizationIdentification): Observable<Organization> {
    return this.http.put<Organization>(`${this.baseUrl}/identification`, request);
  }

  saveSignatory(request: OrganizationSignatory): Observable<Organization> {
    return this.http.put<Organization>(`${this.baseUrl}/signatory`, request);
  }
}
