using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    public enum BonusType
    {
        Tank,
        Ammo
    }

    // Bonus pickup descriptor (server authority): marks a spawned pickup entity and syncs its type via NetCode.
    public struct BonusComponent : IComponentData
    {
        [GhostField] public BonusType Type;
    }
}