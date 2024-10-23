CREATE PROCEDURE create_booking_partition(
    partitionName TEXT,
    fromDateInclusive TIMESTAMP WITHOUT TIME ZONE,
    toDateExclusive TIMESTAMP WITHOUT TIME ZONE
)
LANGUAGE plpgsql AS $$
DECLARE
    partitionExists BOOLEAN;
BEGIN
    -- Check if the partition table exists
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_name = partitionName
    ) INTO partitionExists;

    IF NOT partitionExists THEN
        -- Create the partition table if it does not exist
        EXECUTE format('CREATE TABLE %I PARTITION OF bookings FOR VALUES FROM (%L) TO (%L)',
                       partitionName, fromDateInclusive, toDateExclusive);

--      CREATE TABLE bookings_2022 PARTITION OF bookings
--      FOR VALUES FROM ('2022-01-01') TO ('2023-01-01'); -- FROM (fromDateInclusive) TO(toDateExclusive)

        RAISE NOTICE 'Partition % has been created from % to %.', partitionName, fromDateInclusive, toDateExclusive;

    ELSE
        RAISE NOTICE 'Partition % already exists.', partitionName;
    END IF;
END;
$$;