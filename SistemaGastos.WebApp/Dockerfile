# 1. Etapa de construcción (Build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos TODA la solución junta (todas las carpetas)
COPY . .

# Entramos a la carpeta de tu proyecto principal y lo compilamos
# (Asegurate de que el nombre de la carpeta y del csproj coincidan con los tuyos)
WORKDIR "/SistemaGastos.WebApp"
RUN dotnet restore "SistemaGastos.WebApp.csproj"
RUN dotnet build "SistemaGastos.WebApp.csproj" -c Release -o /app/build

# 2. Etapa de publicación (Publish)
FROM build AS publish
RUN dotnet publish "SistemaGastos.WebApp/SistemaGastos.WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Etapa final (Run)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .

# ATENCIÓN: El nombre del .dll debe ser el mismo que el de tu .csproj principal
ENTRYPOINT ["dotnet", "SistemaGastos.WebApp.dll"]