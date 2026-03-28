using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace ITBS_Classroom.Controllers.Api;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    [HttpPost("ask")]
    public IActionResult AskQuestion([FromBody] object payload)
    {
        var isFr = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
        return Ok(new
        {
            success = true,
            message = isFr ? "Réponse IA simulée" : "Mock AI response",
            payload
        });
    }

    [HttpPost("summarize")]
    public IActionResult Summarize([FromBody] object payload)
    {
        var isFr = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
        return Ok(new
        {
            success = true,
            summary = isFr ? "Ceci est un résumé simulé." : "This is a mock summary.",
            payload
        });
    }

    [HttpPost("quiz")]
    public IActionResult GenerateQuiz([FromBody] object payload)
    {
        return Ok(new
        {
            success = true,
            questions = new[]
            {
                "Q1 mock",
                "Q2 mock"
            },
            payload
        });
    }
}
