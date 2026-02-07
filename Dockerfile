# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first to cache restore
COPY ["TemplateEngine.Api/TemplateEngine.Api.csproj", "TemplateEngine.Api/"]
COPY ["TemplateEngine.Core/TemplateEngine.Core.csproj", "TemplateEngine.Core/"]

# Restore dependencies
RUN dotnet restore "TemplateEngine.Api/TemplateEngine.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build the project
WORKDIR "/src/TemplateEngine.Api"
RUN dotnet build "TemplateEngine.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "TemplateEngine.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create Output directory for processed files
RUN mkdir -p /app/Output
ENV OutputDirectory=/app/Output

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TemplateEngine.Api.dll"]
