FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY KeepSafe.csproj ./
RUN dotnet restore KeepSafe.csproj

# Copy source and publish
COPY . ./
RUN dotnet publish KeepSafe.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install fontconfig and fonts for PDFsharp/Image rendering if needed
RUN apt-get update && apt-get install -y --no-install-recommends \
    fontconfig \
    libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

# Create /keys directory with proper permissions
RUN mkdir -p /keys && chown -R $APP_UID:$APP_UID /keys /app

USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENTRYPOINT ["dotnet", "KeepSafe.dll"]
