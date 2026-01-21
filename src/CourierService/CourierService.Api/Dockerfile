FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/CourierService/CourierService.csproj", "src/CourierService/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/CourierService/CourierService.csproj"

COPY . .
RUN dotnet build "src/CourierService/CourierService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/CourierService/CourierService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5002
ENTRYPOINT ["dotnet", "CourierService.dll"]
