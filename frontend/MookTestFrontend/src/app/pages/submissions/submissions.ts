import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Submission } from '../../models/quiz.model';
import { QuizService } from '../../services/quiz.service';
import { apiError } from '../../services/api-error';
@Component({
  standalone: true, imports: [DatePipe, RouterLink],
  template: `<section class="page-container">
    <div class="page-header"><h1>{{ quizId ? 'Bài nộp của học viên' : 'Bài đã nộp' }}</h1>
      <a class="btn" [routerLink]="quizId ? '/trainer/quizzes' : '/trainee/quizzes'">Danh sách đề</a></div>
    @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
    @if (loading()) { <p role="status">Đang tải…</p> }
    @else {
      @for (submission of submissions(); track submission.quizSubmissionId) {
        <article class="question-card">
          <h2>{{ submission.title }}</h2>
          @if (submission.totalChoices > 0) {
            <p>Điểm trắc nghiệm: {{ (10 * submission.correctChoices / submission.totalChoices).toFixed(1) }}/10
              · Đúng {{ submission.correctChoices }}/{{ submission.totalChoices }} câu.</p>
          }
          @if (submission.unscoredTextQuestions > 0) {
            <p>{{ submission.unscoredTextQuestions }} câu trả lời văn bản chưa được chấm.</p>
          }
          <p>Mã bài: {{ submission.quizSubmissionId }} · {{ submission.submittedAt | date:'dd/MM/yyyy HH:mm' }}
            @if (quizId) { · {{ submission.traineeName || 'Bài nộp cũ' }} }</p>
          <details><summary>Xem câu trả lời</summary>
            @for (answer of submission.answers; track answer.questionId) {
              <div class="submitted-answer"><h3>{{ answer.content }}</h3>
                @for (selected of answer.selectedAnswers; track $index) { <p>• {{ selected }}</p> }
                @if (answer.responseText) { <p class="response-text">{{ answer.responseText }}</p> }
                @if (answer.correctAnswers?.length) { <p>Đáp án đúng: {{ answer.correctAnswers!.join(', ') }}</p> }
              </div>
            }
          </details>
        </article>
      } @empty { <p>Chưa có bài nộp.</p> }
    }
  </section>`
})
export class SubmissionsPage implements OnInit {
  private readonly service = inject(QuizService);
  readonly quizId = Number(inject(ActivatedRoute).snapshot.paramMap.get('id')) || undefined;
  readonly submissions = signal<Submission[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  ngOnInit() {
    this.service.getSubmissions(this.quizId).subscribe({
      next: data => { this.submissions.set(data); this.loading.set(false); },
      error: e => { this.error.set(apiError(e)); this.loading.set(false); }
    });
  }
}

