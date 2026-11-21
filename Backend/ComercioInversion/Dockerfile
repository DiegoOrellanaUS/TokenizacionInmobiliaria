FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ComercioInversion.csproj ./
RUN dotnet restore ComercioInversion.csproj
COPY . .
RUN dotnet publish ComercioInversion.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ComercioInversion.dll"]
