FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Global/Global.csproj Global/
RUN dotnet restore Global/Global.csproj
COPY Global/ Global/
COPY Reportes/Exportacion/ Reportes/Exportacion/
RUN dotnet publish Global/Global.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Global.dll"]
