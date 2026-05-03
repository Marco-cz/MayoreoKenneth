# Yurguen: Imagen lista para Railway / Fly.io / Render (API .NET 9 + Postgres vía cadena externa).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ExhaTechStore.sln ./
COPY src/ExhaTechStore.Api/ExhaTechStore.Api.csproj src/ExhaTechStore.Api/
COPY src/ExhaTechStore.Domain/ExhaTechStore.Domain.csproj src/ExhaTechStore.Domain/
COPY src/ExhaTechStore.Infrastructure/ExhaTechStore.Infrastructure.csproj src/ExhaTechStore.Infrastructure/

RUN dotnet restore src/ExhaTechStore.Api/ExhaTechStore.Api.csproj

COPY src/ ./src/

RUN dotnet publish src/ExhaTechStore.Api/ExhaTechStore.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Yurguen: El host suele exponer HTTPS; dentro del contenedor Kestrel escucha HTTP.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Yurguen: ConnectionStrings__DefaultConnection, Jwt__*, Cors__AllowedOrigins__*, Jobs__*, Store__* vían env vars del PaaS.
ENTRYPOINT ["dotnet", "ExhaTechStore.Api.dll"]
