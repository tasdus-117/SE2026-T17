package game

import (
	"testing"
	"time"
)

func TestSubmitValidatesSequenceAndMovement(t *testing.T) {
	now := time.Now()
	w := NewWorld(20, 20, 1000)
	w.Join("peer", now)
	if !w.Submit("peer", Input{Sequence: 1, ClientNanos: 123, DX: 1}, now) {
		t.Fatal("valid directional input was rejected")
	}
	if w.Submit("peer", Input{Sequence: 1, DX: 1}, now) {
		t.Fatal("duplicate input sequence was accepted")
	}
	if w.Submit("peer", Input{Sequence: 2, DX: 2}, now) {
		t.Fatal("over-speed input vector was accepted")
	}
	w.Tick(now.Add(50 * time.Millisecond))
	s := w.Snapshots()["peer"]
	if s.Ack != 1 || len(s.Entities) != 1 || s.Entities[0].X < 0.999 || s.Entities[0].X > 1.001 {
		t.Fatalf("unexpected authoritative snapshot: %+v", s)
	}
}

func TestSnapshotsCullEntitiesOutsideNeighboringGridCells(t *testing.T) {
	now := time.Now()
	w := NewWorld(20, 20, 1000)
	w.Join("moving", now)
	w.Join("stationary", now)
	if !w.Submit("moving", Input{Sequence: 1, DX: 1}, now) {
		t.Fatal("movement input rejected")
	}
	for i := 0; i < 200; i++ { w.Tick(now) }
	snapshots := w.Snapshots()
	if got := len(snapshots["moving"].Entities); got != 1 {
		t.Fatalf("moving client's AoI contains %d entities, want only itself", got)
	}
}
