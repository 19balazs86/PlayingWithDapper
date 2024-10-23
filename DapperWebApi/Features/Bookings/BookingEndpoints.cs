namespace DapperWebApi.Features.Bookings;

public readonly record struct BookingRequest(int RoomId, DateOnly FromDate, DateOnly ToDate);

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/booking", postBookingRequest);
        app.MapPost("/booking/create-partition", postCreatePartition);
    }

    private static async Task<string> postBookingRequest(IBookingService bookingService, BookingRequest request)
    {
        int? bookingId = await bookingService.AttemptRoomBooking(request);

        return bookingId.HasValue ?
            "The room has been successfully booked" :
            "Failed to book the room";
    }

    private static async Task postCreatePartition(IBookingService bookingService, int year, int month)
    {
        await bookingService.CreatePartitionTable(year, month);
    }
}
