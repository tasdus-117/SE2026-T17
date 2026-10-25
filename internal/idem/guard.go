package idem

import (
	"errors"
	"sync"
	"time"
)

type Status uint8
const ( New Status = iota; InProgress; Completed )
var ErrInProgress = errors.New("idempotent request is already in progress")
type entry struct { status Status; response []byte; expires time.Time }
type IdempotencyGuard struct { mu sync.Mutex; entries map[string]entry; ttl time.Duration; now func() time.Time; nextSweep time.Time; replays uint64 }
func NewGuard(ttl time.Duration) *IdempotencyGuard { if ttl <= 0 { ttl = 60*time.Second }; return &IdempotencyGuard{entries:make(map[string]entry),ttl:ttl,now:time.Now} }

// AcquireOrGet atomically reserves a new key or returns its completed response.
// An in-progress key is rejected so concurrent retries cannot run the mutation.
func (g *IdempotencyGuard) AcquireOrGet(key string) (Status, []byte, error) {
	if key == "" { return New, nil, errors.New("idempotency key is required") }
	now := g.now(); g.mu.Lock(); defer g.mu.Unlock()
	if !g.nextSweep.After(now) { for k, v := range g.entries { if !v.expires.After(now) { delete(g.entries,k) } }; g.nextSweep=now.Add(g.ttl/2) }
	if old, ok := g.entries[key]; ok { if !old.expires.After(now) { delete(g.entries,key) } else if old.status == Completed { g.replays++; old.expires=now.Add(g.ttl);g.entries[key]=old;return Completed, append([]byte(nil), old.response...), nil } else { return InProgress, nil, ErrInProgress } }
	g.entries[key] = entry{status:InProgress,expires:now.Add(g.ttl)}
	return New,nil,nil
}

// Complete commits the response to the cache. Completion refreshes its TTL.
func (g *IdempotencyGuard) Complete(key string, response []byte) error {
	g.mu.Lock(); defer g.mu.Unlock(); e, ok := g.entries[key]; if !ok || e.status != InProgress { return errors.New("idempotency key is not reserved") }
	e.status=Completed; e.response=append([]byte(nil),response...); e.expires=g.now().Add(g.ttl); g.entries[key]=e; return nil
}
func (g *IdempotencyGuard) Abandon(key string) { g.mu.Lock(); delete(g.entries,key); g.mu.Unlock() }
func (g *IdempotencyGuard) ReplayCount() uint64 { g.mu.Lock(); defer g.mu.Unlock(); return g.replays }
