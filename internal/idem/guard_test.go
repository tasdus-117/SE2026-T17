package idem

import (
	"fmt"
	"sync"
	"sync/atomic"
	"testing"
	"time"
)

func TestConcurrentDuplicateMutationRunsOnce(t *testing.T) {
	guard:=NewGuard(time.Minute);var awards,audits atomic.Int32;const workers=50
	var wg sync.WaitGroup;responses:=make(chan []byte,workers);errors:=make(chan error,workers)
	for i:=0;i<workers;i++{wg.Add(1);go func(){defer wg.Done();for{status,response,err:=guard.AcquireOrGet("weapon-pickup-42");if status==Completed{responses<-response;return};if err!=nil{if err==ErrInProgress{time.Sleep(time.Millisecond);continue};errors<-err;return};awards.Add(1);audits.Add(1);response=[]byte("awarded");if err:=guard.Complete("weapon-pickup-42",response);err!=nil{errors<-err;return};responses<-response;return}}()}
	wg.Wait();close(responses);close(errors);for err:=range errors{t.Fatal(err)};for r:=range responses{if string(r)!="awarded"{t.Fatalf("unexpected cached response %q",r)}}
	if got:=awards.Load();got!=1{t.Fatalf("weapon awarded %d times",got)};if got:=audits.Load();got!=1{t.Fatalf("audit records %d, want 1",got)}
	if _,response,err:=guard.AcquireOrGet("weapon-pickup-42");err!=nil||fmt.Sprint(string(response))!="awarded"{t.Fatalf("completed response not replayed: %q, %v",response,err)}
}
