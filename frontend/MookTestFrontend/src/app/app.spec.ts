import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { App } from './app';
import { AuthService } from './services/auth.service';

describe('Navigation permissions', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ imports: [App], providers: [
      provideZonelessChangeDetection(), provideRouter([]), provideHttpClient()
    ] });
  });

  it('shows login and registration to guests', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Đăng nhập');
    expect(text).toContain('Đăng ký');
    expect(text).not.toContain('Đăng xuất');
  });

  it('shows trainer navigation but not trainee history', () => {
    const auth = TestBed.inject(AuthService);
    spyOn(auth, 'user').and.returnValue({ id: 1, email: 't@example.com', displayName: 'Trainer', role: 'Trainer' });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Ngân hàng câu hỏi');
    expect(text).toContain('Đề kiểm tra');
    expect(text).toContain('Người dùng');
    expect(text).not.toContain('Bài đã nộp');
    fixture.nativeElement.querySelector('.account-trigger').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Thông tin tài khoản');
    expect(fixture.nativeElement.textContent).toContain('Đổi mật khẩu');
    expect(fixture.nativeElement.textContent).toContain('Đăng xuất');
  });

  it('shows account options to trainees too', () => {
    const auth = TestBed.inject(AuthService);
    spyOn(auth, 'user').and.returnValue({ id: 2, email: 'student@example.com', displayName: 'Student', role: 'Trainee' });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.account-trigger').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Đổi mật khẩu');
    expect(fixture.nativeElement.textContent).toContain('Đăng xuất');
    const profileButton = Array.from(fixture.nativeElement.querySelectorAll('.account-panel-actions button'))
      .find((button: any) => button.textContent.includes('Thông tin tài khoản')) as HTMLButtonElement;
    profileButton.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('student@example.com');
    expect(fixture.nativeElement.textContent).toContain('Học viên');
  });
});
