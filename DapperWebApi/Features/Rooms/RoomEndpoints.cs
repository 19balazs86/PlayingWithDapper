using Microsoft.AspNetCore.Http.HttpResults;

namespace DapperWebApi.Features.Rooms;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/room", getRoomsByTypes);

        app.MapGet("/room/{id:int}", getRoomById);
    }

    private static async Task<IEnumerable<Room>> getRoomsByTypes(IRoomService roomService, string? roomTypeIds)
    {
        return await roomService.GetRoomsByTypes(roomTypeIds);
    }

    private static async Task<Results<NotFound, JsonHttpResult<Room>>> getRoomById(IRoomService roomService, int id)
    {
        Room? room = await roomService.GetRoomById(id);

        return room is null ? TypedResults.NotFound() : TypedResults.Json(room);
    }
}
