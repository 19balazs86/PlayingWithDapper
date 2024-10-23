using Dapper;
using DapperWebApi.Database;

namespace DapperWebApi.Features.Bookings;

public interface IBookingRepository
{
    public Task<int?> AttemptRoomBooking(BookingRequest bookingRequest, decimal totalPrice);
    public Task CreatePartitionTable(CreatePartitionDetails partitionDetails);
    public Task<int?> CheckIn(int bookingId);
}

public sealed class BookingRepository(IDatabaseSession _dbSession) : IBookingRepository
{
    #region SQL
    private const string _sqlAttemptRoomBooking = "SELECT attempt_room_booking(@RoomId, @StartDate, @EndDate, @TotalPrice)";

    private const string _sqlCreatePartitionTable = "CALL create_booking_partition(@partitionName, @fromDateInclusive, @toDateExclusive)";

    private const string _sqlCheckIn =
        """
        UPDATE bookings
        SET check_in_utc = @checkInTime
        WHERE id = @id
        RETURNING room_id;
        """;
    #endregion

    public async Task<int?> AttemptRoomBooking(BookingRequest bookingRequest, decimal totalPrice)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new
        {
            bookingRequest.RoomId,
            StartDate  = bookingRequest.FromDate.ToDateTime(TimeOnly.MinValue), // DateOnly is not supported
            EndDate    = bookingRequest.ToDate.ToDateTime(TimeOnly.MinValue),
            TotalPrice = totalPrice
        };

        return await connection.ExecuteScalarAsync<int?>(_sqlAttemptRoomBooking, parameters, transaction: _dbSession.Transaction);
    }

    // The bookings table is partitioned by the start_date, and a partition table must be present in order to insert records into the bookings table
    // Otherwise, the insert operation will throw an exception in AttemptRoomBooking method
    public async Task CreatePartitionTable(CreatePartitionDetails partitionDetails)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new
        {
            partitionName     = partitionDetails.PartitionTableName,
            fromDateInclusive = partitionDetails.FromDateInclusive.ToDateTime(TimeOnly.MinValue), // DateOnly is not supported
            toDateExclusive   = partitionDetails.ToDateExclusive.ToDateTime(TimeOnly.MinValue)
        };

        await connection.ExecuteAsync(_sqlCreatePartitionTable, parameters, transaction: _dbSession.Transaction);

        // I had an issue: procedure create_booking_partition(...) does not exist
        // await connection.ExecuteAsync("create_booking_partition", parameters, commandType: CommandType.StoredProcedure, transaction: _dbSession.Transaction);
    }

    public async Task<int?> CheckIn(int bookingId)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new { id = bookingId, checkInTime = DateTime.UtcNow };

        int? roomId = connection.ExecuteScalar<int?>(_sqlCheckIn, parameters, transaction: _dbSession.Transaction);

        return roomId;
    }
}
