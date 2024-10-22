using DapperWebApi.Features.Rooms;

namespace DapperWebApi.Features;

public static class FeatureExtensions
{
    public static void AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<IRoomService, RoomService>();

        services.AddScoped<IRoomRepository, RoomRepository>();
    }

    public static void MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRoomEndpoints();
    }
}
