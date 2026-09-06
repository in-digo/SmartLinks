# syntax=docker/dockerfile:1

ARG NODE_TAG=22.23.2-alpine3.24
ARG NEWMAN_VERSION=6.2.2

FROM node:${NODE_TAG}
ARG NEWMAN_VERSION

RUN npm install --global --ignore-scripts --omit=optional --no-fund "newman@${NEWMAN_VERSION}" \
    && test "$(newman --version)" = "${NEWMAN_VERSION}" \
    && npm cache clean --force

WORKDIR /etc/newman

USER node
ENTRYPOINT ["newman"]