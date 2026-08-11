import { HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/http/api-client.service';
import { PagedResult, Patient, PatientListItem } from '../../core/models/clinic.models';

export type PatientPayload = Pick<Patient, 'name' | 'birthDate' | 'email' | 'phone'>;

@Injectable({ providedIn: 'root' })
export class PatientsService {
  private readonly api = inject(ApiClientService);
  list(term: string, page: number, pageSize: number): Observable<PagedResult<PatientListItem>> {
    const params = new HttpParams().set('term', term).set('page', page).set('pageSize', pageSize);
    return this.api.get<PagedResult<PatientListItem>>('/patients', params);
  }
  getById(id: string): Observable<Patient> { return this.api.get<Patient>(`/patients/${id}`); }
  create(payload: PatientPayload): Observable<Patient> { return this.api.post<Patient>('/patients', payload); }
  update(id: string, payload: PatientPayload): Observable<Patient> { return this.api.put<Patient>(`/patients/${id}`, payload); }
  deactivate(id: string): Observable<void> { return this.api.delete(`/patients/${id}`); }
}
