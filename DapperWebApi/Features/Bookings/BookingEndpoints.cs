﻿namespace DapperWebApi.Features.Bookings;

public readonly record struct BookingRequest(int RoomId, DateOnly FromDate, DateOnly ToDate);

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/booking");

        group.MapPost("/", postBookingRequest);
        group.MapPost("/create-partition", postCreatePartition);
        group.MapPut("/check-in/{bookingId:int}", putCheckIn);
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

    private static async Task putCheckIn(IBookingService bookingService, int bookingId)
    {
        await bookingService.CheckIn(bookingId);
    }
}
