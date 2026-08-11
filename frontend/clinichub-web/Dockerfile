FROM node:22.20-alpine AS build
WORKDIR /workspace
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
RUN addgroup -S -g 10001 clinichub \
    && adduser -S -D -H -u 10001 -G clinichub clinichub \
    && mkdir -p /var/cache/nginx /var/run /usr/share/nginx/html \
    && chown -R clinichub:clinichub /var/cache/nginx /var/run /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build --chown=clinichub:clinichub /workspace/dist/clinichub-web/browser /usr/share/nginx/html
USER clinichub
EXPOSE 8080
