import { HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/http/api-client.service';
import { Payment, RevenueReport } from '../../core/models/clinic.models';

@Injectable({ providedIn: 'root' })
export class FinancialService {
  private readonly api = inject(ApiClientService);
  registerPayment(payload: { appointmentId: string; amount: number; currency: string; method: number }): Observable<Payment> { return this.api.post<Payment>('/payments', payload); }
  revenue(startDate: string, endDate: string): Observable<RevenueReport> {
    return this.api.get<RevenueReport>('/financial/revenue', new HttpParams().set('startDate', startDate).set('endDate', endDate));
  }
}
