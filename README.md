Run these from the repo root, `CourseRegistration.Api`. It holds the `CourseRegistration.Api` and `CourseRegistration.Tests` folders.

**Prerequisite:** .NET 10 SDK (`dotnet --version` should show 10.x).

**1. Build everything**
```powershell
dotnet build CourseRegistration.Api\CourseRegistration.Api.slnx
```

**2. Run the tests**
```powershell
dotnet test CourseRegistration.Api\CourseRegistration.Api.slnx
```
This should report 11 passed.

**3. Start the API**
```powershell
dotnet run --project CourseRegistration.Api\CourseRegistration.Api.csproj
```
It listens on `https://localhost:61157` and `http://localhost:61158`. If your browser warns about the HTTPS certificate, run `dotnet dev-certs https --trust` once.

The browser may open to a 404 page. That's expected, because there is no endpoint at `/`. Use the endpoints below.

**4. Try it out** (use the `http` port to skip certificate trust). The repository seeds one demo offering with capacity 3, ID `11111111-1111-1111-1111-111111111111`.

| Action | Request |
|---|---|
| Health check | `GET http://localhost:61158/api/Health` |
| List offerings and seats | `GET http://localhost:61158/api/CheckEnrollmentOfferings` |
| Enroll or waitlist a learner | `POST http://localhost:61158/api/AddLearnerToEnrollment?courseOfferingId=11111111-1111-1111-1111-111111111111` with body `{ "learnerId": "<any-guid>" }` |
| Drop a learner | `DELETE http://localhost:61158/api/DropLearnerFromEnrollment?courseOfferingId=11111111-1111-1111-1111-111111111111&learnerId=<guid>` |

Example in PowerShell:
```powershell
$id = [guid]::NewGuid()
Invoke-RestMethod -Method Post -ContentType 'application/json' `
  -Uri "http://localhost:61158/api/AddLearnerToEnrollment?courseOfferingId=11111111-1111-1111-1111-111111111111" `
  -Body (@{ learnerId = $id } | ConvertTo-Json)
```

**To see the waitlist work:** enroll 4 different learners. The first 3 return `Enrolled` (200), and the 4th returns `Waitlisted` (202). Drop one of the first 3, then call `CheckEnrollmentOfferings`. The seat count stays at 0 available, because the waitlisted learner was promoted into the freed seat.

**Notes**
- Data is held in memory, so restarting the API resets everything.
- In Visual Studio, open the `.slnx` file, set `CourseRegistration.Api` as the startup project and press F5. Run tests from Test Explorer.