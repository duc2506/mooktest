import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AvailableQuizzesComponent } from './available-quizzes';
describe('Trainee quiz list', () => {
  it('opens details without starting a timed attempt', () => {
    TestBed.configureTestingModule({ imports: [AvailableQuizzesComponent], providers: [
      provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([])
    ] });
    const fixture = TestBed.createComponent(AvailableQuizzesComponent);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/participation/quizzes').flush([]);
    fixture.componentInstance.details(7);
    http.expectOne('/api/participation/quizzes/7').flush({ quizId: 7, title: 'Test', duration: 30 });
    expect(fixture.componentInstance.selected()?.quizId).toBe(7);
    http.expectNone('/api/participation/quizzes/7/start');
    http.verify();
  });
});

