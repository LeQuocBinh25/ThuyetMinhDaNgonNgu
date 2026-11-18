FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ThuyetMinhDaNgonNgu.sln ./
COPY src/ src/
RUN dotnet restore ThuyetMinhDaNgonNgu.sln
RUN dotnet publish src/ThuyetMinhDaNgonNgu.API/ThuyetMinhDaNgonNgu.API.csproj \
    --configuration Release \
    --no-restore \
    --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ThuyetMinhDaNgonNgu.API.dll"]
