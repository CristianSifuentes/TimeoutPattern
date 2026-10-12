using System.Text.Json.Serialization;
using TimeoutPattern.Api.Simulation;
using TimeoutPattern.Api.Timeouts;
using TimeoutPattern.Application;
using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Core.Configuration;
using TimeoutPattern.Core.DependencyInjection;
using TimeoutPattern.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddTimeoutPattern(builder.Configuration.GetSection(TimeoutOptions.SectionName))
    .AddStorefront()
    .AddStorefrontInfrastructure(builder.Configuration)
    .AddDeadlineResponses();

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection(SimulationOptions.SectionName));
builder.Services.AddSingleton<ILatencySimulator, HeaderLatencySimulator>();

builder.Services
    .AddControllers()
    .AddJsonOptions(json => json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseDeadlines();
app.MapControllers();

app.Run();

public partial class Program;
