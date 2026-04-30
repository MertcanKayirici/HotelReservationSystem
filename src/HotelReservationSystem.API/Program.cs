using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Infrastructure.Repositories;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.API.Middlewares;
using Serilog;

// 1. SERILOG CONFIG (builder'dan önce)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// 2. SERILOG'U BUILDER'A BAĞLA (BURASI ÖNEMLİ)
builder.Host.UseSerilog();

// SERVICES
builder.Services.AddControllers();

builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

var app = builder.Build();

// PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Exception middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();