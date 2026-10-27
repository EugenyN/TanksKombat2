using Unity.Burst;
using Unity.Entities;

namespace TanksKombat2.Client
{
    // Client presentation: advances atlas-frame animation state and writes the current
    // frame into MaterialPropertyFrame (_Frame material property) for all animated sprites.
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct SpriteAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpriteAnimation>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new SpriteAnimationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            job.Schedule();
        }
        
        public static void Play(EntityCommandBuffer ecb, Entity entity, AnimationParam animation)
        {
            ecb.SetComponent(entity, new PlayAnimationRequest { Animation = animation });
            ecb.SetComponentEnabled<PlayAnimationRequest>(entity, true);
        }

        [BurstCompile]
        partial struct SpriteAnimationJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref SpriteAnimation anim, ref MaterialPropertyFrame frameProperty)
            {
                if (anim.Stopped)
                    return;

                if (anim.FrameCount <= 0)
                    return;

                if (anim.FPS <= 0f)
                    return;

                // Single-frame PingPong would oscillate between two identical states; treat as Once.
                if (anim.Mode == SpriteAnimationMode.PingPong && anim.FrameCount == 1)
                    anim.Mode = SpriteAnimationMode.Once;

                anim.Timer += DeltaTime;
                float frameDuration = 1f / anim.FPS;
                
                while (anim.Timer >= frameDuration)
                {
                    anim.Timer -= frameDuration;
                    
                    if (anim.Mode == SpriteAnimationMode.PingPong && anim.Reverse)
                        anim.Frame--;
                    else
                        anim.Frame++;

                    var maxFrame = anim.StartFrame + anim.FrameCount - 1;

                    if (anim.Frame > maxFrame)
                    {
                        switch (anim.Mode)
                        {
                            case SpriteAnimationMode.Loop:
                                anim.Frame = anim.StartFrame;
                                break;
                            case SpriteAnimationMode.PingPong:
                                anim.Frame = maxFrame - 1;
                                anim.Reverse = true;
                                if (anim.Frame < anim.StartFrame)
                                    anim.Frame = anim.StartFrame;
                                break;
                            default:
                                anim.Frame = maxFrame;
                                anim.Timer = 0f;
                                anim.Stopped = true;
                                break;
                        }
                    }
                    else if (anim.Frame < anim.StartFrame)
                    {
                        anim.Frame = anim.StartFrame + 1;
                        anim.Reverse = false;
                        if (anim.Frame > maxFrame)
                            anim.Frame = maxFrame;
                    }
                }
                
                if ((int)frameProperty.Value != anim.Frame)
                    frameProperty.Value = anim.Frame;
            }
        }
    }
}
