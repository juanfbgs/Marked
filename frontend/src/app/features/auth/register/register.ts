import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { RegisterRequest } from '@core/models/auth.model';
import { AuthService } from '@core/services/auth.service';
import { email, form, FormField, FormRoot, minLength, required, validate } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-register',
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private auth = inject(AuthService);
  private router = inject(Router);

  registerModel = signal<RegisterRequest>({
    firstName: '',
    lastName: '',
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
  });

  registerForm = form(
    this.registerModel,
    (f) => {
      required(f.firstName, { message: 'First name is required' });
      required(f.lastName, { message: 'Last name is required' });
      required(f.username, { message: 'Username is required' });
      required(f.email, { message: 'Email is required' });
      email(f.email, { message: 'Invalid email address' });
      required(f.password, { message: 'Password is required' });
      minLength(f.password, 6, { message: 'Minimum 6 characters' });
      required(f.confirmPassword, { message: 'Confirm password is required' });

      validate(f.confirmPassword, ({ value, valueOf, stateOf }) => {
        if (!stateOf(f.password).touched()) return undefined;
        if (value() !== valueOf(f.password)) {
          return { kind: 'passwordMismatch', message: 'Passwords do not match' };
        }
        return undefined;
      });
    },
    {
      submission: {
        action: async () => {
          try {
            await firstValueFrom(this.auth.register(this.registerModel()));
            this.router.navigate(['/']);
            return;
          } catch {
            return { kind: 'serverError', message: 'Registration failed' };
          }
        },
      },
    },
  );

}
