--> Create a Composite Type
CREATE TYPE outbox_update_type AS (
    id UUID,
    error TEXT
);

-- Eventually, this type is not used, because I could not get it to work when calling a stored procedure with a composite type in Postgres using Dapper
