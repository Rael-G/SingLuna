FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN apt-get update && \
    apt-get install -y ffmpeg libsodium23 libopus0 && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SingLuna.Core/SingLuna.Core.csproj", "SingLuna.Core/"]

RUN dotnet restore "SingLuna.Core/SingLuna.Core.csproj"

COPY . .

WORKDIR "/src/SingLuna.Core"

RUN dotnet build "SingLuna.Core.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "SingLuna.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

RUN cp /usr/lib/x86_64-linux-gnu/libsodium.so.23 /app/libsodium.so && \
    cp /usr/lib/x86_64-linux-gnu/libopus.so.0 /app/libopus.so

ENTRYPOINT ["dotnet", "SingLuna.Core.dll"]