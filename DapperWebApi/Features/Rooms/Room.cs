namespace DapperWebApi.Features.Rooms;

public sealed class Room
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Available { get; set; }
    public RoomType? RoomType { get; set; }
}
