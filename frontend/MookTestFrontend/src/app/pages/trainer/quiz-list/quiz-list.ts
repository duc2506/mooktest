import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { computed } from '@angular/core';
import { Quiz } from '../../../models/quiz.model';
import { QuizService } from '../../../services/quiz.service';
import { apiError } from '../../../services/api-error';
@Component({ standalone: true, imports: [RouterLink], templateUrl: './quiz-list.html' })
export class QuizListComponent implements OnInit {
  private readonly service = inject(QuizService);
  readonly quizzes = signal<Quiz[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly search = signal('');
  readonly filteredQuizzes = computed(() => this.quizzes().filter(quiz =>
    `${quiz.title} ${quiz.description ?? ''}`.toLocaleLowerCase().includes(this.search().trim().toLocaleLowerCase())));
  ngOnInit() { this.load(); }
  load() {
    this.loading.set(true);
    this.service.getQuizzes().subscribe({
      next: data => { this.quizzes.set(data); this.loading.set(false); },
      error: error => { this.error.set(apiError(error)); this.loading.set(false); }
    });
  }
  deleteQuiz(id: number) {
    if (!confirm('Xóa đề kiểm tra này?')) return;
    this.error.set('');
    this.service.deleteQuiz(id).subscribe({ next: () => this.load(), error: e => this.error.set(apiError(e)) });
  }
}

