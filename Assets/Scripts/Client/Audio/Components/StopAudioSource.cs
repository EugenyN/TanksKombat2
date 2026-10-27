using Unity.Entities;

namespace TanksKombat2.Client
{
    // One-shot client event: requests the audio pool to stop all playing sources; consumed and destroyed by AudioSystem.
    public struct StopAudioSource : IComponentData
    {
    }
}
