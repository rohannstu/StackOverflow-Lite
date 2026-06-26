FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["StackOverflowLite.slnx", "./"]
COPY ["src/StackOverflowLite.Domain/StackOverflowLite.Domain.csproj", "src/StackOverflowLite.Domain/"]
COPY ["src/StackOverflowLite.Application/StackOverflowLite.Application.csproj", "src/StackOverflowLite.Application/"]
COPY ["src/StackOverflowLite.Infrastructure/StackOverflowLite.Infrastructure.csproj", "src/StackOverflowLite.Infrastructure/"]
COPY ["src/StackOverflowLite.Host/StackOverflowLite.Host.csproj", "src/StackOverflowLite.Host/"]

# Restore dependencies
RUN dotnet restore "src/StackOverflowLite.Host/StackOverflowLite.Host.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/src/StackOverflowLite.Host"
RUN dotnet publish "StackOverflowLite.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StackOverflowLite.Host.dll"]
