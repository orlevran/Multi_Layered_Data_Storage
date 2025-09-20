using Movement_Home_Task.Persistence;                   // Storage abstractions (Factory Pattern- cache/file/db, Repository Pattern (MongoDB), Decorator Pattern)
using Movement_Home_Task.Services;                      // Domain services (UserService, AuthService)
using Movement_Home_Task.Repositories;                  // Repository pattern contracts/implement
using Movement_Home_Task.Configurations;                // Strongly-typed config POCOs (MongoDBSettings, RedisSettings)
using Microsoft.IdentityModel.Tokens;                   // JWT token validation (signing keys, algorithms)
using System.Text;                                      // Encoding for symmetric JWT key
using MongoDB.Driver;                                   // Official MongoDB .NET client
using Microsoft.AspNetCore.Authentication.JwtBearer;    // JWT bearer middleware
using Microsoft.Extensions.Options;                     // IOptions<T> pattern
using StackExchange.Redis;                              // Redis ConfigurationOptions

var builder = WebApplication.CreateBuilder(args);

/* --------------------------------- MVC / Controllers ---------------------------------
 * Adds MVC controller support, model binding, automatic 400s with [ApiController], etc.*/
builder.Services.AddControllers();
/* ------------------------- Logging -----------------------------------
 * Adds Microsoft.Extensions.Logging abstractions and default providers. */
builder.Services.AddLogging();

/* ----------------- Dependency Injection: Application Layer -----------------
 * Repository: one-per-request (Scoped). Safe to depend on singletons & scoped. */
builder.Services.AddScoped<IUserRepository, UserRepository>();

/* Storage factory: creates Cache/File/Database storages. Scoped because it may
 * depend on scoped things (like repositories) and must not be captured by Singletons. */
builder.Services.AddScoped<IStorageFactory, StorageFactory>();

// Application services (business logic): one-per-request.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

/* ----------------------- Redis (IDistributedCache) ---------------------
 * Bind configuration section "Redis" -> RedisSettings (Options pattern). */
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));

/* Register the distributed cache using StackExchange.Redis and our config.
 * AddStackExchangeRedisCache wires an IDistributedCache implementation.
 * NOTE: we supply ConfigurationOptions to control endpoints, TLS, timeouts, etc.
 */
builder.Services.AddStackExchangeRedisCache(o =>
{
    var s = builder.Configuration.GetSection("Redis").Get<RedisSettings>()!;
    o.ConfigurationOptions = new ConfigurationOptions
    {
        EndPoints = { { s.Host, s.Port } },
        User = s.User,
        Password = s.Password,
        Ssl = s.Ssl,
        AbortOnConnectFail = s.AbortOnConnectFail,
        ResolveDns = s.ResolveDns,
        ConnectTimeout = s.ConnectTimeout,
        SyncTimeout = s.SyncTimeout
    };
    // InstanceName prefixes keys (e.g., "myapp:user:123") — helpful for namespacing.
    o.InstanceName = s.InstanceName ?? "myapp:";
});

/* -------------------------------------- MongoDB ------------------------------------------------
 * Bind "MongoDBSettings" section to a POCO for typed access elsewhere (IOptions<MongoDBSettings>) */
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

// MongoClient is thread-safe and recommended as a Singleton.
builder.Services.AddSingleton<IMongoClient>(s =>
{
    var cfg = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(cfg.ConnectionString);
});


// IMongoDatabase is cheap to create; registering as Singleton is fine when derived from singleton client.
builder.Services.AddSingleton<IMongoDatabase>(s =>
{
    var cfg = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var client = s.GetRequiredService<IMongoClient>();
    return client.GetDatabase(cfg.DatabaseName);
});

/* ----------------------------- JWT Authentication ----------------------------
 * Read JWT settings from config (Issuer, Audience, Key). */
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);
var key = new SymmetricSecurityKey(keyBytes);

/* Register the JWT Bearer authentication handler (the pipeline that reads
 * Authorization: Bearer <token>, validates signature, issuer, audience & exp).*/
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,          // require 'iss' to match
            ValidateAudience = true,        // require 'aud' to match
            ValidateIssuerSigningKey = true,// verify the HMAC signature with our key
            ValidateLifetime = true,        // check exp/nbf
            ValidIssuer = jwt["Issuer"],    // must match token 'iss'
            ValidAudience = jwt["Audience"],// must match token 'aud'
            IssuerSigningKey = key,         // symmetric key used to sign/validate
            ClockSkew = TimeSpan.Zero       // no slack; avoid false-positives in tests
        };
    });

/* ----------------------------- Authorization (Policies/Roles) ----------------------------
 * Adds a policy requiring the "Admin" role. [Authorize(Policy="AdminOnly")] or
 * [Authorize(Roles="Admin")] can be used on actions. Role claim should be present in JWT. */
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

/* ----------------------------- CORS ------------------------------------------ */

// 1) Read allowed origins from configuration (exact scheme+host+port).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

/* 2) Register a named CORS policy with explicit origins, methods and headers.
 * Browsers enforce CORS; server just needs to emit the right headers. */
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)   // must match scheme+host+port exactly
              .AllowAnyHeader()              // or restrict with .WithHeaders("Authorization","Content-Type")
              .AllowAnyMethod()              // or restrict with .WithMethods("GET","POST","PUT","DELETE")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

var app = builder.Build();

/* ----------------------------- Middleware Order (critical) ------------------- */

// CORS must run early so preflight OPTIONS can succeed before auth/authorization.
app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

// Endpoints: attribute-routed controllers are discovered here.
app.MapControllers();
// Start Kestrel and begin handling requests.
app.Run();


/*
 * ------------- Data Instruction: -------------
 * Singleton: One instance app-wide;
 *            expensive to create;
 *            thread-safe;
 *            immutable or shared.
 *            Reused across requests;
 *            should not depend on Scoped.
 * Scoped:    One instance per HTTP request
 *            (Mostly app services & repositories);
 *            Safe to depend on Scoped/Singleton.
 * Transient: New instance each resolve;
 *            Can have many;
 *            Cheap to create;
 */