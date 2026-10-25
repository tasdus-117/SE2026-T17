package refactor

import (
	"testing"
	"game3d/internal/auth"
	"game3d/internal/game"
	"game3d/internal/network"
)

type legacyCompatibleCommand struct { got *network.RequestContext }
func(c *legacyCompatibleCommand)Execute(ctx *game.RequestContext)error{c.got=ctx;return nil}
func TestRequestContextSignatureAndMetadataRemainCompatible(t *testing.T){ctx:=&network.RequestContext{Security:auth.SecurityContext{UserID:"u",SessionID:"s",IsAuthenticated:true},IdemKey:"k",Timestamp:123,TraceID:"trace",Payload:[]byte("x")};cmd:=&legacyCompatibleCommand{};if err:=game.Handle(ctx,cmd);err!=nil{t.Fatal(err)};if cmd.got!=ctx||cmd.got.TraceID!="trace"||cmd.got.Timestamp!=123{t.Fatalf("request context was not preserved: %+v",cmd.got)}}
