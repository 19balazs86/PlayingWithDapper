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
