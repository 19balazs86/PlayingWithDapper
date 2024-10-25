using DapperWebApi.Database;
using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features.Bookings;

public readonly record struct CreatePartitionDetails(string PartitionTableName, DateOnly FromDateInclusive, DateOnly ToDateExclusive);

public interface IBookingService
{
    public Task<int?> AttemptRoomBooking(BookingRequest bookingRequest);
    public Task CreatePartitionTable(int year, int month);
    public Task CheckIn(int bookingId);
}

public sealed class BookingService(
    IBookingRepository _bookingRepository,
    IRoomRepository _roomRepository,
    IRoomService _roomService,
    IDatabaseUnitOfWork _unitOfWork) : IBookingService
{
    // Maximum number of days allowed for booking
    // This restriction can enhance query performance in finding available rooms by excluding partitions of the bookings table
    public const int MaxBookingDays = 30; // TODO: Place it in configuration

    public async Task<int?> AttemptRoomBooking(BookingRequest bookingRequest)
    {
        int numberOfBookingDays = bookingRequest.ToDate.DayNumber - bookingRequest.FromDate.DayNumber;

        // TODO: Use the Result pattern for validation response
        if (bookingRequest.FromDate >= bookingRequest.ToDate || numberOfBookingDays > MaxBookingDays)
        {
            return null;
        }

        Room? room = await _roomService.GetRoomById(bookingRequest.RoomId);

        if (room?.RoomType is null)
        {
            return null;
        }

        decimal totalPrice = room.RoomType!.Price * numberOfBookingDays;

        // The attempt_room_booking function locks the room to prevent a race condition when booking the same room (more details can be found in the SQL file)
        await _unitOfWork.BeginTransaction();

        int? bookingId = await _bookingRepository.AttemptRoomBooking(bookingRequest, totalPrice);

        if (bookingId.HasValue)
        {
            await _unitOfWork.CommitTransaction();
        }
        else
        {
            // It is not necessary to call RollbackTransaction, as it is not committed and gets disposed
            await _unitOfWork.RollbackTransaction();
        }

        return bookingId;
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

    public async Task CheckIn(int bookingId)
    {
        await _unitOfWork.BeginTransaction();

        int? roomId = await _bookingRepository.CheckIn(bookingId);

        if (roomId.HasValue)
        {
            await _roomRepository.CheckIn(roomId.Value);

            await _unitOfWork.CommitTransaction();
        }
        else
        {
            // It is not necessary to call RollbackTransaction, as it is not committed and gets disposed
            await _unitOfWork.RollbackTransaction();
        }
    }

    private static CreatePartitionDetails quarterOfYear(int year, int month)
    {
        int quarter = month switch
        {
            1 or 2 or 3    => 1,
            4 or 5 or 6    => 2,
            7 or 8 or 9    => 3,
            10 or 11 or 12 => 4,
            _              => throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12")
        };

        DateOnly fromDateInclusive = quarter switch
        {
            1 => new DateOnly(year, 1,  1), // January for Q1
            2 => new DateOnly(year, 4,  1), // April   for Q2
            3 => new DateOnly(year, 7,  1), // July    for Q3
            4 => new DateOnly(year, 10, 1), // October for Q4
            _ => throw new ArgumentOutOfRangeException(nameof(quarter), "Quarter must be between 1 and 4")
        };

        DateOnly toDateExclusive = fromDateInclusive.AddMonths(3);

        return new CreatePartitionDetails($"bookings_{year}_q{quarter}", fromDateInclusive, toDateExclusive);
    }
}
