import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { Patient } from '../../core/models/clinic.models';
import { PatientPayload, PatientsService } from './patients.service';

@Component({ selector: 'app-patients', imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatTableModule], templateUrl: './patients.component.html', styleUrl: './patients.component.scss' })
export class PatientsComponent implements OnInit {
  private readonly service = inject(PatientsService);
  private readonly formBuilder = inject(FormBuilder);
  readonly filter = this.formBuilder.nonNullable.control('');
  readonly form = this.formBuilder.nonNullable.group({ name: ['', Validators.required], birthDate: ['', Validators.required], email: ['', [Validators.required, Validators.email]], phone: ['', Validators.required] });
  readonly patients = signal<Patient[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly editingId = signal<string | null>(null);
  readonly loading = signal(false);
  error = '';
  readonly columns = ['name', 'email', 'phone', 'actions'];

  ngOnInit(): void { this.load(); }
  load(page = this.page()): void {
    this.loading.set(true); this.error = '';
    this.service.list(this.filter.value, page, 10).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (result) => { this.patients.set(result.items); this.totalCount.set(result.totalCount); this.page.set(result.page); },
      error: () => this.error = 'Não foi possível carregar pacientes.'
    });
  }
  edit(patient: Patient): void { this.editingId.set(patient.id); this.form.setValue({ name: patient.name, birthDate: patient.birthDate, email: patient.email, phone: patient.phone }); }
  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const payload = this.form.getRawValue() as PatientPayload;
    const request = this.editingId() ? this.service.update(this.editingId()!, payload) : this.service.create(payload);
    request.subscribe({ next: () => { this.reset(); this.load(1); }, error: (error) => this.error = error.error?.errors?.[0]?.message ?? 'Não foi possível salvar o paciente.' });
  }
  deactivate(patient: Patient): void { this.service.deactivate(patient.id).subscribe({ next: () => this.load(), error: () => this.error = 'Não foi possível desativar o paciente.' }); }
  reset(): void { this.form.reset({ name: '', birthDate: '', email: '', phone: '' }); this.editingId.set(null); }
}
