# Use the official .NET SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project files
COPY ["MyApi/MyApi.csproj", "MyApi/"]
COPY ["MyApi.data/MyApi.data.csproj", "MyApi.data/"]

# Restore dependencies
RUN dotnet restore "MyApi/MyApi.csproj"

# Copy the rest of the application files
COPY . .

# Build the application
WORKDIR "/src/MyApi"
RUN dotnet build "MyApi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "MyApi.csproj" -c Release -o /app/publish

# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApi.dll"]

