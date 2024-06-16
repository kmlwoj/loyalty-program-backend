using lojalBackend.Controllers;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySql.EntityFrameworkCore.Extensions;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "DefaultCors",
        policy =>
        {
            policy.WithOrigins(new[] { "http://localhost:3000", "https://lojfr.ne-quid-nimis.pl", "http://lojfr.ne-quid-nimis.pl" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMySQLServer<LojClientDbContext>(builder.Configuration.GetConnectionString("MainConn") ?? string.Empty);
builder.Services.AddMySQLServer<LojShopDbContext>(builder.Configuration.GetConnectionString("ShopConn") ?? string.Empty);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "lojalBackend - V1",
            Version = "v1"
        });

    var filePath = Path.Combine(AppContext.BaseDirectory, "lojalBackend.xml");
    c.IncludeXmlComments(filePath);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
    };
    options.MapInboundClaims = false;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context  =>
        {
            if (context.Request.Cookies.ContainsKey("X-Access-Token") && !"/api/Login/Login".Equals(context.Request.Path))
            {
                context.Token = context.Request.Cookies["X-Access-Token"];
                var tokenHandler = new JwtSecurityTokenHandler();
                try
                {
                    var principal = tokenHandler.ValidateToken(context.Token, options.TokenValidationParameters, out SecurityToken securityToken);
                }
                catch(SecurityTokenExpiredException)
                {
                    try
                    {
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<LojClientDbContext>();
                        context.Token = LoginController.RefreshToken(
                            new TokenModel(context.Request.Cookies["X-Access-Token"] ?? string.Empty, context.Request.Cookies["X-Refresh-Token"] ?? string.Empty),
                            builder.Configuration["Jwt:Key"] ?? string.Empty,
                            builder.Configuration["Jwt:Issuer"] ?? string.Empty,
                            builder.Configuration["Jwt:Audience"] ?? string.Empty,
                            dbContext
                            );
                    }
                    catch(Exception err)
                    {
                        if("Refresh token is expired".Equals(err.Message) && context.Request.Cookies.ContainsKey("X-Access-Token"))
                        {
                            string[] keys = { "X-Access-Token", "X-Username", "X-Refresh-Token" };
                            var cookieOpt = new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true };
                            cookieOpt.Extensions.Add("Partitioned");
                            foreach (var cookie in context.Request.Cookies.Where(x => keys.Contains(x.Key)))
                            {
                                context.Response.Cookies.Delete(cookie.Key, cookieOpt);
                            }
                        }
                        context.Token = null;
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "text/plain";
                        byte[] data = Encoding.UTF8.GetBytes(err.Message);
                        return context.Response.Body.WriteAsync(data, 0, data.Length);
                    }
                    if (!string.IsNullOrEmpty(context.Token))
                    {
                        var cookieOpt = new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true };
                        cookieOpt.Extensions.Add("Partitioned");
                        context.Response.Cookies.Delete("X-Access-Token", cookieOpt);
                        context.Response.Cookies.Append("X-Access-Token", context.Token, cookieOpt);
                    }
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsLoggedIn", policy => policy.AddRequirements(new lojalBackend.RefreshRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, lojalBackend.RefreshRequirementHandler>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || new[] { "Compose", "Kubernetes" }.Contains(app.Environment.EnvironmentName))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(c => c.Run(async context =>
{
    var exception = context.Features
        .Get<IExceptionHandlerPathFeature>()
        ?.Error;
    var response = new { error = exception?.Message };
    await context.Response.WriteAsJsonAsync(response);
}));

app.UseRouting();

app.UseCors("DefaultCors");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }