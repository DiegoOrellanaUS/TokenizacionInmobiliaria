FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Adminstracion/Adminstracion.csproj Adminstracion/
COPY Login/Login.csproj Login/
RUN dotnet restore Login/Login.csproj
COPY Adminstracion/ Adminstracion/
COPY Login/ Login/
RUN dotnet publish Login/Login.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Login.dll"]
