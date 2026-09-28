import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Question, QuestionType, StartedQuiz, SubmitAnswer, isChoiceQuestion } from '../../../models/quiz.model';
import { QuizService } from '../../../services/quiz.service';
import { AuthService } from '../../../services/auth.service';
import { apiError } from '../../../services/api-error';
@Component({ standalone: true, imports: [FormsModule, RouterLink], templateUrl: './take-quiz.html', styleUrl: './take-quiz.css' })
export class TakeQuizComponent implements OnInit, OnDestroy {
  private readonly service = inject(QuizService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));
  readonly quiz = signal<StartedQuiz | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly remaining = signal(0);
  readonly types = QuestionType;
  readonly isChoice = isChoiceQuestion;
  private readonly responseVersion = signal(0);
  readonly answeredCount = computed(() => {
    this.responseVersion();
    return this.quiz()?.questions.filter(question => this.isAnswered(question)).length ?? 0;
  });
  readonly progressPercent = computed(() => {
    const total = this.quiz()?.questions.length ?? 0;
    return total ? Math.round(this.answeredCount() / total * 100) : 0;
  });
  choices: Record<number, number[]> = {};
  texts: Record<number, string> = {};
  private timer?: ReturnType<typeof setInterval>;
  private draftKey = '';
  ngOnInit() {
    this.service.startQuiz(this.id).subscribe({
      next: quiz => {
        this.quiz.set(quiz);
        this.draftKey = 'mooktest.draft.' + this.auth.user()?.id + '.' + quiz.attemptId;
        try {
          const draft = JSON.parse(sessionStorage.getItem(this.draftKey) ?? 'null');
          this.choices = draft?.choices ?? {}; this.texts = draft?.texts ?? {};
          this.responseVersion.update(value => value + 1);
        } catch { /* Start with empty answers if unavailable. */ }
        this.tick(); this.timer = setInterval(() => this.tick(), 1000);
      },
      error: e => this.error.set(apiError(e))
    });
  }
  ngOnDestroy() { clearInterval(this.timer); }
  private tick() { this.remaining.set(Math.max(0, Math.ceil((Date.parse(this.quiz()!.expiresAt) - Date.now()) / 1000))); }
  get clock() { return Math.floor(this.remaining() / 60) + ':' + String(this.remaining() % 60).padStart(2, '0'); }
  isAnswered(question: Question) {
    return this.isChoice(question.questionType)
      ? (this.choices[question.questionId]?.length ?? 0) > 0
      : !!this.texts[question.questionId]?.trim();
  }
  goToQuestion(id: number) {
    const element = document.getElementById('question-' + id);
    element?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    element?.focus({ preventScroll: true });
  }
  choose(qid: number, aid: number, multiple: boolean, selected: boolean) {
    const previous = this.choices[qid] ?? [];
    this.choices[qid] = multiple ? (selected ? [...new Set([...previous, aid])] : previous.filter(id => id !== aid)) : [aid];
    this.saveDraft();
  }
  saveDraft() {
    this.responseVersion.update(value => value + 1);
    try { sessionStorage.setItem(this.draftKey, JSON.stringify({ choices: this.choices, texts: this.texts })); } catch { /* Optional draft. */ }
  }
  submitQuiz() {
    const quiz = this.quiz();
    if (!quiz || this.busy() || this.remaining() <= 0) return;
    const answers: SubmitAnswer[] = quiz.questions.map(q => ({
      questionId: q.questionId, answerIds: this.isChoice(q.questionType) ? (this.choices[q.questionId] ?? []) : [],
      responseText: this.isChoice(q.questionType) ? null : (this.texts[q.questionId]?.trim() ?? '')
    }));
    if (answers.some(a => a.answerIds.length === 0 && !a.responseText)) {
      this.error.set('Hãy trả lời tất cả câu hỏi trước khi nộp.'); return;
    }
    this.busy.set(true); this.error.set('');
    this.service.submitQuiz(quiz.quizId, { attemptId: quiz.attemptId, answers }).subscribe({
      next: () => {
        try { sessionStorage.removeItem(this.draftKey); } catch { /* Optional draft. */ }
        this.busy.set(false); void this.router.navigate(['/trainee/submissions']);
      },
      error: e => { this.busy.set(false); this.error.set(apiError(e)); }
    });
  }
}

