using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Client networking helpers shared by the RPC effect systems.
    public static class RpcUtils
    {
        // True when the RPC arrived over the network (ReceiveRpcCommandRequest) and its owner
        // is this client — a server echo of an event prediction already handled locally;
        // the effect must not play twice.
        public static bool IsEchoRpc(ref SystemState state, Entity rpcEntity, int rpcNetworkId)
        {
            if (!state.EntityManager.HasComponent<ReceiveRpcCommandRequest>(rpcEntity))
                return false;

            var localConnectionQuery = state.EntityManager.CreateEntityQuery(
                new EntityQueryBuilder(Allocator.Temp).WithAll<LocalConnection>());
            if (!localConnectionQuery.TryGetSingletonEntity<Entity>(out var connectionEntity))
                return false;

            return rpcNetworkId == state.EntityManager.GetComponentData<NetworkId>(connectionEntity).Value;
        }
    }
}