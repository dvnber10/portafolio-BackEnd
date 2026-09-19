FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PortfolioApi.sln ./
COPY src/PortfolioApi.Domain/PortfolioApi.Domain.csproj src/PortfolioApi.Domain/
COPY src/PortfolioApi.Application/PortfolioApi.Application.csproj src/PortfolioApi.Application/
COPY src/PortfolioApi.Infrastructure/PortfolioApi.Infrastructure.csproj src/PortfolioApi.Infrastructure/
COPY src/PortfolioApi.Api/PortfolioApi.Api.csproj src/PortfolioApi.Api/
RUN dotnet restore PortfolioApi.sln --verbosity quiet

COPY src src
RUN dotnet publish src/PortfolioApi.Api/PortfolioApi.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PortfolioApi.Api.dll"]