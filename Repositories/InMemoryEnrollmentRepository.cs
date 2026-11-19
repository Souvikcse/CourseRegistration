using System.Collections.Concurrent;
using CourseRegistration.Api.Models;

namespace CourseRegistration.Api.Repositories;

// Temporary storage for the initial implementation.
// This can be replaced with an EF Core implementation later.
// Enrollments are keyed by (course offering, learner), so a learner has at most one
// enrollment per offering and each single enrollment operation is atomic.
// Each offering's waitlist is a ConcurrentQueue, so queue order is FIFO order. A queue cannot
// remove from the middle or check-then-add atomically, so waitlist changes must be serialized
// by the caller (the service's per-offering gate).
public class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    private readonly ConcurrentDictionary<Guid, CourseOffering> _courseOfferings = new();
    private readonly ConcurrentDictionary<(Guid CourseOfferingId, Guid LearnerId), Enrollment> _enrollments = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<WaitlistEntry>> _waitlists = new();

    // Demo offering so the API is usable without a catalog. Small capacity makes the
    // "course full" case easy to reach by hand.
    public static readonly Guid DemoCourseOfferingId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public InMemoryEnrollmentRepository()
    {
        _courseOfferings[DemoCourseOfferingId] = new CourseOffering { Id = DemoCourseOfferingId, Capacity = 3 };
    }

    public Task<CourseOffering?> GetCourseOfferingAsync(Guid courseOfferingId)
    {
        _courseOfferings.TryGetValue(courseOfferingId, out var courseOffering);
        return Task.FromResult(courseOffering);
    }

    public Task<IReadOnlyCollection<CourseOffering>> GetCourseOfferingsAsync()
    {
        IReadOnlyCollection<CourseOffering> offerings = _courseOfferings.Values.ToList();
        return Task.FromResult(offerings);
    }

    public Task<IReadOnlyCollection<Enrollment>> GetEnrollmentsAsync(Guid courseOfferingId)
    {
        // ConcurrentDictionary enumeration order is not guaranteed, so order explicitly.
        IReadOnlyCollection<Enrollment> enrollments = _enrollments.Values
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        return Task.FromResult(enrollments);
    }

    public Task AddEnrollmentAsync(Enrollment enrollment)
    {
        // TryAdd ignores a second enrollment for the same learner and offering,
        // so duplicate requests cannot create duplicate records.
        _enrollments.TryAdd((enrollment.CourseOfferingId, enrollment.LearnerId), enrollment);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveEnrollmentAsync(Guid courseOfferingId, Guid learnerId)
    {
        // TryRemove is atomic: of any number of concurrent drops, exactly one returns true.
        var removed = _enrollments.TryRemove((courseOfferingId, learnerId), out _);
        return Task.FromResult(removed);
    }

    public Task<bool> AddWaitlistEntryAsync(WaitlistEntry entry)
    {
        var queue = _waitlists.GetOrAdd(entry.CourseOfferingId, _ => new ConcurrentQueue<WaitlistEntry>());

        if (queue.Any(x => x.LearnerId == entry.LearnerId))
        {
            return Task.FromResult(false);
        }

        queue.Enqueue(entry); // Back of the line.
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<WaitlistEntry>> GetWaitlistAsync(Guid courseOfferingId)
    {
        // ToArray is a snapshot in queue (FIFO) order.
        IReadOnlyList<WaitlistEntry> waitlist = _waitlists.TryGetValue(courseOfferingId, out var queue)
            ? queue.ToArray()
            : Array.Empty<WaitlistEntry>();

        return Task.FromResult(waitlist);
    }

    public Task<bool> RemoveWaitlistEntryAsync(Guid courseOfferingId, Guid learnerId)
    {
        if (!_waitlists.TryGetValue(courseOfferingId, out var queue))
        {
            return Task.FromResult(false);
        }

        var entries = queue.ToArray();
        if (!entries.Any(x => x.LearnerId == learnerId))
        {
            return Task.FromResult(false);
        }

        // A queue cannot remove from the middle, so rebuild it without the learner, keeping
        // everyone else's relative order. Swapping in a new queue means readers holding the old
        // one still see a consistent snapshot.
        _waitlists[courseOfferingId] = new ConcurrentQueue<WaitlistEntry>(
            entries.Where(x => x.LearnerId != learnerId));

        return Task.FromResult(true);
    }
}
