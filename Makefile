.PHONY: up down server retention test load benchmark run-786 retention-check

up:
	docker compose up --build -d

down:
	docker compose down

server:
	go run ./cmd/server

retention:
	go run ./cmd/retention

load:
	go run ./test/load -addr 127.0.0.1:7777 -bots 786 -duration 60s

test:
	go test -v -race ./...

run-786:
	go run ./test/load -addr 127.0.0.1:7777 -bots 786 -duration 60s

retention-check:
	go run ./cmd/retention -check

benchmark: up
	go test ./...
	go run ./test/load -addr 127.0.0.1:7777 -bots 786 -duration 60s
