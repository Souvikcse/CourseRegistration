namespace CourseRegistration.Api.Models;

public class Enrollment
{
    public Guid Id { get; set; }
    public Guid CourseOfferingId { get; set; }
    public Guid LearnerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
