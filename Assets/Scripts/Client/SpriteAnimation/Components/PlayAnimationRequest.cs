using Unity.Entities;

namespace TanksKombat2.Client
{
    public struct AnimationParam
    {
        public int StartFrame;
        public int FrameCount;
        public SpriteAnimationMode Mode;
    }

    // One-shot animation request: producer sets AnimationParam and enables; consumer applies and disables.
    public struct PlayAnimationRequest : IComponentData, IEnableableComponent
    {
        public AnimationParam Animation;
    }
}
