# Base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Set environment variable for AlloyDB connection
ENV ConnectionStrings__UPMConnection="Host=35.200.232.153;Port=5432;Username=postgres;Password=postgres;Database=postgres;SSL Mode=Require"

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CareerGuideAI.csproj", "."]
RUN dotnet restore "./CareerGuideAI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./CareerGuideAI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CareerGuideAI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage (running the application)
FROM base AS final
WORKDIR /app

# Copy the published build files
COPY --from=publish /app/publish .

# Entry point for the application
ENTRYPOINT ["dotnet", "CareerGuideAI.dll"]
