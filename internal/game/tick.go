package game

import ("context";"time")

// RunTickLoop runs a fixed 30 Hz authoritative tick. Background work belongs
// on the supplied worker channel and never executes inside the tick callback.
func RunTickLoop(ctx context.Context, tick func(time.Time), background <-chan func(), hz int) {
	if hz<1 {hz=30};ticker:=time.NewTicker(time.Second/time.Duration(hz));defer ticker.Stop()
	for{select{case<-ctx.Done():return;case task,ok:=<-background:if ok&&task!=nil{go task()};case now:=<-ticker.C:if tick!=nil{tick(now)}}}
}
