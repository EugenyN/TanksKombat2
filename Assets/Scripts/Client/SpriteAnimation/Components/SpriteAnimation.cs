using Unity.Entities;

namespace TanksKombat2.Client
{
    public enum SpriteAnimationMode
    {
        Once,
        Loop,
        PingPong
    }

    // Client presentation state of an atlas-frame animation: current frame, playback
    // timing and mode. Consumed by SpriteAnimationSystem.
    public struct SpriteAnimation : IComponentData
    {
        public int Frame;
        public int FrameCount;
        public int StartFrame;

        public float FPS;
        public float Timer;
        public bool Stopped;

        public SpriteAnimationMode Mode;
        public bool Reverse;
    }
}
