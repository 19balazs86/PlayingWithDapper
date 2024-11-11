--> Create a Composite Type
CREATE TYPE outbox_update_type AS (
    id UUID,
    error TEXT
);
