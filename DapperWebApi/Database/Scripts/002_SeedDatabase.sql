--> Dummy: RoomTypes
INSERT INTO room_types (name, description, price) VALUES
    ('Single Room', 'Designed for one person', 100),
    ('Double Room', 'Accommodates two guests, typically with one double bed', 150),
    ('Twin Room', 'Features two single beds for two guests', 160),
    ('Suite', 'A larger room with a separate living area', 250),
    ('Deluxe Room', 'An upgraded version of a standard room with better amenities', 200),
    ('Family Room', 'A larger room to accommodate families', 300),
    ('Accessible Room', 'Designed for guests with disabilities', 120);

--> Dummy: Rooms
INSERT INTO rooms (name, room_type_id) VALUES
    ('Seaside Serenity', 1),            -- Single Room
    ('Mountain Retreat', 2),            -- Double Room
    ('Sunset Haven', 3),                -- Twin Room
    ('Royal Suite Escape', 4),          -- Suite
    ('Lavish Luxe Room', 5),            -- Deluxe Room
    ('Family Adventure Suite', 6),      -- Family Room
    ('Haven for All', 7),               -- Accessible Room
    ('Coral Reef Room', 1),             -- Single Room
    ('Garden Bliss', 2),                -- Double Room
    ('Starry Night Room', 3),           -- Twin Room
    ('Platinum Elegance Suite', 4),     -- Suite
    ('Champagne Deluxe Retreat', 5),    -- Deluxe Room
    ('Spring Family Getaway', 6),       -- Family Room
    ('Accessible Oasis', 7),            -- Accessible Room
    ('Moonlit Beach Room', 1),          -- Single Room
    ('Urban Chic Retreat', 2),          -- Double Room
    ('Adventure Twin Room', 3),         -- Twin Room
    ('Windsor Royal Suite', 4),         -- Suite
    ('Tranquil Deluxe Escape', 5),      -- Deluxe Room
    ('Family Fun Zone', 6),             -- Family Room
    ('Comfort Zone Room', 7),           -- Accessible Room
    ('Sunshine Single', 1),             -- Single Room
    ('Cozy Couples Getaway', 2),        -- Double Room
    ('Two Peas in a Pod Room', 3),      -- Twin Room
    ('Lavish Living Suite', 4),         -- Suite
    ('Elegant Escape Room', 5),         -- Deluxe Room
    ('Family Retreat Room', 6),         -- Family Room
    ('Accessible Comfort Room', 7),     -- Accessible Room
    ('Secluded Beach Room', 1),         -- Single Room
    ('Charming City Dwelling', 2),      -- Double Room
    ('Twinkling Star Room', 3),         -- Twin Room
    ('Majestic Castle Suite', 4),       -- Suite
    ('Deluxe Dream Room', 5),           -- Deluxe Room
    ('Joyful Family Suite', 6),         -- Family Room
    ('Wheelchair-Friendly Retreat', 7), -- Accessible Room
    ('Serene Rest Room', 1),            -- Single Room
    ('Lovers Hideaway', 2),             -- Double Room
    ('Duo Delight Room', 3),            -- Twin Room
    ('Gatsby Suite', 4),                -- Suite
    ('Chateau Deluxe Room', 5),         -- Deluxe Room
    ('Family Adventure Haven', 6),      -- Family Room
    ('Accessible Sunshine Room', 7),    -- Accessible Room
    ('Beachfront Bliss Room', 1),       -- Single Room
    ('Romantic Rendezvous Room', 2),    -- Double Room
    ('Twin Peaks Room', 3),             -- Twin Room
    ('Secluded Forest Suite', 4),       -- Suite
    ('Delightful Deluxe Retreat', 5),   -- Deluxe Room
    ('Charming Family Room', 6),        -- Family Room
    ('Accessible Serenity Suite', 7),   -- Accessible Room
    ('Sunset Bliss Room', 1),           -- Single Room
    ('Intimate Escape Room', 2),        -- Double Room
    ('Partners Paradise Room', 3),      -- Twin Room
    ('Lavender Suite', 4),              -- Suite
    ('Exquisite Deluxe Room', 5),       -- Deluxe Room
    ('Memory Maker Family Room', 6),    -- Family Room
    ('Accessible Comfort Suite', 7);    -- Accessible Room