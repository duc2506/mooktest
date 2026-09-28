import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthPage } from './auth';

describe('AuthPage login form', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [AuthPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('lets users click login and explains invalid fields without sending a request', async () => {
    const fixture = TestBed.createComponent(AuthPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBeFalse();

    button.click();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Vui lòng nhập email hợp lệ.');
    expect(fixture.nativeElement.textContent).toContain('Mật khẩu cần từ 10 đến 128 ký tự.');
    TestBed.inject(HttpTestingController).expectNone('/api/auth/login');
  });

  it('submits browser-autofilled values and shows a server error when credentials are rejected', async () => {
    const fixture = TestBed.createComponent(AuthPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const email = fixture.nativeElement.querySelector('#email') as HTMLInputElement;
    const password = fixture.nativeElement.querySelector('#password') as HTMLInputElement;
    email.value = 'trainer@example.com';
    password.value = 'password1234';

    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ email: 'trainer@example.com', password: 'password1234' });
    request.flush({ message: 'Email hoặc mật khẩu không đúng.' }, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Email hoặc mật khẩu không đúng.');
    expect((fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBeFalse();
  });

  it('opens trainer quizzes after a successful trainer login', async () => {
    const fixture = TestBed.createComponent(AuthPage);
    const navigate = spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
    fixture.detectChanges();
    await fixture.whenStable();
    const email = fixture.nativeElement.querySelector('#email') as HTMLInputElement;
    const password = fixture.nativeElement.querySelector('#password') as HTMLInputElement;
    email.value = 'trainer@example.com';
    password.value = 'password1234';

    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/login');
    request.flush({
      accessToken: 'trainer-token', expiresAt: new Date(Date.now() + 3600000).toISOString(),
      user: { id: 1, email: 'trainer@example.com', displayName: 'Trainer', role: 'Trainer' }
    });
    expect(navigate).toHaveBeenCalledWith('/trainer/quizzes');
  });
});
