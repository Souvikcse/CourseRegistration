using CourseRegistration.Api.DTOs;

namespace CourseRegistration.Api.Services;

public interface IEnrollmentService
{
    /// <summary>Lists every course offering with its capacity, enrolled count and open seats.</summary>
    Task<IReadOnlyCollection<OfferingAvailabilityResponse>> GetOfferingsAsync();

    /// <summary>
    /// Rejects duplicates (already enrolled or waitlisted) and learners who fail the prerequisite.
    /// If the waitlist has entries or the course is full the learner joins the waitlist
    /// ("Waitlisted"); otherwise they are enrolled directly ("Enrolled").
    /// </summary>
    Task<EnrollmentResponse> EnrollAsync(Guid courseOfferingId, EnrollmentRequest request);

    /// <summary>
    /// Drops an enrolled learner from the course offering, freeing a seat that is immediately
    /// offered to the next eligible learner on the waitlist.
    /// Returns a clear result for invalid requests (unknown offering) and
    /// repeated or unknown drops (learner not enrolled) without changing state.
    /// </summary>
    Task<EnrollmentResponse> DropAsync(Guid courseOfferingId, Guid learnerId);
}
