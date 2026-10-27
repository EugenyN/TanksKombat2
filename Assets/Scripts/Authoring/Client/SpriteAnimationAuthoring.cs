using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Authoring for atlas-frame sprite animation driven via the _Frame material property.
    public class SpriteAnimationAuthoring : MonoBehaviour
    {
        [Tooltip("Number of frames in the animation, starting at StartFrame.")]
        public int FrameCount = 16;

        [Tooltip("First frame index in the atlas.")]
        public int StartFrame = 0;

        [Tooltip("Playback speed in frames per second.")]
        public float FPS = 5f;

        [Tooltip("Loop continuously or play once and stop.")]
        public SpriteAnimationMode Mode = SpriteAnimationMode.Once;

        private class Baker : Baker<SpriteAnimationAuthoring>
        {
            public override void Bake(SpriteAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpriteAnimation
                {
                    Frame = authoring.StartFrame,
                    FrameCount = authoring.FrameCount,
                    StartFrame = authoring.StartFrame,
                    FPS = authoring.FPS,
                    Timer = 0f,
                    Mode = authoring.Mode
                });
                AddComponent(entity, new MaterialPropertyFrame { Value = authoring.StartFrame });
            }
        }
    }
}
