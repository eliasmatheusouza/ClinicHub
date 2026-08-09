FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ClinicHub.API/ClinicHub.API.csproj", "src/ClinicHub.API/"]
COPY ["src/ClinicHub.Application/ClinicHub.Application.csproj", "src/ClinicHub.Application/"]
COPY ["src/ClinicHub.Infrastructure/ClinicHub.Infrastructure.csproj", "src/ClinicHub.Infrastructure/"]
COPY ["src/ClinicHub.Domain/ClinicHub.Domain.csproj", "src/ClinicHub.Domain/"]
RUN dotnet restore "src/ClinicHub.API/ClinicHub.API.csproj"
COPY . .
WORKDIR "/src/src/ClinicHub.API"
RUN dotnet publish "ClinicHub.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClinicHub.API.dll"]
