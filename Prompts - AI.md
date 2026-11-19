* **You can start with the basic .NET 10 web API. Just keep in mind about the repository layer. That's it. There is no extra structure provided**



* **"for IEnrollmentService add the following: 1. Dropping an enrolled learner"**

\- \*\*Response:\*\* I added `DropAsync(Guid courseOfferingId, Guid learnerId)` to \[IEnrollmentService.cs](CourseRegistration.Api/Services/IEnrollmentService.cs).

\- \*\*Outcome:\*\* accepted as written. You listed only item 1, so I asked whether more was coming.



* **implement in EnrollmentService - make operations atomic (GetOfferingsAsync, EnrollAsync)**

\*\*Response:\*\* Added to \[IEnrollmentService.cs](CourseRegistration.Api/Services/IEnrollmentService.cs).

\- \*\*Outcome:\*\* accepted as written. You listed only 2 items , so I asked whether more was coming.



* **instead of lock - try to use concurrentDictionary**

&#x20;\*\*Response:\*\* I rewrote the repository around a `ConcurrentDictionary` keyed by (offering, learner). `TryRemove` and `TryAdd` do the atomic work, and results are ordered by `CreatedAtUtc`.

\- \*\*Outcome:\*\* accepted as an edit of the earlier version. I pointed out that this makes single calls atomic but not "check capacity, then add".



* **make the route of this controller - 'api/method'**

\- \*\*Response:\*\* I tried `\[Route("api/method")]`.

\- \*\*Outcome:\*\* \*\*rejected.\*\* You said "no - like 'api/{actionName}'", and I used `\[Route("api/\[action]")]` instead. I used `\[action]` because it's the token that becomes the method name. A literal `{actionName}` would be an ordinary route parameter.





* **Add 2 actions: 1. Check enrollment offerings, 2. Add learner to an enrollment**

&#x20;\*\*Response:\*\* I added `CheckEnrollmentOfferings` (GET) and `AddLearnerToEnrollment` (POST) to \[HomeController.cs](CourseRegistration.Api/Controllers/HomeController.cs). I also implemented `EnrollAsync` with a per-offering `SemaphoreSlim`, registered the services as singletons in \[Program.cs](CourseRegistration.Api/Program.cs), and seeded one demo offering with capacity 3.

\- \*\*Outcome:\*\* accepted, with these decisions made by me:

&#x20; - I read "check offerings" as a list of every offering with its seat counts.

&#x20; - A full course returns `CourseFull`.

&#x20; - I fixed the `View()` error by returning JSON.







* **when a HTTP request comes to the server to add a learner - instead of directly adding it to the data storage, it should visit the waitlist first before checking the record in the dictionary.**

\*\*Response:\*\* The waitlist should be checked before enrollment, but only to determine whether the learner can be enrolled directly or must join the queue.



**Task<EnrollmentResponse> EnrollAsync(**

&#x20;   **Guid courseOfferingId,**

&#x20;   **Guid learnerId)**

**{**

&#x20;   **// Check duplicate enrollment / waitlist entry**



&#x20;   **// Check prerequisites**



&#x20;   **// If waitlist already has entries**

&#x20;   **// OR course capacity is full**

&#x20;   **// Add learner to waitlist**



&#x20;   **// Otherwise enroll directly**



&#x20;   **return response;**

**}**



* **"add private async Task PromoteNextWaitlistedLearnerAsync(Guid courseOfferingId)"**

\- \*\*Response:\*\* I added the method, a `WaitlistEntry` model and three waitlist repository methods.

\- \*\*Outcome:\*\* accepted, with these edits and caveats:

&#x20; - Reviewing my own first draft, I found a race: an entry could be read at position 0 before its sequence number was set. I fixed that before building.

&#x20; - `IsEligibleAsync` is a placeholder that returns `true`, because no prerequisite rule exists.

Use an in-memory FIFO queue - for FIFO waitlist promotion



* **"use Queue<T> for waitlist"**

\- \*\*Response:\*\* I used `ConcurrentQueue<T>` instead of `Queue<T>`, explained why, and removed the `Sequence` field.

\- \*\*Outcome:\*\* \*\*accepted with a deviation.\*\* The plain `Queue<T>` is unsafe under concurrent access. The catch is that queue operations are no longer atomic on their own, so callers must serialize waitlist changes. That is documented in the code.





**"add action for dropping a learner."**

\- \*\*Response:\*\* I added `DropLearnerFromEnrollment` as a DELETE action. It maps `Dropped` to 200, `InvalidRequest` to 400, and `OfferingNotFound` or `NotEnrolled` to 404.

\- \*\*Outcome:\*\* accepted. The 404 for a repeated drop was my choice.



