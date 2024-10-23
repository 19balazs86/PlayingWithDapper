-- PostgreSQL creates indexes automatically for primary keys and unique constraints
-- But NOT for foreign key constraints

CREATE TABLE room_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(25) NOT NULL,
    description VARCHAR(100) NOT NULL,
    price DECIMAL(10, 2) NOT NULL
);

CREATE TABLE rooms (
    id SERIAL PRIMARY KEY,
    room_type_id INTEGER NOT NULL REFERENCES room_types(id),
    name VARCHAR(30) NOT NULL,
    available BOOLEAN DEFAULT TRUE
);

CREATE INDEX idx_room_type ON rooms (room_type_id);

CREATE TABLE bookings (
    id SERIAL,
    room_id INTEGER NOT NULL REFERENCES rooms(id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    total_price DECIMAL(10, 2) NOT NULL,
    check_in_utc TIMESTAMP NULL,
    check_out_utc TIMESTAMP NULL,
    CHECK (start_date < end_date), -- Ensure start_date is less than end_date
    -- When using a partitioned table, the constraint field in the parent table must be included in the primary key
    PRIMARY KEY (id, start_date) -- Include start_date in the primary key
) PARTITION BY RANGE (start_date);

-- PostgreSQL partitioning divides large tables into smaller, manageable partitions for better performance and maintainability

CREATE INDEX idx_booking_room_id ON bookings (room_id);

-- Based on the analysis report, these indexes are not being used by FindAvailableRooms
-- Since the bookings table is partitioned by start_date
-- Using the correct query excludes the unnecessary partitions, and there is no strong need for the index

-- CREATE INDEX idx_bookings_room_dates ON bookings (room_id, start_date, end_date);
-- CREATE INDEX idx_bookings_tsrange ON bookings USING GIST (tsrange(start_date, end_date));
-- CREATE INDEX idx_bookings_dates ON bookings (start_date, end_date);