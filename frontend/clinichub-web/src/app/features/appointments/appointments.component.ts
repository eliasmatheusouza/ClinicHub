import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Appointment, DoctorOption, PatientListItem } from '../../core/models/clinic.models';
import { PatientsService } from '../patients/patients.service';
import { AppointmentsService } from './appointments.service';

@Component({ selector: 'app-appointments', imports: [DatePipe, ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule], templateUrl: './appointments.component.html', styleUrl: './appointments.component.scss' })
export class AppointmentsComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly appointmentsService = inject(AppointmentsService);
  private readonly patientsService = inject(PatientsService);
  readonly patients = signal<PatientListItem[]>([]);
  readonly doctors = signal<DoctorOption[]>([]);
  readonly appointment = signal<Appointment | null>(null);
  readonly form = this.formBuilder.nonNullable.group({ patientId: ['', Validators.required], doctorId: ['', Validators.required], start: ['', Validators.required], durationMinutes: [30, [Validators.required, Validators.min(15), Validators.max(480)]] });
  readonly actionForm = this.formBuilder.nonNullable.group({ appointmentId: ['', Validators.required], start: ['', Validators.required], durationMinutes: [30, Validators.required], reason: [''] });
  error = '';
  message = '';

  ngOnInit(): void {
    this.patientsService.list('', 1, 100).subscribe({ next: (result) => this.patients.set(result.items), error: () => this.error = 'Não foi possível carregar pacientes.' });
    this.appointmentsService.doctors().subscribe({ next: (doctors) => this.doctors.set(doctors), error: () => this.error = 'Não foi possível carregar médicos.' });
  }
  schedule(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    this.appointmentsService.schedule({ patientId: value.patientId, doctorId: value.doctorId, startUtc: new Date(value.start).toISOString(), durationMinutes: value.durationMinutes }).subscribe({ next: (appointment) => this.success(appointment, 'Consulta agendada.'), error: (error) => this.fail(error) });
  }
  confirm(): void { this.run((id) => this.appointmentsService.confirm(id), 'Consulta confirmada; notificação enviada.'); }
  reschedule(): void {
    const value = this.actionForm.getRawValue();
    if (!value.appointmentId || !value.start) return;
    this.appointmentsService.reschedule(value.appointmentId, { startUtc: new Date(value.start).toISOString(), durationMinutes: value.durationMinutes }).subscribe({ next: (appointment) => this.success(appointment, 'Consulta reagendada.'), error: (error) => this.fail(error) });
  }
  cancel(): void {
    const value = this.actionForm.getRawValue();
    if (!value.appointmentId || !value.reason) return;
    this.appointmentsService.cancel(value.appointmentId, value.reason).subscribe({ next: (appointment) => this.success(appointment, 'Consulta cancelada.'), error: (error) => this.fail(error) });
  }
  private run(action: (id: string) => ReturnType<AppointmentsService['confirm']>, message: string): void {
    const id = this.actionForm.controls.appointmentId.value; if (!id) return;
    action(id).subscribe({ next: (appointment) => this.success(appointment, message), error: (error) => this.fail(error) });
  }
  private success(appointment: Appointment, message: string): void { this.appointment.set(appointment); this.actionForm.controls.appointmentId.setValue(appointment.id); this.message = message; this.error = ''; }
  private fail(error: any): void { this.error = error.error?.errors?.[0]?.message ?? 'Não foi possível processar a consulta.'; this.message = ''; }
}
