# Yurguen: Imagen lista para Railway / Fly.io / Render (API .NET 9 + Postgres vía cadena externa).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MayoreoKenneth.sln ./
COPY src/MayoreoKenneth.Api/MayoreoKenneth.Api.csproj src/MayoreoKenneth.Api/
COPY src/MayoreoKenneth.Domain/MayoreoKenneth.Domain.csproj src/MayoreoKenneth.Domain/
COPY src/MayoreoKenneth.Infrastructure/MayoreoKenneth.Infrastructure.csproj src/MayoreoKenneth.Infrastructure/

RUN dotnet restore src/MayoreoKenneth.Api/MayoreoKenneth.Api.csproj

COPY src/ ./src/

RUN dotnet publish src/MayoreoKenneth.Api/MayoreoKenneth.Api.csproj \
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
ENTRYPOINT ["dotnet", "MayoreoKenneth.Api.dll"]
