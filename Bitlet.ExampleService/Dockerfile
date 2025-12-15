# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/

# Copy everything
COPY . .

# Build and publish
WORKDIR /src/Bitlet.ExampleService/
RUN dotnet publish "Bitlet.ExampleService.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Bitlet.ExampleService.dll"]
