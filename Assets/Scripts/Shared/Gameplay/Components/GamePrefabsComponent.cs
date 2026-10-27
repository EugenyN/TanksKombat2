using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Shared singleton holding prefab entity references used to spawn tanks, bullets, bonuses, walls and VFX.
    public struct GamePrefabsComponent : IComponentData
    {
        public Entity Tank;
        public Entity Bullet;
        public Entity Bonus;
        public Entity Wall;
        public Entity Explosion;
        public Entity ShotVfx;
        public Entity HealthBar;
    }
}