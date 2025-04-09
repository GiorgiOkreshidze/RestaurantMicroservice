using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using Restaurant.Infrastructure;
using Restaurant.Application;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Configure Identity
// builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//     .AddEntityFrameworkStores<ApplicationDbContext>()
//     .AddDefaultTokenProviders();

var awsOptions = builder.Configuration.GetAWSOptions();

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddSingleton<IDynamoDBContext>(sp =>
{
    var client = sp.GetRequiredService<IAmazonDynamoDB>();
    return new DynamoDBContext(client);
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add application services
builder.Services.AddApplicationServices();

// Add infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGet("/", () =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("📥 FOR TEST USAGE! DO NOT DELETE: Incoming request to /");
    return "Hello from Docker with logs!";
});
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
