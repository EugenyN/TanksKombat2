using TanksKombat2.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: fills each player HUD entry from the player info buffer and tank state.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class PlayerStatesViewSystem : SystemBase
    {
        private EntityQuery _tankQuery;

        protected override void OnCreate()
        {
            _tankQuery = GetEntityQuery(ComponentType.ReadOnly<TankStateComponent>(), 
                ComponentType.ReadOnly<GhostOwner>());

            RequireForUpdate<MatchStateComponent>();
            RequireForUpdate<ClientSettingsComponent>();
        }

        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var playerStatesView = InGameUIViews.Instance.PlayerStates;
            
            var matchState = SystemAPI.GetSingleton<MatchStateComponent>();
            var settings = SystemAPI.GetSingleton<ClientSettingsComponent>();

            var tankData = new NativeHashMap<int, (int Lives, int Bullets)>(_tankQuery.CalculateEntityCount(), Allocator.Temp);
            foreach (var (tankState, ghostOwner) in SystemAPI.Query<RefRO<TankStateComponent>, RefRO<GhostOwner>>())
                tankData[ghostOwner.ValueRO.NetworkId] = new (tankState.ValueRO.Lives, tankState.ValueRO.Bullets);
            
            var buffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
            for (var index = 0; index < buffer.Length; index++)
            {
                var playerInfo = buffer[index];
                var color = settings.GetColor(playerInfo.ColorIndex);
                int lives = 0;
                int bullets = 0;
                if (tankData.TryGetValue(playerInfo.NetworkId, out var lb))
                {
                    lives = lb.Lives;
                    bullets = lb.Bullets;
                }

                playerStatesView.SetPlayerInfo(index, playerInfo, color, lives, bullets, matchState.State);
            }

            tankData.Dispose();
        }
    }
}
