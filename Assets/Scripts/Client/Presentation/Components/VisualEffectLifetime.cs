using Unity.Entities;

namespace TanksKombat2.Client
{
    // Client effects: remaining lifetime in seconds of a one-shot visual effect;
    // VisualEffectLifetimeSystem consumes it and destroys the entity at zero.
    public struct VisualEffectLifetime : IComponentData
    {
        public float Value;
    }
}