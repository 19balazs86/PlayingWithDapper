using DapperWebApi.Features.Bookings;
using DapperWebApi.Features.Rooms;
using Microsoft.Extensions.Primitives;

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

    public static int[] ToNumbers(this string? roomTypeIds)
    {
        if (string.IsNullOrWhiteSpace(roomTypeIds))
        {
            return [];
        }

        // Span example: https://github.com/19balazs86/PlayingWithYARP/blob/master/ProxyYARP/Miscellaneous/RateLimiterPolicyByIPAddress.cs#L62

        var tokenizer = new StringTokenizer(roomTypeIds, [',']);

        HashSet<int> hashSet = [];

        foreach (StringSegment segment in tokenizer)
        {
            if (int.TryParse(segment, out int id) && id > 0) // No need segment.Trim()
            {
                hashSet.Add(id);
            }
        }

        return [.. hashSet];
    }
}
