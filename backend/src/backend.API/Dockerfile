FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for caching dependency restore
COPY ["src/backend.Domain/backend.Domain.csproj", "src/backend.Domain/"]
COPY ["src/backend.Application/backend.Application.csproj", "src/backend.Application/"]
COPY ["src/backend.Infrastructure/backend.Infrastructure.csproj", "src/backend.Infrastructure/"]
COPY ["src/backend.API/backend.API.csproj", "src/backend.API/"]

RUN dotnet restore "src/backend.API/backend.API.csproj"

# Copy full source and build
COPY src/ src/
WORKDIR "/src/src/backend.API"
RUN dotnet build "backend.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "backend.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "backend.API.dll"]
