# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Directory.Packages.props first (needed for restore)
COPY ["Directory.Packages.props", "."]

# Copy solution file if it exists (helps with project discovery)
COPY ["*.sln", "./"]

# Copy all project files while preserving directory structure
# We copy everything first, then remove test projects
COPY . .

# Remove unit test projects before restore to avoid unnecessary dependencies
RUN rm -rf ./**/*.UnitTests/ || true && \
    rm -rf ./**/*.Tests/ || true && \
    find . -type f \( -name "*.UnitTests.csproj" -o -name "*.Tests.csproj" \) -delete || true

# Restore dependencies for the API project
RUN dotnet restore "./Miniclip.Simulator.Api/Miniclip.Simulator.Api.csproj"

# Build the project
WORKDIR "/src/Miniclip.Simulator.Api"
RUN dotnet build "Miniclip.Simulator.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Miniclip.Simulator.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Miniclip.Simulator.Api.dll"]