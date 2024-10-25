using Dapper;
using DapperWebApi.Database;
using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features.Bookings;

public interface IBookingRepository
{
    public Task<int?> AttemptRoomBooking(BookingRequest bookingRequest, decimal totalPrice);
    public Task CreatePartitionTable(CreatePartitionDetails partitionDetails);
    public Task<int?> CheckIn(int bookingId);
    public Task<Booking[]> FindBookingsByRoomTypes(int[] roomTypeIds);
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

    private string _sqlFindBookingsByRoomTypes =
        """
        SELECT b.id, b.room_id as RoomId, b.start_date AS StartDate, b.end_date AS EndDate, b.total_price AS TotalPrice, b.check_in_utc AS CheckInUtc, b.check_out_utc AS CheckOutUtc,
               r.id, r.room_type_id AS RoomTypeId, r.name, r.available
        FROM bookings b
        INNER JOIN rooms r ON r.id = b.room_id and r.room_type_id in ({0}) -- The expression "IN @roomTypeIds" does not function correctly as an array parameter
        LIMIT 100; -- This is enough...
        """;
    #endregion

    public async Task<int?> AttemptRoomBooking(BookingRequest bookingRequest, decimal totalPrice)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new
        {
            bookingRequest.RoomId,
            StartDate  = bookingRequest.FromDate.ToDateTime(), // DateOnly is not supported
            EndDate    = bookingRequest.ToDate.ToDateTime(),
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
            fromDateInclusive = partitionDetails.FromDateInclusive.ToDateTime(), // DateOnly is not supported
            toDateExclusive   = partitionDetails.ToDateExclusive.ToDateTime()
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

    public async Task<Booking[]> FindBookingsByRoomTypes(int[] roomTypeIds)
    {
        string sql = string.Format(_sqlFindBookingsByRoomTypes, string.Join(',', roomTypeIds));

        var connection = await _dbSession.OpenConnection();

        Dictionary<int, Room> roomsDictionary = [];

        IEnumerable<Booking> bookings = await connection.QueryAsync<Booking, Room, Booking>(sql,
            map: (booking, room) =>
            {
                // Using Relationships: https://www.learndapper.com/relationships

                // When using Many-to-One or One-to-Many queries, you need to handle the mapping between entities due to the SQL query’s structure, which can result in data duplication related to the 'One' side.
                // Similar to the SplitQuery in Entity Framework, you can use the QueryMultiple method with multiple selects. See the example in RoomRepository.GetRoomsByTypes
                // If it is the other way around and you populate the Room.Bookings list. You still keep the Rooms in the dictionary by ID, but when you retrieve it, you add a booking to the list

                if (roomsDictionary.TryGetValue(room.Id, out Room? existingRoom))
                {
                    booking.Room = existingRoom;
                }
                else
                {
                    booking.Room = room;

                    roomsDictionary[room.Id] = room;
                }

                return booking;
            },
            splitOn: "id",
            transaction: _dbSession.Transaction
        );

        return [.. bookings];
    }
}
