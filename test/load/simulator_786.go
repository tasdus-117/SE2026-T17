// Command load simulates authoritative UDP clients and checks latency and loss.
package main

import (
	"encoding/binary"
	"flag"
	"fmt"
	"math"
	"math/rand"
	"net"
	"os"
	"runtime"
	"sort"
	"sync"
	"sync/atomic"
	"time"
	"game3d/internal/auth"
)

type sample struct { rtt time.Duration }
type heapSample struct { at time.Time; bytes uint64 }
type bot struct { conn *net.UDPConn; sent atomic.Uint32; ack atomic.Uint32; acknowledged atomic.Uint32; received atomic.Uint64; lastSnapshot atomic.Int64; failures atomic.Uint64; actionRequests atomic.Uint64; actionReplies atomic.Uint64; idempotentReplays atomic.Uint64; auditMutations atomic.Uint64; mu sync.Mutex; latencies []time.Duration; heaps []heapSample }

func main() {
	addr:=flag.String("addr","127.0.0.1:7777","server UDP address")
	botsN:=flag.Int("bots",786,"concurrent virtual users")
	duration:=flag.Duration("duration",60*time.Second,"measurement duration")
	flag.Parse()
	if *botsN<1||*botsN>10000 { fmt.Fprintln(os.Stderr,"bots must be between 1 and 10000");os.Exit(2) }
	bots:=make([]*bot,*botsN)
	for i:=range bots {
		c,err:=net.DialUDP("udp",nil,mustResolve(*addr));if err!=nil{fatal("dial: %v",err)}
		b:=&bot{conn:c};bots[i]=b
		secret:=os.Getenv("GAME_AUTH_SECRET");if len(secret)<16{fatal("GAME_AUTH_SECRET must contain at least 16 bytes")};token:=auth.SignToken([]byte(secret),fmt.Sprintf("bot-%d",i),fmt.Sprintf("session-%d",i),time.Now().Add(*duration+2*time.Minute),"player")
		if _,err=c.Write(append([]byte{1},[]byte(token)...));err!=nil{fatal("join: %v",err)}
		if err:=c.SetReadDeadline(time.Now().Add(3*time.Second));err!=nil{fatal("set welcome deadline: %v",err)};welcome:=make([]byte,64);n,err:=c.Read(welcome);if err!=nil||n!=5||welcome[0]!=0x81{fatal("bot %d did not receive welcome: %v",i,err)}
	}
	fmt.Printf("connected %d bots to %s\n",*botsN,*addr)
	start:=time.Now();stop:=make(chan struct{});var wg sync.WaitGroup
	for i,b:=range bots { wg.Add(1);go func(i int,b *bot){defer wg.Done();runBot(i,b,stop)}(i,b) }
	var before runtime.MemStats;runtime.ReadMemStats(&before)
	time.Sleep(*duration);close(stop);wg.Wait();elapsed:=time.Since(start)
	all:=make([]time.Duration,0);var sent,acked,received,failures,actionRequests,actionReplies,idempotentReplays,auditMutations uint64;var heaps []heapSample
	for _,b:=range bots { b.mu.Lock();all=append(all,b.latencies...);heaps=append(heaps,b.heaps...);b.mu.Unlock();sent+=uint64(b.sent.Load());acked+=uint64(b.acknowledged.Load());received+=b.received.Load();failures+=b.failures.Load();actionRequests+=b.actionRequests.Load();actionReplies+=b.actionReplies.Load();idempotentReplays+=b.idempotentReplays.Load();auditMutations+=b.auditMutations.Load();if closeErr:=b.conn.Close();closeErr!=nil{failures++} }
	var after runtime.MemStats;runtime.ReadMemStats(&after)
	if len(all)==0 {fatal("no snapshots received; server may have crashed")}
	sort.Slice(all,func(i,j int)bool{return all[i]<all[j]})
	p95:=all[(len(all)-1)*95/100];loss:=float64(0);if sent>0&&acked<sent{loss=float64(sent-acked)/float64(sent)}
	drops:=uint64(0);if acked<sent{drops=sent-acked}
	var mem runtime.MemStats;runtime.ReadMemStats(&mem)
	// Ignore the first five seconds of startup allocations, then compare server
	// heap growth against the first sample taken after warm-up.
	var baseline, peak uint64;var baselineAt time.Time
	for _,h:=range heaps { if h.at.Sub(start)>=5*time.Second { if baselineAt.IsZero()||h.at.Before(baselineAt){baseline=h.bytes;baselineAt=h.at};if h.bytes>peak{peak=h.bytes} } }
	growth:=uint64(0);if peak>baseline{growth=peak-baseline}
	fmt.Printf("elapsed=%s active_bots=%d snapshots=%d sent=%d acked=%d estimated_unacked=%.2f%% p95_rtt=%s action_retries=%d action_replies=%d idempotent_replays=%d audit_mutations=%d drops=%d errors=%d server_heap_growth=%d harness_heap=%d\n",elapsed,len(bots),received,sent,acked,loss*100,p95,actionRequests,actionReplies,idempotentReplays,auditMutations,drops,failures,growth,mem.HeapAlloc)
	if failures>0 {fatal("observed client I/O errors")}
	if p95>2500*time.Millisecond {fatal("p95 response latency %s exceeds the 2.5 second SLA",p95)}
	if loss>0.05 {fatal("estimated unacknowledged input rate %.2f%% exceeds 5%%",loss*100)}
	if baselineAt.IsZero() {fatal("server did not return memory telemetry after warm-up")}
	if growth>64*1024*1024 {fatal("server heap grew by %d bytes after warm-up",growth)}
	for i,b:=range bots {last:=time.Unix(0,b.lastSnapshot.Load());if last.IsZero()||time.Since(last)>time.Second{fatal("bot %d stopped receiving snapshots; server liveness check failed",i)}}
	if len(bots)!=*botsN {fatal("active bots=%d, want %d",len(bots),*botsN)}
	fmt.Println("PASS: active bots, snapshots, p95 latency and input-loss assertions passed")
}

