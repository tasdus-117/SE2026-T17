# Contract v1 and ownership

`POST /v1/combat`, UTF-8 JSON, Authorization: Bearer <local token>.
Development server binds loopback and maps its configured token to alice. It is not
a multi-user identity provider. Never expose this experimental server directly to the Internet.
Input exactly `{ "matchId": "m1", "commandId": "c1" }`; each identifier matches
`[a-zA-Z0-9_-]{1,64}`. Client cannot supply actor, health, coordinates or damage.
One fixed stationary target belongs to each match. Server-owned distance determines
eligibility <=1.5 m; successful unarmed attack subtracts 1 from 10 initial HP.
This slice does not simulate real 3D geometry, occlusion, tick rate or cooldown.

HTTP 200: `{ "version":1, "matchId":"m1", "commandId":"c1", "health":9, "status":"playing" }`.
At HP 0 status is finished. Replaying a committed identity returns the exact original
outcome, not necessarily current HP. Identity scope: (matchId, commandId), bound to owner.
Errors: 400 malformed JSON/fields/identifier, 401 invalid bearer, 403 wrong owner,
404 endpoint or match absent, 409 finished/out-of-range/conflicting identity,
413 body >4096 bytes, 500 unexpected failure with no internal details exposed.
Authentication precedes parsing. Ownership precedes replay. Existing committed
replays remain successful even after finish. A failed transaction does not reserve identity.

Compatibility: v1 route and response remain stable across v1/v2 storage writers;
unknown request fields rejected, so new input fields require a new contract version.
New writer's nullable events.detail is invisible to API v1. combat.v1 durable event
is inserted inside the same transaction; optional onCommitted observer runs afterward,
is best effort and may repeat. Mandatory effects must consume a future durable outbox.

Application owns command coordination; Match owns business rules; Store owns all
domain tables and transaction execution. Retention accesses only Store.purge.
No module may independently UPDATE health. UnitOfWork is a structural interface:
transaction(callback), callback receives match/command/save/remember/event operations.
Composition root injects Store; domain imports no transport or database implementation.
SQLite serializes writers using BEGIN IMMEDIATE. One writer per database file is assumed.

Configuration: 764 concurrent tasks, p95 <=4500 ms; operational events retained 7 days.
Identity and match data are correctness data and do not expire with operational logs.
Respond to change requests within 12 hours (organizational obligation; not automated
completion SLA). Request/response body bounded; no production queue admission policy yet.

Rollback: expand nullable detail, deploy new writer, compare HTTP result with authoritative
state, stop failed version, restore old writer against expanded schema. Keep new data;
do not DROP detail on rollback. Contract stage requires separately retiring old writers
and approving a rollback window. The local demo never executes destructive DOWN.
