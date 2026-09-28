import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

export type Role = 'Trainer' | 'Trainee';
export interface User { id: number; email: string; displayName: string; role: Role; }
export interface AuthResponse { accessToken: string; expiresAt: string; user: User; }
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<AuthResponse | null>(this.readSession());
  readonly user = computed(() => this.session()?.user ?? null);
  private expiryTimer?: ReturnType<typeof setTimeout>;
  constructor() { this.scheduleExpiry(); }
  get token(): string | null {
    const session = this.session();
    if (!session || Date.parse(session.expiresAt) <= Date.now()) {
      if (session) this.logout();
      return null;
    }
    return session.accessToken;
  }
  get landingPage(): string { return this.user()?.role === 'Trainer' ? '/trainer/quizzes' : '/trainee/quizzes'; }
  login(email: string, password: string) {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(tap(s => this.save(s)));
  }
  register(displayName: string, email: string, password: string) {
    return this.http.post<AuthResponse>('/api/auth/register', { displayName, email, password }).pipe(tap(s => this.save(s)));
  }
  logout() {
    clearTimeout(this.expiryTimer);
    this.session.set(null);
    try { sessionStorage.removeItem('mooktest.auth'); } catch { /* Storage may be disabled. */ }
  }
  private save(session: AuthResponse) {
    this.session.set(session);
    try { sessionStorage.setItem('mooktest.auth', JSON.stringify(session)); } catch { /* Keep in memory. */ }
    this.scheduleExpiry();
  }
  private readSession(): AuthResponse | null {
    try {
      const data = JSON.parse(sessionStorage.getItem('mooktest.auth') ?? 'null') as AuthResponse | null;
      return data?.accessToken && ['Trainer', 'Trainee'].includes(data.user?.role) &&
        Date.parse(data.expiresAt) > Date.now() ? data : null;
    } catch { return null; }
  }
  private scheduleExpiry() {
    clearTimeout(this.expiryTimer);
    const session = this.session();
    if (session) this.expiryTimer = setTimeout(() => this.logout(), Math.max(0, Date.parse(session.expiresAt) - Date.now()));
  }
}

