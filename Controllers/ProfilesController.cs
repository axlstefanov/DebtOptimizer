using DebtOptimizer.Dtos;
using DebtOptimizer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DebtOptimizer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfilesController(PaymentPlanService paymentPlanService, ProfileService profileService) : ControllerBase
    {
        [HttpPost("plan")]
        public ActionResult<PaymentPlanResponse> CreatePlan(CreateProfileRequest request)
        {
            return Ok(paymentPlanService.CreatePlan(request));
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateProfile(CreateProfileRequest request)
        {
            var id = await profileService.SaveProfileAsync(request);
            return CreatedAtAction(nameof(GetProfile), new { id }, id);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProfileResponse>> GetProfile(int id)
        {
            var profile = await profileService.GetProfileAsync(id);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost("{id}/plan")]
        public async Task<ActionResult<PaymentPlanResponse>> CreatePlanFromProfile(int id)
        {
            var request = await profileService.GetProfileRequestAsync(id);
            if (request == null) return NotFound();
            return Ok(paymentPlanService.CreatePlan(request));
        }
    }
}
