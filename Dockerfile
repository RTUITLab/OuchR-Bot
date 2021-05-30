FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

WORKDIR /app

COPY . .

RUN dotnet publish -c Release -o /output ./API


FROM mcr.microsoft.com/dotnet/aspnet:5.0

WORKDIR /output
COPY --from=build-env /output .


ENV ASPNETCORE_URLS=http://*:5000

CMD dotnet API.dll