using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Marks a real (non-bot) connection that joined as a spectator. Set by
    // ServerConnectionApprovalSystem when a JoinApprovalRequestRpc with Spectator=true arrives,
    // and directly by ClientConnectionApprovalSystem for a single-world host in spectator mode.
    public struct SpectatorTag : IComponentData
    {
    }
}
