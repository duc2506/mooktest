export enum QuestionType {
  MultipleChoice = 1, SingleChoice = 2, TrueFalse = 3,
  FillInTheBlanks = 4, ShortAnswer = 5, LongAnswer = 6
}
export const questionTypes = [
  { value: QuestionType.MultipleChoice, label: 'Chọn nhiều đáp án' },
  { value: QuestionType.SingleChoice, label: 'Chọn một đáp án' },
  { value: QuestionType.TrueFalse, label: 'Đúng / Sai' },
  { value: QuestionType.FillInTheBlanks, label: 'Điền chỗ trống' },
  { value: QuestionType.ShortAnswer, label: 'Trả lời ngắn' },
  { value: QuestionType.LongAnswer, label: 'Tự luận' }
];
export const isChoiceQuestion = (type: QuestionType) =>
  [QuestionType.MultipleChoice, QuestionType.SingleChoice, QuestionType.TrueFalse].includes(type);
export interface Answer { answerId: number; content: string; isCorrect?: boolean; }
export interface Question { questionId: number; bankQuestionId?: number | null; content: string; questionType: QuestionType; answers: Answer[]; }
export interface BankQuestionRequest { content: string; questionType: QuestionType; answers: CreateAnswerRequest[]; }
export interface Quiz { quizId: number; title: string; description: string | null; duration: number; showAnswersAfterSubmit: boolean; questions: Question[]; }
export interface StartedQuiz extends Quiz { attemptId: string; startedAt: string; expiresAt: string; }
export interface CreateQuizRequest { title: string; description: string | null; duration: number; showAnswersAfterSubmit: boolean; }
export type UpdateQuizRequest = CreateQuizRequest;
export interface CreateQuestionRequest { content: string; questionType: QuestionType; }
export interface CreateAnswerRequest { content: string; isCorrect: boolean; }
export interface SubmitAnswer { questionId: number; answerIds: number[]; responseText: string | null; }
export interface SubmitQuizRequest { attemptId: string; answers: SubmitAnswer[]; }
export interface Submission {
  quizSubmissionId: number; quizId: number; title: string; traineeName: string | null; submittedAt: string;
  correctChoices: number; totalChoices: number; unscoredTextQuestions: number; showCorrectAnswers: boolean;
  answers: { questionId: number; content: string; selectedAnswers: string[]; responseText: string | null;
    correctAnswers: string[] | null }[];
}

