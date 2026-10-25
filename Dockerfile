FROM golang:1.23-alpine AS build
WORKDIR /src
COPY go.mod ./
RUN go mod download
COPY . .
RUN mkdir -p /out && CGO_ENABLED=0 go build -trimpath -ldflags="-s -w" -o /out/server ./cmd/server && \
    CGO_ENABLED=0 go build -trimpath -ldflags="-s -w" -o /out/retention ./cmd/retention

FROM alpine:3.21
RUN adduser -D -H game
COPY --from=build /out/server /server
COPY --from=build /out/retention /retention
USER game
EXPOSE 7777/udp
ENTRYPOINT ["/server"]
