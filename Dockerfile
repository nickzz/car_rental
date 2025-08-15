# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CarRentalAPI/CarRentalAPI.csproj CarRentalAPI/
RUN dotnet restore CarRentalAPI/CarRentalAPI.csproj

# Copy the rest of the source code
COPY CarRentalAPI/ CarRentalAPI/

# Publish the application
WORKDIR /src/CarRentalAPI
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CarRentalAPI.dll"]
