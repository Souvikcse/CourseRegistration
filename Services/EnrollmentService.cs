using System.Collections.Concurrent;
using CourseRegistration.Api.DTOs;
using CourseRegistration.Api.Models;
using CourseRegistration.Api.Repositories;

namespace CourseRegistration.Api.Services;

// Must be registered as a singleton: the per-offering gates only work if shared by all requests.
public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _repository;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _offeringGates = new();

    public EnrollmentService(IEnrollmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<OfferingAvailabilityResponse>> GetOfferingsAsync()
    {
        var offerings = await _repository.GetCourseOfferingsAsync();
        var result = new List<OfferingAvailabilityResponse>();

        foreach (var offering in offerings)
        {
            var enrolled = (await _repository.GetEnrollmentsAsync(offering.Id)).Count;
            result.Add(new OfferingAvailabilityResponse
            {
                CourseOfferingId = offering.Id,
                Capacity = offering.Capacity,
                EnrolledCount = enrolled,
                SeatsAvailable = Math.Max(0, offering.Capacity - enrolled)
            });
        }

        return result;
    }

    public async Task<EnrollmentResponse> EnrollAsync(Guid courseOfferingId, EnrollmentRequest request)
    {
        if (request is null || request.LearnerId == Guid.Empty)
        {
            return new EnrollmentResponse
            {
                Status = "InvalidRequest",
                Message = "A valid learner id is required."
            };
        }

        var offering = await _repository.GetCourseOfferingAsync(courseOfferingId);
        if (offering is null)
        {
            return new EnrollmentResponse
            {
                Status = "OfferingNotFound",
                Message = $"Course offering '{courseOfferingId}' was not found."
            };
        }

        var learnerId = request.LearnerId;

        // Checking capacity/queue and then enrolling or queueing is a compound operation, so it
        // runs under a per-offering gate. Without it, two requests could both see one free seat
        // and overbook the course, or take queue positions out of order.
        var gate = _offeringGates.GetOrAdd(courseOfferingId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var enrollments = await _repository.GetEnrollmentsAsync(courseOfferingId);
            var waitlist = await _repository.GetWaitlistAsync(courseOfferingId);

            // Check duplicate enrollment / waitlist entry
            if (enrollments.Any(x => x.LearnerId == learnerId))
            {
                return new EnrollmentResponse
                {
                    Status = "AlreadyEnrolled",
                    Message = $"Learner '{learnerId}' is already enrolled in this course offering."
                };
            }

            if (waitlist.Any(x => x.LearnerId == learnerId))
            {
                return new EnrollmentResponse
                {
                    Status = "AlreadyWaitlisted",
                    Message = $"Learner '{learnerId}' is already on the waitlist for this course offering."
                };
            }

            // Check prerequisites. An ineligible learner is rejected up front: not enrolled and
            // not queued. (Learners already queued who later become ineligible are skipped by
            // promotion and keep their place.)
            if (!await IsEligibleAsync(learnerId, offering))
            {
                return new EnrollmentResponse
                {
                    Status = "PrerequisiteNotMet",
                    Message = $"Learner '{learnerId}' does not meet the prerequisite for this course offering."
                };
            }

            // If the waitlist already has entries OR the course is full, join the waitlist.
            // Joining the queue even when a seat is open keeps FIFO fair: nobody jumps ahead of
            // learners who were waiting first.
            if (waitlist.Count > 0 || enrollments.Count >= offering.Capacity)
            {
                await _repository.AddWaitlistEntryAsync(new WaitlistEntry
                {
                    Id = Guid.NewGuid(),
                    CourseOfferingId = courseOfferingId,
                    LearnerId = learnerId,
                    JoinedAtUtc = DateTime.UtcNow
                });

                // A seat can be open while the queue is non-empty (everyone ahead was skipped as
                // ineligible). Let promotion decide, so a seat is never left empty beside an
                // eligible waiting learner.
                if (enrollments.Count < offering.Capacity)
                {
                    await PromoteNextWaitlistedLearnerAsync(courseOfferingId);

                    var afterPromotion = await _repository.GetEnrollmentsAsync(courseOfferingId);
                    if (afterPromotion.Any(x => x.LearnerId == learnerId))
                    {
                        return new EnrollmentResponse
                        {
                            Status = "Enrolled",
                            Message = "Learner was enrolled."
                        };
                    }
                }

                var position = (await _repository.GetWaitlistAsync(courseOfferingId))
                    .Select((entry, index) => (entry, index))
                    .First(x => x.entry.LearnerId == learnerId).index + 1;

                return new EnrollmentResponse
                {
                    Status = "Waitlisted",
                    Message = $"The course offering is full. Learner was added to the waitlist at position {position}."
                };
            }

            // Otherwise enroll directly.
            await _repository.AddEnrollmentAsync(new Enrollment
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = courseOfferingId,
                LearnerId = learnerId,
                CreatedAtUtc = DateTime.UtcNow
            });

            return new EnrollmentResponse
            {
                Status = "Enrolled",
                Message = "Learner was enrolled."
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Fills one open seat from the waitlist, earliest first.
    /// Policy: an ineligible learner is SKIPPED and KEEPS their place in the queue; they are
    /// re-checked on every later promotion. The next eligible learner behind them is promoted.
    /// Callers must hold the offering's gate, since this reads capacity and then enrolls.
    /// </summary>
    private async Task PromoteNextWaitlistedLearnerAsync(Guid courseOfferingId)
    {
        var offering = await _repository.GetCourseOfferingAsync(courseOfferingId);
        if (offering is null)
        {
            return;
        }

        var enrollments = await _repository.GetEnrollmentsAsync(courseOfferingId);
        if (enrollments.Count >= offering.Capacity)
        {
            return; // No open seat, so nobody is promoted.
        }

        var waitlist = await _repository.GetWaitlistAsync(courseOfferingId); // FIFO order
        foreach (var entry in waitlist)
        {
            if (!await IsEligibleAsync(entry.LearnerId, offering))
            {
                continue; // Skipped, keeps position.
            }

            // Remove from the queue first. If that fails the entry is already gone, so enrolling
            // would create a duplicate; move on to the next entry instead.
            if (!await _repository.RemoveWaitlistEntryAsync(courseOfferingId, entry.LearnerId))
            {
                continue;
            }

            await _repository.AddEnrollmentAsync(new Enrollment
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = courseOfferingId,
                LearnerId = entry.LearnerId,
                CreatedAtUtc = DateTime.UtcNow
            });

            return; // One seat, one promotion.
        }
    }

    // PLACEHOLDER: no prerequisite rule has been defined yet, so every learner is eligible.
    // Implement the rule here once decided; promotion already skips ineligible learners.
    private static Task<bool> IsEligibleAsync(Guid learnerId, CourseOffering offering)
    {
        return Task.FromResult(true);
    }

    public async Task<EnrollmentResponse> DropAsync(Guid courseOfferingId, Guid learnerId)
    {
        if (learnerId == Guid.Empty)
        {
            return new EnrollmentResponse
            {
                Status = "InvalidRequest",
                Message = "A valid learner id is required."
            };
        }

        var offering = await _repository.GetCourseOfferingAsync(courseOfferingId);
        if (offering is null)
        {
            return new EnrollmentResponse
            {
                Status = "OfferingNotFound",
                Message = $"Course offering '{courseOfferingId}' was not found."
            };
        }

        // Dropping and promoting is a compound operation, so it shares the offering's gate with
        // EnrollAsync. Otherwise an enroll could take the freed seat between the drop and the
        // promotion, or promotion could run concurrently with a waitlist change.
        var gate = _offeringGates.GetOrAdd(courseOfferingId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            // The remove is atomic and reports whether anything was removed, so a repeated
            // or concurrent drop for the same learner can only succeed once.
            var removed = await _repository.RemoveEnrollmentAsync(courseOfferingId, learnerId);
            if (!removed)
            {
                return new EnrollmentResponse
                {
                    Status = "NotEnrolled",
                    Message = $"Learner '{learnerId}' is not enrolled in this course offering."
                };
            }

            // Hand the freed seat to the next eligible waitlisted learner, if any.
            await PromoteNextWaitlistedLearnerAsync(courseOfferingId);

            return new EnrollmentResponse
            {
                Status = "Dropped",
                Message = "Learner was dropped."
            };
        }
        finally
        {
            gate.Release();
        }
    }
}
