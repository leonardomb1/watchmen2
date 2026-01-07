FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 1001 appuser && \
    useradd --system --uid 1001 --gid appuser --shell /sbin/nologin appuser

COPY --from=build /app/publish .

RUN mkdir -p .aspnet/DataProtection-Keys && \
    chown -R appuser:appuser .aspnet && \
    chmod 700 .aspnet/DataProtection-Keys && \
    chown -R appuser:appuser /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Watchmen.Api.dll"]