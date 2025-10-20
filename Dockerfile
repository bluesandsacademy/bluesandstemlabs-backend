# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files (no prefix needed anymore)
COPY ["BlueSandsLMS.Api/BlueSandsLMS.Api.csproj", "BlueSandsLMS.Api/"]
COPY ["BlueSandsLMS.Application/BlueSandsLMS.Application.csproj", "BlueSandsLMS.Application/"]
COPY ["BlueSandsLMS.Infrastructure/BlueSandsLMS.Infrastructure.csproj", "BlueSandsLMS.Infrastructure/"]
COPY ["BlueSandsLMS.Core/BlueSandsLMS.Core.csproj", "BlueSandsLMS.Core/"]
COPY ["BlueSandsLMS.Common/BlueSandsLMS.Common.csproj", "BlueSandsLMS.Common/"]

# Restore dependencies
RUN dotnet restore "BlueSandsLMS.Api/BlueSandsLMS.Api.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/BlueSandsLMS.Api"
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 80

ENTRYPOINT ["dotnet", "BlueSandsLMS.Api.dll"]
