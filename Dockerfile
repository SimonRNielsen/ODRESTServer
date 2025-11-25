# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ODRESTServer.csproj", "."]
RUN dotnet restore "./ODRESTServer.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ODRESTServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ODRESTServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/app_data
COPY --from=publish app_data/achievements.json /app/app_data/achievements.json 
COPY --from=publish app_data/users.json /app/app_data/users.json
COPY --from=publish app_data/highscore.json /app/app_data/highscore.json
ENTRYPOINT ["dotnet", "ODRESTServer.dll"]