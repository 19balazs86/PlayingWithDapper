using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features.Bookings;

public sealed class Booking
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? CheckInUtc { get; set; }

    public DateTime? CheckOutUtc { get; set; }

    public Room? Room { get; set; }
}
