import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/http/api-client.service';
import { Appointment, DoctorOption } from '../../core/models/clinic.models';

@Injectable({ providedIn: 'root' })
export class AppointmentsService {
  private readonly api = inject(ApiClientService);
  doctors(): Observable<DoctorOption[]> { return this.api.get<DoctorOption[]>('/users/doctors'); }
  schedule(payload: { patientId: string; doctorId: string; startUtc: string; durationMinutes: number }): Observable<Appointment> { return this.api.post<Appointment>('/appointments', payload); }
  confirm(id: string): Observable<Appointment> { return this.api.post<Appointment>(`/appointments/${id}/confirm`, {}); }
  reschedule(id: string, payload: { startUtc: string; durationMinutes: number }): Observable<Appointment> { return this.api.put<Appointment>(`/appointments/${id}/schedule`, payload); }
  cancel(id: string, reason: string): Observable<Appointment> { return this.api.post<Appointment>(`/appointments/${id}/cancel`, { reason }); }
}
