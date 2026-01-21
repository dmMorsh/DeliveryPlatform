FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/OrderService.Api/OrderService.Api.csproj", "src/OrderService/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/OrderService.Api/OrderService.Api.csproj"

COPY . .
RUN dotnet build "src/OrderService.Api/OrderService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/OrderService.Api/OrderService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5001
ENTRYPOINT ["dotnet", "OrderService.dll"]
