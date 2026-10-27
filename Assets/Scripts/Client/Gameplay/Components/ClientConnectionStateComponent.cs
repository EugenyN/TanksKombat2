using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Last connection event seen for the client's server connection, written by
    // ClientConnectionStateSystem every receive tick. Lives on a dedicated singleton entity.
    public struct ClientConnectionStateComponent : IComponentData
    {
        public Entity ConnectionEntity;
        public ConnectionState.State State;
        public NetworkStreamDisconnectReason Reason;
    }
}
