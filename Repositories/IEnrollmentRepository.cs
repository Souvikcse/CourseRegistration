using CourseRegistration.Api.Models;

namespace CourseRegistration.Api.Repositories;

public interface IEnrollmentRepository
{
    Task<CourseOffering?> GetCourseOfferingAsync(Guid courseOfferingId);
    Task<IReadOnlyCollection<CourseOffering>> GetCourseOfferingsAsync();
    Task<IReadOnlyCollection<Enrollment>> GetEnrollmentsAsync(Guid courseOfferingId);
    Task AddEnrollmentAsync(Enrollment enrollment);

    /// <summary>
    /// Atomically removes the learner's enrollment. Returns false if no such enrollment exists
    /// (e.g. never enrolled or already dropped), so callers can detect repeated drops.
    /// </summary>
    Task<bool> RemoveEnrollmentAsync(Guid courseOfferingId, Guid learnerId);

    // Waitlist methods are not atomic on their own: callers must serialize waitlist changes
    // per offering (the service does this with its per-offering gate).

    /// <summary>
    /// Appends the learner to the back of the offering's waitlist queue.
    /// Returns false (and changes nothing) if the learner is already waitlisted.
    /// </summary>
    Task<bool> AddWaitlistEntryAsync(WaitlistEntry entry);

    /// <summary>Returns the offering's waitlist in FIFO order (earliest first).</summary>
    Task<IReadOnlyList<WaitlistEntry>> GetWaitlistAsync(Guid courseOfferingId);

    /// <summary>Atomically removes a waitlist entry. Returns false if it no longer exists.</summary>
    Task<bool> RemoveWaitlistEntryAsync(Guid courseOfferingId, Guid learnerId);
}
