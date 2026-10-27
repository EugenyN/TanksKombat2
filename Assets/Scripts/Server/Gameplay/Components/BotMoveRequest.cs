using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Server
{
    // One-shot movement command written by BotAiSystem and consumed (then disabled)
    // by BotMovementSystem the same tick.
    public struct BotMoveRequest : IComponentData, IEnableableComponent
    {
        public int2 TargetCell;
        public bool GetUpClose;
        public bool Stop; // true = cancel the current path instead of building a new one
    }
}