using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Repositories;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Get the API project directory
var apiProjectDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
if (string.IsNullOrEmpty(apiProjectDir))
{
    throw new DirectoryNotFoundException("Could not resolve API project directory");
}

// Navigate up to find the solution directory
var solutionDir = Directory.GetParent(apiProjectDir)?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
if (string.IsNullOrEmpty(solutionDir))
{
    throw new DirectoryNotFoundException("Could not resolve solution directory");
}

// Combine with the database path
var resolvedPath = Path.GetFullPath(Path.Combine(solutionDir, "Database", "ECommerceDB.accdb"));

// Verify the file exists
VerifyDatabasePath(resolvedPath);
Console.WriteLine($"Database Path: {resolvedPath}");
builder.Configuration["Database:DatabasePath"] = resolvedPath;

// Add services to the container
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECommerce API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
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
            new List<string>()
        }
    });

    c.AddServer(new OpenApiServer
    {
        Url = "https://localhost:7213", // Replace with your actual API URL
        Description = "Local API Server"
    });
});

// Configure JWT Authentication
var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>();
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
        };
    });

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddSingleton<ITextSummarizationService>(sp => 
{
    var modelPath = Path.Combine(builder.Environment.ContentRootPath, "Models");
    return new LocalTextSummarizationService(modelPath);
});

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Configure Database Options
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("Database"));

// Add Database Context
builder.Services.AddScoped<AccessDbContext>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("Content-Disposition");
        });
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

// Add these lines in the middleware pipeline
app.UseExceptionHandler("/Error");
app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API v1"));
    // Add after app.UseSwaggerUI();
    app.UseMiddleware<ECommerce.API.Middleware.ExceptionMiddleware>();
}

// Add this after app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Add this method to Program.cs
static void VerifyDatabasePath(string path)
{
    Console.WriteLine($"Checking database path: {path}");
    if (!File.Exists(path))
    {
        var directory = Path.GetDirectoryName(path);
        Console.WriteLine($"Directory exists: {Directory.Exists(directory)}");
        Console.WriteLine($"Available files in directory:");
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                Console.WriteLine($"  {file}");
            }
        }
        throw new FileNotFoundException($"Database file not found at: {path}");
    }
}
