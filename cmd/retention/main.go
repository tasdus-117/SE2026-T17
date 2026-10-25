package main

import (
	"context"
	"database/sql"
	"log/slog"
	"os"
	"os/signal"
	"strconv"
	"syscall"
	"time"

	_ "github.com/jackc/pgx/v5/stdlib"
	"game3d/internal/storage"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
	url := os.Getenv("DATABASE_URL")
	if url == "" { logger.Error("DATABASE_URL is required"); os.Exit(2) }
	db, err := sql.Open("pgx", url)
	if err != nil { logger.Error("open database", "error", err); os.Exit(1) }
	defer func(){if closeErr:=db.Close();closeErr!=nil{logger.Error("database close failed","error",closeErr)}}()
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()
	capacity := uint64(20 * 1024 * 1024 * 1024)
	if raw := os.Getenv("STORAGE_CAPACITY_BYTES"); raw != "" {
		parsed, parseErr := strconv.ParseUint(raw, 10, 64)
		if parseErr != nil || parsed == 0 { logger.Error("STORAGE_CAPACITY_BYTES must be a positive integer"); os.Exit(2) }
		capacity = parsed
	}
	if err := storage.RunRetention(ctx, db, logger, time.Hour, capacity); err != nil && err != context.Canceled {
		logger.Error("retention scheduler stopped", "error", err); os.Exit(1)
	}
}
