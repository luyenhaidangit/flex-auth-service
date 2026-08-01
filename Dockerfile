FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Flex.Auth/Flex.Auth.csproj", "Flex.Auth/"]
COPY ["src/Flex.Domain/Flex.Domain.csproj", "Flex.Domain/"]
COPY ["src/Flex.Infrastructures/Flex.Infrastructures.csproj", "Flex.Infrastructures/"]
RUN dotnet restore "./Flex.Auth/Flex.Auth.csproj"
COPY src/. .
WORKDIR "/src/Flex.Auth"
RUN dotnet build "./Flex.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Flex.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flex.Auth.dll"]
