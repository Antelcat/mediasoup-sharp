FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Antelcat.MediasoupSharp.AspNetCore/Antelcat.MediasoupSharp.AspNetCore.csproj", "Antelcat.MediasoupSharp.AspNetCore/"]
COPY ["Antelcat.MediasoupSharp/Antelcat.MediasoupSharp.csproj", "Antelcat.MediasoupSharp/"]
COPY ["../libuv-sharp/src/Antelcat.LibuvSharp/Antelcat.LibuvSharp.csproj", "../libuv-sharp/src/Antelcat.LibuvSharp/"]
RUN dotnet restore "Antelcat.MediasoupSharp.AspNetCore/Antelcat.MediasoupSharp.AspNetCore.csproj"
COPY . .
WORKDIR "/src/Antelcat.MediasoupSharp.AspNetCore"
RUN dotnet build "Antelcat.MediasoupSharp.AspNetCore.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Antelcat.MediasoupSharp.AspNetCore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Antelcat.MediasoupSharp.AspNetCore.dll"]
