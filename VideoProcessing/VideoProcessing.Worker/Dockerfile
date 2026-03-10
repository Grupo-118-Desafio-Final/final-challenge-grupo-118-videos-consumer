FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["VideoProcessing/VideoProcessing.Worker/VideoProcessing.Worker.csproj", "VideoProcessing.Worker/"]
COPY ["VideoProcessing/VideoProcessing.Application/VideoProcessing.Application.csproj", "VideoProcessing.Application/"]
COPY ["VideoProcessing/VideoProcessing.Domain/VideoProcessing.Domain.csproj", "VideoProcessing.Domain/"]
COPY ["VideoProcessing/VideoProcessing.Infrastructure/VideoProcessing.Infrastructure.csproj", "VideoProcessing.Infrastructure/"]

RUN dotnet restore "VideoProcessing.Worker/VideoProcessing.Worker.csproj"

COPY VideoProcessing/VideoProcessing.Worker/ VideoProcessing.Worker/
COPY VideoProcessing/VideoProcessing.Application/ VideoProcessing.Application/
COPY VideoProcessing/VideoProcessing.Domain/ VideoProcessing.Domain/
COPY VideoProcessing/VideoProcessing.Infrastructure/ VideoProcessing.Infrastructure/

WORKDIR "/src/VideoProcessing.Worker"
RUN dotnet build "./VideoProcessing.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoProcessing.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

RUN apk add --no-cache \
    icu-data-full \
    icu-libs \
    curl \
    ffmpeg

COPY --from=publish /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "VideoProcessing.Worker.dll"]
