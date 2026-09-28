using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("TestApi__Url")
    ?? throw new Exception("Set TestApi__Url to a backend using a disposable TEST database.");
var trainerEmail = Environment.GetEnvironmentVariable("Trainer__Email")
    ?? throw new Exception("Set Trainer__Email.");
var trainerPassword = Environment.GetEnvironmentVariable("Trainer__Password")
    ?? throw new Exception("Set Trainer__Password.");
var signingKey = Environment.GetEnvironmentVariable("Jwt__Key")
    ?? throw new Exception("Set Jwt__Key to the test backend key, so invalid-token checks can run.");
using var client = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(30) };
var passed = 0;
async Task<JsonNode?> Request(string method, string path, int status, object? body = null, string? token = null)
{
    using var request = new HttpRequestMessage(new HttpMethod(method), path);
    if (body != null) request.Content = JsonContent.Create(body);
    if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    using var response = await client.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    if ((int)response.StatusCode != status)
        throw new Exception($"{method} {path}: expected {status}, got {(int)response.StatusCode}. {text}");
    passed++;
    Console.WriteLine($"PASS {method} {path} -> {status}");
    return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
}
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    passed++;
    Console.WriteLine("PASS " + description);
}
int Id(JsonNode? data, string field) => data![field]!.GetValue<int>();
string Token(JsonNode? data) => data!["accessToken"]!.GetValue<string>();
string SignToken(string role, int userId, string issuer = "MookTest", string audience = "MookTestFrontend", int lifetime = 600)
{
    static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    var header = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
    var payload = Encode(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new {
        sub = userId.ToString(), role, iss = issuer, aud = audience,
        exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + lifetime })));
    var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(header + "." + payload));
    return header + "." + payload + "." + Encode(signature);
}

await Request("GET", "/api/quizzes", 401);
await Request("GET", "/api/participation/quizzes", 401);
await Request("POST", "/api/auth/login", 401, new { email = trainerEmail, password = "incorrect-password" });
var trainer = Token(await Request("POST", "/api/auth/login", 200, new { email = trainerEmail, password = trainerPassword }));
var suffix = Guid.NewGuid().ToString("N");
var email = $"student-{suffix}@example.test";
var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
var student = await Request("POST", "/api/auth/register", 200,
    new { displayName = "API test student", email, password, role = "Trainer" });
var trainee = Token(student);
var userId = Id(student!["user"], "id");
Check(student!["user"]!["role"]!.GetValue<string>() == "Trainee", "registration cannot grant Trainer");
var other = Token(await Request("POST", "/api/auth/register", 200,
    new { displayName = "Other student", email = $"other-{suffix}@example.test", password }));
await Request("POST", "/api/auth/register", 409, new { displayName = "Duplicate", email = email.ToUpperInvariant(), password });
await Request("GET", "/api/auth/me", 200, token: trainee);
await Request("GET", "/api/quizzes", 403, token: trainee);
await Request("POST", "/api/quizzes", 403, new { title = "Forbidden", duration = 30 }, trainee);
await Request("GET", "/api/participation/quizzes", 403, token: trainer);
await Request("GET", "/api/quizzes", 401, token: "invalid.jwt.token");
await Request("GET", "/api/participation/quizzes", 401, token: SignToken("Trainee", userId, lifetime: -60));
await Request("GET", "/api/participation/quizzes", 401, token: SignToken("Trainee", userId, issuer: "wrong"));
await Request("GET", "/api/participation/quizzes", 401, token: SignToken("Trainee", userId, audience: "wrong"));

// CRUD on a disposable, unstarted quiz.
var disposable = Id(await Request("POST", "/api/quizzes", 201, new { title = "Delete test", duration = 10 }, trainer), "quizId");
var root = "/api/quizzes/" + disposable;
var disposableQuestion = Id(await Request("POST", root + "/questions", 200,
    new { content = "Temporary", questionType = 2 }, trainer), "questionId");
var qroot = root + "/questions/" + disposableQuestion;
var disposableAnswer = Id(await Request("POST", qroot + "/answers", 200,
    new { content = "Before", isCorrect = false }, trainer), "answerId");
await Request("PUT", qroot + "/answers/" + disposableAnswer, 200, new { content = "After", isCorrect = true }, trainer);
await Request("DELETE", qroot + "/answers/" + disposableAnswer, 200, token: trainer);
await Request("PUT", qroot, 200, new { content = "Essay instead", questionType = 6 }, trainer);
await Request("DELETE", qroot, 200, token: trainer);
await Request("DELETE", root, 200, token: trainer);
await Request("GET", root, 404, token: trainer);

