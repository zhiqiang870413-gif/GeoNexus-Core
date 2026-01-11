using Microsoft.EntityFrameworkCore;
using GisProject.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// 優先讀取 Render 提供的環境變數，如果沒有則使用本機連線字串
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                              ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (rawConnectionString!.StartsWith("postgres://"))
    {
        // --- 核心：將 Render 的 postgres:// 格式轉為 .NET 格式 ---
        var databaseUri = new Uri(rawConnectionString);
        var userInfo = databaseUri.UserInfo.Split(':');

        var convertedConnectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

        options.UseNpgsql(convertedConnectionString);
    }
    else
    {
        // 本地開發繼續用 SQLite
        options.UseSqlite(rawConnectionString);
    }
});

builder.Services.AddControllers();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllers();

app.Run();
