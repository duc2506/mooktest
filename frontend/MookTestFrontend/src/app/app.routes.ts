import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './services/auth.guard';
import { AuthPage } from './pages/auth/auth';
import { QuizListComponent } from './pages/trainer/quiz-list/quiz-list';
import { QuizFormComponent } from './pages/trainer/quiz-form/quiz-form';
import { QuizDetailComponent } from './pages/trainer/quiz-detail/quiz-detail';
import { AvailableQuizzesComponent } from './pages/trainee/available-quizzes/available-quizzes';
import { TakeQuizComponent } from './pages/trainee/take-quiz/take-quiz';
import { SubmissionsPage } from './pages/submissions/submissions';
import { UserManagementComponent } from './pages/trainer/user-management/user-management';
import { QuestionBankComponent } from './pages/trainer/question-bank/question-bank';
import { TraineeSubmissionsComponent } from './pages/trainee/submissions/trainee-submissions';
export const routes: Routes = [
  { path: 'login', component: AuthPage, canActivate: [guestGuard] },
  { path: 'register', component: AuthPage, canActivate: [guestGuard], data: { register: true } },
  { path: 'trainer', canActivateChild: [authGuard], data: { role: 'Trainer' }, children: [
    { path: 'quizzes', component: QuizListComponent },
    { path: 'quizzes/create', component: QuizFormComponent },
    { path: 'quizzes/edit/:id', component: QuizFormComponent },
    { path: 'quizzes/:id/submissions', component: SubmissionsPage },
    { path: 'quizzes/:id', component: QuizDetailComponent },
    { path: 'question-bank', component: QuestionBankComponent },
    { path: 'users', component: UserManagementComponent }
  ] },
  { path: 'trainee', canActivateChild: [authGuard], data: { role: 'Trainee' }, children: [
    { path: 'quizzes', component: AvailableQuizzesComponent },
    { path: 'quizzes/:id/take', component: TakeQuizComponent },
    { path: 'submissions', component: TraineeSubmissionsComponent }
  ] },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];
