import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './layout/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((component) => component.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then((component) => component.RegisterComponent)
  },
  {
    path: 'confirm-email',
    loadComponent: () => import('./features/auth/confirm-email/confirm-email.component').then((component) => component.ConfirmEmailComponent)
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then((component) => component.DashboardComponent) },
      { path: 'patients', loadComponent: () => import('./features/patients/patients.component').then((component) => component.PatientsComponent) },
      { path: 'appointments', loadComponent: () => import('./features/appointments/appointments.component').then((component) => component.AppointmentsComponent) },
      { path: 'financial', loadComponent: () => import('./features/financial/financial.component').then((component) => component.FinancialComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
