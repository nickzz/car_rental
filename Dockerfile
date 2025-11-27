# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY CarRentalAPI/*.csproj ./CarRentalAPI/
RUN dotnet restore CarRentalAPI/CarRentalAPI.csproj
COPY . .
WORKDIR /src/CarRentalAPI
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:${PORT}
ENTRYPOINT ["dotnet", "CarRentalAPI.dll"]
