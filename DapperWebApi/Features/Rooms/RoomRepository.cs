using Dapper;
using DapperWebApi.Database;
using DapperWebApi.Features.Bookings;

namespace DapperWebApi.Features.Rooms;

public interface IRoomRepository
{
    public Task<(Room[], RoomType[])> GetRoomsByTypes(int[] roomTypeIds);
    public Task<Room?> GetRoomById(int id);
    public Task<int[]> FindAvailableRooms(DateOnly fromDate, DateOnly toDate);
    public Task CheckIn(int roomId);
}

public sealed class RoomRepository(IDatabaseSession _dbSession) : IRoomRepository
{
    #region SQL
    private const string _sqlRooms = // By using this concept with the nameof keyword, you can avoid issues when renaming or deleting properties
        $"""
        SELECT
            id AS {nameof(Room.Id)},
            room_type_id AS {nameof(Room.RoomTypeId)},
            name AS {nameof(Room.Name)},
            available AS {nameof(Room.Available)}
        FROM rooms;
        ---
        SELECT
            id AS {nameof(RoomType.Id)},
            name AS {nameof(RoomType.Name)},
            description AS {nameof(RoomType.Description)},
            price AS {nameof(RoomType.Price)}
        FROM room_types;
        """;

    private const string _sqlRoomsWithRoomTypes =
        """
        SELECT id, room_type_id AS RoomTypeId, name, available
        FROM rooms
        WHERE room_type_id IN ({0});
        ---
        SELECT id, name, description, price
        FROM room_types
        WHERE id IN ({0}); -- The expression "id IN @roomTypeIds" does not function correctly as an array parameter
        """;

    private const string _sqlRoomById =
        """
        SELECT r.id, r.room_type_id AS RoomTypeId, r.name, r.available,
               rt.id, rt.name, rt.description, rt.price
        FROM rooms r
        INNER JOIN room_types rt ON r.room_type_id = rt.id
        WHERE r.id = @id;
        """;

    private const string _sqlFindAvailableRooms =
        """
        SELECT r.id
        FROM rooms r
        LEFT JOIN bookings b
          ON r.id = b.room_id
            AND b.start_date >= @pastBookingDate   -- Given start_date - 30 days | exclude bookings that are too old
            AND b.start_date <= @futureBookingDate -- Given end_date   + 30 days | exclude bookings that are too far in the future
        	AND ((b.start_date, b.end_date) OVERLAPS (@fromDate, @toDate))
        WHERE b.id IS NULL;
        """;

    private const string _sqlCheckIn =
        """
        UPDATE rooms
        SET available = @isAvailable
        WHERE id = @id;
        """;
    #endregion SQL

    public async Task<(Room[], RoomType[])> GetRoomsByTypes(int[] roomTypeIds)
    {
        var connection = await _dbSession.OpenConnection();

        string query = roomTypeIds.Any() ?
            string.Format(_sqlRoomsWithRoomTypes, string.Join(',', roomTypeIds)) :
            _sqlRooms;

        // Other example of Many-to-One or One-to-Many in BookingRepository.FindBookingsByRoomTypes
        await using var reader = await connection.QueryMultipleAsync(query, transaction: _dbSession.Transaction);

        IEnumerable<Room> rooms = await reader.ReadAsync<Room>();

        IEnumerable<RoomType> roomTypes = await reader.ReadAsync<RoomType>();

        return ([.. rooms], [.. roomTypes]);
    }

    public async Task<Room?> GetRoomById(int id)
    {
        var connection = await _dbSession.OpenConnection();

        // Room? room = await connection.QuerySingleOrDefaultAsync<Room?>("SELECT id, room_type_id AS RoomTypeId, name, available FROM rooms WHERE id = @id;", new { id }, transaction: _dbSession.Transaction);

        IEnumerable <Room> rooms = await connection.QueryAsync<Room, RoomType, Room>(_sqlRoomById,
            map: static (room, roomType) =>
            {
                room.RoomType = roomType;

                return room;
            },

            param: new { id },

            // A comma-separated value is used for multiple joins in a single query
            // Specifies the column name(s) where Dapper should start mapping the results to a different object. In this case, it is rt.id.
            // Simple test, change the sql: ... rt.id AS RTID, 99 as id, rt.name ... > splitOn: "RTID" and the RoomType.Id = 99
            splitOn: "id",

            transaction: _dbSession.Transaction
        );

        return rooms.SingleOrDefault();
    }

    public async Task<int[]> FindAvailableRooms(DateOnly fromDate, DateOnly toDate)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new
        {
            fromDate          = fromDate.ToDateTime(), // DateOnly is not supported
            toDate            = toDate.ToDateTime(),
            pastBookingDate   = fromDate.AddDays(-BookingService.MaxBookingDays).ToDateTime(),
            futureBookingDate = toDate.AddDays(BookingService.MaxBookingDays).ToDateTime(),
        };

        IEnumerable<int> roomIds = await connection.QueryAsync<int>(_sqlFindAvailableRooms, parameters, transaction: _dbSession.Transaction);

        return [.. roomIds];
    }

    public async Task CheckIn(int roomId)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new { id = roomId, isAvailable = false };

        await connection.ExecuteAsync(_sqlCheckIn, parameters, transaction: _dbSession.Transaction);
    }
}