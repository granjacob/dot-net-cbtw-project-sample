FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["ServiceFlow.Requests.slnx", "."]
COPY ["src/ServiceFlow.Requests.Domain/ServiceFlow.Requests.Domain.csproj", "src/ServiceFlow.Requests.Domain/"]
COPY ["src/ServiceFlow.Requests.Application/ServiceFlow.Requests.Application.csproj", "src/ServiceFlow.Requests.Application/"]
COPY ["src/ServiceFlow.Requests.Infrastructure/ServiceFlow.Requests.Infrastructure.csproj", "src/ServiceFlow.Requests.Infrastructure/"]
COPY ["src/ServiceFlow.Requests.Api/ServiceFlow.Requests.Api.csproj", "src/ServiceFlow.Requests.Api/"]
RUN dotnet restore "src/ServiceFlow.Requests.Api/ServiceFlow.Requests.Api.csproj"

COPY . .
RUN dotnet publish "src/ServiceFlow.Requests.Api/ServiceFlow.Requests.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiceFlow.Requests.Api.dll"]
