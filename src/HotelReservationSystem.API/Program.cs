using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Infrastructure;
using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.API.Middlewares;
using Serilog;
using StackExchange.Redis;

// 🔥 SERILOG
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

Console.WriteLine("🚀 APP STARTING...");

// 🔥 REDIS OPTIONAL (config'e bağladık)
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
var redisConn = builder.Configuration["Redis:Connection"];

if (redisEnabled)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        try
        {
            Console.WriteLine("Redis connecting...");
            return ConnectionMultiplexer.Connect(redisConn);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Redis FAILED: " + ex.Message);
            return null!;
        }
    });
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer?>(sp => null);
}

// 🔥 SERVICES
builder.Services.AddScoped<IRedisLockService, RedisLockService>();

builder.Services.AddControllers();

builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IReservationCoordinator, ReservationCoordinator>();

// 🔥 UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔥 WORKER (KRİTİK)
builder.Services.AddHostedService<OutboxWorker>();

var app = builder.Build();

Console.WriteLine("APP BUILT");

// PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🔥 APP RUNNING");

app.Run();