var quiz = Id(await Request("POST", "/api/quizzes", 201, new { title = "Integration " + suffix, duration = 30 }, trainer), "quizId");
root = "/api/quizzes/" + quiz;
await Request("PUT", root, 200, new { title = "Integration edited", duration = 30 }, trainer);
await Request("POST", root + "/questions", 400, new { content = "Invalid type", questionType = 999 }, trainer);
var multi = Id(await Request("POST", root + "/questions", 200, new { content = "Select two", questionType = 1 }, trainer), "questionId");
var essay = Id(await Request("POST", root + "/questions", 200, new { content = "Explain", questionType = 6 }, trainer), "questionId");
var single = Id(await Request("POST", root + "/questions", 200, new { content = "Select one", questionType = 2 }, trainer), "questionId");
var a1 = Id(await Request("POST", root + "/questions/" + multi + "/answers", 200, new { content = "A", isCorrect = true }, trainer), "answerId");
var a2 = Id(await Request("POST", root + "/questions/" + multi + "/answers", 200, new { content = "B", isCorrect = true }, trainer), "answerId");
var a3 = Id(await Request("POST", root + "/questions/" + single + "/answers", 200, new { content = "C", isCorrect = true }, trainer), "answerId");
var a4 = Id(await Request("POST", root + "/questions/" + single + "/answers", 200, new { content = "D", isCorrect = false }, trainer), "answerId");
var participation = "/api/participation/quizzes/" + quiz;
var start = await Request("POST", participation + "/start", 200, new { }, trainee);
Check(!start!.ToJsonString().Contains("isCorrect", StringComparison.OrdinalIgnoreCase), "trainee payload hides correct answers");
Check(start["title"]!.GetValue<string>() == "Integration edited", "start uses the same title field as CRUD");
var attempt = start["attemptId"]!.GetValue<string>();
var resumed = await Request("POST", participation + "/start", 200, new { }, trainee);
Check(resumed!["attemptId"]!.GetValue<string>() == attempt, "refresh resumes the existing attempt");
await Request("PUT", root, 409, new { title = "Locked", duration = 10 }, trainer);
await Request("DELETE", root, 409, token: trainer);
await Request("GET", root, 403, token: trainee);
var valid = new {
    attemptId = attempt, answers = new object[] {
        new { questionId = multi, answerIds = new[] { a1, a2 } },
        new { questionId = essay, answerIds = Array.Empty<int>(), responseText = "Written response" },
        new { questionId = single, answerIds = new[] { a3 } }
    }
};
await Request("POST", participation + "/submit", 404, valid, other);
await Request("POST", participation + "/submit", 400, new { attemptId = attempt, answers = Array.Empty<object>() }, trainee);
await Request("POST", participation + "/submit", 400, new { attemptId = attempt, answers = new object[] {
    new { questionId = multi, answerIds = new[] { a3 } },
    new { questionId = essay, answerIds = Array.Empty<int>(), responseText = "Text" },
    new { questionId = single, answerIds = new[] { a3 } } } }, trainee);
await Request("POST", participation + "/submit", 400, new { attemptId = attempt, answers = new object[] {
    new { questionId = multi, answerIds = new[] { a1 } },
    new { questionId = essay, answerIds = Array.Empty<int>(), responseText = "Text" },
    new { questionId = single, answerIds = new[] { a3, a4 } } } }, trainee);
var result = await Request("POST", participation + "/submit", 200, valid, trainee);
var submitted = result!["answers"]!.AsArray();
Check(submitted.Single(a => Id(a, "questionId") == multi)!["selectedAnswers"]!.AsArray().Count == 2, "multiple answers persisted");
Check(submitted.Single(a => Id(a, "questionId") == essay)!["responseText"]!.GetValue<string>() == "Written response", "nullable AnswerId permits written responses");
await Request("POST", participation + "/submit", 409, valid, trainee);
var mine = await Request("GET", "/api/participation/submissions", 200, token: trainee);
Check(mine!.AsArray().Count == 1, "trainee sees their submission");
var theirs = await Request("GET", "/api/participation/submissions", 200, token: other);
Check(theirs!.AsArray().Count == 0, "another trainee cannot see this submission");
await Request("GET", root + "/submissions", 403, token: trainee);
var review = await Request("GET", root + "/submissions", 200, token: trainer);
Check(review!.AsArray().Count == 1, "trainer can review submissions");
Console.WriteLine($"Completed: {passed} checks passed. Test fixtures remain in the TEST database.");

