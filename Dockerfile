FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY . .
RUN dotnet restore src/PokemonApi.Api/PokemonApi.Api.csproj
RUN dotnet publish src/PokemonApi.Api/PokemonApi.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet PokemonApi.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
