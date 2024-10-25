using Microsoft.Extensions.Primitives;

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
        int[] roomTypeIdArray = toNumbers(roomTypeIds);

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

    private static int[] toNumbers(string? roomTypeIds)
    {
        if (string.IsNullOrWhiteSpace(roomTypeIds))
        {
            return [];
        }

        // Span example: https://github.com/19balazs86/PlayingWithYARP/blob/master/ProxyYARP/Miscellaneous/RateLimiterPolicyByIPAddress.cs#L62

        var tokenizer = new StringTokenizer(roomTypeIds, [',']);

        HashSet<int> hashSet = [];

        foreach (StringSegment segment in tokenizer)
        {
            if (int.TryParse(segment, out int id) && id > 0) // No need segment.Trim()
            {
                hashSet.Add(id);
            }
        }

        return [.. hashSet];
    }
}
