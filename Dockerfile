FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CanHappy/CanHappy.csproj CanHappy/
RUN dotnet restore CanHappy/CanHappy.csproj

COPY CanHappy/ CanHappy/
RUN dotnet publish CanHappy/CanHappy.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql loads the Kerberos/GSSAPI library when connecting to PostgreSQL; the slim runtime image doesn't ship it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The app writes logs and uploaded media under the content root, so the non-root user must own them.
RUN mkdir -p /app/log /app/wwwroot \
    && chown -R $APP_UID:$APP_UID /app/log /app/wwwroot

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "CanHappy.dll"]
