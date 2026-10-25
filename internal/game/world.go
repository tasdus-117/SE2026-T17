package game

import (
	"math"
	"sync"
	"time"
)

const CellSize = 64.0

type Input struct {
	Sequence uint32
	ClientNanos int64
	DX, DY, DZ float32
}

type EntitySnapshot struct { ID uint32; X, Y, Z float32 }
type Snapshot struct { Tick uint64; SelfID, Ack uint32; EchoNanos int64; Entities []EntitySnapshot }

type entity struct {
	id uint32
	x, y, z float64
	lastInput Input
	hasInput bool
	lastSequence uint32
	lastSeen time.Time
}

type World struct {
	mu sync.RWMutex
	entities map[string]*entity
	byID map[uint32]*entity
	grid map[cell]map[uint32]*entity
	nextID uint32
	tick uint64
	maxSpeed, bound float64
	step time.Duration
	sessionTimeout time.Duration
}
type cell struct { X, Y, Z int }

func NewWorld(hz int, maxSpeed, bound float64) *World {
	if hz < 1 { hz = 20 }; if maxSpeed <= 0 { maxSpeed = 20 }; if bound <= 0 { bound = 1000 }
	return &World{entities: map[string]*entity{}, byID: map[uint32]*entity{}, grid: map[cell]map[uint32]*entity{}, maxSpeed:maxSpeed, bound:bound, step:time.Second/time.Duration(hz), sessionTimeout:15*time.Second}
}
func (w *World) Join(key string, now time.Time) uint32 {
	w.mu.Lock(); defer w.mu.Unlock()
	if e := w.entities[key]; e != nil { e.lastSeen = now; return e.id }
	w.nextID++; if w.nextID == 0 { w.nextID++ }
	e := &entity{id:w.nextID,lastSeen:now}; w.entities[key]=e; w.byID[e.id]=e; w.putGrid(e)
	return e.id
}
func (w *World) Submit(key string, in Input, now time.Time) bool {
	w.mu.Lock(); defer w.mu.Unlock()
	e := w.entities[key]; if e == nil { return false }
	if e.hasInput && !seqAfter(in.Sequence, e.lastSequence) { return false }
	if !finite(float64(in.DX)) || !finite(float64(in.DY)) || !finite(float64(in.DZ)) { return false }
	norm := math.Sqrt(float64(in.DX*in.DX + in.DY*in.DY + in.DZ*in.DZ))
	if norm > 1.001 { return false }
	e.lastSequence=in.Sequence; e.lastInput=in; e.hasInput=true; e.lastSeen=now
	return true
}
func seqAfter(a,b uint32) bool { return int32(a-b)>0 }
func finite(v float64) bool { return !math.IsNaN(v) && !math.IsInf(v,0) }
func (w *World) Tick(now time.Time) {
	w.mu.Lock(); defer w.mu.Unlock(); w.tick++
	for key,e := range w.entities {
		if now.Sub(e.lastSeen)>w.sessionTimeout { delete(w.entities,key); delete(w.byID,e.id); w.removeGrid(e); continue }
		if !e.hasInput { continue }
		old := locate(e)
		dt:=w.step.Seconds(); nx:=e.x+float64(e.lastInput.DX)*w.maxSpeed*dt; ny:=e.y+float64(e.lastInput.DY)*w.maxSpeed*dt; nz:=e.z+float64(e.lastInput.DZ)*w.maxSpeed*dt
		// Clamp at the arena edge so valid movement cannot accumulate out-of-bounds state.
		e.x=math.Max(-w.bound,math.Min(w.bound,nx)); e.y=math.Max(-w.bound,math.Min(w.bound,ny)); e.z=math.Max(-w.bound,math.Min(w.bound,nz))
		if old != locate(e) { w.removeGridAt(e,old); w.putGrid(e) }
	}
}
func (w *World) Snapshots() map[string]Snapshot {
	w.mu.RLock(); defer w.mu.RUnlock()
	out:=make(map[string]Snapshot,len(w.entities))
	for key,self:=range w.entities {
		c:=locate(self); s:=Snapshot{Tick:w.tick,SelfID:self.id,Ack:self.lastSequence,EchoNanos:self.lastInput.ClientNanos}
		s.Entities=append(s.Entities,EntitySnapshot{self.id,float32(self.x),float32(self.y),float32(self.z)})
		for dx:=-1;dx<=1;dx++ { for dy:=-1;dy<=1;dy++ { for dz:=-1;dz<=1;dz++ {
			for _,e:=range w.grid[cell{c.X+dx,c.Y+dy,c.Z+dz}] { if e.id==self.id||len(s.Entities)>=73{continue}; s.Entities=append(s.Entities,EntitySnapshot{e.id,float32(e.x),float32(e.y),float32(e.z)}) }
		} } }
		out[key]=s
	}
	return out
}
func (w *World) TickCount() uint64 { w.mu.RLock(); defer w.mu.RUnlock(); return w.tick }
func locate(e *entity) cell { return cell{int(math.Floor(e.x/CellSize)),int(math.Floor(e.y/CellSize)),int(math.Floor(e.z/CellSize))} }
func (w *World) putGrid(e *entity) { c:=locate(e); if w.grid[c]==nil { w.grid[c]=map[uint32]*entity{} }; w.grid[c][e.id]=e }
func (w *World) removeGrid(e *entity) { w.removeGridAt(e,locate(e)) }
func (w *World) removeGridAt(e *entity,c cell) { if m:=w.grid[c];m!=nil { delete(m,e.id); if len(m)==0 { delete(w.grid,c) } } }
