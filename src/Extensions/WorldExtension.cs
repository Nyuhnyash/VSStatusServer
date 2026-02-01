using StatusServer.ServerListPing.Standard.Extension;
using Vintagestory.API.Server;


namespace StatusServer.Extensions
{
    /// <summary>
    /// Extension that adds world/calendar information to the status response.
    /// </summary>
    public class WorldExtension : IStatusExtension
    {
        public string Name => "world";
        
        public void Apply(ExtendedStatusPayload payload, ICoreServerAPI api)
        {
            var calendar = api.World?.Calendar;
            payload.World = new WorldPayload
            {
                Datetime = calendar?.PrettyDate() ?? "",
            };
        }
    }
}
