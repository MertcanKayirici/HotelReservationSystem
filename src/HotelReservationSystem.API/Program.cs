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

// SERILOG
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// 🔥 REDIS OPTIONAL (CRASH YOK)
builder.Services.AddSingleton<IConnectionMultiplexer?>(sp =>
{
    try
    {
        return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
    }
    catch
    {
        return null;
    }
});

// 🔥 SERVICES
builder.Services.AddScoped<IRedisLockService, RedisLockService>();

builder.Services.AddControllers();

builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IReservationCoordinator, ReservationCoordinator>();

// 🔥 EKLE
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHostedService<OutboxWorker>();

var app = builder.Build();

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

app.Run();