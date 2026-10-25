package storage

import (
	"testing"
	"time"
)

func TestPartitionRetentionBoundary(t *testing.T) {
	now := time.Date(2026, 10, 25, 12, 0, 0, 0, time.UTC)
	if !PartitionShouldDrop(time.Date(2026, 9, 5, 0, 0, 0, 0, time.UTC), now, RetentionDays) {
		t.Fatal("partition ending before the 49-day cutoff was retained")
	}
	if PartitionShouldDrop(time.Date(2026, 9, 6, 0, 0, 0, 0, time.UTC), now, RetentionDays) {
		t.Fatal("partition containing data newer than the cutoff was dropped")
	}
}
