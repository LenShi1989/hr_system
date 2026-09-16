using System.Text;
using HrSystem.Api.Data;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<HrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<WorkSchedule>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PayrollCalculator>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddHttpContextAccessor();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var code in PermissionCatalog.AllCodes)
    {
        options.AddPolicy(code, policy => policy.RequireClaim(TokenService.PermissionClaimType, code));
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HrDbContext>();
    await db.Database.MigrateAsync();

    var seedPasswords = new Dictionary<string, string>();
    builder.Configuration.GetSection("SeedPasswords").Bind(seedPasswords);

    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    var seeder = new DbSeeder(db, hasher, seedPasswords);
    await seeder.SeedAsync();
}

app.Run();