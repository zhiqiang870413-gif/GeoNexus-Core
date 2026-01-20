using Microsoft.EntityFrameworkCore;
using GisProject.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 修改 1：解決 Port 轉發問題 ---
// Render 會透過 PORT 環境變數告知程式該監聽哪個位置
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://*:{port}");

// --- 修改 2：開放 CORS ---
// 讓你的前端 Vue 網頁可以跨網域抓取這個 API 的資料
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();

// --- 修改 3：強健的資料庫解析 (Render 專用) ---
builder.Services.AddDbContext<AppDbContext>(options => {
    var rawConn = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    if (!string.IsNullOrEmpty(rawConn) && rawConn.StartsWith("postgres://")) {
        // 解析 postgres://username:password@host:port/database 格式
        var uri = new Uri(rawConn);
        var userInfo = uri.UserInfo.Split(':');
        var connString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        options.UseNpgsql(connString);
    } else {
        // 本地開發時，如果沒有 DATABASE_URL，就使用 appsettings.json 裡的 SQLite
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// 1. 從設定檔讀取值
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// 2. 設定 JWT 驗證邏輯
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GeoNexus GIS API", Version = "v1" });

    // 1. 定義 JWT 驗證方式
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "zq87872",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    // 2. 讓所有 API 預設都顯示這個鎖頭圖示
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 開發模式下顯示詳細錯誤頁面 (方便我們在 Render 上除錯)
app.UseDeveloperExceptionPage();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "我的 GIS API V1");
        c.RoutePrefix = "swagger"; // 這樣網址就會是 localhost:10000/swagger
    });
}

// --- 修改 4：自動建立資料表 (解決 500 錯誤) ---
// 程式啟動時會檢查 PostgreSQL 裡面有沒有表，沒有就自動建好
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 打印出資料庫實際的路徑，看看它到底在哪裡
        Console.WriteLine($"--- 正在使用的資料庫路徑: {context.Database.GetDbConnection().ConnectionString} ---");

        var created = context.Database.EnsureCreated();
        if (created) Console.WriteLine("--- 成功建立全新資料庫與資料表 ---");

        if (!context.Vendors.Any())
        {
            context.Vendors.Add(new Vendor
            {
                Name = "測試噴水池",
                Lat = 25.0339,
                Lng = 121.5644
            });
            context.SaveChanges();
            Console.WriteLine("--- 已自動塞入測試資料 ---");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--- 初始化失敗: {ex.Message} ---");
    }
}

app.UseCors("AllowAll");

app.UseAuthentication(); // 認證 (你是誰)
app.UseAuthorization();  // 授權 (你能做什麼)

app.MapControllers();

app.Run();