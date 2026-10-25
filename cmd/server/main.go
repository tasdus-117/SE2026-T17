package main

import (
	"context"
	"log/slog"
	"os"
	"os/signal"
	"syscall"
	"game3d/internal/game"
	"game3d/internal/modules"
	"game3d/internal/network"
)
func main(){
	logger:=slog.New(slog.NewJSONHandler(os.Stdout,nil));hz:=30
	if raw:=os.Getenv("TICK_HZ");raw!=""{if raw!="20"&&raw!="30"{logger.Error("TICK_HZ must be 20 or 30");os.Exit(2)};if raw=="30"{hz=30}}
	world:=game.NewWorld(hz,20,1000);aux:=modules.NewSupervisor(logger)
	addr:=os.Getenv("SERVER_ADDR");if addr==""{addr=":7777"};server,err:=network.Listen(addr,world,hz,logger,aux);if err!=nil{logger.Error("listen failed","error",err);os.Exit(1)};defer func(){if closeErr:=server.Close();closeErr!=nil{logger.Error("server socket close failed","error",closeErr)}}()
	ctx,stop:=signal.NotifyContext(context.Background(),os.Interrupt,syscall.SIGTERM);defer stop();go aux.Run(ctx)
	logger.Info("authoritative server started","addr",addr,"tick_hz",hz)
	if err:=server.Run(ctx);err!=nil{logger.Error("server stopped","error",err);os.Exit(1)}
}
