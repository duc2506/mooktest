import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TakeQuizComponent } from './take-quiz';
describe('Taking a quiz', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ imports: [TakeQuizComponent], providers: [
      provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } }
    ] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('submits multiple selections and text with the server attempt ID', () => {
    const fixture = TestBed.createComponent(TakeQuizComponent);
    fixture.detectChanges();
    const start = http.expectOne('/api/participation/quizzes/1/start');
    expect(start.request.method).toBe('POST');
    start.flush({ quizId: 1, title: 'Test', description: '', duration: 30, attemptId: 'attempt-1',
      startedAt: new Date().toISOString(), expiresAt: new Date(Date.now() + 60000).toISOString(),
      questions: [{ questionId: 1, content: 'Multiple', questionType: 1, answers: [] },
        { questionId: 2, content: 'Essay', questionType: 6, answers: [] }] });
    const component = fixture.componentInstance;
    component.choose(1, 10, true, true);
    component.choose(1, 11, true, true);
    component.texts[2] = '  Written response  ';
    component.submitQuiz();
    const submit = http.expectOne('/api/participation/quizzes/1/submit');
    expect(submit.request.body).toEqual({ attemptId: 'attempt-1', answers: [
      { questionId: 1, answerIds: [10, 11], responseText: null },
      { questionId: 2, answerIds: [], responseText: 'Written response' }
    ] });
    submit.flush({ quizSubmissionId: 1, quizId: 1, title: 'Test', correctChoices: 1,
      totalChoices: 1, unscoredTextQuestions: 1, showCorrectAnswers: false, answers: [] });
    expect(component.submission()?.correctChoices).toBe(1);
    fixture.destroy();
  });
  it('prevents submitting incomplete responses', () => {
    const fixture = TestBed.createComponent(TakeQuizComponent);
    fixture.detectChanges();
    http.expectOne('/api/participation/quizzes/1/start').flush({
      quizId: 1, attemptId: 'a', expiresAt: new Date(Date.now() + 60000).toISOString(),
      questions: [{ questionId: 1, content: 'Essay', questionType: 6, answers: [] }]
    });
    fixture.componentInstance.submitQuiz();
    expect(fixture.componentInstance.error()).toContain('tất cả câu hỏi');
    http.expectNone('/api/participation/quizzes/1/submit');
    fixture.destroy();
  });
});

