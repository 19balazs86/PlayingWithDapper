using Dapper;
using DapperWebApi.Database;

namespace DapperWebApi.Features.Bookings;

public interface IBookingRepository
{
    public Task<int?> AttemptRoomBooking(BookingRequest bookingRequest, decimal totalPrice);
}

public sealed class BookingRepository(IDatabaseSession _dbSession) : IBookingRepository
{
    #region SQL
    private const string _sqlAttemptRoomBooking = "SELECT attempt_room_booking(@RoomId, @StartDate, @EndDate, @TotalPrice)";
    #endregion

    public async Task<int?> AttemptRoomBooking(BookingRequest bookingRequest, decimal totalPrice)
    {
        var connection = await _dbSession.OpenConnection();

        var parameters = new
        {
            bookingRequest.RoomId,
            StartDate  = bookingRequest.FromDate.ToDateTime(TimeOnly.MinValue), // DateOnly is not supported
            EndDate    = bookingRequest.ToDate.ToDateTime(TimeOnly.MinValue),
            TotalPrice = totalPrice
        };

        return await connection.ExecuteScalarAsync<int?>(_sqlAttemptRoomBooking, parameters, transaction: _dbSession.Transaction);
    }
}
