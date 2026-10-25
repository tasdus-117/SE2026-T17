# GAME-3D-03: authoritative arena server spike

## Project structure

```text
.
├── cmd/server/main.go              # UDP listener, fixed-rate authoritative simulation
├── cmd/retention/main.go           # PostgreSQL retention scheduler
├── internal/game/                  # fixed tick, movement validation, spatial grid/AoI
├── internal/network/               # bounded UDP ingress, binary protocol, snapshots
├── internal/modules/               # isolated auxiliary service supervisor
├── internal/storage/               # retention job and capacity logging guard
├── migrations/001_activity_logs.sql
├── test/load/simulator_786.go      # standalone 786 bot simulator and SLO assertions
├── docker-compose.yml
├── Makefile
└── .env.example
```

## Run

Install Go 1.23+ and Docker Compose. Set `GAME_AUTH_SECRET` to a random value of at least 16 bytes and copy `.env.example` to `.env`, then run:

```sh
make up       # PostgreSQL, authoritative server, and retention scheduler
make run-786  # starts 786 headless UDP clients and asserts the latency/drop SLOs
make down
```

The server defaults to UDP `:7777`, 30 Hz, a 20 unit/second movement cap, and a
1000 unit arena. Set `TICK_HZ=30` for 30 Hz. Snapshots are binary and only include
entities in the recipient's own or neighboring 64-unit grid cells. Inputs are
latest-value queued and consumed only on a server tick. Sequence numbers must
increase, vectors are finite and bounded, and disconnected sessions expire.

The server is authoritative: client coordinates are never accepted. Join packets
must carry a signed HMAC token; missing, expired, unsigned, or wrong-role sessions
are rejected before they can submit commands. `GAME_AUTH_SECRET` is required by
both server and simulator; production deployments should provision and rotate it
through their secret manager. The protocol uses UDP for this small-arena spike.
It has bounded ingress and chat queues plus a bounded snapshot output queue;
overloaded clients can lose snapshots without blocking the simulation tick.

`make run-786` starts 786 headless UDP clients for 60 seconds. The simulator records RTT from echoed client timestamps, datagram loss from input
sequence acknowledgements, and server process memory where the OS exposes it. It
exits nonzero if any bot fails to connect, snapshots stop, p95 latency exceeds
2.5 seconds, observed input loss exceeds 5%, or server heap grows by
more than 64 MiB after warm-up. These are spike acceptance checks, not a substitute for a
long-duration production soak test.

## Retention and operations

Apply `migrations/001_activity_logs.sql` once; `cmd/retention` runs its purge
immediately and then hourly. Complete daily partitions are dropped only after
their end boundary is at or before `NOW() - INTERVAL '49 days'`.
The capacity guard compares
`pg_database_size` with `STORAGE_CAPACITY_BYTES` (20 GiB by default), suppresses
INFO logs at 95%, and emits a CRITICAL diagnostic.

```sh
DATABASE_URL='postgres://game:game@localhost:5432/game?sslmode=disable' make retention
```

Operational, activity, session, snapshot, and metric records use daily range
partitions. The hourly scheduler creates partitions up to seven days ahead and
drops complete daily partitions once their upper bound is at or before the exact
49-day cutoff. Run `make retention-check` to validate that boundary without a
database connection.

`make test` runs the complete suite with the Go race detector. `make run-786`
expects the authoritative server to be running with the same `GAME_AUTH_SECRET`.
