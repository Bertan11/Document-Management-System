# ---- Build Stage ----
FROM node:18 AS build
WORKDIR /app

# Dependencies installieren
COPY package*.json ./
RUN npm install

# Projekt kopieren & builden
COPY . .
RUN npm run build

# ---- Production Stage ----
FROM nginx:alpine
# React-Build in Nginx Webroot kopieren
COPY --from=build /app/build /usr/share/nginx/html

# Eigene nginx.conf (damit React-Router funktioniert)
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
