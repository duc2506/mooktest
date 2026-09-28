import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { QuizFormComponent } from './quiz-form';
describe('Quiz form validation', () => {
  it('rejects blank titles and fractional durations before sending', () => {
    TestBed.configureTestingModule({ imports: [QuizFormComponent], providers: [
      provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([])
    ] });
    const component = TestBed.createComponent(QuizFormComponent).componentInstance;
    component.draft.set({ title: ' ', description: '', duration: 1 });
    component.submit();
    expect(component.error()).toBeTruthy();
    component.draft.set({ title: 'Test', description: '', duration: 1.5 });
    component.submit();
    expect(component.error()).toContain('nguyên dương');
    TestBed.inject(HttpTestingController).expectNone('/api/quizzes');
  });
});

