CREATE PROCEDURE create_booking_partition(
    partition_table_name TEXT,
    from_date_inclusive DATE,
    to_date_exclusive DATE
)
LANGUAGE plpgsql AS $$
DECLARE
    partitionExists BOOLEAN;
BEGIN
    -- Check if the partition table exists
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_name = partition_table_name
    ) INTO partitionExists;

    IF NOT partitionExists THEN
        -- Create the partition table if it does not exist
        EXECUTE format('CREATE TABLE %I PARTITION OF bookings FOR VALUES FROM (%L) TO (%L)',
                       partition_table_name, from_date_inclusive, to_date_exclusive);

--      CREATE TABLE bookings_2022 PARTITION OF bookings
--      FOR VALUES FROM ('2022-01-01') TO ('2023-01-01'); -- FROM (from_date_inclusive) TO(to_date_exclusive)

        RAISE NOTICE 'Partition % has been created from % to %.', partition_table_name, from_date_inclusive, to_date_exclusive;

    ELSE
        RAISE NOTICE 'Partition % already exists.', partition_table_name;
    END IF;
END;
$$;
