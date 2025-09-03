# Use official .NET SDK for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CarRentalAPI/*.csproj ./CarRentalAPI/
RUN dotnet restore CarRentalAPI/CarRentalAPI.csproj

# Copy the rest of the app
COPY . .
WORKDIR /src/CarRentalAPI

# Build and publish the app
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# ✅ Ensure ASP.NET Core listens on Render's PORT
ENV ASPNETCORE_URLS=http://+:${PORT}

# Expose port (optional, Render uses $PORT anyway)
EXPOSE 5000

# Run the app
ENTRYPOINT ["dotnet", "CarRentalAPI.dll"]
