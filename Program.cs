using System.Text;
using LanguageLearningApp.Api.Application.Interfaces;
using LanguageLearningApp.Api.Application.Settings;
using LanguageLearningApp.Api.Infrastructure.Data;
using LanguageLearningApp.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting up LanguageLearningApp.Api");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Serilog'u host'a entegre ediyoruz
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext()
                     .WriteTo.Console());
    
    // Connection String ve DbContext ayarları
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    
    // JWT Settings
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    builder.Services.Configure<JwtSettings>(jwtSection);
    var jwtSettings = jwtSection.Get<JwtSettings>();
    
    // Authentication - JWT
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // For development
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            ClockSkew = TimeSpan.Zero 
        };
        
        // JWT event loglamaları
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Error("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("Token validated for user: {User}", context.Principal.Identity.Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Log.Warning("OnChallenge error: {Error}, Description: {Description}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });
    
    builder.Services.AddHttpClient<ILlamaService, LlamaService>();

    builder.Services.AddControllers();
    
    // Swagger ayarları
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "LanguageLearningApp.Api", Version = "v1" });
    
        // JWT için SecurityDefinition
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Enter your JWT token with Bearer prefix (e.g., 'Bearer <token>')",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
    
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                new string[] {}
            }
        });
    });
    
    var app = builder.Build();
    
    // Geliştirme ortamında Swagger'ı etkinleştiriyoruz
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseRouting();
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    
    app.Run();
}
catch(Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
