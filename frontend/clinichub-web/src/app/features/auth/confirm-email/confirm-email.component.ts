import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-confirm-email',
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.scss'
})
export class ConfirmEmailComponent {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  loading = false;
  message = '';
  error = '';

  confirm(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) { this.error = 'O link de confirmação não contém um token válido.'; return; }
    this.loading = true;
    this.auth.confirmEmail(token).pipe(finalize(() => this.loading = false)).subscribe({
      next: (result) => this.message = result.message,
      error: (error) => this.error = error.error?.errors?.[0]?.message ?? 'Não foi possível confirmar o e-mail.'
    });
  }
}
