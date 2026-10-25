-- Daily RANGE partitions make the 49-day cutoff enforceable by dropping whole
-- expired partitions. Ensure partitions for the retention window and one week
-- ahead during deployment; the hourly scheduler drops partitions after expiry.
CREATE TABLE IF NOT EXISTS arena_activity_logs (
    created_at timestamptz NOT NULL DEFAULT now(),
    id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL,
    event_type text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);

CREATE TABLE IF NOT EXISTS activity_logs (
    created_at timestamptz NOT NULL DEFAULT now(), id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL, event_type text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb, PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);
CREATE TABLE IF NOT EXISTS session_logs (
    created_at timestamptz NOT NULL DEFAULT now(), id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL, event_type text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb, PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);
CREATE TABLE IF NOT EXISTS activity_snapshots (
    created_at timestamptz NOT NULL DEFAULT now(), id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL, snapshot jsonb NOT NULL, PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);
CREATE TABLE IF NOT EXISTS system_metrics (
    created_at timestamptz NOT NULL DEFAULT now(), id bigint GENERATED ALWAYS AS IDENTITY,
    metric_name text NOT NULL, metric_value double precision NOT NULL,
    labels jsonb NOT NULL DEFAULT '{}'::jsonb, PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);

CREATE OR REPLACE FUNCTION ensure_arena_activity_partitions() RETURNS void
LANGUAGE plpgsql AS $$
DECLARE day_start date; day_end date; partition_name text; offset_day integer; table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['arena_activity_logs','activity_logs','activity_snapshots','session_logs','system_metrics'] LOOP
        FOR offset_day IN -49..7 LOOP
            day_start := (current_date + offset_day);
            day_end := day_start + 1;
            partition_name := table_name || '_' || to_char(day_start, 'YYYYMMDD');
            EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)', partition_name, table_name, day_start, day_end);
        END LOOP;
    END LOOP;
END $$;

SELECT ensure_arena_activity_partitions();
CREATE INDEX IF NOT EXISTS arena_activity_logs_session_created_idx ON arena_activity_logs (session_id, created_at DESC);
