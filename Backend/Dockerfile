FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-env
WORKDIR /App

COPY . ./
RUN dotnet restore
RUN dotnet publish ./MultiVoice.Server/MultiVoice.Server.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /App

COPY --from=build-env /App/out .

# appsettings.json binds to localhost for dev; unprefixed env vars override it
ENV Urls=http://+:5000
EXPOSE 5000/tcp

ENTRYPOINT ["dotnet", "MultiVoice.Server.dll"]
