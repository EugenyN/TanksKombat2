using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Central registry of gameplay prefab references baked into a single GamePrefabsComponent.
    [DisallowMultipleComponent]
    public class GamePrefabsAuthoring : MonoBehaviour
    {
        [Tooltip("Player tank prefab.")]
        public GameObject Tank;

        [Tooltip("Tank shell projectile prefab.")]
        public GameObject Bullet;

        [Tooltip("Spawnable bonus pickup prefab.")]
        public GameObject Bonus;

        [Tooltip("Destructible wall prefab.")]
        public GameObject Wall;

        [Tooltip("Explosion effect prefab.")]
        public GameObject Explosion;

        [Tooltip("Muzzle shot effect prefab.")]
        public GameObject ShotVfx;

        [Tooltip("Floating health bar prefab attached to tanks.")]
        public GameObject HealthBar;

        private class Baker : Baker<GamePrefabsAuthoring>
        {
            public override void Bake(GamePrefabsAuthoring authoring)
            {
                var component = default(GamePrefabsComponent);
                component.Tank = GetEntity(authoring.Tank, TransformUsageFlags.Dynamic);
                component.Bullet = GetEntity(authoring.Bullet, TransformUsageFlags.Dynamic);
                component.Bonus = GetEntity(authoring.Bonus, TransformUsageFlags.Dynamic);
                component.Wall = GetEntity(authoring.Wall, TransformUsageFlags.Dynamic);
                component.Explosion = GetEntity(authoring.Explosion, TransformUsageFlags.Dynamic);
                component.ShotVfx = GetEntity(authoring.ShotVfx, TransformUsageFlags.Dynamic);
                component.HealthBar = GetEntity(authoring.HealthBar, TransformUsageFlags.Dynamic);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}