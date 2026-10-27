using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Tank animation parameters in atlas-frame coordinates (before direction row offset).
    // Immutable data — safe to keep across play sessions, no cleanup needed.
    [NoAutoStaticsCleanup]
    public static partial class TankAnimations
    {
        public static readonly AnimationParam Idle = new() {
            StartFrame = 0, FrameCount = 2, Mode = SpriteAnimationMode.Loop
        };
        
        public static readonly AnimationParam Shoot = new() {
            StartFrame = 2, FrameCount = 1, Mode = SpriteAnimationMode.Once
        };
    }
    
    // Client presentation: drives tank sprite animation — applies queued PlayAnimationRequest
    // commands and falls back to the idle loop on direction change or when the animation stopped.
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteAnimationSystem))]
    [BurstCompile]
    public partial struct TankAnimationSystem : ISystem
    {
        private const int DirectionRowOffset = 3;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, spriteAnimation, playRequest, enabled) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<SpriteAnimation>, RefRO<PlayAnimationRequest>, EnabledRefRW<PlayAnimationRequest>>()
                         .WithAll<TankStateComponent>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                ref var animValue = ref spriteAnimation.ValueRW;
                var direction = LevelHelper.GetDirection(transform.ValueRO.Rotation);
                var directionOffset = direction * DirectionRowOffset;
                var currentRow = animValue.StartFrame / DirectionRowOffset;
                var directionChanged = direction != currentRow;

                // play idle if current animation stopped
                if (directionChanged || animValue.Stopped)
                {
                    PlayAnimation(ref animValue, TankAnimations.Idle, directionOffset);
                }

                if (enabled.ValueRO)
                {
                    PlayAnimation(ref animValue, playRequest.ValueRO.Animation, directionOffset);
                    enabled.ValueRW = false;
                }
            }
        }

        private static void PlayAnimation(ref SpriteAnimation anim, AnimationParam param, int directionOffset)
        {
            anim.StartFrame = param.StartFrame + directionOffset;
            anim.Frame = anim.StartFrame;
            anim.FrameCount = param.FrameCount;
            anim.Timer = 0;
            anim.Mode = param.Mode;
            anim.Stopped = false;
            anim.Reverse = false;
        }
    }
}
