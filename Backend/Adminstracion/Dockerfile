FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Adminstracion.csproj ./
RUN dotnet restore Adminstracion.csproj
COPY . .
RUN dotnet publish Adminstracion.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Adminstracion.dll"]
