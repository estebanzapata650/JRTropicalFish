# Contenedor de respaldos: pg_dump (misma versión mayor que el servidor) + cron + aws-cli.
FROM postgres:16-alpine
RUN apk add --no-cache aws-cli tzdata
COPY backup.sh restaurar.sh entrypoint.sh /scripts/
RUN chmod +x /scripts/*.sh
ENTRYPOINT ["/scripts/entrypoint.sh"]
