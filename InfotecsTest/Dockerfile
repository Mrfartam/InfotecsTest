# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY ["InfotecsTest.csproj", "./"]
RUN dotnet restore "InfotecsTest.csproj"

# Копируем весь остальной код бэкенда
COPY . .
RUN dotnet publish "InfotecsTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап рантайма
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InfotecsTest.dll"]