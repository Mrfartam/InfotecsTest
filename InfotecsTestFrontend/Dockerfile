# Этап 1: Сборка приложения
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

# Этап 2: Раздача через Nginx
FROM nginx:alpine
COPY --from=build /app/dist/InfotecsTestFrontend/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]