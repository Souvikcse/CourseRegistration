namespace CourseRegistration.Api.Models;

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid CourseOfferingId { get; set; }
    public Guid LearnerId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
}
