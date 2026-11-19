using CourseRegistration.Api.DTOs;
using CourseRegistration.Api.Repositories;
using CourseRegistration.Api.Services;

namespace CourseRegistration.Tests
{
    [TestFixture]
    public class EnrollmentServiceTests
    {
        // The in-memory repository seeds one offering with capacity 3.
        private const int Capacity = 3;

        private InMemoryEnrollmentRepository _repository = null!;
        private EnrollmentService _service = null!;
        private Guid _courseOfferingId;

        [SetUp]
        public void Setup()
        {
            _repository = new InMemoryEnrollmentRepository();
            _service = new EnrollmentService(_repository);
            _courseOfferingId = InMemoryEnrollmentRepository.DemoCourseOfferingId;
        }

        private Task<EnrollmentResponse> Enroll(Guid learnerId) =>
            _service.EnrollAsync(_courseOfferingId, new EnrollmentRequest { LearnerId = learnerId });

        private async Task<List<Guid>> FillCourseAsync()
        {
            var learners = new List<Guid>();
            for (var i = 0; i < Capacity; i++)
            {
                var id = Guid.NewGuid();
                learners.Add(id);
                Assert.That((await Enroll(id)).Status, Is.EqualTo("Enrolled"));
            }
            return learners;
        }

        [Test]
        public async Task EnrollAsync_WhenSeatsAvailable_ShouldEnrollLearner()
        {
            var result = await Enroll(Guid.NewGuid());

            Assert.That(result.Status, Is.EqualTo("Enrolled"));
        }

        [Test]
        public async Task EnrollAsync_WhenLearnerIdEmpty_ShouldReturnInvalidRequest()
        {
            var result = await Enroll(Guid.Empty);

            Assert.That(result.Status, Is.EqualTo("InvalidRequest"));
        }

        [Test]
        public async Task EnrollAsync_WhenOfferingUnknown_ShouldReturnOfferingNotFound()
        {
            var result = await _service.EnrollAsync(
                Guid.NewGuid(), new EnrollmentRequest { LearnerId = Guid.NewGuid() });

            Assert.That(result.Status, Is.EqualTo("OfferingNotFound"));
        }

        [Test]
        public async Task EnrollAsync_WhenAlreadyEnrolled_ShouldReturnAlreadyEnrolled()
        {
            var learnerId = Guid.NewGuid();
            await Enroll(learnerId);

            var result = await Enroll(learnerId);

            Assert.That(result.Status, Is.EqualTo("AlreadyEnrolled"));
        }

        [Test]
        public async Task EnrollAsync_WhenCourseFull_ShouldWaitlistLearner()
        {
            await FillCourseAsync();

            var result = await Enroll(Guid.NewGuid());

            Assert.That(result.Status, Is.EqualTo("Waitlisted"));
            Assert.That(result.Message, Does.Contain("position 1"));
        }

        [Test]
        public async Task EnrollAsync_WhenAlreadyWaitlisted_ShouldReturnAlreadyWaitlisted()
        {
            await FillCourseAsync();
            var learnerId = Guid.NewGuid();
            await Enroll(learnerId);

            var result = await Enroll(learnerId);

            Assert.That(result.Status, Is.EqualTo("AlreadyWaitlisted"));
        }

        [Test]
        public async Task EnrollAsync_ConcurrentRequests_ShouldNeverOverbook()
        {
            var results = await Task.WhenAll(
                Enumerable.Range(0, 20).Select(_ => Task.Run(() => Enroll(Guid.NewGuid()))));

            Assert.That(results.Count(r => r.Status == "Enrolled"), Is.EqualTo(Capacity));
            Assert.That(results.Count(r => r.Status == "Waitlisted"), Is.EqualTo(20 - Capacity));
        }

        [Test]
        public async Task DropAsync_WhenEnrolled_ShouldDropLearner()
        {
            var learnerId = Guid.NewGuid();
            await Enroll(learnerId);

            var result = await _service.DropAsync(_courseOfferingId, learnerId);

            Assert.That(result.Status, Is.EqualTo("Dropped"));
        }

        [Test]
        public async Task DropAsync_WhenRepeated_ShouldReturnNotEnrolled()
        {
            var learnerId = Guid.NewGuid();
            await Enroll(learnerId);
            await _service.DropAsync(_courseOfferingId, learnerId);

            var result = await _service.DropAsync(_courseOfferingId, learnerId);

            Assert.That(result.Status, Is.EqualTo("NotEnrolled"));
        }

        [Test]
        public async Task DropAsync_WhenInvalidInput_ShouldReturnErrorStatus()
        {
            Assert.That((await _service.DropAsync(_courseOfferingId, Guid.Empty)).Status,
                Is.EqualTo("InvalidRequest"));
            Assert.That((await _service.DropAsync(Guid.NewGuid(), Guid.NewGuid())).Status,
                Is.EqualTo("OfferingNotFound"));
        }

        [Test]
        public async Task DropAsync_WhenWaitlistNotEmpty_ShouldPromoteEarliestWaitlistedLearner()
        {
            var enrolled = await FillCourseAsync();
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            await Enroll(first);
            await Enroll(second);

            await _service.DropAsync(_courseOfferingId, enrolled[0]);

            var enrollments = await _repository.GetEnrollmentsAsync(_courseOfferingId);
            var waitlist = await _repository.GetWaitlistAsync(_courseOfferingId);
            Assert.That(enrollments.Select(e => e.LearnerId), Does.Contain(first));
            Assert.That(enrollments.Count, Is.EqualTo(Capacity));
            Assert.That(waitlist.Select(w => w.LearnerId), Is.EqualTo(new[] { second }));
        }
    }
}
