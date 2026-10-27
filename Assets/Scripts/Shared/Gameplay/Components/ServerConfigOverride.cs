using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Optional, transient override entity created in the server world before it starts ticking.
    // Carries the operator's/host's match-config choices. ServerConfigApplySystem consumes it
    // once (merges all fields into the baked ServerSettingsComponent.MatchConfig, then destroys
    // this entity), so the override has no lifetime beyond server boot.
    //
    // Who creates it:
    //   - dedicated server: GameBootstrap parses CLI args into it (#if UNITY_SERVER);
    //   - self-host: ConnectionManager snapshots the host's GameSettings into it.
    // When absent (e.g. editor GameScene quick-start), ServerConfigApplySystem does nothing and
    // the baked ServerSettingsAuthoring.levelData defaults apply.
    //
    // This is an "always overwrite" override — every field is applied; there is no per-field
    // mask. Creators should seed unspecified fields with sensible defaults rather than leave
    // them zero so the baked choice is not partially clobbered.
    public struct ServerConfigOverride : IComponentData
    {
        public MapType MapType;
        public CustomMap CustomMap;
        public MapTheme MapTheme;
        public GameMode GameMode;
        public BotSkill BotSkill;
        public int BotCount;
        public int RoundsToWin;
    }
}