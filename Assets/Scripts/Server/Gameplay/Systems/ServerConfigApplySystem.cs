using TanksKombat2.Shared;
using Unity.Entities;
using Unity.Burst;

namespace TanksKombat2.Server
{
    // One-shot server system that merges an optional MatchConfigOverride entity into the baked
    // ServerSettingsComponent singleton, then destroys the override entity.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateBefore(typeof(ServerLevelLoadSystem))]
    [UpdateBefore(typeof(BotConnectionsCreateSystem))]
    public partial struct ServerConfigApplySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ServerSettingsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var serverConfigQuery = SystemAPI.QueryBuilder().WithAllRW<ServerConfigOverride>().Build();
            if (serverConfigQuery.IsEmptyIgnoreFilter)
                return;

            var overrideEntity = serverConfigQuery.GetSingletonEntity();
            var serverConfig = state.EntityManager.GetComponentData<ServerConfigOverride>(overrideEntity);

            var settings = SystemAPI.GetSingletonRW<ServerSettingsComponent>();

            // All override fields land in the shared, client-visible match config.
            ref var matchConfig = ref settings.ValueRW.DefaultMatchConfig;

            matchConfig.MapType = serverConfig.MapType;
            matchConfig.CustomMap = serverConfig.CustomMap;
            matchConfig.MapTheme = serverConfig.MapTheme;
            matchConfig.GameMode = serverConfig.GameMode;
            matchConfig.BotSkill = serverConfig.BotSkill;
            matchConfig.BotCount = serverConfig.BotCount;
            matchConfig.RoundsToWin = serverConfig.RoundsToWin;

            state.EntityManager.DestroyEntity(overrideEntity);
        }
    }
}
