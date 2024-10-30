using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Dapper;
using OutboxProcessorWorker.Database;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Outbox;

public sealed class OutboxProcessorSqlServerWithStoredProc(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<OutboxProcessorSqlServerWithStoredProc> _logger,
    IMessagePublisher _messagePublisher) : OutboxProcessorSqlServer(_connectionStringProvider, _logger, _messagePublisher)
{
    protected override int _batchSize => 1_000;

    protected override async Task updateOutboxMessages(ConcurrentQueue<OutboxUpdate> updateQueue, DbConnection connection, DbTransaction transaction)
    {
        if (updateQueue.IsEmpty)
        {
            return;
        }

        var dataTable = new DataTable();

        dataTable.Columns.Add(nameof(OutboxMessage.Id),    typeof(Guid));
        dataTable.Columns.Add(nameof(OutboxMessage.Error), typeof(string));

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
