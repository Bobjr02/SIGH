# Stage 1: Build & Test Stage using .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS test
WORKDIR /app

# Copy Solution and Project Files for Optimized Caching
COPY SIGH/backend/SIGH.sln ./SIGH/backend/
COPY SIGH/backend/global.json ./SIGH/backend/
COPY SIGH/backend/src/SIGH.Domain/SIGH.Domain.csproj ./SIGH/backend/src/SIGH.Domain/
COPY SIGH/backend/src/SIGH.Application/SIGH.Application.csproj ./SIGH/backend/src/SIGH.Application/
COPY SIGH/backend/src/SIGH.Persistence/SIGH.Persistence.csproj ./SIGH/backend/src/SIGH.Persistence/
COPY SIGH/backend/src/SIGH.Infrastructure/SIGH.Infrastructure.csproj ./SIGH/backend/src/SIGH.Infrastructure/
COPY SIGH/backend/src/SIGH.Api/SIGH.Api.csproj ./SIGH/backend/src/SIGH.Api/
COPY SIGH/backend/tests/SIGH.UnitTests/SIGH.UnitTests.csproj ./SIGH/backend/tests/SIGH.UnitTests/
COPY SIGH/backend/tests/SIGH.IntegrationTests/SIGH.IntegrationTests.csproj ./SIGH/backend/tests/SIGH.IntegrationTests/

# Restore NuGet Dependencies
RUN dotnet restore ./SIGH/backend/SIGH.sln

# Copy Entire Source Code
COPY SIGH/backend/ ./SIGH/backend/

# Build Solution in Release Configuration
RUN dotnet build ./SIGH/backend/SIGH.sln --configuration Release --no-restore

# Run Tests
RUN dotnet test ./SIGH/backend/tests/SIGH.UnitTests/SIGH.UnitTests.csproj --configuration Release --no-build --logger "trx;LogFileName=unit-tests.trx"
RUN dotnet test ./SIGH/backend/tests/SIGH.IntegrationTests/SIGH.IntegrationTests.csproj --configuration Release --no-build --logger "trx;LogFileName=integration-tests.trx"

# Stage 2: Publish Stage
FROM test AS publish
RUN dotnet publish ./SIGH/backend/src/SIGH.Api/SIGH.Api.csproj -c Release -o /app/publish --no-build

# Stage 3: Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000
ENTRYPOINT ["dotnet", "SIGH.Api.dll"]
