using Restaurant.Infrastructure;
using Restaurant.Application;
using Restaurant.API.Middleware;
using FluentValidation;
using Restaurant.Application.DTOs.Auth;
using Restaurant.API.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddEnvironmentVariables();
builder.RegisterAuthorization();
builder.Services.AddControllers();

builder.Services.Configure<JwtSettings>(options => {
    options.Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new ArgumentNullException(nameof(JwtSettings.Key), "JWT_KEY environment variable is not set");
    options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new ArgumentNullException(nameof(JwtSettings.Issuer), "JWT_ISSUER environment variable is not set");
    options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new ArgumentNullException(nameof(JwtSettings.Audience), "JWT_ISSUER environment variable is not set"); ;

    if (int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY"), out int accessExpiry))
        options.AccessTokenExpiryMinutes = accessExpiry;

    if (int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS"), out int refreshExpiry))
        options.RefreshTokenExpiryInDays = refreshExpiry;
});

builder.Services.AddValidatorsFromAssembly(
    typeof(Restaurant.Application.AssemblyMarker).Assembly);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5173") // Allow requests from this origin
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddSwaggerDoc();
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("📥 FOR TEST USAGE! DO NOT DELETE: Incoming request to / to check if pod really running");
    return "POD IS ALIVE!";
});
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseCors();

app.Run();
