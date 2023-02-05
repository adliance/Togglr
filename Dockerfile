FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder

WORKDIR /src

COPY src/ .
RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install git && \
    cd /src/Adliance.Togglr/ && \
    dotnet pack && \
    dotnet tool install --global --add-source ./nupkg adliance.togglr

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime

WORKDIR /app
COPY --from=builder /src/Adliance.Togglr/bin/Debug/net7.0/publish/ /app/
RUN apt-get update -y && \
    apt-get upgrade -y

ENTRYPOINT [ "/app/Adliance.Togglr" ]
