import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({ selector: 'app-dashboard', imports: [MatCardModule, MatIconModule], template: `<h1>Dashboard</h1><p class="subtitle">Visão geral da operação da clínica.</p><section><mat-card><mat-icon>people</mat-icon><strong>Pacientes</strong><span>Gerencie cadastros e histórico.</span></mat-card><mat-card><mat-icon>calendar_month</mat-icon><strong>Agendamentos</strong><span>Organize a agenda médica.</span></mat-card><mat-card><mat-icon>payments</mat-icon><strong>Financeiro</strong><span>Acompanhe recebimentos.</span></mat-card></section>`, styles: [`section{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:1rem}mat-card{display:grid;gap:.6rem;padding:1.5rem}mat-icon{color:#0b57d0}.subtitle{color:#5f6368}@media(max-width:800px){section{grid-template-columns:1fr}}`] })
export class DashboardComponent {}
