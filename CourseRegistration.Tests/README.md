# CourseRegistration.Tests

Unit testing project for the Course Registration API, built using **.NET 9 and NUnit 4**.

## 1. Prerequisites

Ensure the following are installed:

- .NET 9 SDK
- Visual Studio 2022 or Visual Studio Code
- Git

Verify the .NET SDK installation:

```bash
dotnet --version
```

## 2. Project Dependencies

The test project uses the following NuGet packages:

| Package | Version | Purpose |
|---|---|---|
| NUnit | 4.2.2 | Unit testing framework |
| NUnit3TestAdapter | 4.6.0 | Test discovery and execution |
| Microsoft.NET.Test.Sdk | 17.12.0 | .NET test execution infrastructure |
| NUnit.Analyzers | 4.4.0 | NUnit code analysis |
| coverlet.collector | 6.0.2 | Code coverage collection |

## 3. Project Reference

The test project references the CourseRegistration API project:

```xml
<ProjectReference Include="..\CourseRegistration.Api.csproj" />
```

This allows the test project to access the API's services, repositories, models, and other public classes.

## 4. Restore Dependencies

Navigate to the test project directory:

```bash
cd CourseRegistration.Tests
```

Restore the NuGet dependencies:

```bash
dotnet restore
```

## 5. Build the Test Project

Execute:

```bash
dotnet build
```

This compiles the test project and verifies that all project references and dependencies are resolved.

## 6. Run Unit Tests

Execute all unit tests:

```bash
dotnet test
```

Run tests with detailed console output:

```bash
dotnet test --logger "console;verbosity=normal"
```

Run tests without rebuilding:

```bash
dotnet test --no-build
```

## 7. Code Coverage

Generate code coverage using Coverlet:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

The coverage results are generated under:

```text
TestResults/
```

The output includes coverage data in Cobertura format.

## 8. Running Tests from Visual Studio

1. Open the solution in Visual Studio.
2. Build the solution.
3. Navigate to **Test → Test Explorer**.
4. Wait for NUnit tests to be discovered.
5. Select **Run All Tests**.

You can also execute individual tests or test fixtures from Test Explorer.

## 9. Adding New Tests

Create a new test class inside the `CourseRegistration.Tests` project.

Example:

```csharp
using NUnit.Framework;

namespace CourseRegistration.Tests;

[TestFixture]
public class EnrollmentServiceTests
{
    [Test]
    public void SampleTest_ShouldPass()
    {
        Assert.That(1 + 1, Is.EqualTo(2));
    }
}
```

### NUnit Attributes

| Attribute | Description |
|---|---|
| `[TestFixture]` | Identifies a test fixture |
| `[Test]` | Marks a method as a test |
| `[SetUp]` | Executes before each test |
| `[TearDown]` | Executes after each test |
| `[TestCase]` | Runs a test with different input values |

## 10. Useful Commands

| Command | Description |
|---|---|
| `dotnet restore` | Restore dependencies |
| `dotnet build` | Build the test project |
| `dotnet test` | Execute all tests |
| `dotnet test --no-build` | Execute tests without rebuilding |
| `dotnet test --list-tests` | List discovered tests |
| `dotnet test --collect:"XPlat Code Coverage"` | Generate code coverage |

## Notes

- The project targets .NET 9.
- NUnit 4 is used as the unit testing framework.
- Tests should be isolated and independent.
- Each new feature or service should have corresponding unit tests.
- The test project should reference the API project, not the other way around.