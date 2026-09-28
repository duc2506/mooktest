import { HttpClient } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BankQuestionRequest, Question, QuestionType, isChoiceQuestion, questionTypes } from '../../../models/quiz.model';
import { apiError } from '../../../services/api-error';

@Component({ standalone: true, imports: [FormsModule], templateUrl: './question-bank.html' })
export class QuestionBankComponent implements OnInit {
  private readonly http = inject(HttpClient);
  readonly questions = signal<Question[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly search = signal('');
  readonly filtered = computed(() => this.questions().filter(q =>
    q.content.toLocaleLowerCase().includes(this.search().trim().toLocaleLowerCase())));
  readonly types = questionTypes;
  readonly trueFalseType = QuestionType.TrueFalse;
  readonly isChoice = isChoiceQuestion;
  editingId: number | null = null;
  content = '';
  questionType = QuestionType.SingleChoice;
  answers: { content: string; isCorrect: boolean }[] = this.emptyAnswers();

  ngOnInit() { this.load(); }
  load() {
    this.loading.set(true);
    this.http.get<Question[]>('/api/question-bank').subscribe({
      next: data => { this.questions.set(data); this.loading.set(false); },
      error: error => { this.error.set(apiError(error)); this.loading.set(false); }
    });
  }
  typeName(type: QuestionType) { return this.types.find(item => item.value === type)?.label ?? 'Câu hỏi'; }
  emptyAnswers() { return [{ content: '', isCorrect: true }, { content: '', isCorrect: false }]; }
  changeType(type: QuestionType) {
    this.questionType = type;
    this.answers = isChoiceQuestion(type) ? this.emptyAnswers() : [];
  }
  addAnswer() { if (this.answers.length < 20) this.answers.push({ content: '', isCorrect: false }); }
  removeAnswer(index: number) { if (this.answers.length > 2) this.answers.splice(index, 1); }
  setCorrect(index: number, checked: boolean) {
    if (this.questionType !== QuestionType.MultipleChoice && checked)
      this.answers.forEach(answer => answer.isCorrect = false);
    this.answers[index].isCorrect = checked;
  }
  edit(question: Question) {
    this.editingId = question.questionId;
    this.content = question.content;
    this.questionType = question.questionType;
    this.answers = question.answers.map(answer => ({ content: answer.content, isCorrect: !!answer.isCorrect }));
    this.error.set(''); this.success.set('');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  reset() {
    this.editingId = null; this.content = ''; this.questionType = QuestionType.SingleChoice;
    this.answers = this.emptyAnswers();
  }
  save() {
    if (this.busy()) return;
    const content = this.content.trim();
    const answers = this.answers.map(answer => ({ content: answer.content.trim(), isCorrect: answer.isCorrect }));
    if (!content) { this.error.set('Hãy nhập nội dung câu hỏi.'); return; }
    if (isChoiceQuestion(this.questionType) && (answers.length < 2 || answers.some(a => !a.content) ||
        !answers.some(a => a.isCorrect) ||
        (this.questionType !== QuestionType.MultipleChoice && answers.filter(a => a.isCorrect).length !== 1) ||
        (this.questionType === QuestionType.TrueFalse && answers.length !== 2))) {
      this.error.set('Cần ít nhất 2 đáp án và số đáp án đúng phù hợp với loại câu hỏi.'); return;
    }
    const data: BankQuestionRequest = { content, questionType: this.questionType,
      answers: isChoiceQuestion(this.questionType) ? answers : [] };
    this.busy.set(true); this.error.set(''); this.success.set('');
    const request = this.editingId === null
      ? this.http.post<Question>('/api/question-bank', data)
      : this.http.put<Question>('/api/question-bank/' + this.editingId, data);
    request.subscribe({
      next: () => { this.busy.set(false); this.success.set('Đã lưu câu hỏi vào ngân hàng.'); this.reset(); this.load(); },
      error: error => { this.busy.set(false); this.error.set(apiError(error)); }
    });
  }
  remove(question: Question) {
    if (this.busy() || !confirm('Xóa câu hỏi khỏi ngân hàng? Các đề đã lấy câu này vẫn giữ bản sao.')) return;
    this.busy.set(true); this.error.set(''); this.success.set('');
    this.http.delete('/api/question-bank/' + question.questionId).subscribe({
      next: () => { this.busy.set(false); if (this.editingId === question.questionId) this.reset(); this.load(); },
      error: error => { this.busy.set(false); this.error.set(apiError(error)); }
    });
  }
}
