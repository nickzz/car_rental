# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything
COPY . . 

# Restore dependencies
RUN dotnet restore CarRentalAPI/CarRentalAPI.csproj

# Build and publish
RUN dotnet publish CarRentalAPI/CarRentalAPI.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CarRentalAPI.dll"]
