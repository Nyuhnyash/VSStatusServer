using StatusServer.ServerListPing.Standard.Extension;
using Vintagestory.API.Server;


namespace StatusServer.Extensions
{
    /// <summary>
    /// Interface for status payload extensions.
    /// Implement this interface to add custom data to the status response.
    /// </summary>
    public interface IStatusExtension
    {
        /// <summary>
        /// The unique name of this extension (used in config to enable/disable).
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Apply extension data to the payload.
        /// Called for each status request.
        /// </summary>
        /// <param name="payload">The payload to modify</param>
        /// <param name="api">The server API for accessing game data</param>
        void Apply(ExtendedStatusPayload payload, ICoreServerAPI api);
    }
}
