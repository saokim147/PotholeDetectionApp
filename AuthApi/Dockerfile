FROM node:23.3.0-alpine

WORKDIR /usr/src/app
COPY package*.json ./

RUN npm install

COPY . .

EXPOSE 3000
cmd ["npm","run","dev"]