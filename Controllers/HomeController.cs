using CourseRegistration.Api.DTOs;
using CourseRegistration.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseRegistration.Api.Controllers
{
    [ApiController]
    [Route("api/[action]")]
    public class HomeController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public HomeController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        // GET api/Index
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { Status = "Course Registration API" });
        }

        // GET api/CheckEnrollmentOfferings
        [HttpGet]
        public async Task<IActionResult> CheckEnrollmentOfferings()
        {
            return Ok(await _enrollmentService.GetOfferingsAsync());
        }

        // POST api/AddLearnerToEnrollment?courseOfferingId={id}   body: { "learnerId": "{id}" }
        [HttpPost]
        public async Task<IActionResult> AddLearnerToEnrollment(
            [FromQuery] Guid courseOfferingId,
            [FromBody] EnrollmentRequest request)
        {
            var response = await _enrollmentService.EnrollAsync(courseOfferingId, request);

            return response.Status switch
            {
                "Enrolled" => Ok(response),
                "Waitlisted" => Accepted(response), // 202: accepted into the queue, not yet enrolled
                "InvalidRequest" => BadRequest(response),
                "OfferingNotFound" => NotFound(response),
                "PrerequisiteNotMet" => UnprocessableEntity(response),
                _ => Conflict(response) // AlreadyEnrolled, AlreadyWaitlisted
            };
        }

        // DELETE api/DropLearnerFromEnrollment?courseOfferingId={id}&learnerId={id}
        [HttpDelete]
        public async Task<IActionResult> DropLearnerFromEnrollment(
            [FromQuery] Guid courseOfferingId,
            [FromQuery] Guid learnerId)
        {
            var response = await _enrollmentService.DropAsync(courseOfferingId, learnerId);

            return response.Status switch
            {
                "Dropped" => Ok(response),
                "InvalidRequest" => BadRequest(response),
                _ => NotFound(response) // OfferingNotFound, NotEnrolled (including a repeated drop)
            };
        }
    }
}
