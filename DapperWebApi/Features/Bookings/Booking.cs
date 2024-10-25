using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features.Bookings;

public sealed class Booking
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? CheckInUtc { get; set; }

    public DateTime? CheckOutUtc { get; set; }

    public Room? Room { get; set; }
}
