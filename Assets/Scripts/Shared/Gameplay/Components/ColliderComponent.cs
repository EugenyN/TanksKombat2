using System;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    [Flags]
    public enum CollisionFlags
    {
        None = 0,
        Tank = 1,
        Bullet = 2,
        Bonus = 4
    }

    // Minimal collision descriptor consumed by CollisionWorld: layer memberships, collision mask and trigger mode.
    public struct ColliderComponent : IComponentData
    {
        public CollisionFlags BelongsTo;
        [GhostField] public CollisionFlags CollidesWith;
        public bool IsTrigger;
    }
}