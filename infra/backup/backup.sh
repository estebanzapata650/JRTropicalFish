#!/bin/sh
# Respaldo diario de PostgreSQL con rotación. Lo ejecuta el servicio `backup` de
# docker-compose.prod.yml (cron dentro del contenedor) o, si se prefiere, el cron del VPS:
#   0 2 * * * cd /opt/jrtropicalfish && docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm backup
#
# Retención: 7 diarios, 4 semanales (domingo), 6 mensuales (día 1).
# Si BACKUP_S3_BUCKET está definido, copia además el archivo al almacén S3 (fuera del VPS).
set -eu

DESTINO=/backups
FECHA=$(date +%Y-%m-%d_%H%M)
DIA_SEMANA=$(date +%u)
DIA_MES=$(date +%d)

mkdir -p "$DESTINO/diario" "$DESTINO/semanal" "$DESTINO/mensual"

ARCHIVO="$DESTINO/diario/jrtropicalfish_$FECHA.dump"
echo "[backup] generando $ARCHIVO"
PGPASSWORD="$POSTGRES_PASSWORD" pg_dump -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f "$ARCHIVO"

[ "$DIA_SEMANA" = "7" ] && cp "$ARCHIVO" "$DESTINO/semanal/"
[ "$DIA_MES" = "01" ] && cp "$ARCHIVO" "$DESTINO/mensual/"

# Rotación
ls -1t "$DESTINO/diario"/*.dump 2>/dev/null | tail -n +8 | xargs -r rm -f
ls -1t "$DESTINO/semanal"/*.dump 2>/dev/null | tail -n +5 | xargs -r rm -f
ls -1t "$DESTINO/mensual"/*.dump 2>/dev/null | tail -n +7 | xargs -r rm -f

if [ -n "${BACKUP_S3_BUCKET:-}" ]; then
  echo "[backup] copiando a s3://$BACKUP_S3_BUCKET/postgres/"
  aws ${S3_ENDPOINT:+--endpoint-url "$S3_ENDPOINT"} s3 cp "$ARCHIVO" "s3://$BACKUP_S3_BUCKET/postgres/"
fi

echo "[backup] listo"
