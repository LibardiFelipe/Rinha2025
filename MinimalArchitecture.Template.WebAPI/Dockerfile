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
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MinimalArchitecture.Template.WebAPI/MinimalArchitecture.Template.WebAPI.csproj", "MinimalArchitecture.Template.WebAPI/"]
COPY ["MinimalArchitecture.Template.IoC/MinimalArchitecture.Template.IoC.csproj", "MinimalArchitecture.Template.IoC/"]
COPY ["MinimalArchitecture.Template.Infrastructure/MinimalArchitecture.Template.Infrastructure.csproj", "MinimalArchitecture.Template.Infrastructure/"]
COPY ["MinimalArchitecture.Template.Application/MinimalArchitecture.Template.Application.csproj", "MinimalArchitecture.Template.Application/"]
COPY ["MinimalArchitecture.Template.Domain/MinimalArchitecture.Template.Domain.csproj", "MinimalArchitecture.Template.Domain/"]
RUN dotnet restore "./MinimalArchitecture.Template.WebAPI/MinimalArchitecture.Template.WebAPI.csproj"
COPY . .
WORKDIR "/src/MinimalArchitecture.Template.WebAPI"
RUN dotnet build "./MinimalArchitecture.Template.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MinimalArchitecture.Template.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS aotdebug
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gdb
USER app

FROM ${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:9.0} AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["./MinimalArchitecture.Template.WebAPI", "--server.urls", "http://+:8080"]