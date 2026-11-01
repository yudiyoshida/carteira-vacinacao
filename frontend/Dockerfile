FROM node:22-slim AS build
WORKDIR /app

# The install comes before the code, so that a code change reuses the packages of the layer cache.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:stable-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/vaccination-card-web/browser /usr/share/nginx/html
