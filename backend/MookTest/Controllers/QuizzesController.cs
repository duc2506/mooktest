using Microsoft.AspNetCore.Authorization;
using MookTest.Model;
using Microsoft.AspNetCore.Mvc;
using MookTest.DTO;
using MookTest.Services.Interfaces;


namespace MookTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Authorize(Roles = Roles.Trainer)]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet("{quizId:int}/submissions")]
        public async Task<IActionResult> Submissions(int quizId,
            [FromServices] IQuizParticipationService participation)
            => Ok(await participation.GetSubmissionsAsync(null, quizId));

        // UC01
        // GET api/quizzes
        [HttpGet]
        public async Task<IActionResult> GetQuizzes()
        {
            var quizzes =
                await _quizService.GetQuizzesAsync();

            return Ok(quizzes);
        }

        // UC02
        // GET api/quizzes/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetQuiz(int id)
        {
            var quiz =
                await _quizService.GetQuizByIdAsync(id);

            if (quiz == null)
            {
                return NotFound(new
                {
                    message = "Quiz not found."
                });
            }

            return Ok(quiz);
        }

        // UC03
        // POST api/quizzes
        [HttpPost]
        public async Task<IActionResult> CreateQuiz(
            CreateQuizDto dto)
        {
            var quiz =
                await _quizService.CreateQuizAsync(dto);

            return CreatedAtAction(
                nameof(GetQuiz),
                new { id = quiz.QuizId },
                quiz);
        }

        // UC04
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateQuiz(
            int id,
            UpdateQuizDto dto)
        {
            bool result =
                await _quizService.UpdateQuizAsync(
                    id,
                    dto);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Quiz not found."
                });
            }

            return Ok(new
            {
                message = "Quiz updated successfully."
            });
        }

        // UC05
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            bool result =
                await _quizService.DeleteQuizAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Quiz not found."
                });
            }

            return Ok(new
            {
                message = "Quiz deleted successfully."
            });
        }

        // UC06 Add
        [HttpPost("{quizId:int}/questions")]
        public async Task<IActionResult> AddQuestion(
            int quizId,
            CreateQuestionDto dto)
        {
            var question =
                await _quizService.AddQuestionAsync(
                    quizId,
                    dto);

            if (question == null)
            {
                return NotFound(new
                {
                    message = "Quiz not found."
                });
            }

            return Ok(question);
        }

        // UC06 Update
        [HttpPut(
            "{quizId:int}/questions/{questionId:int}")]
        public async Task<IActionResult> UpdateQuestion(
            int quizId,
            int questionId,
            UpdateQuestionDto dto)
        {
            var question =
                await _quizService.UpdateQuestionAsync(
                    quizId,
                    questionId,
                    dto);

            if (question == null)
            {
                return NotFound(new
                {
                    message =
                        "Quiz or question not found."
                });
            }

            return Ok(question);
        }

        // UC06 Delete
        [HttpDelete(
            "{quizId:int}/questions/{questionId:int}")]
        public async Task<IActionResult> DeleteQuestion(
            int quizId,
            int questionId)
        {
            bool result =
                await _quizService.DeleteQuestionAsync(
                    quizId,
                    questionId);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Question not found."
                });
            }

            return Ok(new
            {
                message =
                    "Question deleted successfully."
            });
        }

        // UC07 Add
        [HttpPost(
            "{quizId:int}/questions/{questionId:int}/answers")]
        public async Task<IActionResult> AddAnswer(
            int quizId,
            int questionId,
            CreateAnswerDto dto)
        {
            var answer =
                await _quizService.AddAnswerAsync(
                    quizId,
                    questionId,
                    dto);

            if (answer == null)
            {
                return NotFound(new
                {
                    message =
                        "Quiz or question not found."
                });
            }

            return Ok(answer);
        }

        // UC07 Update
        [HttpPut(
            "{quizId:int}/questions/{questionId:int}/answers/{answerId:int}")]
        public async Task<IActionResult> UpdateAnswer(
            int quizId,
            int questionId,
            int answerId,
            UpdateAnswerDto dto)
        {
            var answer =
                await _quizService.UpdateAnswerAsync(
                    quizId,
                    questionId,
                    answerId,
                    dto);

            if (answer == null)
            {
                return NotFound(new
                {
                    message = "Answer not found."
                });
            }

            return Ok(answer);
        }

        // UC07 Delete
        [HttpDelete(
            "{quizId:int}/questions/{questionId:int}/answers/{answerId:int}")]
        public async Task<IActionResult> DeleteAnswer(
            int quizId,
            int questionId,
            int answerId)
        {
            bool result =
                await _quizService.DeleteAnswerAsync(
                    quizId,
                    questionId,
                    answerId);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Answer not found."
                });
            }

            return Ok(new
            {
                message =
                    "Answer deleted successfully."
            });
        }
    }
}
