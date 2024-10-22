CREATE FUNCTION attempt_room_booking(
    roomId INT,
    startDate TIMESTAMP WITH TIME ZONE,
    endDate TIMESTAMP WITH TIME ZONE,
    totalPrice NUMERIC
) RETURNS INT AS $$
DECLARE
    newBookingId INT;  -- Variable to hold the newly inserted booking ID
BEGIN
    -- Check for conflicting bookings
    -- Note: The following solution is not safe for race conditions but is sufficient
    IF NOT EXISTS (
        SELECT 1 FROM bookings
--        WHERE room_id = roomId AND ((start_date, end_date) OVERLAPS (startDate, endDate)) -- This works well
        WHERE room_id = roomId AND (
            (start_date <  endDate   AND end_date >  startDate) OR
            (start_date <  startDate AND end_date >  endDate)   OR
            (start_date >= startDate AND end_date <= endDate)
        )
    ) THEN
        -- Create the new booking and return the new ID
        INSERT INTO bookings(room_id, start_date, end_date, total_price)
        VALUES (roomId, startDate, endDate, totalPrice)
        RETURNING id INTO newBookingId;  -- Capture the new booking ID
    ELSE
        RETURN NULL;  -- Return NULL if booking was not created due to conflicts
    END IF;
    -- Return the newly created booking ID
    RETURN newBookingId;
END;
$$ LANGUAGE plpgsql;