# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
RUN mkdir code
WORKDIR code
COPY src/*.sln .
COPY src/API ./API
COPY src/API.Tests ./API.Tests
RUN dotnet restore
RUN dotnet test
RUN dotnet build
RUN dotnet publish -c release -o /out --no-restore API/API.csproj

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "API.dll"]