import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  private auth = inject(AuthService);

  isLoggedIn = this.auth.isLoggedIn;
  currentUser = this.auth.currentUser;

  onLogout() {
    this.auth.logout().subscribe();
  }
}
