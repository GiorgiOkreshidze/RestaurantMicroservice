using Restaurant.Infrastructure;
using Restaurant.Application;
using Restaurant.API.Middleware;
using FluentValidation;
using Restaurant.API.BackgroundServices;
using Restaurant.Application.DTOs.Auth;
using Restaurant.API.Utilities;
using Restaurant.Application.DTOs.Aws;
using Restaurant.Application.DTOs.RabbitMq;
using Restaurant.Application.DTOs.Reports;
using Restaurant.Infrastructure.ExternalServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddEnvironmentVariables();
builder.RegisterAuthorization();
builder.Services.AddControllers();

builder.Services.Configure<JwtSettings>(options => {
    options.Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new ArgumentNullException(nameof(JwtSettings.Key), "JWT_KEY environment variable is not set");
    options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new ArgumentNullException(nameof(JwtSettings.Issuer), "JWT_ISSUER environment variable is not set");
    options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new ArgumentNullException(nameof(JwtSettings.Audience), "JWT_ISSUER environment variable is not set");

    if (int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY"), out int accessExpiry))
        options.AccessTokenExpiryMinutes = accessExpiry;

    if (int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS"), out int refreshExpiry))
        options.RefreshTokenExpiryInDays = refreshExpiry;
});

builder.Services.Configure<AwsSettings>(options => {
    options.SqsQueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL") ?? throw new ArgumentNullException(nameof(AwsSettings.SqsQueueUrl), "SQS_QUEUE_URL environment variable is not set");
});

builder.Services.Configure<ReportSettings>(options => {
    options.BaseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? throw new ArgumentNullException(nameof(ReportSettings.BaseUrl), "BASE_URL environment variable is not set");
});

builder.Services.Configure<RabbitMqSettings>(options => {
    options.HostName = Environment.GetEnvironmentVariable("HOST_NAME") ?? throw new ArgumentNullException(nameof(RabbitMqSettings.HostName), "HOST_NAME environment variable is not set");
    options.UserName = Environment.GetEnvironmentVariable("RABBIT_MQ_USERNAME") ?? throw new ArgumentNullException(nameof(RabbitMqSettings.UserName), "RABBIT_MQ_USERNAME environment variable is not set");
    options.Password = Environment.GetEnvironmentVariable("RABBIT_MQ_PASSWORD") ?? throw new ArgumentNullException(nameof(RabbitMqSettings.Password), "RABBIT_MQ_PASSWORD environment variable is not set");
    
    if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int port))
        options.Port = port;
});

builder.Services.Configure<SesEmailSettings>(options => {
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? throw new ArgumentNullException(nameof(SesEmailSettings.FromEmail), "FROM_EMAIL environment variable is not set");
    options.ToEmail = Environment.GetEnvironmentVariable("TO_EMAIL") ?? throw new ArgumentNullException(nameof(SesEmailSettings.ToEmail), "TO_EMAIL environment variable is not set");
});

builder.Services.AddValidatorsFromAssembly(
    typeof(AssemblyMarker).Assembly);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<IReportServiceClient, ReportServiceClient>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(b =>
    {
        b.WithOrigins("http://localhost:5173", "https://frontend-run7team2-api-handler-dev.development.krci-dev.cloudmentor.academy") // Allow requests from this origin
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
builder.Services.AddHostedService<AwsCredentialsRefresher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddSwaggerDoc();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<MongoDbSeeder>();
    await seeder.SeedAsync();
}
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseCors();

app.Run();
