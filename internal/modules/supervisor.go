package modules

import (
	"context"
	"log/slog"
	"strings"
	"sync/atomic"
	"time"
)
type Message struct { Peer,Text string }
type Supervisor struct { Input chan Message; logger *slog.Logger; failures atomic.Int32 }
func NewSupervisor(logger *slog.Logger)*Supervisor{return &Supervisor{Input:make(chan Message,256),logger:logger}}
// Run isolates auxiliary work from the authoritative loop. Each task is recovered independently.
func(s *Supervisor)Run(ctx context.Context){for{select{case<-ctx.Done():return;case m:=<-s.Input:s.handleSafely(m)}}}
func(s *Supervisor)handleSafely(m Message){
	defer func(){if v:=recover();v!=nil{n:=s.failures.Add(1);if s.logger!=nil{s.logger.Error("auxiliary chat panic recovered","panic",v,"failures",n)};time.Sleep(time.Duration(min(int(n),5))*100*time.Millisecond)}}()
	if strings.TrimSpace(m.Text)==""{return};text:=strings.Map(func(r rune)rune{if r<32&&r!='\t'{return -1};return r},m.Text);if len(text)>240{text=text[:240]}
	if s.logger!=nil{s.logger.Debug("chat message accepted","peer",m.Peer,"bytes",len(text))}
}
