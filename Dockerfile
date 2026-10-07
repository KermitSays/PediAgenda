FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["api/PediAgenda.Api.csproj", "api/"]
COPY ["nucleo/PediAgenda.Nucleo.csproj", "nucleo/"]

RUN dotnet restore "api/PediAgenda.Api.csproj"

COPY api/ api/
COPY nucleo/ nucleo/

RUN dotnet publish "api/PediAgenda.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "PediAgenda.Api.dll"]