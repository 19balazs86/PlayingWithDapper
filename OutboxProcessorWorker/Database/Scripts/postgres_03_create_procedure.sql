--> Create procedure that takes the outbox_update_type Composite Type as input and updates the outboxmessages table
CREATE PROCEDURE update_outbox_messages(update_data outbox_update_type[])
    LANGUAGE plpgsql AS $$
DECLARE
    current_utc TIMESTAMP;
BEGIN
    current_utc := NOW() AT TIME ZONE 'UTC';

    -- Update the OutboxMessages table
    UPDATE outboxmessages OM
    SET
        OM.processed_on_utc = current_utc,
        OM.error            = ud.error
    FROM unnest(update_data) AS ud
    WHERE OM.id = ud.id;
END;
$$;

-- Eventually, this procedure is not used, because I could not get it to work when calling a stored procedure with a composite type in Postgres using Dapper
