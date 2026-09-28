using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MookTest.DTO;
using MookTest.Model;
using MookTest.Services.Interfaces;

namespace MookTest.Controllers;
[ApiController, Route("api/participation"), Authorize(Roles = Roles.Trainee)]
public class QuizParticipationController(IQuizParticipationService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue("sub")!);
    [HttpGet("quizzes")]
    public async Task<IActionResult> List() => Ok(await service.GetAvailableQuizzesAsync(UserId));
    [HttpGet("quizzes/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var quiz = await service.GetQuizDetailsAsync(id, UserId);
        return quiz is null ? NotFound() : Ok(quiz);
    }
    [HttpPost("quizzes/{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        var quiz = await service.StartQuizAsync(id, UserId);
        return quiz is null ? NotFound() : Ok(quiz);
    }
    [HttpPost("quizzes/{quizId:int}/submit")]
    public async Task<IActionResult> Submit(int quizId, SubmitQuizDto dto)
    {
        var result = await service.SubmitQuizAsync(quizId, UserId, dto);
        return result is null ? NotFound() : Ok(result);
    }
    [HttpGet("submissions")]
    public async Task<IActionResult> History() => Ok(await service.GetSubmissionsAsync(UserId));
}

