using LanguageLearningApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String'i çekiyoruz
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. DbContext'i ekliyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 3. Controller ve diğer servisleri ekliyoruz
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
