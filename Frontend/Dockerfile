FROM node:24-alpine AS build-env
WORKDIR /App

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts
COPY . ./
RUN npm run build

FROM node:24-alpine
WORKDIR /App

COPY --from=build-env /App/.output .

# NUXT_PUBLIC_SERVER_URL sets the voice server address at runtime
ENV PORT=3000
EXPOSE 3000/tcp

ENTRYPOINT ["node", "server/index.mjs"]
