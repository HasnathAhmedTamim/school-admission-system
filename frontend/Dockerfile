# syntax=docker/dockerfile:1

FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM deps AS build
COPY . .
RUN npm run build

FROM deps AS test
COPY . .
CMD ["npm", "test"]

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Scripts in /docker-entrypoint.d run before nginx starts; this one writes config.js from VITE_API_BASE_URL.
COPY --chmod=0755 docker/write-runtime-config.sh /docker-entrypoint.d/40-write-runtime-config.sh
COPY --from=build /app/dist /usr/share/nginx/html
ENV VITE_API_BASE_URL=http://localhost:8080
EXPOSE 3000
