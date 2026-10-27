using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Server
{
    // Bot AI state machine
    public enum BotAiState : byte
    {
        Undefined = 0,
        Pursuit,
        Attack,
        Escape,
        CollectBonus,
        Strafe
    }

    // Server-only state driving a bot tank. Added at runtime on the server,
    // never ghosted: clients just see a regular enemy tank.
    public struct BotComponent : IComponentData
    {
        // AI decision tick
        public float AiTimer;
        public float AiSpeed;

        // State machine
        public BotAiState State;
        public Entity TargetEntity;
        public int AttacksInARow;

        // Movement path-following (driven by BotMovementSystem)
        public bool IsMovingToward;
        public float MoveStepTimer;
        public float MoveStepDelay;
        public float MoveStepDelayRandom;
        public bool IsTargetReached;
        public int PathIndex;
        public int2 TargetCell;

        // Misc
        public Random Rand;
    }
}
