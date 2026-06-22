FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SistemaGastos.Domain/ SistemaGastos.Domain/
COPY SistemaGastos.Application/ SistemaGastos.Application/
COPY SistemaGastos.Infraestructure/ SistemaGastos.Infraestructure/
COPY SistemaGastos.WebApp/ SistemaGastos.WebApp/

WORKDIR /src/SistemaGastos.WebApp
RUN dotnet restore "SistemaGastos.WebApp.csproj"
RUN dotnet publish "SistemaGastos.WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SistemaGastos.WebApp.dll"]
