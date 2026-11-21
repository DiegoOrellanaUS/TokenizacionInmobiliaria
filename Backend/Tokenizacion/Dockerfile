FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Tokenizacion.csproj ./
RUN dotnet restore Tokenizacion.csproj
COPY . .
RUN dotnet publish Tokenizacion.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ARG TARGETARCH
RUN set -eu; \
    case "$TARGETARCH" in \
      amd64) arch=x86_64; checksum=48c70074020e09af0a3ebbd2bf62d53c9aec47935e49a112d3cd90fb4e37658f ;; \
      arm64) arch=aarch64; checksum=145785bb4bfd9711a7af1b4cf13a38ae31034421401a5fed061be35baf01373a ;; \
      *) exit 1 ;; \
    esac; \
    archivo="stellar-cli-30.0.1-${arch}-unknown-linux-gnu.tar.gz"; \
    apt-get update; \
    apt-get install -y --no-install-recommends curl; \
    curl --fail --location --retry 3 "https://github.com/stellar/stellar-cli/releases/download/v30.0.1/$archivo" -o /tmp/stellar.tar.gz; \
    echo "$checksum  /tmp/stellar.tar.gz" | sha256sum -c; \
    tar -xzf /tmp/stellar.tar.gz -C /usr/local/bin stellar; \
    chmod 755 /usr/local/bin/stellar; \
    stellar --version; \
    rm -rf /tmp/stellar.tar.gz /var/lib/apt/lists/*
ENV Stellar__Ejecutable=/usr/local/bin/stellar
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tokenizacion.dll"]
