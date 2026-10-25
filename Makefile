.PHONY: up down server retention test load benchmark

up:
	docker compose up --build -d

down:
	docker compose down

server:
	go run ./cmd/server

retention:
	go run ./cmd/retention

load:
	go run ./test/load -addr 127.0.0.1:7777 -bots 582 -duration 30s

test:
	go test ./...

benchmark: up
	go test ./...
	go run ./test/load -addr 127.0.0.1:7777 -bots 582 -duration 30s
