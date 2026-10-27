FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ResellerOrderApi.Api/ResellerOrderApi.Api.csproj src/ResellerOrderApi.Api/
COPY src/ResellerOrderApi.Core/ResellerOrderApi.Core.csproj src/ResellerOrderApi.Core/
COPY src/ResellerOrderApi.Infrastructure/ResellerOrderApi.Infrastructure.csproj src/ResellerOrderApi.Infrastructure/
RUN dotnet restore src/ResellerOrderApi.Api/ResellerOrderApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/ResellerOrderApi.Api/ResellerOrderApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ResellerOrderApi.Api.dll"]