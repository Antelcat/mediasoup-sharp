FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY . .
EXPOSE 80

ENTRYPOINT ["dotnet", "Antelcat.MediasoupSharp.Demo.dll"]
