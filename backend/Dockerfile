# 1. 指定編譯階段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 2. 複製專案檔並還原套件 (這是為了加速快取)
# 注意：這裡不寫具體檔名，直接用 *.csproj
COPY *.csproj ./
RUN dotnet restore

# 3. 複製其餘所有程式碼並進行編譯
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# 4. 指定執行階段
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 5. 從編譯階段把產出物抓過來
COPY --from=build /app/publish .

# 6. 設定 Render 預設的 10000 埠位
ENV ASPNETCORE_URLS=http://+:10000

# 7. 啟動程式 (請將下面改成你正確的 dll 名稱)
# 如果你的資料夾叫 backend，通常產出就是 backend.dll
ENTRYPOINT ["dotnet", "GisProject.dll"]