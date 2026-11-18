#!/bin/sh
# Restaura un respaldo sobre la base de producción. DESTRUCTIVO: reemplaza los datos actuales.
#   docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm backup \
#     /scripts/restaurar.sh /backups/diario/jrtropicalfish_2026-11-18_0200.dump
# Detener antes la API (`docker compose ... stop api`) para que nadie escriba durante la restauración.
set -eu
ARCHIVO="${1:?Uso: restaurar.sh <archivo.dump>}"
echo "[restaurar] restaurando $ARCHIVO sobre $POSTGRES_DB"
PGPASSWORD="$POSTGRES_PASSWORD" pg_restore -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner "$ARCHIVO"
echo "[restaurar] listo"
