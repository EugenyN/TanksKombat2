using System;
using TanksKombat2.Shared;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Server
{
    [Serializable]
    public struct MapGenerationConfig
    {
        public uint Seed;
        public float ChanceToStartAlive;
        public int NumberOfSteps;
        public int DeathLimit;
        public int BirthLimit;
    }

    [Serializable]
    public struct GameModeConfigs
    {
        public GameModeConfig Standard;
        public GameModeConfig Pro;
        public GameModeConfig FireAndIron;
        public GameModeConfig QuickDeath;
    }

    [Serializable]
    public struct BotAiConfig
    {
        [Tooltip("Decision-tick cadence (seconds). Lower = more reactive.")]
        public float AiSpeed;

        public float AiSpeedSkillFactor;

        [Tooltip("Seconds between Move events while following a path.")]
        public float MoveStepDelay;

        public float MoveStepDelaySkillFactor;

        [Tooltip("Seconds between Move events while following a path. Random component")]
        public float MoveStepDelayRandom;

        [Tooltip("Probability of choosing ESCAPE over THINK when no target is available.")] [Range(0f, 1f)]
        public float EscapeChance;

        public float EscapeChanceSkillFactor;

        [Tooltip("Probability of making a shoot.")] [Range(0f, 1f)]
        public float AttackChance;

        public float AttackChanceSkillFactor;

        [Tooltip("Probability of making a one-cell strafe step sideways when standing on the firing line.")]
        [Range(0f, 1f)]
        public float StrafeChance;

        public float StrafeChanceSkillFactor;

        [Tooltip("Half-extent of the square in which ESCAPE picks a random walkable cell.")]
        public int EscapeSearchRadius;

        [Tooltip("Number of attempts before ESCAPE gives up finding a walkable cell.")]
        public int EscapeAttempts;
    }

    // Server-only gameplay tunables that never leave the server: map generation
    // (MapGenerationConfig), the per-mode rules table (GameModeConfigs) the active
    // GameModeConfig is resolved from, and bot AI settings (BotAiConfig).
    [Serializable]
    [AutoStaticsCleanup]
    public partial struct ServerGameplayConfigComponent : IComponentData
    {
        public MapGenerationConfig MapGenerationConfig;
        public GameModeConfigs GameModeConfigs;
        public BotAiConfig BotAiConfig;

        public static ServerGameplayConfigComponent Default = new()
        {
            MapGenerationConfig = new MapGenerationConfig
            {
                Seed = 0,
                ChanceToStartAlive = 0.15f,
                NumberOfSteps = 2,
                DeathLimit = 1,
                BirthLimit = 3,
            },

            GameModeConfigs = new GameModeConfigs()
            {
                Standard = new GameModeConfig
                {
                    Lives = 5,
                    Ammo = 20,
                    BonusSpawnDelay = 10,
                    BonusSpawnDelayRandom = 10,
                    MaxBonusesOnScene = 5,
                    AmmoInBonus = 10,
                    AmmoChance = 0.6f,
                    InvulnerabilityTime = 4
                },
                Pro = new GameModeConfig
                {
                    Lives = 3,
                    Ammo = 10,
                    BonusSpawnDelay = 10,
                    BonusSpawnDelayRandom = 10,
                    MaxBonusesOnScene = 5,
                    AmmoInBonus = 5,
                    AmmoChance = 0.6f,
                    InvulnerabilityTime = 3
                },
                FireAndIron = new GameModeConfig
                {
                    Lives = 5,
                    Ammo = 1,
                    BonusSpawnDelay = 2,
                    BonusSpawnDelayRandom = 2,
                    MaxBonusesOnScene = 5,
                    AmmoInBonus = 1,
                    AmmoChance = 0.7f,
                    InvulnerabilityTime = 1
                },
                QuickDeath = new GameModeConfig
                {
                    Lives = 16,
                    Ammo = 40,
                    BonusSpawnDelay = 5,
                    BonusSpawnDelayRandom = 5,
                    MaxBonusesOnScene = 5,
                    AmmoInBonus = 10,
                    AmmoChance = 0.6f,
                    InvulnerabilityTime = 0
                }
            },

            BotAiConfig = new BotAiConfig
            {
                AiSpeed = 0.6f,
                AiSpeedSkillFactor = -0.1f,
                MoveStepDelay = 0.2f,
                MoveStepDelaySkillFactor = -0.05f,
                MoveStepDelayRandom = 0.1f,
                EscapeChance = 0.5f,
                EscapeChanceSkillFactor = 0.2f,
                StrafeChance = 0.6f,
                StrafeChanceSkillFactor = 0.2f,
                AttackChance = 0.8f,
                AttackChanceSkillFactor = 0.2f,
                EscapeSearchRadius = 8,
                EscapeAttempts = 12
            }
        };
    }
}