func runBot(index int,b *bot,stop <-chan struct{}) {
	defer func(){if recover()!=nil{b.failures.Add(1)}}()
	rng:=rand.New(rand.NewSource(int64(index+1)));readDone:=make(chan struct{})
	go func(){defer close(readDone);buf:=make([]byte,2048);for{select{case<-stop:return;default:}
		if deadlineErr:=b.conn.SetReadDeadline(time.Now().Add(200*time.Millisecond));deadlineErr!=nil{b.failures.Add(1);return};n,err:=b.conn.Read(buf);if err!=nil{if ne,ok:=err.(net.Error);ok&&ne.Timeout(){continue};select{case<-stop:return;default:b.failures.Add(1);return}}
		if n==5&&buf[0]==0x85 {b.acknowledged.Add(1);ack:=binary.BigEndian.Uint32(buf[1:5]);for{old:=b.ack.Load();if int32(ack-old)<=0||b.ack.CompareAndSwap(old,ack){break}};continue}
		if n>=6&&buf[0]==0x86{b.actionReplies.Add(1);continue}
		if n==33&&buf[0]==0x84 {h:=heapSample{at:time.Now(),bytes:binary.BigEndian.Uint64(buf[9:17])};b.idempotentReplays.Store(binary.BigEndian.Uint64(buf[21:29]));b.auditMutations.Store(uint64(binary.BigEndian.Uint32(buf[29:33])));b.mu.Lock();b.heaps=append(b.heaps,h);b.mu.Unlock();continue}
		if n<27||buf[0]!=0x82{continue};b.received.Add(1);b.lastSnapshot.Store(time.Now().UnixNano());ack:=binary.BigEndian.Uint32(buf[13:17]);for{old:=b.ack.Load();if int32(ack-old)<=0||b.ack.CompareAndSwap(old,ack){break}}
		echo:=int64(binary.BigEndian.Uint64(buf[17:25]));if echo>0{rtt:=time.Since(time.Unix(0,echo));if rtt>=0{b.mu.Lock();b.latencies=append(b.latencies,rtt);b.mu.Unlock()}}
	}}()
	statusTicker:=time.NewTicker(5*time.Second);defer statusTicker.Stop();if _,err:=b.conn.Write([]byte{4});err!=nil{b.failures.Add(1);return}
	ticker:=time.NewTicker(50*time.Millisecond);defer ticker.Stop();var x,y,z float32
	for{select{case<-stop:<-readDone;return;case<-statusTicker.C:if _,err:=b.conn.Write([]byte{4});err!=nil{b.failures.Add(1);return};case<-ticker.C:
		x+=float32(rng.Float64()*0.4-0.2);y+=float32(rng.Float64()*0.4-0.2);z+=float32(rng.Float64()*0.4-0.2);norm:=float32(math.Sqrt(float64(x*x+y*y+z*z)));if norm>1{x/=norm;y/=norm;z/=norm}
		seq:=b.sent.Add(1);packet:=make([]byte,25);packet[0]=2;binary.BigEndian.PutUint32(packet[1:5],seq);binary.BigEndian.PutUint64(packet[5:13],uint64(time.Now().UnixNano()));binary.BigEndian.PutUint32(packet[13:17],math.Float32bits(x));binary.BigEndian.PutUint32(packet[17:21],math.Float32bits(y));binary.BigEndian.PutUint32(packet[21:25],math.Float32bits(z));if _,err:=b.conn.Write(packet);err!=nil{b.failures.Add(1);return}
		if seq%50==0{kind:=byte(5);if (seq/50)%2==0{kind=6};key:=fmt.Sprintf("bot-%d-%d",index,seq);action:=append([]byte{kind},[]byte(key)...);b.actionRequests.Add(2);if _,err:=b.conn.Write(action);err!=nil{b.failures.Add(1);return};if _,err:=b.conn.Write(action);err!=nil{b.failures.Add(1);return}}
	}}
}
func mustResolve(addr string)*net.UDPAddr{a,err:=net.ResolveUDPAddr("udp",addr);if err!=nil{fatal("resolve: %v",err)};return a}
func fatal(format string,args ...any){fmt.Fprintf(os.Stderr,"FAIL: "+format+"\n",args...);os.Exit(1)}
