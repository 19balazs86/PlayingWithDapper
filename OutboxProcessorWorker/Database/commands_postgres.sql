--> Create table
CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID PRIMARY KEY,
    type VARCHAR(255) NOT NULL,
    content JSONB NOT NULL,
    occurred_on_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    processed_on_utc TIMESTAMP WITH TIME ZONE NULL,
    error TEXT NULL);

--> Create a filtered index on unprocessed messages, including all necessary columns
CREATE INDEX IF NOT EXISTS idx_outbox_messages_unprocessed
    ON outbox_messages (occurred_on_utc, processed_on_utc)
    INCLUDE (id, type, content)
    WHERE processed_on_utc IS NULL;

--> Query: Select
SELECT id AS Id, type AS Type, content AS Content
FROM outbox_messages
WHERE processed_on_utc IS NULL
ORDER BY occurred_on_utc
LIMIT @BatchSize
FOR UPDATE -- SKIP LOCKED -- If you require parallel processing

--> Query: Update
UPDATE outbox_messages
SET processed_on_utc = v.processed_on_utc,
    error = v.error
    FROM (VALUES
    (@Id0, @ProcessedOn0, @Error0),
    (@Id1, @ProcessedOn1, @Error1),
    (@Id2, @ProcessedOn2, @Error2),
    -- A few hundred rows in beteween
    (@Id999, @ProcessedOn999, @Error999)
) AS v(id, processed_on_utc, error)
WHERE outbox_messages.id = v.id::uuid

--> Create a Composite Type
CREATE TYPE outbox_update_type AS (
    id UUID,
    processed_on_utc TIMESTAMP WITH TIME ZONE,
    error TEXT
);

--> Create procedure that takes the outbox_update_type Composite Type as input and updates the outbox_messages table
CREATE PROCEDURE update_outbox_messages(update_data outbox_update_type[])
    LANGUAGE plpgsql AS $$
DECLARE
    current_utc TIMESTAMP;
BEGIN
    current_utc := NOW() AT TIME ZONE 'UTC';

    -- Update the outbox_messages table
    UPDATE outbox_messages
    SET
        processed_on_utc = current_utc,
        error            = ud.error
        FROM unnest(update_data) AS ud
    WHERE outbox_messages.id = ud.id;
END;
$$;
