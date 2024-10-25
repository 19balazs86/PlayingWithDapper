namespace DapperWebApi.Features.Rooms;

public interface IRoomService
{
    public Task<Room[]> GetRoomsByTypes(string? roomTypeIds);
    public Task<Room?> GetRoomById(int id);
    public Task<int[]> FindAvailableRooms(DateOnly fromDate, DateOnly toDate);
}

public sealed class RoomService(IRoomRepository _roomRepository) : IRoomService
{
    public async Task<Room[]> GetRoomsByTypes(string? roomTypeIds)
    {
        int[] roomTypeIdArray = roomTypeIds.ToNumbers();

        (Room[] rooms, RoomType[] roomTypes) = await _roomRepository.GetRoomsByTypes(roomTypeIdArray);

        foreach (Room room in rooms)
        {
            room.RoomType = roomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId);
        }

        return rooms;
    }

    public async Task<Room?> GetRoomById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _roomRepository.GetRoomById(id);
    }

    public async Task<int[]> FindAvailableRooms(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate >= toDate)
        {
            return [];
        };

        return await _roomRepository.FindAvailableRooms(fromDate, toDate);
    }
}
