﻿using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features.Bookings;

public readonly record struct CreatePartitionDetails(string PartitionTableName, DateOnly FromDateInclusive, DateOnly ToDateExclusive);

public interface IBookingService
{
    public Task<int?> AttemptRoomBooking(BookingRequest bookingRequest);
    public Task CreatePartitionTable(int year, int month);
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

    public async Task CreatePartitionTable(int year, int month)
    {
        if (year <= 0 || month is < 1 or > 12) // Dummy validation
        {
            // TODO: Instead of throwing an exception, use the Result pattern
            throw new InvalidOperationException("Invalid year or month");
        }

        CreatePartitionDetails partitionDetails = quarterOfYear(year, month);

        await _bookingRepository.CreatePartitionTable(partitionDetails);
    }

    private static CreatePartitionDetails quarterOfYear(int year, int month)
    {
        int quarter = month switch
        {
            1 or 2 or 3    => 1,
            4 or 5 or 6    => 2,
            7 or 8 or 9    => 3,
            10 or 11 or 12 => 4,
            _              => throw new ArgumentOutOfRangeException("Month must be between 1 and 12")
        };

        DateOnly fromDateInclusive = quarter switch
        {
            1 => new DateOnly(year, 1, 1),  // January for Q1
            2 => new DateOnly(year, 4, 1),  // April   for Q2
            3 => new DateOnly(year, 7, 1),  // July    for Q3
            4 => new DateOnly(year, 10, 1), // October for Q4
            _ => throw new ArgumentOutOfRangeException("Quarter must be between 1 and 4")
        };

        DateOnly toDateExclusive = fromDateInclusive.AddMonths(3);

        return new CreatePartitionDetails($"bookings_{year}_q{quarter}", fromDateInclusive, toDateExclusive);
    }
}
