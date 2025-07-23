ARG LAUNCHING_FROM_VS
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Instala a PRAGA do curl
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Rinha2025.WebAPI/Rinha2025.WebAPI.csproj", "Rinha2025.WebAPI/"]
COPY ["Rinha2025.IoC/Rinha2025.IoC.csproj", "Rinha2025.IoC/"]
COPY ["Rinha2025.Infrastructure/Rinha2025.Infrastructure.csproj", "Rinha2025.Infrastructure/"]
COPY ["Rinha2025.Application/Rinha2025.Application.csproj", "Rinha2025.Application/"]
COPY ["Rinha2025.Domain/Rinha2025.Domain.csproj", "Rinha2025.Domain/"]
RUN dotnet restore "./Rinha2025.WebAPI/Rinha2025.WebAPI.csproj"
COPY . .
WORKDIR "/src/Rinha2025.WebAPI"
RUN dotnet build "./Rinha2025.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Rinha2025.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["./Rinha2025.WebAPI", "--server.urls", "http://+:8080"]