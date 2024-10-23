using Dapper;
using DapperWebApi.Database;

namespace DapperWebApi.Features.Rooms;

public interface IRoomRepository
{
    public Task<(IEnumerable<Room>, IEnumerable<RoomType>)> GetRoomsByTypes(IEnumerable<int> roomTypeIds);
    public Task<Room?> GetRoomById(int id);
    public Task<IEnumerable<int>> FindAvailableRooms(DateOnly fromDate, DateOnly toDate);
    public Task CheckIn(int roomId);
}

public sealed class RoomRepository(IDatabaseSession _dbSession) : IRoomRepository
{
    #region SQL
    private const string _sqlRooms =
        """
        SELECT id, room_type_id AS RoomTypeId, name, available
        FROM rooms;
        ---
        SELECT id, name, description, price
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

    public async Task<(IEnumerable<Room>, IEnumerable<RoomType>)> GetRoomsByTypes(IEnumerable<int> roomTypeIds)
    {
        var connection = await _dbSession.OpenConnection();

        string query = roomTypeIds.Any() ?
            string.Format(_sqlRoomsWithRoomTypes, string.Join(',', roomTypeIds)) :
            _sqlRooms;

        await using var reader = await connection.QueryMultipleAsync(query, transaction: _dbSession.Transaction);

        IEnumerable<Room> rooms = await reader.ReadAsync<Room>();

        IEnumerable<RoomType> roomTypes = await reader.ReadAsync<RoomType>();

        return (rooms, roomTypes);
    }

    public async Task<Room?> GetRoomById(int id)
    {
        var connection = await _dbSession.OpenConnection();

        IEnumerable<Room> rooms = await connection.QueryAsync<Room, RoomType, Room>(_sqlRoomById,
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

    public async Task<IEnumerable<int>> FindAvailableRooms(DateOnly fromDate, DateOnly toDate)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new
        {
            fromDate = fromDate.ToDateTime(TimeOnly.MinValue), // DateOnly is not supported
            toDate   = toDate.ToDateTime(TimeOnly.MinValue)
        };

        return await connection.QueryAsync<int>(_sqlFindAvailableRooms, parameters, transaction: _dbSession.Transaction);
    }

    public async Task CheckIn(int roomId)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new { id = roomId, isAvailable = false };

        int numberOfROwsAffected = await connection.ExecuteAsync(_sqlCheckIn, parameters, transaction: _dbSession.Transaction);
    }
}
