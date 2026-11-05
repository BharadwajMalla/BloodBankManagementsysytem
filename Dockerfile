# Build Stage (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition and restore dependencies
COPY ["backend/BloodBank.API/BloodBank.API.csproj", "backend/BloodBank.API/"]
RUN dotnet restore "backend/BloodBank.API/BloodBank.API.csproj"

# Copy source code and publish release build
COPY backend/BloodBank.API/ backend/BloodBank.API/
WORKDIR "/src/backend/BloodBank.API"
RUN dotnet publish -c Release -o /app/publish

# Runtime Stage (.NET 10 ASP.NET Core Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose standard container port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BloodBank.API.dll"]
