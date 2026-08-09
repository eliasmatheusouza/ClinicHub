import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { RevenueReport } from '../../core/models/clinic.models';
import { FinancialService } from './financial.service';

@Component({ selector: 'app-financial', imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule, CurrencyPipe], templateUrl: './financial.component.html', styleUrl: './financial.component.scss' })
export class FinancialComponent {
  private readonly service = inject(FinancialService);
  private readonly formBuilder = inject(FormBuilder);
  readonly paymentForm = this.formBuilder.nonNullable.group({ appointmentId: ['', Validators.required], amount: [0, [Validators.required, Validators.min(0.01)]], currency: ['BRL', Validators.required], method: [4, Validators.required] });
  readonly reportForm = this.formBuilder.nonNullable.group({ startDate: ['', Validators.required], endDate: ['', Validators.required] });
  readonly report = signal<RevenueReport | null>(null);
  readonly columns = ['date', 'currency', 'grossRevenue', 'paymentCount'];
  message = ''; error = '';
  registerPayment(): void {
    if (this.paymentForm.invalid) { this.paymentForm.markAllAsTouched(); return; }
    this.service.registerPayment(this.paymentForm.getRawValue()).subscribe({ next: () => { this.message = 'Pagamento registrado.'; this.error = ''; this.paymentForm.controls.appointmentId.setValue(''); }, error: (error) => this.fail(error) });
  }
  loadReport(): void {
    if (this.reportForm.invalid) { this.reportForm.markAllAsTouched(); return; }
    const value = this.reportForm.getRawValue();
    this.service.revenue(value.startDate, value.endDate).subscribe({ next: (report) => { this.report.set(report); this.error = ''; }, error: (error) => this.fail(error) });
  }
  private fail(error: any): void { this.error = error.error?.errors?.[0]?.message ?? 'Não foi possível concluir a operação financeira.'; this.message = ''; }
}
