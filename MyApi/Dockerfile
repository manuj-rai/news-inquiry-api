# Use the official .NET ASP.NET Core runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use the official .NET SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy the .csproj file from the correct path and restore dependencies
COPY ["News and Inquiry Management/MyApi/MyApi.csproj", "News and Inquiry Management/MyApi/"]
RUN dotnet restore "News and Inquiry Management/MyApi/MyApi.csproj"

# Copy the remaining source files to the container
COPY . .

# Set the working directory to your API project
WORKDIR "/src/News and Inquiry Management/MyApi"
RUN dotnet build "MyApi.csproj" -c Release -o /app/build

# Publish the app to a folder inside the container
FROM build AS publish
RUN dotnet publish "MyApi.csproj" -c Release -o /app/publish

# Use the base image to serve the published API
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApi.dll"]
