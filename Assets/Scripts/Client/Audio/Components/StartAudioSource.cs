using Unity.Entities;

namespace TanksKombat2.Client
{
    // One-shot client event: requests the audio pool to play the clip with the given key; consumed and destroyed by AudioSystem.
    public struct StartAudioSource : IComponentData
    {
        public AudioClipKeys Key;
    }
}
