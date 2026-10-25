package game

import (
	"sync"
	"testing"
	"time"
	"game3d/internal/idem"
)

func TestWeaponPickupAssignedExactlyOnce(t *testing.T){a:=NewArena();const n=64;var wg sync.WaitGroup;var mu sync.Mutex;responses:=0
	guard:=idem.NewGuard(time.Minute)
	for i:=0;i<n;i++{wg.Add(1);go func(i int){defer wg.Done();for{ok,weapon,err:=a.CollectWeaponIdempotent(guard,"pickup-transaction","player");if err==idem.ErrInProgress{time.Sleep(time.Millisecond);continue};if err!=nil{t.Errorf("pickup request: %v",err);return};if ok&&weapon=="rifle"{mu.Lock();responses++;mu.Unlock()};return}}(i)};wg.Wait()
	if responses!=n||a.Inventory("player")!="rifle"||a.AuditRecords()!=1{t.Fatalf("cached responses=%d inventory=%q audit=%d",responses,a.Inventory("player"),a.AuditRecords())}
}
