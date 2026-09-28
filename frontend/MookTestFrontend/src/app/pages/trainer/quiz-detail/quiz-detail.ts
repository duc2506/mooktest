import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Answer, Question, QuestionType, Quiz, questionTypes, isChoiceQuestion } from '../../../models/quiz.model';
import { QuizService } from '../../../services/quiz.service';
import { apiError } from '../../../services/api-error';
@Component({ standalone: true, imports: [FormsModule, RouterLink], templateUrl: './quiz-detail.html' })
export class QuizDetailComponent implements OnInit {
  private readonly service = inject(QuizService);
  private readonly http = inject(HttpClient);
  readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));
  readonly quiz = signal<Quiz | null>(null);
  readonly error = signal('');
  readonly success = signal('');
  readonly busy = signal(false);
  readonly types = questionTypes;
  readonly isChoice = isChoiceQuestion;
  readonly showBank = signal(false);
  readonly bankQuestions = signal<Question[]>([]);
  readonly bankLoading = signal(false);
  readonly bankSearch = signal('');
  readonly selectedBankIds = signal<number[]>([]);
  readonly availableBankQuestions = computed(() => this.bankQuestions().filter(bank =>
    !this.quiz()?.questions.some(question => question.bankQuestionId === bank.questionId) &&
    bank.content.toLocaleLowerCase().includes(this.bankSearch().trim().toLocaleLowerCase())));
  private bankLoaded = false;
  questionId: number | null = null;
  questionContent = '';
  questionType = QuestionType.SingleChoice;
  answerQuestionId: number | null = null;
  answerId: number | null = null;
  answerContent = '';
  answerCorrect = false;
  ngOnInit() { this.load(); }
  load() {
    this.service.getQuizById(this.id).subscribe({ next: q => this.quiz.set(q), error: e => this.error.set(apiError(e)) });
  }
  typeName(type: QuestionType) { return this.types.find(t => t.value === type)?.label; }
  toggleBank() {
    this.showBank.update(value => !value);
    if (this.bankLoaded || !this.showBank()) return;
    this.bankLoading.set(true);
    this.http.get<Question[]>('/api/question-bank').subscribe({
      next: questions => { this.bankQuestions.set(questions); this.bankLoaded = true; this.bankLoading.set(false); },
      error: error => { this.error.set(apiError(error)); this.bankLoading.set(false); }
    });
  }
  toggleBankSelection(id: number, checked: boolean) {
    this.selectedBankIds.update(ids => checked ? [...ids, id] : ids.filter(value => value !== id));
  }
  importFromBank() {
    const ids = this.selectedBankIds();
    if (!ids.length) { this.error.set('Hãy chọn ít nhất một câu hỏi.'); return; }
    this.mutate(this.http.post('/api/quizzes/' + this.id + '/questions/from-bank', { questionIds: ids }), () => {
      this.selectedBankIds.set([]); this.showBank.set(false);
    });
  }
  saveToBank(question: Question) {
    const data = { content: question.content, questionType: question.questionType,
      answers: question.answers.map(answer => ({ content: answer.content, isCorrect: !!answer.isCorrect })) };
    this.mutate(this.http.post('/api/question-bank', data), () => {
      this.success.set('Đã lưu câu hỏi vào ngân hàng.'); this.bankLoaded = false; this.showBank.set(false);
    });
  }
  editQuestion(q: Question) { this.questionId = q.questionId; this.questionContent = q.content; this.questionType = q.questionType; }
  resetQuestion() { this.questionId = null; this.questionContent = ''; this.questionType = QuestionType.SingleChoice; }
  saveQuestion() {
    if (!this.questionContent.trim()) { this.error.set('Nhập nội dung câu hỏi.'); return; }
    const data = { content: this.questionContent.trim(), questionType: this.questionType };
    this.mutate(this.questionId ? this.service.updateQuestion(this.id, this.questionId, data)
      : this.service.addQuestion(this.id, data), () => this.resetQuestion());
  }
  deleteQuestion(id: number) {
    if (confirm('Xóa câu hỏi và các đáp án?')) this.mutate(this.service.deleteQuestion(this.id, id), () => {
      this.resetQuestion(); this.answerQuestionId = null;
    });
  }
  editAnswer(qid: number, answer?: Answer) {
    this.answerQuestionId = qid; this.answerId = answer?.answerId ?? null;
    this.answerContent = answer?.content ?? ''; this.answerCorrect = answer?.isCorrect ?? false;
  }
  saveAnswer() {
    if (!this.answerQuestionId || !this.answerContent.trim()) { this.error.set('Nhập nội dung đáp án.'); return; }
    const data = { content: this.answerContent.trim(), isCorrect: this.answerCorrect };
    this.mutate(this.answerId ? this.service.updateAnswer(this.id, this.answerQuestionId, this.answerId, data)
      : this.service.addAnswer(this.id, this.answerQuestionId, data), () => { this.answerQuestionId = null; });
  }
  deleteAnswer(qid: number, aid: number) {
    if (confirm('Xóa đáp án?')) this.mutate(this.service.deleteAnswer(this.id, qid, aid), () => { this.answerQuestionId = null; });
  }
  private mutate(request: Observable<unknown>, done: () => void) {
    if (this.busy()) return;
    this.busy.set(true); this.error.set(''); this.success.set('');
    request.subscribe({
      next: () => { done(); this.busy.set(false); this.load(); },
      error: e => { this.busy.set(false); this.error.set(apiError(e)); }
    });
  }
}

