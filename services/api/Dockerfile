# Sahno API container. Host-neutral: the same image runs on Render (test),
# DigitalOcean App Platform (production, D-065), or `docker run` locally.
# Build context is services/api:
#
#   docker build -t sahno-api services/api
#
# Configuration arrives only through environment variables at run time
# (ConnectionStrings__Sahno, Auth0__Domain, ...). Nothing secret is baked in.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project files alone, so the NuGet layer is cached
# until a dependency actually changes.
COPY src/Sahno.Api/Sahno.Api.csproj src/Sahno.Api/
COPY src/Sahno.Application/Sahno.Application.csproj src/Sahno.Application/
COPY src/Sahno.Contracts/Sahno.Contracts.csproj src/Sahno.Contracts/
COPY src/Sahno.Domain/Sahno.Domain.csproj src/Sahno.Domain/
COPY src/Sahno.Infrastructure/Sahno.Infrastructure.csproj src/Sahno.Infrastructure/
RUN dotnet restore src/Sahno.Api/Sahno.Api.csproj

COPY src/ src/
RUN dotnet publish src/Sahno.Api/Sahno.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Listens on 8080 unless the host supplies PORT (Program.cs honours it).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# The .NET images ship an unprivileged "app" user; run as it, not root.
USER $APP_UID

ENTRYPOINT ["dotnet", "Sahno.Api.dll"]
