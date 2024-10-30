--> Create table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
BEGIN
CREATE TABLE OutboxMessages (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY,
    [Type] VARCHAR(255) NOT NULL,
    [Content] VARCHAR(MAX) NOT NULL,
    [ProcessedOnUtc] DATETIME,
    [Error] NVARCHAR(MAX),
    [OccurredOnUtc] DATETIME NOT NULL
    );
END

--> Create a filtered index on unprocessed messages, including all necessary columns
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_OutboxMessages_unprocessed'
    AND object_id = OBJECT_ID('dbo.OutboxMessages')
)
BEGIN
    CREATE NONCLUSTERED INDEX idx_OutboxMessages_unprocessed
    ON dbo.OutboxMessages ([OccurredOnUtc], [ProcessedOnUtc])
    INCLUDE ([Id], [Type], [Content])
    WHERE [ProcessedOnUtc] IS NULL;
END

--> Query: Select
SELECT TOP (@BatchSize) [Id], [Type], [Content]
FROM OutboxMessages WITH (ROWLOCK, UPDLOCK) -- Use WITH (..., READPAST), if you require parallel processing
WHERE [ProcessedOnUtc] IS NULL
ORDER BY [OccurredOnUtc]

--> Query: Update
MERGE INTO OutboxMessages AS target
    USING (VALUES
       (@Id0, @ProcessedOn0, @Error0),
       (@Id1, @ProcessedOn1, @Error1),
       (@Id2, @ProcessedOn2, @Error2),
       -- A few hundred rows in between
       (@Id999, @ProcessedOn999, @Error999)
    ) AS source ([Id], [ProcessedOnUtc], [Error])
    ON target.[Id] = source.[Id]
    WHEN MATCHED THEN
        UPDATE SET
            target.[ProcessedOnUtc] = source.[ProcessedOnUtc],
            target.[Error] = source.[Error];

--> Create a User-Defined Table Type
IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'OutboxUpdateType')
BEGIN
CREATE TYPE OutboxUpdateType AS TABLE (
    Id UNIQUEIDENTIFIER,
    ProcessedOnUtc DATETIME,
    Error NVARCHAR(MAX)
    );
END

--> Check if the stored procedure exists and drop it
IF OBJECT_ID('UpdateOutboxMessages', 'P') IS NOT NULL
BEGIN
    -- Drop the existing procedure if needed
    DROP PROCEDURE UpdateOutboxMessages;
END
GO

--> Create stored procedure that takes the OutboxUpdateType user-defined table type as input and updates the OutboxMessages table
CREATE PROCEDURE UpdateOutboxMessages
    @UpdateData OutboxUpdateType READONLY
AS
BEGIN
    -- Update the OutboxMessages table
    UPDATE OM
    SET
        OM.ProcessedOnUtc = UDT.ProcessedOnUtc, -- Minor improvement: set it to GETUTCDATE()
        OM.Error          = UDT.Error
        FROM
            OutboxMessages OM
        INNER JOIN @UpdateData UDT ON OM.Id = UDT.Id
END
