FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenTelemetry.Console/OpenTelemetry.Console.csproj", "OpenTelemetry.Console/"]
RUN dotnet restore "OpenTelemetry.Console/OpenTelemetry.Console.csproj"
COPY . .
WORKDIR "/src/OpenTelemetry.Console"
RUN dotnet build "./OpenTelemetry.Console.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OpenTelemetry.Console.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenTelemetry.Console.dll"]
