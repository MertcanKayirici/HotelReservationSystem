using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Infrastructure;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.API.Middlewares;
using Serilog;
using Serilog.Context;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext() // 🔥 CorrelationId buradan akacak
    .WriteTo.Console(outputTemplate:
        "{Timestamp:HH:mm:ss} [{Level}] ({CorrelationId}) {Message}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    //.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()) // 👉 istersen JSON moda geç
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

Console.WriteLine("🚀 APP STARTING...");

// CONFIG
var redisEnabled = builder.Configuration.GetValue<bool?>("Redis:Enabled") ?? false;
var redisConn = builder.Configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";

var kafkaEnabled = builder.Configuration.GetValue<bool?>("Kafka:Enabled") ?? false;

Console.WriteLine($"Redis Enabled: {redisEnabled}");
Console.WriteLine($"Kafka Enabled: {kafkaEnabled}");

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 REDIS (DOĞRU PATTERN)
if (redisEnabled)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        Console.WriteLine("🔗 Redis connecting...");

        var options = ConfigurationOptions.Parse(redisConn);
        options.AbortOnConnectFail = false;

        var connection = ConnectionMultiplexer.Connect(options);

        Console.WriteLine("✅ Redis CONNECTED");

        return connection;
    });
}
else
{
    Console.WriteLine("⚠️ Redis DISABLED");
}

// SERVICES
builder.Services.AddScoped<IRedisLockService, RedisLockService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IReservationCoordinator, ReservationCoordinator>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAppLogger, AppLogger>();

builder.Services.AddControllers();

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// WORKERS
builder.Services.AddHostedService<OutboxWorker>();

if (kafkaEnabled)
{
    builder.Services.AddHostedService<ReservationConsumer>();
}
else
{
    Console.WriteLine("⚠️ Kafka Consumer DISABLED");
}

var app = builder.Build();

Console.WriteLine("✅ APP BUILT");

// PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🔥 APP RUNNING");

app.Run();