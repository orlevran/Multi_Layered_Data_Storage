using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using Movement_Home_Task.Persistence;
using Movement_Home_Task.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Movement_Home_Task.Configurations;
using Microsoft.Extensions.Options;
using Movement_Home_Task.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllers();

builder.Services.AddLogging();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStorageFactory, StorageFactory>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Redis
var redis = builder.Configuration.GetSection("Redis");

builder.Services.AddStackExchangeRedisCache(o =>
{
    o.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { "redis-15572.c1.eu-west-1-3.ec2.redns.redis-cloud.com:15572" },
        User = "default",
        Password = "uETsSfrgvJfvrqOIXTPtdNiN62eAlKbr",
        Ssl = false,
        AbortOnConnectFail = false,
        ResolveDns = true,
        ConnectTimeout = 8000,
        SyncTimeout = 8000
    };
    o.InstanceName = "myapp:";
});

// MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton<IMongoClient>(s =>
{
    var r = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(r.ConnectionString);
});
builder.Services.AddSingleton<IMongoDatabase>(s =>
{
    var r = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var client = s.GetRequiredService<IMongoClient>();
    return client.GetDatabase(r.DatabaseName);
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);
var key = new SymmetricSecurityKey(keyBytes);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});
 
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();