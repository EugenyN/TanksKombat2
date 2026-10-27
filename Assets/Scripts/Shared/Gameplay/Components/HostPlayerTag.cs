using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Marks the connection of the player who created/hosted the session (its process owns the server world).
    public struct HostPlayerTag : IComponentData
    {
    }
}
