using TanksKombat2.Shared;
using Unity.Entities;

namespace TanksKombat2.Server
{
    // Server-authoritative session configuration holder: pairs the shared MatchConfigComponent with
    // the server-only ServerGameplayConfigComponent (tunables that never leave the server).
    // Populated once at server boot by ServerConfigApplySystem, which merges an optional
    // MatchConfigOverride entity into both baked configs
    public struct ServerSettingsComponent : IComponentData
    {
        public MatchConfigComponent DefaultMatchConfig;
        public ServerGameplayConfigComponent DefaultGameplayConfig;
    }
}