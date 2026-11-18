# Web Next.js (salida standalone). Contexto de build: la raíz del repositorio.
#   docker build -f infra/docker/web.Dockerfile -t jrtropicalfish-web .
FROM node:24-slim AS build
WORKDIR /src
COPY apps/web/package*.json ./
RUN npm ci --ignore-scripts
COPY apps/web/ .
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:24-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
COPY --from=build --chown=node:node /src/public ./public
COPY --from=build --chown=node:node /src/.next/standalone ./
COPY --from=build --chown=node:node /src/.next/static ./.next/static
USER node
EXPOSE 3000
CMD ["node", "server.js"]
