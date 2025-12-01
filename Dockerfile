FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files for dependency resolution
COPY ["src/Grimoire.Api/Grimoire.Api.csproj", "src/Grimoire.Api/"]
COPY ["src/Grimoire.Application/Grimoire.Application.csproj", "src/Grimoire.Application/"]
COPY ["src/Grimoire.Infrastructure/Grimoire.Infrastructure.csproj", "src/Grimoire.Infrastructure/"]
COPY ["src/Grimoire.Domain/Grimoire.Domain.csproj", "src/Grimoire.Domain/"]

# Restore dependencies
RUN dotnet restore "src/Grimoire.Api/Grimoire.Api.csproj"

# Copy the entire source code
COPY src/ src/

# Build the application
WORKDIR "/src/src/Grimoire.Api"
RUN dotnet build "Grimoire.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Grimoire.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Grimoire.Api.dll"]
