# 05 · Despliegue en VPS (Docker Compose)

Guía para poner JR Tropical Fish en producción en un servidor virtual propio.

## 1. Requisitos del servidor

| Recurso | Mínimo | Recomendado |
|---|---|---|
| CPU / RAM | 2 vCPU / 4 GB | 4 vCPU / 8 GB |
| Disco | 40 GB SSD | 80 GB SSD |
| SO | Ubuntu 24.04 LTS | — |
| Red | IP pública, puertos 80 y 443 abiertos | — |

Proveedores de referencia: Hetzner, DigitalOcean, Contabo, Vultr. Un dominio con un registro `A` apuntando a
la IP del VPS (ej. `app.jrtropicalfish.com`).

## 2. Preparar el servidor (una vez)

```bash
# Usuario de despliegue, sin login por contraseña
adduser deploy && usermod -aG sudo deploy
# Copiar la llave SSH pública a ~deploy/.ssh/authorized_keys y en /etc/ssh/sshd_config:
#   PasswordAuthentication no   PermitRootLogin no

# Firewall: solo SSH, HTTP y HTTPS
ufw allow OpenSSH && ufw allow 80/tcp && ufw allow 443/tcp && ufw allow 443/udp && ufw enable

# Actualizaciones de seguridad automáticas
apt install -y unattended-upgrades

# Docker Engine + plugin compose (repositorio oficial de Docker)
curl -fsSL https://get.docker.com | sh
usermod -aG docker deploy

# Zona horaria
timedatectl set-timezone America/Bogota
```

## 3. Primer despliegue

```bash
sudo mkdir -p /opt/jrtropicalfish && sudo chown deploy: /opt/jrtropicalfish
cd /opt/jrtropicalfish
git clone https://github.com/estebanzapata650/JRTropicalFish.git .

cp .env.prod.example .env.prod
nano .env.prod          # DOMINIO, ACME_EMAIL, POSTGRES_PASSWORD, JWT_SIGNING_KEY, S3_*, SENDGRID_API_KEY
chmod 600 .env.prod
# Generar secretos: openssl rand -base64 48

npm run prod:build      # o: docker compose -f docker-compose.prod.yml --env-file .env.prod build
npm run prod:migrar     # aplica migraciones (la API con --migrar) y sale
npm run prod:up         # levanta todo y espera healthchecks
```

Si el VPS no tiene Node, usar los comandos `docker compose` equivalentes que están en `package.json`.

### Crear el primer administrador

En producción no se siembra un usuario por configuración. Crear el primero con SQL, usando un hash generado
por la propia aplicación:

1. En desarrollo, crear el usuario desde la pantalla de Usuarios (cuando exista) o tomar el
   `password_hash` de un usuario local con la contraseña deseada.
2. En el VPS:
   ```bash
   docker compose -f docker-compose.prod.yml --env-file .env.prod exec postgres \
     psql -U jrtropicalfish -d jrtropicalfish -c \
     "CALL sp_usuario_crear(gen_random_uuid(), 'Nombre'::text, 'correo@dominio.com'::text, '<hash>'::text,
       fn_perfil_id_por_nombre('Administrador'), true, now());"
   ```
3. Iniciar sesión y cambiar la contraseña.

> Pendiente (cuando exista el módulo de usuarios): comando `--crear-admin` en la API para evitar este paso manual.

## 4. Actualizar a una nueva versión

```bash
cd /opt/jrtropicalfish
git pull
export VERSION=$(git rev-parse --short HEAD)    # etiqueta las imágenes con el commit
npm run prod:build
npm run prod:migrar                              # si falla, NO se toca lo que está corriendo
npm run prod:up
docker image prune -f
```

## 5. Rollback

```bash
git checkout <commit-anterior>
export VERSION=<hash-anterior>                   # si la imagen sigue en el servidor, no recompila
npm run prod:up
```

⚠️ Las migraciones **no** se revierten solas. Si la versión nueva cambió el esquema de forma incompatible,
restaurar el respaldo previo al despliegue (paso 6) o aplicar la migración `Down` de forma controlada.
Regla práctica: tomar un respaldo manual antes de cada despliegue con migraciones.

## 6. Respaldos

- El servicio `backup` corre `pg_dump` según `BACKUP_CRON` (por defecto 02:00) y deja los archivos en
  `/opt/jrtropicalfish/backups/{diario,semanal,mensual}`, con retención 7 / 4 / 6.
- Si `BACKUP_S3_BUCKET` tiene valor, copia cada respaldo al bucket (fuera del VPS). **Recomendado.**
- Respaldo manual inmediato:
  ```bash
  docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm backup /scripts/backup.sh
  ```
- Restaurar (destructivo):
  ```bash
  docker compose -f docker-compose.prod.yml --env-file .env.prod stop api
  docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm backup \
    /scripts/restaurar.sh /backups/diario/jrtropicalfish_AAAA-MM-DD_HHMM.dump
  docker compose -f docker-compose.prod.yml --env-file .env.prod start api
  ```
- **Probar una restauración al menos una vez por trimestre** en una base aparte. Un respaldo que nunca se
  restauró no es un respaldo.
- Archivos (S3): con R2/S3 gestionado, activar versionado del bucket. Con Garage propio, respaldar sus
  volúmenes.

## 7. Operación diaria

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod ps
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f api
curl -s https://<DOMINIO>/login -o /dev/null -w "%{http_code}\n"
```

La API no se expone: para revisar su salud desde el VPS,
`docker compose ... exec api curl -s localhost:8080/api/v1/health`.

## 8. Correo (SendGrid) — pasos manuales

1. Crear la cuenta y una API key con permiso solo de *Mail Send*.
2. **Autenticar el dominio** (registros DNS CNAME que da SendGrid). Sin esto SendGrid responde 202 y no
   entrega.
3. Poner `CORREO_PROVEEDOR=SendGrid`, `SENDGRID_API_KEY` y `CORREO_REMITENTE` (del dominio autenticado) en
   `.env.prod`.
4. En un ambiente de pruebas, `CORREO_REDIRIGIR_TODO_A=<tu correo>` evita escribirle a clientes reales.

## 9. Seguridad: checklist

- [ ] SSH solo con llave, sin root; `ufw` activo; `unattended-upgrades` activo.
- [ ] `.env.prod` con permisos 600 y secretos generados (no reutilizar los de desarrollo).
- [ ] Solo Caddy publica puertos (`docker compose ps` no muestra 5432 ni 8080 hacia afuera).
- [ ] HTTPS con HSTS (Caddyfile) y certificado válido.
- [ ] Respaldos con copia fuera del VPS y una restauración probada.
- [ ] Monitoreo externo de disponibilidad sobre `https://<DOMINIO>/login`.

## CI/CD (fase 2)

Cuando el repositorio esté en uso continuo, automatizar con GitHub Actions:

1. En cada PR: `dotnet test`, `npm run lint`, `npm run build:web`.
2. En `main`: construir imágenes y publicarlas en GitHub Container Registry (`ghcr.io`) con la etiqueta del
   commit.
3. Desplegar por SSH: `git pull`, `docker compose pull`, `--migrar`, `up -d`.

Secretos del workflow: llave SSH de `deploy`, host del VPS. Los secretos de la aplicación siguen viviendo
solo en `.env.prod` del servidor.
