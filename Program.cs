using FoodiGOAPI.Data;
using FoodiGOAPI.Models;
using FoodiGOAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;
using TokenServiceInterface = FoodiGOAPI.Services.ITokenService;
using TokenServiceImpl = FoodiGOAPI.Services.TokenService;

var builder = WebApplication.CreateBuilder(args);

// Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is missing from configuration");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = key
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Dependency Injection (no ambiguity – using the imported namespace)
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<TokenServiceInterface, TokenServiceImpl>();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // No HTTPS redirection in development
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    if (!context.Users.Any(u => u.Role == "admin"))
    {
        context.Users.Add(new User
        {
            Email = "admin@foodigo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FullName = "Administrator",
            Role = "admin",
            IsActive = true
        });
        context.SaveChanges();
    }
}

app.Run();