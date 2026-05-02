using HotelReservationSystem.API.Middlewares;
using HotelReservationSystem.Application.Interfaces;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Application.Services;
using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Repositories;
using HotelReservationSystem.Infrastructure.Security;
using HotelReservationSystem.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using StackExchange.Redis;
using System.Text;

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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register MVC controllers
builder.Services.AddControllers();

// ------------------------------------------------------
// Swagger / OpenAPI configuration
// ------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer token gir: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes("THIS_IS_A_SUPER_LONG_SECRET_KEY_1234567890");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

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
app.UseAuthentication();
app.UseAuthorization();

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

// Enforce HTTPS redirection
app.UseHttpsRedirection();

// Authorization middleware (no authentication configured here)
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

Console.WriteLine("APP RUNNING");

// Start application
app.Run();