FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The restore comes before the code, so that a code change reuses the packages of the layer cache.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/VaccinationCard.Api/VaccinationCard.Api.csproj src/VaccinationCard.Api/
RUN dotnet restore src/VaccinationCard.Api/VaccinationCard.Api.csproj

COPY src/ src/
RUN dotnet publish src/VaccinationCard.Api/VaccinationCard.Api.csproj --configuration Release --no-restore --output /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0

# "Today" depends on the zone of App__TimeZone (RN12). The image has tzdata today, but the 10.0 tag moves.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VaccinationCard.Api.dll"]
