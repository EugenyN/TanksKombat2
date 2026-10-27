using Unity.Entities;

namespace TanksKombat2.Server
{
    // Marks a real client connection that already received the current level snapshot
    // (LoadLevelRpc) from ServerLevelLoadSendSystem. Bots never get it.
    public struct LevelSentTag : IComponentData
    {
    }
}