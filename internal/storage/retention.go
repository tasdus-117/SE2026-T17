package storage

import (
	"context"
	"database/sql"
	"fmt"
	"log/slog"
	"regexp"
	"strings"
	"sync/atomic"
	"time"
)

const RetentionDays = 49
var partitionTables = []string{"arena_activity_logs","activity_logs","activity_snapshots","session_logs","system_metrics"}
var partitionName = regexp.MustCompile(`^(arena_activity_logs|activity_logs|activity_snapshots|session_logs|system_metrics)_([0-9]{8})$`)

// PurgeExpiredPartitions drops only complete daily partitions whose upper
// boundary is at or before now-retentionDays. It never interpolates catalog
// input into SQL without validating and quoting the identifier.
func PurgeExpiredPartitions(db *sql.DB, retentionDays int) error {
	if retentionDays <= 0 { return fmt.Errorf("retention days must be positive") }
	return PurgeExpiredPartitionsContext(context.Background(),db,retentionDays)
}
func PurgeExpiredPartitionsContext(ctx context.Context, db *sql.DB, retentionDays int) error {
	if db==nil || retentionDays<=0 { return fmt.Errorf("database and positive retention days are required") }
	if _,err:=db.ExecContext(ctx,`SELECT ensure_arena_activity_partitions()`);err!=nil{return err}
	cutoff:=RetentionCutoff(time.Now(),retentionDays)
	rows,err:=db.QueryContext(ctx,`SELECT parent.relname, child.relname FROM pg_inherits i JOIN pg_class parent ON parent.oid=i.inhparent JOIN pg_class child ON child.oid=i.inhrelid JOIN pg_namespace n ON n.oid=parent.relnamespace WHERE n.nspname=current_schema()`)
	if err!=nil{return err};defer rows.Close()
	type drop struct{parent,child string};var drops []drop
	for rows.Next(){var parent,child string;if err:=rows.Scan(&parent,&child);err!=nil{return err};if !allowedParent(parent){continue};m:=partitionName.FindStringSubmatch(child);if m==nil||m[1]!=parent{continue};d,err:=time.Parse("20060102",m[2]);if err!=nil{continue};if !d.AddDate(0,0,1).After(cutoff){drops=append(drops,drop{parent,child})}}
	if err:=rows.Err();err!=nil{return err};if err:=rows.Close();err!=nil{return err};tx,err:=db.BeginTx(ctx,nil);if err!=nil{return err}
	for _,p:=range drops { if _,err=tx.ExecContext(ctx,`DROP TABLE IF EXISTS `+quoteIdent(p.child));err!=nil{_ = tx.Rollback();return err} }
	return tx.Commit()
}
func allowedParent(name string)bool{for _,v:=range partitionTables{if name==v{return true}};return false}
func quoteIdent(s string)string{return `"`+strings.ReplaceAll(s,`"`,`""`)+`"`}

// Keep the legacy row purge callable for deployments that have not yet applied
// the partition migration; the scheduler uses partition drops exclusively.
func PurgeExpired(ctx context.Context,db *sql.DB)(int64,error){if err:=PurgeExpiredPartitionsContext(ctx,db,RetentionDays);err!=nil{return 0,err};return 0,nil}
func RunRetention(ctx context.Context,db *sql.DB,logger *slog.Logger,interval time.Duration,capacityBytes uint64)error{
	if logger==nil{logger=slog.Default()};if interval<=0{interval=time.Hour};guard:=CapacityGuard{Logger:logger};if capacityBytes==0{capacityBytes=20*1024*1024*1024}
	if err:=PurgeExpiredPartitionsContext(ctx,db,RetentionDays);err!=nil{return err};if guard.AllowDatabaseInfo(ctx,db,capacityBytes){logger.Info("49-day partition retention purge completed")}
	ticker:=time.NewTicker(interval);defer ticker.Stop();for{select{case<-ctx.Done():return ctx.Err();case<-ticker.C:if err:=PurgeExpiredPartitionsContext(ctx,db,RetentionDays);err!=nil{logger.Error("partition retention purge failed","error",err);continue};if guard.AllowDatabaseInfo(ctx,db,capacityBytes){logger.Info("49-day partition retention purge completed")}}}
}
type CapacityGuard struct{Logger *slog.Logger;critical atomic.Bool}
func(g *CapacityGuard)AllowInfo(usedBytes,totalBytes uint64)bool{if totalBytes==0{return false};if usedBytes>=totalBytes-totalBytes/20{if g.critical.CompareAndSwap(false,true)&&g.Logger!=nil{g.Logger.Error("CRITICAL: storage capacity at or above 95%","used_bytes",usedBytes,"total_bytes",totalBytes)};return false};g.critical.Store(false);return true}
func(g *CapacityGuard)AllowDatabaseInfo(ctx context.Context,db *sql.DB,capacityBytes uint64)bool{if capacityBytes==0{return g.AllowInfo(0,0)};var used int64;if err:=db.QueryRowContext(ctx,`SELECT pg_database_size(current_database())`).Scan(&used);err!=nil{if g.critical.CompareAndSwap(false,true)&&g.Logger!=nil{g.Logger.Error("CRITICAL: unable to inspect storage capacity","error",err)};return false};return g.AllowInfo(uint64(used),capacityBytes)}

// RetentionCutoff is exposed to make the 49-day boundary independently testable.
func RetentionCutoff(now time.Time,days int)time.Time{return now.Add(-time.Duration(days)*24*time.Hour)}
func PartitionShouldDrop(partitionStart,now time.Time,days int)bool{return days>0&&!partitionStart.AddDate(0,0,1).After(RetentionCutoff(now,days))}
