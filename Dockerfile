FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
# Plain HTTP only; TLS is handled by Cloudflare in front of the tunnel.
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FishyAPI.csproj", "./"]
RUN dotnet restore "FishyAPI.csproj"
COPY . .
RUN dotnet publish "FishyAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FishyAPI.dll"]
