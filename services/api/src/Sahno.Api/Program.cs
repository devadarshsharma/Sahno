using Sahno.Api.Health;
using Sahno.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("Sahno")
    ?? throw new InvalidOperationException(
        "Connection string 'Sahno' is not configured.");

builder.Services.AddControllers();
builder.Services.AddInfrastructure(connectionString);
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql");
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program
{
}
