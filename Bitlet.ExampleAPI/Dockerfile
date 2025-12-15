# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/

# Copy everything
COPY . .

# Build and publish
WORKDIR /src/Bitlet.ExampleAPI/
RUN dotnet publish "Bitlet.ExampleAPI.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Bitlet.ExampleAPI.dll"]
