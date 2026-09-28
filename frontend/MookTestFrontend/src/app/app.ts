import { Component, ElementRef, HostListener, ViewChild, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';
import { apiError } from './services/api-error';
@Component({
  selector: 'app-root', standalone: true, imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './app.html', styleUrl: './app.css'
})
export class App {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  @ViewChild('accountMenu') private accountMenu?: ElementRef<HTMLElement>;
  readonly accountOpen = signal(false);
  readonly accountView = signal<'menu' | 'profile' | 'password'>('menu');
  readonly passwordBusy = signal(false);
  readonly passwordError = signal('');
  readonly passwordSuccess = signal('');
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  constructor() {
    effect(() => {
      if (!this.auth.user() && /\/(trainer|trainee)(\/|$)/.test(this.router.url))
        void this.router.navigate(['/login']);
    });
  }
  toggleAccount() {
    if (this.accountOpen()) this.closeAccount();
    else { this.accountView.set('menu'); this.accountOpen.set(true); }
  }

  showAccountView(view: 'menu' | 'profile' | 'password') {
    this.accountView.set(view);
    this.passwordError.set('');
    this.passwordSuccess.set('');
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
  }

  closeAccount() {
    if (this.passwordBusy()) return;
    this.accountOpen.set(false);
    this.showAccountView('menu');
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (this.accountOpen() && !this.accountMenu?.nativeElement.contains(event.target as Node))
      this.closeAccount();
  }

  @HostListener('document:keydown.escape')
  onEscape() { this.closeAccount(); }

  changePassword() {
    if (this.passwordBusy()) return;
    this.passwordError.set('');
    this.passwordSuccess.set('');
    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.passwordError.set('Vui lòng điền đầy đủ các trường.'); return;
    }
    if (this.newPassword.length < 10) {
      this.passwordError.set('Mật khẩu mới cần ít nhất 10 ký tự.'); return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError.set('Xác nhận mật khẩu không khớp.'); return;
    }
    if (this.currentPassword === this.newPassword) {
      this.passwordError.set('Mật khẩu mới phải khác mật khẩu hiện tại.'); return;
    }
    this.passwordBusy.set(true);
    this.http.post<void>('/api/auth/change-password', {
      currentPassword: this.currentPassword, newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.passwordBusy.set(false);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.passwordSuccess.set('Đổi mật khẩu thành công.');
      },
      error: error => { this.passwordBusy.set(false); this.passwordError.set(apiError(error)); }
    });
  }

  logout() { this.closeAccount(); this.auth.logout(); void this.router.navigate(['/login']); }
}
