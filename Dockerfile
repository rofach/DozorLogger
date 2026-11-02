# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["DozorLogger.csproj", "./"]
RUN dotnet restore "DozorLogger.csproj"

# Copy source code and build
COPY . .
RUN dotnet publish "DozorLogger.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

COPY --from=build /app/publish .

# Environment variables defaults
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    POLL_INTERVAL_SECONDS=5 \
    STOP_RADIUS_METERS=100 \
    SPEED_STOP_THRESHOLD_KMH=5.0 \
    MIN_STOP_DURATION_SECONDS=4.0

ENTRYPOINT ["dotnet", "DozorLogger.dll"]

