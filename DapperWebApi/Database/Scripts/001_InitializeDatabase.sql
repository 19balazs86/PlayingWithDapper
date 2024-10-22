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
    id SERIAL PRIMARY KEY,
    room_id INTEGER NOT NULL REFERENCES rooms(id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    total_price DECIMAL(10, 2) NOT NULL,
    check_in_utc TIMESTAMP NULL,
    check_out_utc TIMESTAMP NULL,
    CHECK (start_date < end_date) -- Ensure start_date is less than end_date
);

CREATE INDEX idx_booking_room_id ON bookings (room_id);
CREATE INDEX idx_bookings_room_dates ON bookings (room_id, start_date, end_date);