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
├── test/load/main.go               # standalone 582 bot simulator and SLO assertions
├── docker-compose.yml
├── Makefile
└── .env.example
```

## Run

Install Go 1.23+ and Docker Compose. Copy `.env.example` to `.env`, then run:

```sh
make up       # PostgreSQL, authoritative server, and retention scheduler
make load     # starts 582 headless UDP clients and asserts the latency/drop SLOs
make down
```

The server defaults to UDP `:7777`, 20 Hz, a 20 unit/second movement cap, and a
1000 unit arena. Set `TICK_HZ=30` for 30 Hz. Snapshots are binary and only include
entities in the recipient's own or neighboring 64-unit grid cells. Inputs are
latest-value queued and consumed only on a server tick. Sequence numbers must
increase, vectors are finite and bounded, and disconnected sessions expire.

The server is authoritative: client coordinates are never accepted. The protocol
uses UDP for this small-arena spike; it has no encryption or account authentication
and should sit behind an authenticated transport/gateway before public deployment.
It has bounded ingress and chat queues plus a bounded snapshot output queue;
overloaded clients can lose snapshots without blocking the simulation tick.

`make load` records RTT from echoed client timestamps, datagram loss from input
sequence acknowledgements, and server process memory where the OS exposes it. It
exits nonzero if any bot fails to connect, snapshots stop, p95 broadcast/echo
latency reaches 150 ms, observed input loss exceeds 5%, or server heap grows by
more than 64 MiB after warm-up. These are spike acceptance checks, not a substitute for a
long-duration production soak test.

## Retention and operations

Apply `migrations/001_activity_logs.sql` once; `cmd/retention` runs its purge
immediately and then hourly. Rows where `created_at < NOW() - INTERVAL '59 days'`
are deleted from activity logs, activity snapshots, session logs, and metrics.
The capacity guard compares
`pg_database_size` with `STORAGE_CAPACITY_BYTES` (20 GiB by default), suppresses
INFO logs at 95%, and emits a CRITICAL diagnostic.

```sh
DATABASE_URL='postgres://game:game@localhost:5432/game?sslmode=disable' make retention
```

All four data classes are partitioned monthly. The hourly scheduler keeps the
current and next two monthly partitions ready, then deletes expired rows using
the exact 59-day cutoff.
