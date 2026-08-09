FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ClinicHub.Notifications.Worker/ClinicHub.Notifications.Worker.csproj", "src/ClinicHub.Notifications.Worker/"]
COPY ["src/ClinicHub.Application/ClinicHub.Application.csproj", "src/ClinicHub.Application/"]
COPY ["src/ClinicHub.Infrastructure/ClinicHub.Infrastructure.csproj", "src/ClinicHub.Infrastructure/"]
COPY ["src/ClinicHub.Domain/ClinicHub.Domain.csproj", "src/ClinicHub.Domain/"]
RUN dotnet restore "src/ClinicHub.Notifications.Worker/ClinicHub.Notifications.Worker.csproj"
COPY . .
WORKDIR "/src/src/ClinicHub.Notifications.Worker"
RUN dotnet publish "ClinicHub.Notifications.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClinicHub.Notifications.Worker.dll"]
