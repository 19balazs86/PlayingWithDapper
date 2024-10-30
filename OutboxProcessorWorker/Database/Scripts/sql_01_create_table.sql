-- Create table
CREATE TABLE OutboxMessages (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY,
    [Type] VARCHAR(255) NOT NULL,
    [Content] VARCHAR(MAX) NOT NULL,
    [ProcessedOnUtc] DATETIME,
    [Error] NVARCHAR(MAX),
    [OccurredOnUtc] DATETIME NOT NULL
);

-- Create a filtered index on unprocessed messages, including all necessary columns
CREATE NONCLUSTERED INDEX idx_OutboxMessages_unprocessed
    ON dbo.OutboxMessages ([OccurredOnUtc], [ProcessedOnUtc])
    INCLUDE ([Id], [Type], [Content])
    WHERE [ProcessedOnUtc] IS NULL;
