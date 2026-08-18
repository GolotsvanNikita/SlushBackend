using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Slush.Application.Interfaces;
using Slush.Infrastructure.Data;
using Slush.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"\n[AUTH ERROR] Token validation failed: {context.Exception.Message}\n");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"\n[AUTH WARNING] 401 Unauthorized returned. Details: {context.ErrorDescription}\n");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Slush API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement();
    var schemeReference = new OpenApiSecuritySchemeReference("Bearer");
    securityRequirement.Add(schemeReference, []);
    c.AddSecurityRequirement(_ => securityRequirement);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.Migrate();

    if (!db.Users.Any(u => u.Role == Slush.Domain.Enums.UserRole.Admin))
    {
        var admin = new Slush.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = "SlushAdmin",
            Email = "admin@slush.com",

            PasswordHash = "$2a$11$3obaCy1iMcoEe125.yuL1ugKA6bEmZhDLaIDJ2N13cTz5zm3mFgrq",

            Role = Slush.Domain.Enums.UserRole.Admin,
            IsBanned = false,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        db.SaveChanges();

        Console.WriteLine("[SEED] Default admin user created successfully.");
    }
}

app.Run();