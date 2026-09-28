import { Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { apiError } from '../../services/api-error';
@Component({
  standalone: true, imports: [FormsModule, RouterLink],
  template: `
    <section class="form-page">
      <h1>{{ registering ? 'Đăng ký học viên' : 'Đăng nhập' }}</h1>
      <form #form="ngForm" (ngSubmit)="submit(form, $event)" novalidate>
        @if (registering) {
          <div class="form-group"><label for="name">Họ tên</label>
          <input id="name" name="displayName" [(ngModel)]="displayName" #nameControl="ngModel" required minlength="2" maxlength="100" autocomplete="name">
          @if (nameControl.touched && nameControl.invalid) { <small class="error">Họ tên cần từ 2 đến 100 ký tự.</small> }
          </div>
        }
        <div class="form-group"><label for="email">Email</label>
          <input id="email" type="email" name="email" [(ngModel)]="email" #emailControl="ngModel" required email maxlength="254" autocomplete="username">
          @if (emailControl.touched && emailControl.invalid) { <small class="error">Vui lòng nhập email hợp lệ.</small> }
          </div>
        <div class="form-group"><label for="password">Mật khẩu (10–128 ký tự)</label>
          <div class="password-field">
            <input id="password" [type]="showPassword ? 'text' : 'password'" name="password" [(ngModel)]="password" #passwordControl="ngModel" required minlength="10" maxlength="128"
              [autocomplete]="registering ? 'new-password' : 'current-password'">
            <button class="password-toggle" type="button" [attr.aria-label]="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
              [attr.aria-pressed]="showPassword" (click)="showPassword = !showPassword">
              {{ showPassword ? 'Ẩn' : 'Hiện' }}
            </button>
          </div>
          @if (passwordControl.touched && passwordControl.invalid) { <small class="error">Mật khẩu cần từ 10 đến 128 ký tự.</small> }
        </div>
        @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        <button type="submit" class="btn btn-primary" [disabled]="busy()">{{ busy() ? 'Đang xử lý…' : (registering ? 'Đăng ký' : 'Đăng nhập') }}</button>
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
  submit(form: NgForm, event: Event) {
    if (this.busy()) return;
    // Autofill can update visible fields without notifying ngModel.
    const fields = new FormData(event.target as HTMLFormElement);
    this.email = String(fields.get('email') ?? '').trim();
    this.password = String(fields.get('password') ?? '');
    if (this.registering) this.displayName = String(fields.get('displayName') ?? '').trim();
    form.control.patchValue({ email: this.email, password: this.password,
      ...(this.registering ? { displayName: this.displayName } : {}) });
    if (!form.controls['email'] || !form.controls['password'] ||
        (this.registering && !form.controls['displayName']) || form.invalid ||
        !this.email || this.password.length < 10 || this.password.length > 128 ||
        (this.registering && this.displayName.trim().length < 2)) {
      form.control.markAllAsTouched();
      this.error.set('Vui lòng kiểm tra các thông tin đã nhập.');
      return;
    }
    this.busy.set(true); this.error.set('');
    const request = this.registering ? this.auth.register(this.displayName.trim(), this.email.trim(), this.password)
      : this.auth.login(this.email.trim(), this.password);
    request.subscribe({
      next: () => { this.busy.set(false); void this.router.navigateByUrl(this.auth.landingPage); },
      error: error => { this.busy.set(false); this.error.set(apiError(error)); }
    });
  }
}

