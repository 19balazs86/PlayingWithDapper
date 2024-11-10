CREATE FUNCTION attempt_room_booking(
    roomId INT,
    startDate DATE,
    endDate DATE,
    totalPrice NUMERIC
) RETURNS INT AS $$
DECLARE
    newBookingId INT;
BEGIN
    -- Lock the chosen room for update to prevent concurrent bookings for the same room
    -- Note: Unfortunately, this lock prevents booking even in different time intervals for the same room
    -- This may not be necessary, depending on the business needs and potential race conditions
    PERFORM 1 FROM rooms WHERE id = roomId FOR UPDATE;
    -- `PERFORM` keyword is used in PL/pgSQL to execute an SQL query without returning any results

    -- Check for conflicting bookings
    IF NOT EXISTS (
        SELECT 1 FROM bookings
     -- WHERE room_id = roomId AND ((start_date, end_date) OVERLAPS (startDate, endDate)) -- This works well
        WHERE room_id = roomId AND (
            (start_date <  endDate   AND end_date >  startDate) OR
            (start_date <  startDate AND end_date >  endDate)   OR
            (start_date >= startDate AND end_date <= endDate)
        )
    ) THEN
        -- Create the new booking and return the new ID
        INSERT INTO bookings(room_id, start_date, end_date, total_price)
        VALUES (roomId, startDate, endDate, totalPrice)
        RETURNING id INTO newBookingId;
    ELSE
        RETURN NULL;  -- Return NULL if booking was not created due to conflicts
    END IF;
    -- Return the newly created booking ID
    RETURN newBookingId;
END;
$$ LANGUAGE plpgsql;
