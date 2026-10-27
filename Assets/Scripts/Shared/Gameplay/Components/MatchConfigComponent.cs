using System;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;

namespace TanksKombat2.Shared
{
    // Match-choice enums shared by client settings UI, ServerConfigOverride and the
    // server-only ServerGameplayConfigComponent (which consumes them server-side).
    public enum MapType
    {
        SmallCave,
        LargeCave,
        ExtraLargeCave,
        CustomRandom,
        Custom
    }

    public enum CustomMap
    {
        Alien,
        Arena,
        Outpost,
        Jack,
        Skull,
        Spider
    }

    public enum GameMode
    {
        Standard,
        Pro,
        FireAndIron,
        QuickDeath
    }

    public enum BotSkill
    {
        Easy,
        Normal,
        Hard
    }

    public enum ConnectionMode
    {
        UnityRelay,
        DirectIP
    }

    public enum MapSizePreset
    {
        Small,
        Large,
        ExtraLarge
    }

    public enum MapTheme
    {
        Random,
        Hyrule,
        Crateria,
        Arctica,
        Castle,
        Ruins,
        SkyFi,
        Cave,
        Dungeon
    }

    [Serializable]
    public struct GameModeConfig
    {
        public byte Lives;
        public byte Ammo;
        public float BonusSpawnDelay;
        public float BonusSpawnDelayRandom;
        public int MaxBonusesOnScene;
        public byte AmmoInBonus;
        public float AmmoChance;
        public int InvulnerabilityTime;
    }

    // Shared match configuration: the operator's/player's choices (map, mode, bots) plus
    // per-round values resolved by the server (theme, size, active GameModeConfig). Sent to
    // the client via LoadLevelRpc for presentation and prediction. Server-only tunables
    // (generation, mode table, bot AI) live in ServerGameplayConfigComponent.
    [Serializable]
    [AutoStaticsCleanup]
    public partial struct MatchConfigComponent : IComponentData
    {
        public MapType MapType;
        public CustomMap CustomMap;
        public MapTheme MapTheme;
        public MapSizePreset MapSizePreset;

        public GameMode GameMode;
        public BotSkill BotSkill;
        public int BotCount;

        public int RoundsToWin;
        public float BulletVelocity;
        public GameModeConfig GameModeConfig;

        public static MatchConfigComponent Default = new()
        {
            MapType = MapType.SmallCave,
            CustomMap = CustomMap.Alien,
            MapSizePreset = MapSizePreset.Small,

            GameMode = GameMode.Standard,
            BotSkill = BotSkill.Normal,
            BotCount = 0,

            RoundsToWin = 3,
            BulletVelocity = 1200,

            GameModeConfig = new GameModeConfig
            {
                Lives = 5,
                Ammo = 20,
                BonusSpawnDelay = 10,
                BonusSpawnDelayRandom = 10,
                MaxBonusesOnScene = 5,
                AmmoInBonus = 10,
                AmmoChance = 0.6f,
                InvulnerabilityTime = 4
            }
        };
    }
}