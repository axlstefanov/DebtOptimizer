using DebtOptimizer.Dtos;
using DebtOptimizer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DebtOptimizer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExtractController(IDebtExtractor debtExtractor) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<ExtractionResponse>> Extract(ExtractionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Text is required.");

            ExtractionResult extraction;
            try
            {
                extraction = await debtExtractor.ExtractAsync(request.Text);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            return Ok(new ExtractionResponse
            {
                Extraction = extraction,
                FollowUpQuestions = questions,
                IsComplete = questions.Count == 0
            });
        }
    }
}
