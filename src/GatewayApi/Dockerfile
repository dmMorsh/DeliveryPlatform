FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/GatewayApi/GatewayApi.csproj", "src/GatewayApi/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/GatewayApi/GatewayApi.csproj"

COPY . .
RUN dotnet build "src/GatewayApi/GatewayApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/GatewayApi/GatewayApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "GatewayApi.dll"]
