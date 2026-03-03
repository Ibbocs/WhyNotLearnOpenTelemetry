using Common.Shared;
using Logging.Shared;
using OpenTelemetry.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Host.UseSerilog(Logging.Shared.Logging.ConfigureLogging);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetryExt(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestAndResponseActivityMiddleware>();
app.UseHttpsRedirection();
app.UseMiddleware<OpenTelemetryTraceIdMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();