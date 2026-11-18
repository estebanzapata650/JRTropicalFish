#!/bin/sh
# Sin argumentos: programa el respaldo con cron y queda en primer plano (servicio `backup`).
# Con argumentos: ejecuta ese comando una vez (ej. `/scripts/backup.sh` o `/scripts/restaurar.sh x.dump`).
set -eu

if [ "$#" -gt 0 ]; then
  exec "$@"
fi

# El cron de busybox no hereda las variables del contenedor: se guardan (ya escapadas) para el job.
export -p | grep -E "export (POSTGRES_|BACKUP_|S3_ENDPOINT|AWS_|TZ)" > /scripts/env.sh || true
echo "${BACKUP_CRON:-0 2 * * *} . /scripts/env.sh && /scripts/backup.sh >> /proc/1/fd/1 2>&1" > /etc/crontabs/root
echo "[backup] programado: ${BACKUP_CRON:-0 2 * * *}"
exec crond -f -l 8
