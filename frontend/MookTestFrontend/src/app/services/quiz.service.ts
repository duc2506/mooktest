import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Quiz, Question, Answer, CreateQuizRequest, CreateQuestionRequest,
  CreateAnswerRequest, SubmitQuizRequest, StartedQuiz, Submission } from '../models/quiz.model';

@Injectable({ providedIn: 'root' })
export class QuizService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api';
  getQuizzes() { return this.http.get<Quiz[]>(this.api + '/quizzes'); }
  getQuizById(id: number) { return this.http.get<Quiz>(this.api + '/quizzes/' + id); }
  createQuiz(data: CreateQuizRequest) { return this.http.post<Quiz>(this.api + '/quizzes', data); }
  updateQuiz(id: number, data: CreateQuizRequest) { return this.http.put(this.api + '/quizzes/' + id, data); }
  deleteQuiz(id: number) { return this.http.delete(this.api + '/quizzes/' + id); }
  addQuestion(id: number, data: CreateQuestionRequest) {
    return this.http.post<Question>(this.api + '/quizzes/' + id + '/questions', data);
  }
  updateQuestion(id: number, qid: number, data: CreateQuestionRequest) {
    return this.http.put<Question>(this.api + '/quizzes/' + id + '/questions/' + qid, data);
  }
  deleteQuestion(id: number, qid: number) {
    return this.http.delete(this.api + '/quizzes/' + id + '/questions/' + qid);
  }
  addAnswer(id: number, qid: number, data: CreateAnswerRequest) {
    return this.http.post<Answer>(this.api + '/quizzes/' + id + '/questions/' + qid + '/answers', data);
  }
  updateAnswer(id: number, qid: number, aid: number, data: CreateAnswerRequest) {
    return this.http.put<Answer>(this.api + '/quizzes/' + id + '/questions/' + qid + '/answers/' + aid, data);
  }
  deleteAnswer(id: number, qid: number, aid: number) {
    return this.http.delete(this.api + '/quizzes/' + id + '/questions/' + qid + '/answers/' + aid);
  }
  getAvailableQuizzes() { return this.http.get<Quiz[]>(this.api + '/participation/quizzes'); }
  getTraineeQuizDetails(id: number) { return this.http.get<Quiz>(this.api + '/participation/quizzes/' + id); }
  startQuiz(id: number) { return this.http.post<StartedQuiz>(this.api + '/participation/quizzes/' + id + '/start', {}); }
  submitQuiz(id: number, data: SubmitQuizRequest) {
    return this.http.post<Submission>(this.api + '/participation/quizzes/' + id + '/submit', data);
  }
  getSubmissions(quizId?: number) {
    return this.http.get<Submission[]>(quizId === undefined ? this.api + '/participation/submissions'
      : this.api + '/quizzes/' + quizId + '/submissions');
  }
}

