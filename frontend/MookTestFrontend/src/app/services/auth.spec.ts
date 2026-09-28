import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService, AuthResponse } from './auth.service';
import { authInterceptor } from './auth.interceptor';
import { authGuard } from './auth.guard';
import { QuizService } from './quiz.service';
describe('JWT session, interceptor and guards', () => {
  let http: HttpTestingController;
  let auth: AuthService;
  const response = (): AuthResponse => ({ accessToken: 'test-token',
    expiresAt: new Date(Date.now() + 3600000).toISOString(),
    user: { id: 1, email: 'student@example.com', displayName: 'Student', role: 'Trainee' } });
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [
      provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()
    ] });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });
  afterEach(() => { http.verify(); auth.logout(); });
  function login() { auth.login('student@example.com', 'password1234').subscribe(); http.expectOne('/api/auth/login').flush(response()); }
  it('stores a session and attaches bearer token to quiz requests', () => {
    login();
    TestBed.inject(QuizService).getAvailableQuizzes().subscribe();
    const request = http.expectOne('/api/participation/quizzes');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush([]);
  });
  it('does not attach existing credentials to registration', () => {
    login();
    auth.register('Student', 'new@example.com', 'password1234').subscribe();
    const request = http.expectOne('/api/auth/register');
    expect(request.request.headers.has('Authorization')).toBeFalse();
    expect(request.request.body.role).toBeUndefined();
    request.flush(response());
  });
  it('clears a rejected session and redirects to login', () => {
    login();
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    TestBed.inject(QuizService).getAvailableQuizzes().subscribe({ error: () => {} });
    http.expectOne('/api/participation/quizzes').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.token).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
  it('redirects guests and prevents a trainee from entering trainer pages', () => {
    const route = { data: { role: 'Trainer' } } as unknown as ActivatedRouteSnapshot;
    const state = {} as RouterStateSnapshot;
    const check = () => TestBed.runInInjectionContext(() => authGuard(route, state)) as UrlTree;
    expect(check().toString()).toBe('/login');
    login();
    expect(check().toString()).toBe('/trainee/quizzes');
    route.data = { role: 'Trainee' };
    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBeTrue();
  });
  it('drops expired stored sessions', () => {
    auth.logout();
    const data = response(); data.expiresAt = new Date(Date.now() - 1000).toISOString();
    sessionStorage.setItem('mooktest.auth', JSON.stringify(data));
    const restored = TestBed.runInInjectionContext(() => new AuthService());
    expect(restored.token).toBeNull();
    expect(restored.user()).toBeNull();
  });
});

