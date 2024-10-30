using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using OutboxProcessorWorker.Database;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Outbox;

public sealed class SqlServerOutboxProcessorWithStoredProc(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<SqlServerOutboxProcessorWithStoredProc> _logger,
    IMessagePublisher _messagePublisher) : OutboxProcessorBase(_logger, _messagePublisher)
{
    protected override string _querySql { get; } =
        """
        SELECT TOP (@BatchSize) [Id], [Type], [Content]
        FROM OutboxMessages WITH (ROWLOCK, UPDLOCK) -- Use WITH (..., READPAST), if you require parallel processing
        WHERE [ProcessedOnUtc] IS NULL
        ORDER BY [OccurredOnUtc]
        """;

    protected override string _updateSql { get; } = string.Empty;

    protected override async Task<DbConnection> openConnection(CancellationToken ct = default)
    {
        // _batchSize = 650;

        var connection = new SqlConnection(_connectionStringProvider.ConnectionString);

        await connection.OpenAsync(ct);

        return connection;
    }

    protected override async Task updateOutboxMessages(ConcurrentQueue<OutboxUpdate> updateQueue, DbConnection connection, DbTransaction transaction)
    {
        if (updateQueue.IsEmpty)
        {
            return;
        }

        // Create a DataTable to represent the TVP
        var dataTable = new DataTable();

        dataTable.Columns.Add(nameof(OutboxMessage.Id),    typeof(Guid));
        dataTable.Columns.Add(nameof(OutboxMessage.Error), typeof(string));

        // Add data to the DataTable
        foreach (OutboxUpdate outboxUpdate in updateQueue)
        {
            dataTable.Rows.Add(outboxUpdate.Id, outboxUpdate.Error);
        }

        var parameters = new DynamicParameters();

        parameters.Add("UpdateData", dataTable.AsTableValuedParameter("OutboxUpdateType")); // Make sure this is the correct user-defined table type name in the SQL Server

        await connection.ExecuteAsync("UpdateOutboxMessages", parameters, transaction, commandType: CommandType.StoredProcedure);

        // NON-Dapper version
        // Create command
        // await using var command = connection.CreateCommand() as SqlCommand;
        //
        // command!.CommandType = CommandType.StoredProcedure;
        // command.CommandText  = "UpdateOutboxMessages";
        // command.Transaction  = transaction as SqlTransaction;
        //
        // // Add the TVP parameter
        // SqlParameter tvpParameter = command.Parameters.AddWithValue("UpdateData", dataTable);
        // tvpParameter.SqlDbType    = SqlDbType.Structured;
        // tvpParameter.TypeName     = "OutboxUpdateType"; // Make sure this is the correct user-defined table type name in the SQL Server
        //
        // // Execute the stored procedure
        // command.ExecuteNonQuery();
    }
}
