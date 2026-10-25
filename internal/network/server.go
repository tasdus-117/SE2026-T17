package network

import (
	"context"
	"fmt"
	"log/slog"
	"net"
	"os"
	"runtime"
	"sync"
	"sync/atomic"
	"time"
	"game3d/internal/game"
	"game3d/internal/auth"
	"game3d/internal/modules"
	"game3d/internal/idem"
)

type outbound struct { addr *net.UDPAddr; payload []byte }
type Server struct { conn *net.UDPConn; world *game.World; arena *game.Arena; idem *idem.IdempotencyGuard; hz int; log *slog.Logger; chat *modules.Supervisor; clients sync.Map; security sync.Map; verifier auth.TokenVerifier; output chan outbound; droppedSnapshots atomic.Uint64 }
func Listen(addr string,w *game.World,hz int,logger *slog.Logger,chat *modules.Supervisor)(*Server,error){
	secret:=os.Getenv("GAME_AUTH_SECRET");if len(secret)<16{return nil,fmt.Errorf("GAME_AUTH_SECRET must contain at least 16 bytes")};a,err:=net.ResolveUDPAddr("udp",addr); if err!=nil{return nil,err}; c,err:=net.ListenUDP("udp",a);if err!=nil{return nil,err};if hz<1{hz=20};return &Server{conn:c,world:w,arena:game.NewArena(),idem:idem.NewGuard(60*time.Second),hz:hz,log:logger,chat:chat,verifier:auth.HMACTokenVerifier{Secret:[]byte(secret)},output:make(chan outbound,4096)},nil
}
func(s *Server)Close()error{return s.conn.Close()}
func(s *Server)Run(ctx context.Context)error{
	readErr:=make(chan error,1);go func(){readErr<-s.readLoop(ctx)}()
	go s.outputLoop(ctx)
	ticker:=time.NewTicker(time.Second/time.Duration(s.hz));defer ticker.Stop()
	for{select{case<-ctx.Done():return nil;case err:=<-readErr:return err;case now:=<-ticker.C:
		s.world.Tick(now); snapshots:=s.world.Snapshots()
		for key,snap:=range snapshots { v,ok:=s.clients.Load(key);if !ok{continue};select{case s.output<-outbound{addr:v.(*net.UDPAddr),payload:encodeSnapshot(snap)}:default:n:=s.droppedSnapshots.Add(1);if n==1||n%1000==0{logger:=s.log;if logger==nil{logger=slog.Default()};logger.Warn("snapshot output queue full; updates dropped","dropped",n)}} }
		s.clients.Range(func(key, _ any)bool{if _,alive:=snapshots[key.(string)];!alive{s.clients.Delete(key)};return true})
	}}
}
func(s *Server)outputLoop(ctx context.Context){for{select{case<-ctx.Done():return;case item:=<-s.output:s.send(item.addr,item.payload)}}}
func(s *Server)readLoop(ctx context.Context)error{
	buf:=make([]byte,maxPacket+1)
	for{if err:=s.conn.SetReadDeadline(time.Now().Add(time.Second));err!=nil{return err};n,addr,err:=s.conn.ReadFromUDP(buf);if err!=nil{if ne,ok:=err.(net.Error);ok&&ne.Timeout(){select{case<-ctx.Done():return nil;default:continue}};select{case<-ctx.Done():return nil;default:return err}}
		key:=addr.String();msg,err:=decode(buf[:n]);if err!=nil{if s.log!=nil{s.log.Debug("malformed client datagram","remote",key,"error",err)};continue}
		switch msg.kind{
		case packetJoin:
			packet:=auth.Packet{Token:msg.token,UserID:key,SessionID:key,Payload:append([]byte(nil),buf[:n]...)}
			handler:=auth.AuthMiddleware(s.verifier,func(_ context.Context,_ auth.Packet,security auth.SecurityContext)error{id:=s.world.Join(key,time.Now());s.clients.Store(key,addr);s.security.Store(key,security);s.send(addr,encodeWelcome(id));return nil})
			if authErr:=handler(ctx,packet,auth.SecurityContext{});authErr!=nil&&s.log!=nil{s.log.Debug("unauthenticated join rejected","remote",key)}
		case packetStatus:
			if !s.isAuthorized(key){continue};var mem runtime.MemStats;runtime.ReadMemStats(&mem);var sessions uint32;s.clients.Range(func(_, _ any)bool{sessions++;return true});s.send(addr,encodeStatus(s.world.TickCount(),mem.HeapAlloc,sessions,s.idem.ReplayCount(),uint32(s.arena.AuditRecords())))
		case packetInput:
			if !s.isAuthorized(key){continue};if s.world.Submit(key,msg.input,time.Now()){s.send(addr,encodeInputAck(msg.input.Sequence))}
		case packetChat:
			if !s.isAuthorized(key)||s.chat==nil{continue};select{case s.chat.Input<-modules.Message{Peer:key,Text:msg.chat}:default:s.send(addr,encodeChatReply("chat busy"))}
		case packetPickup,packetPunch:
			if !s.isAuthorized(key){continue};security,_:=s.security.Load(key);verified:=security.(auth.SecurityContext);request:=&RequestContext{Security:verified,IdemKey:verified.SessionID+":"+msg.idemKey,Timestamp:time.Now().UnixNano(),TraceID:key+":"+msg.idemKey,Payload:append([]byte(nil),buf[:n]...)};command:=&arenaCommand{arena:s.arena,guard:s.idem,kind:msg.kind,player:key};if game.Handle(request,command)==nil{s.send(addr,encodeActionReply(msg.kind,command.health,command.weapon))}
		}
	}
}
func(s *Server)isAuthorized(key string)bool{if _,ok:=s.clients.Load(key);!ok{return false};v,ok:=s.security.Load(key);if !ok{return false};security:=v.(auth.SecurityContext);return security.IsAuthenticated&&security.Expiry.After(time.Now())&&security.HasRole("player")}
type arenaCommand struct{arena *game.Arena;guard *idem.IdempotencyGuard;kind byte;player string;health int;weapon string}
func(c *arenaCommand)Execute(ctx *game.RequestContext)error{if c.kind==packetPickup{ok,weapon,err:=c.arena.CollectWeaponIdempotent(c.guard,ctx.IdemKey,c.player);if ok{c.health=1};c.weapon=weapon;return err};health,err:=c.arena.PunchIdempotent(c.guard,ctx.IdemKey);c.health=health;return err}
func(s *Server)send(addr *net.UDPAddr,payload []byte){if _,err:=s.conn.WriteToUDP(payload,addr);err!=nil{logger:=s.log;if logger==nil{logger=slog.Default()};logger.Debug("client datagram send failed","remote",addr.String(),"error",err)}}
