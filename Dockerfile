# Etapa 1: Build-ul aplicatiei
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiem fisierul de proiect si restauram pachetele (NuGet)
COPY *.csproj ./
RUN dotnet restore

# Copiem restul codului si compilam aplicatia in modul Release
COPY . ./
RUN dotnet publish -c Release -o out

# Etapa 2: Rularea aplicatiei (imagine optimizata pentru productie)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expunem portul necesar pentru Render
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comanda de start
ENTRYPOINT ["dotnet", "PremierAuto.dll"]