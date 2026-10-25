using HotelReservationSystem.Infrastructure.Kafka;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.API.Middlewares;
using Serilog;
using Serilog.Context;
using StackExchange.Redis;
using HotelReservationSystem.Infrastructure.UnitOfWork;
using HotelReservationSystem.Infrastructure.Repositories;
using HotelReservationSystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

// ------------------------------------------------------
// Configure global Serilog logger for structured logging
// ------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    // Enrich logs with contextual data (e.g., CorrelationId from middleware)
    .Enrich.FromLogContext()
    // Console sink with custom output template including CorrelationId
    .WriteTo.Console(outputTemplate:
        "{Timestamp:HH:mm:ss} [{Level}] ({CorrelationId}) {Message}{NewLine}{Exception}")
    // File sink with daily rolling logs
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    // Optional JSON logging format for structured log systems
    //.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateLogger();

// ------------------------------------------------------
// Build application host
// ------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog();

Console.WriteLine("APP STARTING...");

// ------------------------------------------------------
// Read configuration flags (feature toggles)
// ------------------------------------------------------
var redisEnabled = builder.Configuration.GetValue<bool?>("Redis:Enabled") ?? false;
var redisConn = builder.Configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";

var kafkaEnabled = builder.Configuration.GetValue<bool?>("Kafka:Enabled") ?? false;

// Log enabled/disabled infrastructure components
Console.WriteLine($"Redis Enabled: {redisEnabled}");
Console.WriteLine($"Kafka Enabled: {kafkaEnabled}");

// ------------------------------------------------------
// Database configuration (Entity Framework Core)
// ------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured with at least 32 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ------------------------------------------------------
// Redis configuration (singleton connection multiplexer)
// ------------------------------------------------------
if (redisEnabled)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        Console.WriteLine("Redis connecting...");

        // Parse Redis configuration and allow retry on failure
        var options = ConfigurationOptions.Parse(redisConn);
        options.AbortOnConnectFail = false;

        var connection = ConnectionMultiplexer.Connect(options);

        Console.WriteLine("Redis CONNECTED");

        return connection;
    });
}
else
{
    Console.WriteLine("Redis DISABLED");
}

// ------------------------------------------------------
// Dependency Injection - Application Services
// ------------------------------------------------------
builder.Services.AddScoped<IRedisLockService, RedisLockService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IReservationCoordinator, ReservationCoordinator>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAppLogger, AppLogger>();

// Register MVC controllers
builder.Services.AddControllers();

// ------------------------------------------------------
// Swagger / OpenAPI configuration
// ------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token girin."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddSingleton<IKafkaProducerFactory, KafkaProducerFactory>();

// ------------------------------------------------------
// Background workers (hosted services)
// ------------------------------------------------------
// Outbox pattern worker for eventual consistency
builder.Services.AddHostedService<OutboxWorker>();

// Kafka consumer worker (conditionally enabled)
if (kafkaEnabled)
{
    builder.Services.AddHostedService<ReservationConsumer>();
}
else
{
    Console.WriteLine("Kafka Consumer DISABLED");
}

// ------------------------------------------------------
// Build application
// ------------------------------------------------------
var app = builder.Build();

Console.WriteLine("APP BUILT");

// ------------------------------------------------------
// Configure HTTP request pipeline
// ------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI in development environment
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom middleware pipeline (order is critical)
// 1. Request logging (captures full lifecycle)
// 2. Exception handling (wraps downstream errors)
// 3. Correlation (adds traceability across logs/services)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

Console.WriteLine("APP RUNNING");

// Start application
app.Run();