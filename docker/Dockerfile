# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore first for better layer caching
COPY ECommerce.sln ./
COPY Directory.Build.props ./
COPY src/ECommerce.Domain/*.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/*.csproj src/ECommerce.Application/
COPY src/ECommerce.Infrastructure/*.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.Api/*.csproj src/ECommerce.Api/
COPY tests/ECommerce.UnitTests/*.csproj tests/ECommerce.UnitTests/
COPY tests/ECommerce.IntegrationTests/*.csproj tests/ECommerce.IntegrationTests/

RUN dotnet restore ECommerce.sln

# Copy all source and publish
COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/ECommerce.Api/ECommerce.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]