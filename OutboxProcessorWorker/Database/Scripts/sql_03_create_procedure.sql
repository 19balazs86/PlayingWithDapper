--> Create stored procedure that takes the OutboxUpdateType user-defined table type as input and updates the OutboxMessages table
CREATE PROCEDURE UpdateOutboxMessages
    @UpdateData OutboxUpdateType READONLY
AS
BEGIN
    DECLARE @CurrentUtcDate DATETIME = GETUTCDATE();

    -- Update the OutboxMessages table
    UPDATE OM
    SET
        OM.ProcessedOnUtc = @CurrentUtcDate,
        OM.Error          = UDT.Error
        FROM
            OutboxMessages OM
        INNER JOIN @UpdateData UDT ON OM.Id = UDT.Id
END
