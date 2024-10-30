--> Create a User-Defined Table Type
CREATE TYPE OutboxUpdateType AS TABLE (
    Id UNIQUEIDENTIFIER,
    Error NVARCHAR(MAX)
);
