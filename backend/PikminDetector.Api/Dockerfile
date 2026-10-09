FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /source
COPY PikminDetector.Api/PikminDetector.Api.csproj PikminDetector.Api/
RUN dotnet restore PikminDetector.Api/PikminDetector.Api.csproj
COPY PikminDetector.Api/ PikminDetector.Api/
RUN dotnet publish PikminDetector.Api/PikminDetector.Api.csproj \
    -c Release --no-restore -o /publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false \
    && rm -rf /publish/x86 /publish/x64

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libtesseract5 tesseract-ocr-eng tesseract-ocr-chi-tra \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/x64 /app/logs \
    && ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so \
    && ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so \
    && ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /app/x64/libdl.so \
    && chown -R app:app /app
ENV LD_LIBRARY_PATH=/app/x64
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

FROM runtime AS final
COPY --from=build --chown=app:app /publish/ .
RUN test -f /app/PikminDetector.Api.dll \
    && test -f "$TESSDATA_PREFIX/eng.traineddata" \
    && test -f "$TESSDATA_PREFIX/chi_tra.traineddata" \
    && test -r /app/x64/libtesseract50.so \
    && test -r /app/x64/libleptonica-1.82.0.so \
    && unwanted="$(find /app \( -iname '*test*' -o -name '*.cs' -o -name '*.csproj' \
        -o -name '*.pdb' -o -name 'appsettings*.json' -o -name '.env*' \
        -o -name 'secrets.json' -o -name 'Dockerfile' -o -name 'x86' \
        -o -name 'tesseract50.dll' -o -name 'leptonica-1.82.0.dll' \) -print)" \
    && if [ -n "$unwanted" ]; then printf 'Unexpected runtime contents:\n%s\n' "$unwanted"; exit 1; fi
USER app
ENTRYPOINT ["dotnet", "PikminDetector.Api.dll"]
