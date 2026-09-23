# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventHub.Api/EventHub.Api.csproj", "src/EventHub.Api/"]
COPY ["src/EventHub.Application/EventHub.Application.csproj", "src/EventHub.Application/"]
COPY ["src/EventHub.Domain/EventHub.Domain.csproj", "src/EventHub.Domain/"]
COPY ["src/EventHub.Infrastructure/EventHub.Infrastructure.csproj", "src/EventHub.Infrastructure/"]

RUN dotnet restore "src/EventHub.Api/EventHub.Api.csproj"

COPY . .
RUN dotnet publish "src/EventHub.Api/EventHub.Api.csproj" \
	--configuration Release \
	--output /app/publish \
	--no-restore \
	/p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "EventHub.Api.dll"]
