FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ServiceFlow.Notifications.Domain/ServiceFlow.Notifications.Domain.csproj", "src/ServiceFlow.Notifications.Domain/"]
COPY ["src/ServiceFlow.Notifications.Application/ServiceFlow.Notifications.Application.csproj", "src/ServiceFlow.Notifications.Application/"]
COPY ["src/ServiceFlow.Notifications.Infrastructure/ServiceFlow.Notifications.Infrastructure.csproj", "src/ServiceFlow.Notifications.Infrastructure/"]
COPY ["src/ServiceFlow.Notifications.Api/ServiceFlow.Notifications.Api.csproj", "src/ServiceFlow.Notifications.Api/"]
RUN dotnet restore "src/ServiceFlow.Notifications.Api/ServiceFlow.Notifications.Api.csproj"

COPY . .
RUN dotnet publish "src/ServiceFlow.Notifications.Api/ServiceFlow.Notifications.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ServiceFlow.Notifications.Api.dll"]
