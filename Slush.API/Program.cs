using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Slush.API.Hubs;
using Slush.Application.Interfaces;
using Slush.Infrastructure.Data;
using Slush.Infrastructure.Services;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ISnapshotService, SnapshotService>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

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
builder.Services.AddSignalR();
builder.Services.AddSingleton<PresenceStateService>();
builder.Services.AddScoped<IGeoLocationService, GeoLocationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();
builder.Services.AddScoped<IProfileService, Slush.Infrastructure.Services.ProfileService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddDataProtection();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ISteamCatalogService, Slush.Infrastructure.Services.SteamCatalogService>();
builder.Services.AddHttpClient<ICatalogService, Slush.Infrastructure.Services.CheapSharkCatalogService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SlushPlatform/1.0 (contact@slush.com)");
});
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("PostgreSQL");

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

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<Slush.Application.Mappings.MappingProfile>();
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(Slush.Infrastructure.Repositories.Repository<>));
builder.Services.AddScoped<IUnitOfWork, Slush.Infrastructure.Repositories.UnitOfWork>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors(policy => policy
    .WithOrigins(
        "https://slush-front-aqna.vercel.app",
        "http://localhost:3000",
        "http://localhost:5173"
    )
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.Migrate();

    var admin = db.Users.FirstOrDefault(u => u.Email == "admin@slush.com");

    if (admin == null)
    {
        admin = new Slush.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = "SlushAdmin",
            Email = "admin@slush.com",
            PasswordHash = "$2a$11$3obaCy1iMcoEe125.yuL1ugKA6bEmZhDLaIDJ2N13cTz5zm3mFgrq",
            Role = Slush.Domain.Enums.UserRole.SuperAdmin,
            IsBanned = false,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        db.SaveChanges();
        Console.WriteLine("[SEED] Default SuperAdmin user created successfully.");
    }
    else if (admin.Role != Slush.Domain.Enums.UserRole.SuperAdmin)
    {
        admin.Role = Slush.Domain.Enums.UserRole.SuperAdmin;
        db.SaveChanges();
        Console.WriteLine("[SEED] Upgraded existing default admin to SuperAdmin.");
    }
}

app.MapHub<GlobeHub>("/hubs/globe");
app.MapHub<OnlineHub>("/hubs/online");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
});

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<ISnapshotService>(
        "take-activity-snapshot",
        service => service.TakeActivitySnapshotAsync(),
        Cron.Hourly);

    recurringJobManager.AddOrUpdate<ISnapshotService>(
        "sync-game-catalog",
        service => service.SyncGameCatalogAsync(),
        Cron.Daily(3));
}

app.MapHealthChecks("/health");

app.Run();