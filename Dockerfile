ARG ASPNET_VERSION=5.0.5
ARG ASPNET_SHA512=149b378b2377b60980a9bc0fa2f345293439f0da18bc75e8bffadc2faba8c3c175325fdd4edc9faaf898c4836f76a1685d55a0efdb679a5034b9e761450a88a4
ARG AMAZON_LINUX=public.ecr.aws/lambda/provided:al2

FROM $AMAZON_LINUX AS base

FROM base AS builder-deps
COPY --from=base / /rootfs
RUN yum install -d1 -y --installroot=/rootfs \
    ca-certificates \
    \
    # .NET dependencies
    libc6 \
    libgcc1 \
    libgssapi-krb5-2 \
    libicu63 \
    libssl1.1 \
    libstdc++6 \
    zlib1g

FROM base
COPY --from=builder-deps /rootfs /

# Setup custom dotnet env variables
# See here for more info: https://github.com/dotnet/docs/blob/master/docs/core/tools/dotnet.md
ENV \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Lambda is optionated about installing tooling under /var
    DOTNET_ROOT=/var/lang/dotnet \
    # Don't display welcome message on first run
    DOTNET_NOLOGO=true \
    # Disable Microsoft's telemetry collection
    DOTNET_CLI_TELEMETRY_OPTOUT=true

FROM base AS builder-net5
ARG ASPNET_VERSION
ARG ASPNET_SHA512

WORKDIR /dotnet

# Install tar and gzip for unarchiving downloaded tar.gz
RUN yum install tar --assumeyes
RUN yum install gzip --assumeyes

# Install the ASP.NET Core shared framework
RUN curl -SL --output aspnetcore.tar.gz https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/$ASPNET_VERSION/aspnetcore-runtime-$ASPNET_VERSION-linux-x64.tar.gz \
    && aspnetcore_sha512=$ASPNET_SHA512 \
    && echo "$aspnetcore_sha512  aspnetcore.tar.gz" | sha512sum -c - \
    && tar -ozxf aspnetcore.tar.gz -C /dotnet \
    && rm aspnetcore.tar.gz


FROM base as final
ARG ASPNET_VERSION

ENV DOTNET_VERSION $ASPNET_VERSION

COPY --from=builder-net5 ["/dotnet", "/var/lang/bin"]

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS builder
WORKDIR /src
COPY ["AccountManagementService.csproj", "base/"]  
RUN dotnet restore "base/AccountManagementService.csproj"  
WORKDIR /src
COPY . .  
RUN dotnet build "AccountManagementService.csproj" --configuration Release --output /app/build  

FROM builder AS publish
RUN dotnet publish "AccountManagementService.csproj" \  
            --configuration Release \   
            --runtime linux-x64 \  
            --self-contained false \   
            --output /app/publish \  
            -p:PublishReadyToRun=true    

FROM final
WORKDIR /var/task
ENV \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    # Lambda is opinionated about installing tooling under /var
    DOTNET_ROOT=/var/lang/bin \
    # Don't display welcome message on first run
    DOTNET_NOLOGO=true \
    # Disable Microsoft's telemetry collection
    DOTNET_CLI_TELEMETRY_OPTOUT=true
COPY --from=publish /app/publish .

CMD ["./AccountManagementService", " "]
