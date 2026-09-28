import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { QuizDetailComponent } from './quiz-detail';
describe('Editing quiz questions', () => {
  it('sends edits to the existing question and surfaces locked-quiz errors', () => {
    TestBed.configureTestingModule({ imports: [QuizDetailComponent], providers: [
      provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } }
    ] });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(QuizDetailComponent);
    fixture.detectChanges();
    http.expectOne('/api/quizzes/1').flush({ quizId: 1, title: 'Test', questions: [] });
    fixture.componentInstance.editQuestion({ questionId: 2, content: 'Before', questionType: 2, answers: [] });
    fixture.componentInstance.questionContent = 'After';
    fixture.componentInstance.saveQuestion();
    const update = http.expectOne('/api/quizzes/1/questions/2');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body.content).toBe('After');
    update.flush({ message: 'Đề đã có lượt làm bài.' }, { status: 409, statusText: 'Conflict' });
    expect(fixture.componentInstance.error()).toContain('lượt làm bài');
    http.verify();
  });
});

