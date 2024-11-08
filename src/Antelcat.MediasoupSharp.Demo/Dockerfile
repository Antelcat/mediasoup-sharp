FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

COPY . .
ENTRYPOINT ["dotnet", "Antelcat.MediasoupSharp.Demo.dll", "--urls https://[::]:80"]
