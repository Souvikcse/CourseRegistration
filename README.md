# Course Registration API

Initial .NET 9 Web API scaffold for the course enrollment and waitlist assessment.

## Structure

- `Controllers`: HTTP endpoints
- `Services`: enrollment and waitlist business rules
- `Repositories`: persistence abstraction and initial in-memory implementation
- `Models`: domain/data models
- `DTOs`: API request and response contracts

## Run

```bash
dotnet restore
dotnet run
```

The initial health endpoint is `GET /api/health`.

## Notes

The in-memory repository is a starting point only. The enrollment workflow, prerequisite checks, waitlist records, and promotion behavior have not been implemented yet. The repository interface will be expanded as those requirements are designed.
