import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { computed } from '@angular/core';
import { Quiz } from '../../../models/quiz.model';
import { QuizService } from '../../../services/quiz.service';
import { apiError } from '../../../services/api-error';
@Component({ standalone: true, imports: [RouterLink], templateUrl: './available-quizzes.html', styleUrl: './available-quizzes.css' })
export class AvailableQuizzesComponent implements OnInit {
  private readonly service = inject(QuizService);
  readonly quizzes = signal<Quiz[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);
  readonly selected = signal<Quiz | null>(null);
  readonly selectedId = signal<number | null>(null);
  readonly detailsLoading = signal(false);
  readonly search = signal('');
  readonly filtered = computed(() => this.quizzes().filter(quiz =>
    `${quiz.title} ${quiz.description ?? ''}`.toLocaleLowerCase().includes(this.search().trim().toLocaleLowerCase())));
  ngOnInit() {
    this.service.getAvailableQuizzes().subscribe({
      next: q => { this.quizzes.set(q); this.loading.set(false); },
      error: e => { this.error.set(apiError(e)); this.loading.set(false); }
    });
  }
  details(id: number) {
    if (this.selectedId() === id) { this.selectedId.set(null); this.selected.set(null); return; }
    this.selectedId.set(id);
    this.selected.set(null);
    this.detailsLoading.set(true);
    this.error.set('');
    this.service.getTraineeQuizDetails(id).subscribe({
      next: quiz => { if (this.selectedId() === id) { this.selected.set(quiz); this.detailsLoading.set(false); } },
      error: error => { if (this.selectedId() === id) { this.error.set(apiError(error)); this.selectedId.set(null); this.detailsLoading.set(false); } }
    });
  }
}

