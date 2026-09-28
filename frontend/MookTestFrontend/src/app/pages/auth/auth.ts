import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { apiError } from '../../services/api-error';
@Component({
  standalone: true, imports: [FormsModule, RouterLink],
  template: `
    <section class="form-page">
      <h1>{{ registering ? 'Đăng ký học viên' : 'Đăng nhập' }}</h1>
      <form #form="ngForm" (ngSubmit)="submit()" >
        @if (registering) {
          <div class="form-group"><label for="name">Họ tên</label>
          <input id="name" name="displayName" [(ngModel)]="displayName" required minlength="2" maxlength="100" autocomplete="name"></div>
        }
        <div class="form-group"><label for="email">Email</label>
          <input id="email" type="email" name="email" [(ngModel)]="email" required email maxlength="254" autocomplete="username"></div>
        <div class="form-group"><label for="password">Mật khẩu (10–128 ký tự)</label>
          <div class="password-field">
            <input id="password" [type]="showPassword ? 'text' : 'password'" name="password" [(ngModel)]="password" required minlength="10" maxlength="128"
              [autocomplete]="registering ? 'new-password' : 'current-password'">
            <button class="password-toggle" type="button" [attr.aria-label]="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
              [attr.aria-pressed]="showPassword" (click)="showPassword = !showPassword">
              {{ showPassword ? 'Ẩn' : 'Hiện' }}
            </button>
          </div>
        </div>
        @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        <button class="btn btn-primary" [disabled]="form.invalid || busy()">{{ busy() ? 'Đang xử lý…' : (registering ? 'Đăng ký' : 'Đăng nhập') }}</button>
      </form>
      <p><a [routerLink]="registering ? '/login' : '/register'">{{ registering ? 'Đã có tài khoản? Đăng nhập' : 'Tạo tài khoản học viên' }}</a></p>
    </section>`
})
export class AuthPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly registering = inject(ActivatedRoute).snapshot.data['register'] === true;
  displayName = ''; email = ''; password = ''; showPassword = false;
  readonly busy = signal(false);
  readonly error = signal('');
  submit() {
    if (this.busy()) return;
    this.busy.set(true); this.error.set('');
    const request = this.registering ? this.auth.register(this.displayName.trim(), this.email.trim(), this.password)
      : this.auth.login(this.email.trim(), this.password);
    request.subscribe({
      next: () => { this.busy.set(false); void this.router.navigateByUrl(this.auth.landingPage); },
      error: error => { this.busy.set(false); this.error.set(apiError(error)); }
    });
  }
}

