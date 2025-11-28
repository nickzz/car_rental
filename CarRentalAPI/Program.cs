using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CarRentalAPI.Middleware;
using CarRentalAPI.HealthChecks;
using CarRentalAPI.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ===== LOGGING CONFIGURATION =====
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ===== DATABASE CONFIGURATION =====
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(dbUrl))
{
    var uri = new Uri(dbUrl);
    var userInfo = uri.UserInfo.Split(':');

    var host = uri.Host;
    var port = uri.Port != -1 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    connectionString =
        $"Host={host};Port={port};Database={database};" +
        $"Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .LogTo(Console.WriteLine, LogLevel.Warning)
);

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "database" })
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"),
        tags: new[] { "api" });

// ===== API VERSIONING =====
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ===== RESPONSE COMPRESSION =====
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// ===== CONTROLLERS =====
builder.Services.AddControllers(options =>
{
    options.MaxModelBindingCollectionSize = 1000;
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddEndpointsApiExplorer();

// ===== SWAGGER CONFIGURATION =====
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Car Rental API",
        Version = "v1",
        Description = "Car Rental Management System API with JWT Authentication",
        Contact = new OpenApiContact
        {
            Name = "Car Rental Support",
            Email = "support@carrental.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ===== AUTHORIZATION POLICIES =====
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});

// ===== REGISTER SERVICES =====
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CarService>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddScoped<TokenService>();

// ===== BACKGROUND SERVICES =====
builder.Services.AddHostedService<TokenCleanupService>();

// ===== JWT AUTHENTICATION =====
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("X-Request-Id", "Token-Expired");
        });
    }
    else
    {
        var allowedOrigins = new[]
        {
            "https://car-rental-frontend.onrender.com",
            "https://car-rental-8h8v.onrender.com",
            "https://car-rental-web-9mzp.onrender.com"
        };

        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("X-Request-Id", "Token-Expired");
        });
    }
});

// ===== RATE LIMITING =====
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ===== MINI PROFILER =====  disabled for cloud hosting
// builder.Services.AddMiniProfiler(options =>
// {
//     options.RouteBasePath = "/profiler";
//     options.TrackConnectionOpenClose = true;
//     options.ColorScheme = StackExchange.Profiling.ColorScheme.Dark;
// }).AddEntityFramework();

// ===== BUILD APP =====
var app = builder.Build();

// ===== DATABASE INITIALIZATION =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbLogger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        dbLogger.LogInformation("Running database migrations...");
        db.Database.Migrate();
        dbLogger.LogInformation("Database is up to date.");

        // if (app.Environment.IsDevelopment())
        // {
        //     dbLogger.LogInformation("Applying database migrations...");
        //     db.Database.Migrate();
        //     dbLogger.LogInformation("Database migrations applied successfully");
        // }
        // else
        // {
        //     var canConnect = await db.Database.CanConnectAsync();
        //     if (!canConnect)
        //     {
        //         dbLogger.LogError("Cannot connect to database");
        //         throw new InvalidOperationException("Database connection failed");
        //     }
        //     dbLogger.LogInformation("Database connection verified");
        // }
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "An error occurred during database initialization");
        throw;
    }
}

// ===== MIDDLEWARE PIPELINE =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Car Rental API V1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Car Rental API Documentation";
    });
}

app.UseResponseCompression();
app.UseRequestLogging();
app.UseGlobalExceptionHandler();
app.UseRouting();
app.UseCors("AllowAll");

// ===== HEALTH CHECK ENDPOINTS =====  //disable if debugging issues
// app.MapHealthChecks("/health", new HealthCheckOptions
// {
//     ResponseWriter = async (context, report) =>
//     {
//         context.Response.ContentType = "application/json";

//         var result = JsonSerializer.Serialize(new
//         {
//             status = report.Status.ToString(),
//             checks = report.Entries.Select(e => new
//             {
//                 name = e.Key,
//                 status = e.Value.Status.ToString(),
//                 description = e.Value.Description,
//                 duration = e.Value.Duration.TotalMilliseconds,
//                 data = e.Value.Data
//             }),
//             totalDuration = report.TotalDuration.TotalMilliseconds
//         }, new JsonSerializerOptions
//         {
//             WriteIndented = true
//         });

//         await context.Response.WriteAsync(result);
//     }
// });

// app.MapHealthChecks("/health/ready", new HealthCheckOptions
// {
//     Predicate = check => check.Tags.Contains("database"),
//     ResponseWriter = async (context, report) =>
//     {
//         var result = report.Status == HealthStatus.Healthy ? "Ready" : "Not Ready";
//         await context.Response.WriteAsync(result);
//     }
// });

// app.MapHealthChecks("/health/live", new HealthCheckOptions
// {
//     Predicate = check => check.Tags.Contains("api")
// });

// ===== STANDARD ENDPOINTS =====
app.MapGet("/", () => Results.Ok(new
{
    status = "running",
    message = "Car Rental API is running 🚀",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}));

app.MapGet("/api/info", () => Results.Ok(new
{
    name = "Car Rental API",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        healthReady = "/health/ready",
        healthLive = "/health/live",
        profiler = "/profiler/results"
    }
}));

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// app.UseMiniProfiler();  disabled for cloud hosting
app.MapControllers();

// ===== STARTUP LOGGING =====
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("========================================");
logger.LogInformation("🚗 Car Rental API Started Successfully!");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Swagger UI: http://localhost:5052/swagger");
logger.LogInformation("Health Check: http://localhost:5052/health");
logger.LogInformation("========================================");

// ===== GRACEFUL SHUTDOWN =====
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Application is shutting down...");
});

app.Run();