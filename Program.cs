var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Singletons: the in-memory repository holds all state, and the service holds per-offering locks.
builder.Services.AddSingleton<CourseRegistration.Api.Repositories.IEnrollmentRepository,
    CourseRegistration.Api.Repositories.InMemoryEnrollmentRepository>();
builder.Services.AddSingleton<CourseRegistration.Api.Services.IEnrollmentService,
    CourseRegistration.Api.Services.EnrollmentService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
