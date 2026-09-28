import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { map } from 'rxjs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { QuizService } from '../../../services/quiz.service';
import { apiError } from '../../../services/api-error';
@Component({ standalone: true, imports: [FormsModule, RouterLink], templateUrl: './quiz-form.html' })
export class QuizFormComponent implements OnInit {
  private readonly service = inject(QuizService);
  private readonly router = inject(Router);
  readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id')) || null;
  readonly draft = signal({ title: '', description: '', duration: 30 });
  readonly busy = signal(false);
  readonly error = signal('');
  ngOnInit() {
    if (this.id) {
      this.busy.set(true);
      this.service.getQuizById(this.id).subscribe({
        next: q => { this.draft.set({ title: q.title, description: q.description ?? '', duration: q.duration }); this.busy.set(false); },
        error: e => { this.error.set(apiError(e)); this.busy.set(false); }
      });
    }
  }
  submit() {
    if (this.busy()) return;
    const data = { ...this.draft(), title: this.draft().title.trim() };
    if (!data.title || !Number.isInteger(data.duration) || data.duration < 1) {
      this.error.set('Nhập tên đề và thời gian là số phút nguyên dương.'); return;
    }
    this.busy.set(true); this.error.set('');
    const request = this.id
      ? this.service.updateQuiz(this.id, data).pipe(map(() => this.id!))
      : this.service.createQuiz(data).pipe(map(quiz => quiz.quizId));
    request.subscribe({
      next: id => { this.busy.set(false); void this.router.navigate(['/trainer/quizzes', id]); },
      error: e => { this.busy.set(false); this.error.set(apiError(e)); }
    });
  }
}

