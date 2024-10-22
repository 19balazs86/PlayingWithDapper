using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features.Bookings;

public interface IBookingService
{
    public Task<int?> AttemptRoomBooking(BookingRequest bookingRequest);
}

public sealed class BookingService(IBookingRepository _bookingRepository, IRoomService _roomService) : IBookingService
{
    public async Task<int?> AttemptRoomBooking(BookingRequest bookingRequest)
    {
        if (bookingRequest.FromDate >= bookingRequest.ToDate)
        {
            return null;
        }

        Room? room = await _roomService.GetRoomById(bookingRequest.RoomId);

        if (room is null || room.RoomType is null)
        {
            return null;
        }

        int numberOfBookingDays = bookingRequest.ToDate.DayNumber - bookingRequest.FromDate.DayNumber;

        decimal totalPrice = room.RoomType!.Price * numberOfBookingDays;

        return await _bookingRepository.AttemptRoomBooking(bookingRequest, totalPrice);
    }
}
