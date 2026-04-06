using BigDaddyProject.Domain.Entities;
using BigDaddyProject.Infrastructure.Data;
using BigDaddyProject.Web.Components;
using BigDaddyProject.Web.Middleware;
using BigDaddyProject.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;
using BigDaddyProject.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();
builder.Host.UseSerilog();  

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── API Controllers + Swagger ──────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BigDaddy API",
        Version = "v1",
        Description = "Authentication & Authorization API for BigDaddyProject Mobile App"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",  
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your_token}"
    });

    c.AddSecurityRequirement(document =>
new OpenApiSecurityRequirement
{
 [new OpenApiSecuritySchemeReference("Bearer", document)] = []
});
});


// ── ASP.NET Core Identity (int primary keys) ───────────────────────────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT ────────────────────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection("Jwt");
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
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
        ClockSkew = TimeSpan.Zero
    };
    // Allow JWT from cookie for Blazor SSR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token)) ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

// ── Authorization Policies ─────────────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("System Administrator"))
    .AddPolicy("ManagerOrAdmin", p => p.RequireRole("System Administrator", "Manager"))
    .AddPolicy("AnyUser", p => p.RequireAuthenticatedUser());

// ── Infrastructure + Web DI ────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<SessionStorageService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddHttpContextAccessor();

// ── CORS ───────────────────────────────────────────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("MobileApp", p =>
    p.WithOrigins(
        builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
        ?? ["https://localhost:3000"])
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));


var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BigDaddy API v1"));
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("MobileApp");

// ORDER MATTERS:
// 1. JwtMiddleware must come before UseAuthentication
// 2. UseAuthentication before UseAuthorization
// 3. AdminPortalGuardMiddleware after UseAuthorization
app.UseMiddleware<JwtMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AdminPortalGuardMiddleware>();

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


// ── Seed Database ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var rm = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await DbSeeder.SeedAsync(db, um, rm);
}

app.Run();
