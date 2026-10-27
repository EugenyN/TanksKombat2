using Unity.Entities;

namespace TanksKombat2.Client
{
    // Marker that we've already sent the JoinApprovalRequestRpc for this connection entity,
    // so we don't send it again every frame while waiting in the Approval state.
    public struct ApprovalStartedTag : IComponentData
    { }
}