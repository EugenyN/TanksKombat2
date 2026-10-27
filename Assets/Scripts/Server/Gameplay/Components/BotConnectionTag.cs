using Unity.Entities;

namespace TanksKombat2.Server
{
    // Marks a fake "connection" entity that represents a bot player.
    // Has NetworkId + JoinedPlayerComponent + CommandTarget so it integrates with
    // existing spawn / hit / scoring / round-lifecycle systems, but no real network stream.
    public struct BotConnectionTag : IComponentData
    {
    }
}