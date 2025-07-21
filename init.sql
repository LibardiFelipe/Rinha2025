CREATE UNLOGGED TABLE payments (
    correlation_id UUID PRIMARY KEY,
    amount DECIMAL NOT NULL,
    processed_by TEXT NOT NULL,
    requested_at_utc TIMESTAMPTZ NOT NULL
);

create index idx_payments_requested_at_utc ON payments(requested_at_utc);