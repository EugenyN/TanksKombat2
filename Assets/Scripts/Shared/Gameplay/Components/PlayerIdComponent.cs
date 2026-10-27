using Unity.Entities;

namespace TanksKombat2.Shared
{
    public struct PlayerIdComponent : IComponentData
    {
        public Entity PlayerEntity;
    }
}