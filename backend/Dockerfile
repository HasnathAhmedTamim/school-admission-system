# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so package downloads are cached until a project file changes.
COPY SchoolAdmission.sln ./
COPY src/SchoolAdmission.Core/SchoolAdmission.Core.csproj src/SchoolAdmission.Core/
COPY src/SchoolAdmission.Application/SchoolAdmission.Application.csproj src/SchoolAdmission.Application/
COPY src/SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj src/SchoolAdmission.Infrastructure/
COPY src/SchoolAdmission.API/SchoolAdmission.API.csproj src/SchoolAdmission.API/
COPY tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj tests/SchoolAdmission.Tests/
RUN dotnet restore SchoolAdmission.sln

COPY . .

# Used by the "test" compose profile: docker compose --profile test run --rm backend-tests
FROM build AS test
RUN dotnet build tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj -c Release --no-restore
ENTRYPOINT ["dotnet", "test", "tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]

FROM build AS publish
RUN dotnet publish src/SchoolAdmission.API/SchoolAdmission.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SchoolAdmission.API.dll"]
