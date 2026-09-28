import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Submission } from '../../../models/quiz.model';
import { QuizService } from '../../../services/quiz.service';
import { apiError } from '../../../services/api-error';

@Component({ standalone: true, imports: [DatePipe, RouterLink],
  templateUrl: './trainee-submissions.html', styleUrl: './trainee-submissions.css' })
export class TraineeSubmissionsComponent implements OnInit {
  private readonly service = inject(QuizService);
  readonly submissions = signal<Submission[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    this.service.getSubmissions().subscribe({
      next: data => { this.submissions.set(data); this.loading.set(false); },
      error: error => { this.error.set(apiError(error)); this.loading.set(false); }
    });
  }
}
