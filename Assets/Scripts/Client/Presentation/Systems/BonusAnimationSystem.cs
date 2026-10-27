using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;

namespace TanksKombat2.Client
{
    // Bonus pickup animation parameters in atlas-frame coordinates.
    // Immutable data — safe to keep across play sessions, no cleanup needed.
    [NoAutoStaticsCleanup]
    public static partial class BonusAnimations
    {
        public static readonly AnimationParam Ammo = new() {
            StartFrame = 0, FrameCount = 3, Mode = SpriteAnimationMode.PingPong
        };

        public static readonly AnimationParam Tank = new() {
            StartFrame = 3, FrameCount = 3, Mode = SpriteAnimationMode.PingPong
        };
    }

    // Client presentation: (re)starts bonus sprite animation when the bonus type changes.
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteAnimationSystem))]
    [BurstCompile]
    public partial struct BonusAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (spriteAnimation, bonusComponent) in SystemAPI.Query<RefRW<SpriteAnimation>, 
                         RefRO<BonusComponent>>().WithChangeFilter<BonusComponent>())
            {
                ref var animValue = ref spriteAnimation.ValueRW;

                if (bonusComponent.ValueRO.Type == BonusType.Ammo)
                    PlayAnimation(ref animValue, BonusAnimations.Ammo);
                if (bonusComponent.ValueRO.Type == BonusType.Tank)
                    PlayAnimation(ref animValue, BonusAnimations.Tank);
            }
        }

        private static void PlayAnimation(ref SpriteAnimation anim, AnimationParam param)
        {
            anim.StartFrame = param.StartFrame;
            anim.Frame = anim.StartFrame;
            anim.FrameCount = param.FrameCount;
            anim.Timer = 0;
            anim.Mode = param.Mode;
            anim.Stopped = false;
            anim.Reverse = false;
        }
    }
}
