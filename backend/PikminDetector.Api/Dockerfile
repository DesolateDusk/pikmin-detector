FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /source
COPY PikminDetector.Api/PikminDetector.Api.csproj PikminDetector.Api/
RUN dotnet restore PikminDetector.Api/PikminDetector.Api.csproj
COPY PikminDetector.Api/ PikminDetector.Api/
RUN dotnet publish PikminDetector.Api/PikminDetector.Api.csproj \
    -c Release --no-restore -o /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libtesseract5 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/x64 /app/logs \
    && ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so \
    && ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so \
    && ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /app/x64/libdl.so \
    && chown -R app:app /app
ENV LD_LIBRARY_PATH=/app/x64
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

# Run the existing recognition fixtures on Linux with the runtime's native libraries.
FROM build AS test
RUN apt-get update \
    && apt-get install -y --no-install-recommends libtesseract5 \
    && rm -rf /var/lib/apt/lists/*
COPY PikminDetector.Api.Tests/ PikminDetector.Api.Tests/
COPY test-image/ test-image/
RUN dotnet build PikminDetector.Api.Tests/PikminDetector.Api.Tests.csproj -c Release
COPY --from=runtime /app/x64/ PikminDetector.Api.Tests/bin/Release/net10.0/x64/
ENV LD_LIBRARY_PATH=/source/PikminDetector.Api.Tests/bin/Release/net10.0/x64
RUN dotnet test PikminDetector.Api.Tests/PikminDetector.Api.Tests.csproj -c Release --no-build

FROM runtime AS final
COPY --from=build --chown=app:app /publish/ .
USER app
ENTRYPOINT ["dotnet", "PikminDetector.Api.dll"]
