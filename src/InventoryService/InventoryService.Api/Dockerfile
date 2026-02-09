FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "src/InventoryService/InventoryService.Api/InventoryService.Api.csproj"
RUN dotnet publish "src/InventoryService/InventoryService.Api/InventoryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
