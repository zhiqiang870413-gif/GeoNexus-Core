FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# 複製並還原
COPY . ./
RUN dotnet restore

# 發佈
RUN dotnet publish -c Release -o out

# 執行環境
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "GisProject.dll"]