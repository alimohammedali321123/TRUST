using BLL.Interfaces;
using BLL.IServices;
using BLL.Repositories;
using BLL.Services;
using DAL.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TRUSTAPI.AddRoles;
using TRUSTAPI.JWT;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddTransient<EmailService, EmailServiceRepo>();
builder.Services.AddDbContext<TrustDB>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("trustdb"))
);
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
}
).AddEntityFrameworkStores<TrustDB>().AddDefaultTokenProviders();
builder.Services.JWTConfig(builder.Configuration);
builder.Services.SwaggerConfig();
var google = builder.Configuration.GetSection("Authentication:Google");
builder.Services.AddAuthentication().AddGoogle(options =>
{
    options.ClientId = google["ClientId"]!;
    options.ClientSecret = google["ClientSecret"]!;
});
builder.Services.AddRateLimiter(limiter =>
{
    limiter.AddPolicy("limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(20),
                QueueLimit = 0
            }));

    limiter.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("كثير من المحاولات حاول مجددا فى وقت لاحق", token);
    };
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("All", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
       .AllowAnyHeader()
       .AllowAnyMethod();
    }
    );
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    await Roles.AddRoles(service);
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("All");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();
