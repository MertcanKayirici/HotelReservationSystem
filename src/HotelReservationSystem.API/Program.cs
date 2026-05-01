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

// 🔥 CONFIG
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
var redisConn = builder.Configuration["Redis:Connection"];

var kafkaEnabled = builder.Configuration.GetValue<bool>("Kafka:Enabled");

Console.WriteLine($"Redis Enabled: {redisEnabled}");
Console.WriteLine($"Kafka Enabled: {kafkaEnabled}");


// 🔥 DB (EN ÖNEMLİ)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 🔥 REDIS (SAFE MODE)
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

        var connection = ConnectionMultiplexer.Connect(options);

        Console.WriteLine(connection.IsConnected
            ? "✅ Redis CONNECTED"
            : "❌ Redis NOT CONNECTED");

        return connection;
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Redis FAILED: " + ex.Message);
        return null;
    }
});


// 🔥 SERVICES (HEPSİ SCOPED → DOĞRU)
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


// 🔥 WORKERLAR (SAFE MODE)
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