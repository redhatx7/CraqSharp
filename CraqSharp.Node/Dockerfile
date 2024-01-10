FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CraqSharp.Node/CraqSharp.Node.csproj", "CraqSharp.Node/"]
RUN dotnet restore "CraqSharp.Node/CraqSharp.Node.csproj"
COPY . .
WORKDIR "/src/CraqSharp.Node"
RUN dotnet build "CraqSharp.Node.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CraqSharp.Node.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CraqSharp.Node.dll"]
