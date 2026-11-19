namespace CourseRegistration.Api.DTOs;

public class OfferingAvailabilityResponse
{
    public Guid CourseOfferingId { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public int SeatsAvailable { get; set; }
}
