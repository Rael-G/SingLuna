FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SingLuna.Core/SingLuna.Core.csproj", "SingLuna.Core/"]
COPY ["SingLuna.Dependency/SingLuna.Dependency.csproj", "SingLuna.Dependency/"]

RUN dotnet restore "SingLuna.Core/SingLuna.Core.csproj"

COPY . .

WORKDIR "/src/SingLuna.Core"

RUN dotnet build "SingLuna.Core.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "SingLuna.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SingLuna.Core.dll"]