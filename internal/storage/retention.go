package storage

import (
	"context"
	"database/sql"
	"errors"
	"log/slog"
	"sync/atomic"
	"time"
)

const PurgeSQL = `DELETE FROM activity_logs WHERE created_at < NOW() - INTERVAL '59 days'`
var purgeTables = []string{"activity_logs", "activity_snapshots", "session_logs", "system_metrics"}

// PurgeExpired removes records older than the fixed 59-day retention window.
func PurgeExpired(ctx context.Context, db *sql.DB) (int64, error) {
	tx, err := db.BeginTx(ctx, nil)
	if err != nil { return 0, err }
	if _, err := tx.ExecContext(ctx, `SELECT ensure_game_log_partitions()`); err != nil {
		if rollbackErr := tx.Rollback(); rollbackErr != nil { return 0, errors.Join(err, rollbackErr) }
		return 0, err
	}
	var total int64
	for _, table := range purgeTables {
		result, execErr := tx.ExecContext(ctx, `DELETE FROM `+table+` WHERE created_at < NOW() - INTERVAL '59 days'`)
		if execErr != nil { if rollbackErr := tx.Rollback(); rollbackErr != nil { return 0, errors.Join(execErr, rollbackErr) }; return 0, execErr }
		n, rowsErr := result.RowsAffected()
		if rowsErr != nil { if rollbackErr := tx.Rollback(); rollbackErr != nil { return 0, errors.Join(rowsErr, rollbackErr) }; return 0, rowsErr }
		total += n
	}
	if err := tx.Commit(); err != nil { return 0, err }
	return total, nil
}

// RunRetention performs a purge at startup and then on each interval until canceled.
func RunRetention(ctx context.Context, db *sql.DB, logger *slog.Logger, interval time.Duration, capacityBytes uint64) error {
	if logger == nil { logger = slog.Default() }
	if interval <= 0 { interval = time.Hour }
	guard := CapacityGuard{Logger: logger}
	if capacityBytes == 0 { capacityBytes = 20 * 1024 * 1024 * 1024 }
	if _, err := PurgeExpired(ctx, db); err != nil { return err }
	if guard.AllowDatabaseInfo(ctx, db, capacityBytes) { logger.Info("retention purge completed") }
	ticker := time.NewTicker(interval)
	defer ticker.Stop()
	for {
		select {
		case <-ctx.Done(): return ctx.Err()
		case <-ticker.C:
			n, err := PurgeExpired(ctx, db)
			if err != nil { logger.Error("activity retention purge failed", "error", err); continue }
			if guard.AllowDatabaseInfo(ctx, db, capacityBytes) { logger.Info("retention purge completed", "deleted", n) }
		}
	}
}

// CapacityGuard gates informational logs and emits a critical alert at threshold.
type CapacityGuard struct { Logger *slog.Logger; critical atomic.Bool }

// AllowInfo reports whether informational storage logs are permitted.
func (g *CapacityGuard) AllowInfo(usedBytes, totalBytes uint64) bool {
	if totalBytes == 0 { return false }
	if usedBytes >= totalBytes-totalBytes/20 {
		if g.critical.CompareAndSwap(false, true) && g.Logger != nil { g.Logger.Error("CRITICAL: storage capacity at or above 95%", "used_bytes", usedBytes, "total_bytes", totalBytes) }
		return false
	}
	g.critical.Store(false)
	return true
}

// AllowDatabaseInfo checks database size against the configured storage budget.
func (g *CapacityGuard) AllowDatabaseInfo(ctx context.Context, db *sql.DB, capacityBytes uint64) bool {
	if capacityBytes == 0 { return g.AllowInfo(0, 0) }
	var used int64
	if err := db.QueryRowContext(ctx, `SELECT pg_database_size(current_database())`).Scan(&used); err != nil {
		if g.critical.CompareAndSwap(false, true) && g.Logger != nil { g.Logger.Error("CRITICAL: unable to inspect storage capacity", "error", err) }
		return false
	}
	return g.AllowInfo(uint64(used), capacityBytes)
}
