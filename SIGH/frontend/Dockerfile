# Front do SIGH: build com Node (mesma versão do CI) e servido por nginx SEM root.
#   docker build -t sigh-web SIGH/frontend
# Em runtime, /api/ é encaminhado para ${SIGH_API_UPSTREAM} (padrão http://api:3000).

# Stage 1: build (Node 24, como em .github/workflows/frontend-ci.yml)
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npx vite build

# Stage 2: nginx sem root (escuta 8080, usuário nginx uid 101)
FROM nginxinc/nginx-unprivileged:1.29-alpine AS final
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
ENV SIGH_API_UPSTREAM=http://api:3000
EXPOSE 8080
USER 101
