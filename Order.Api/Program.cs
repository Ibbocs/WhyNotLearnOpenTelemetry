using Common.Shared;
using Logging.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Shared;
using Order.API.Models;
using Order.API.OrderServices;
using Order.API.RedisServices;
using Order.API.StockServices;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog(Logging.Shared.Logging.ConfigureLogging);
//builder.AddOpenTelemetryLog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging();

builder.Services.AddOpenTelemetryExt(builder.Configuration);


builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<StockService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddHttpClient<StockService>(options =>
{
    options.BaseAddress = new Uri(builder.Configuration.GetSection("ApiServices")["StockApi"]!);
});

builder.Services.AddSingleton(_ => { return new RedisService(builder.Configuration); });

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisService = sp.GetService<RedisService>();

    return redisService!.GetConnectionMultiplexer;
}); //insturment IConnectionMultiplexer interface isteyir deye, bunu ioc elave edirik


builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", host =>
        {
            host.Username("guest");
            host.Password("guest");
        });
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<RequestAndResponseActivityMiddleware>();
app.UseMiddleware<OpenTelemetryTraceIdMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();