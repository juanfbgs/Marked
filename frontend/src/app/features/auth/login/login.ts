import { Component, inject, signal } from '@angular/core';
import { email, form, FormField, FormRoot, minLength, required } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { LoginRequest } from '@core/models/auth.model';
import { AuthService } from '@core/services/auth.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [FormRoot, FormField, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  loginModel = signal<LoginRequest>({
    email: '',
    password: ''
  });

  loginForm = form(
    this.loginModel,
    (f) => {
      required(f.email, { message: 'Email is required' });
      email(f.email, { message: 'Invalid email address' });
      required(f.password, { message: 'Password is required' });
      minLength(f.password, 6, { message: 'Minimum 6 characters' });
    },
    {
      submission: {
        action: async () => {
          try {
            await firstValueFrom(this.auth.login(this.loginModel()));
            this.router.navigate(['/']);
            return;
          } catch {
            return { kind: 'serverError', message: 'Login failed' };
          }
        },
      },
    },
  );
}
