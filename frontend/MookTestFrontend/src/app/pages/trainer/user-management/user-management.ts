import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ManagedUser } from '../../../models/user.model';
import { apiError } from '../../../services/api-error';

@Component({ standalone: true, imports: [FormsModule], templateUrl: './user-management.html' })
export class UserManagementComponent implements OnInit {
  private readonly http = inject(HttpClient);
  readonly users = signal<ManagedUser[]>([]);
  readonly error = signal('');
  readonly busy = signal(false);
  editing: number | null = null;
  displayName = '';
  email = '';
  password = '';

  ngOnInit() { this.load(); }

  load() {
    this.http.get<ManagedUser[]>('/api/users').subscribe({
      next: users => { this.users.set(users); this.error.set(''); },
      error: error => this.error.set(apiError(error))
    });
  }

  edit(user: ManagedUser) {
    this.editing = user.id;
    this.displayName = user.displayName;
    this.email = user.email;
    this.password = '';
  }

  reset() {
    this.editing = null;
    this.displayName = '';
    this.email = '';
    this.password = '';
  }

  save() {
    if (this.busy()) return;
    if (!this.displayName.trim() || !this.email.trim() || (this.editing === null && this.password.length < 10)) {
      this.error.set('Vui lòng nhập đầy đủ thông tin hợp lệ.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const data = { displayName: this.displayName.trim(), email: this.email.trim(),
      ...(this.editing === null ? { password: this.password } : {}) };
    const request = this.editing === null
      ? this.http.post('/api/users', data)
      : this.http.put('/api/users/' + this.editing, data);
    request.subscribe({
      next: () => { this.reset(); this.busy.set(false); this.load(); },
      error: error => { this.busy.set(false); this.error.set(apiError(error)); }
    });
  }

  remove(id: number) {
    if (this.busy() || !confirm('Xóa học viên này?')) return;
    this.busy.set(true);
    this.error.set('');
    this.http.delete('/api/users/' + id).subscribe({
      next: () => { this.busy.set(false); this.load(); },
      error: error => { this.busy.set(false); this.error.set(apiError(error)); }
    });
  }
}
