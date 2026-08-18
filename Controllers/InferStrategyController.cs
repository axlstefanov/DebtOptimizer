using DebtOptimizer.Dtos;
using DebtOptimizer.Models;
using DebtOptimizer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DebtOptimizer.Controllers
{
    [Route("api/infer-strategy")]
    [ApiController]
    public class InferStrategyController(IStrategyClassifier strategyClassifier) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<StrategyInferenceResponse>> InferStrategy(StrategyInferenceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Text is required.");

            StrategyInference inference;
            try
            {
                inference = await strategyClassifier.ClassifyAsync(request.Text);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }

            if (inference.Strategy != PayoffStrategy.Target) inference.TargetDebtName = null;

            return Ok(new StrategyInferenceResponse
            {
                Inference = inference,
                EffectiveStrategy = inference.Strategy ?? PayoffStrategy.Avalanche,
                IsClear = inference.Strategy != null
            });
        }
    }
}
