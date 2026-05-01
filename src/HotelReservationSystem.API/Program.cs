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

// 🔥 CONFIG SAFE READ
var redisEnabled = builder.Configuration.GetValue<bool?>("Redis:Enabled") ?? false;
var redisConn = builder.Configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";

var kafkaEnabled = builder.Configuration.GetValue<bool?>("Kafka:Enabled") ?? false;

Console.WriteLine($"Redis Enabled: {redisEnabled}");
Console.WriteLine($"Kafka Enabled: {kafkaEnabled}");


// 🔥 DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 🔥 REDIS (SAFE MODE - HARDENED)
builder.Services.AddSingleton<IConnectionMultiplexer?>(sp =>
{
    if (!redisEnabled)
    {
        Console.WriteLine("⚠️ Redis DISABLED");
        return null;
    }

    try
    {
        Console.WriteLine("🔗 Redis connecting...");

        var options = ConfigurationOptions.Parse(redisConn);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 2;
        options.ConnectTimeout = 2000;

        var connection = ConnectionMultiplexer.Connect(options);

        if (!connection.IsConnected)
        {
            Console.WriteLine("❌ Redis NOT CONNECTED → fallback");
            return null;
        }

        Console.WriteLine("✅ Redis CONNECTED");
        return connection;
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Redis FAILED: " + ex.Message);
        return null;
    }
});


// 🔥 SERVICES
builder.Services.AddScoped<IRedisLockService, RedisLockService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IReservationCoordinator, ReservationCoordinator>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddControllers();


// 🔥 SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 🔥 WORKERS (SAFE START)
builder.Services.AddHostedService<OutboxWorker>();

if (kafkaEnabled)
{
    builder.Services.AddHostedService<ReservationConsumer>();
}
else
{
    Console.WriteLine("⚠️ Kafka Consumer DISABLED (fallback mode)");
}


var app = builder.Build();

Console.WriteLine("✅ APP BUILT");


// 🔥 PIPELINE
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