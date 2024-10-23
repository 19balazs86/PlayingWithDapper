using DapperWebApi.Features.Bookings;
using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features;

public static class FeatureExtensions
{
    public static void AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<IRoomService,    RoomService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddScoped<IRoomRepository,    RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
    }

    public static void MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRoomEndpoints();
        app.MapBookingEndpoints();
    }

    public static DateTime ToDateTime(this DateOnly dateOnly)
    {
        return dateOnly.ToDateTime(TimeOnly.MinValue);
    }
}
