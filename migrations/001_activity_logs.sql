CREATE TABLE IF NOT EXISTS activity_logs (
    created_at timestamptz NOT NULL DEFAULT now(),
    id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL,
    event_type text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);

CREATE TABLE IF NOT EXISTS session_logs (
    created_at timestamptz NOT NULL DEFAULT now(),
    id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL,
    event_type text NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);

CREATE TABLE IF NOT EXISTS system_metrics (
    created_at timestamptz NOT NULL DEFAULT now(),
    id bigint GENERATED ALWAYS AS IDENTITY,
    metric_name text NOT NULL,
    metric_value double precision NOT NULL,
    labels jsonb NOT NULL DEFAULT '{}'::jsonb,
    PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);

CREATE TABLE IF NOT EXISTS activity_snapshots (
    created_at timestamptz NOT NULL DEFAULT now(),
    id bigint GENERATED ALWAYS AS IDENTITY,
    session_id text NOT NULL,
    snapshot jsonb NOT NULL,
    PRIMARY KEY (created_at, id)
) PARTITION BY RANGE (created_at);

-- Keep partitions ready for the current month and two upcoming months.
CREATE OR REPLACE FUNCTION ensure_game_log_partitions() RETURNS void
LANGUAGE plpgsql AS $$
DECLARE
    table_name text;
    month_offset integer;
    month_start date;
    month_end date;
    partition_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['activity_logs', 'activity_snapshots', 'session_logs', 'system_metrics'] LOOP
        FOR month_offset IN 0..2 LOOP
            month_start := (date_trunc('month', current_date) + make_interval(months => month_offset))::date;
            month_end := (month_start + interval '1 month')::date;
            partition_name := table_name || '_' || to_char(month_start, 'YYYYMM');
            EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)', partition_name, table_name, month_start, month_end);
        END LOOP;
    END LOOP;
END $$;

SELECT ensure_game_log_partitions();

CREATE INDEX IF NOT EXISTS activity_logs_session_created_idx
    ON activity_logs (session_id, created_at DESC);
CREATE INDEX IF NOT EXISTS session_logs_session_created_idx
    ON session_logs (session_id, created_at DESC);
CREATE INDEX IF NOT EXISTS activity_snapshots_session_created_idx
    ON activity_snapshots (session_id, created_at DESC);
