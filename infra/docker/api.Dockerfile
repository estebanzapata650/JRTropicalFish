# API .NET. Contexto de build: la raíz del repositorio.
#   docker build -f infra/docker/api.Dockerfile -t jrtropicalfish-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo lo que define dependencias, para que la capa de `restore` se reutilice mientras no
# cambien los paquetes. Se copia la solución .slnx (no *.sln: la solución usa el formato nuevo).
COPY apps/api/JRTropicalFish.slnx apps/api/Directory.Build.props apps/api/global.json ./
COPY apps/api/src/JRTropicalFish.Api/JRTropicalFish.Api.csproj src/JRTropicalFish.Api/
COPY apps/api/src/JRTropicalFish.Application/JRTropicalFish.Application.csproj src/JRTropicalFish.Application/
COPY apps/api/src/JRTropicalFish.Domain/JRTropicalFish.Domain.csproj src/JRTropicalFish.Domain/
COPY apps/api/src/JRTropicalFish.Infrastructure/JRTropicalFish.Infrastructure.csproj src/JRTropicalFish.Infrastructure/
RUN dotnet restore src/JRTropicalFish.Api/JRTropicalFish.Api.csproj

COPY apps/api/src/ src/
RUN dotnet publish src/JRTropicalFish.Api/JRTropicalFish.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# QuestPDF necesita fontconfig para medir y dibujar texto. Sin esto el PDF sale sin contenido al
# desplegar, y el fallo NO se reproduce en Windows, donde las fuentes ya están disponibles.
# tzdata: para que TZ=America/Bogota funcione (fechas de vencimiento, "hoy").
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core tzdata curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "JRTropicalFish.Api.dll"]
