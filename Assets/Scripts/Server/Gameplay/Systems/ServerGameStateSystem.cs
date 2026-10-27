using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Server
{
    // Server-authoritative match flow: handles StartNewMatchRequestRpc / PlayerReadyRpc,
    // drives the MatchState singleton through its phases (WaitingForPlayers, RoundInProgress,
    // RoundFinished, MatchFinished), ends rounds, scores wins and cleans up level entities.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerGameStateSystem : ISystem
    {
        private EntityQuery _levelGroup;
        private EntityQuery _tanksGroup;
        private EntityQuery _bulletsGroup;

        // High-water mark of tank count captured at the moment the round switches to RoundInProgress.
        private int _maxTanksSeen;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _levelGroup = state.GetEntityQuery(ComponentType.ReadWrite<LevelDataComponent>());
            _tanksGroup = state.GetEntityQuery(ComponentType.ReadOnly<TankStateComponent>());
            _bulletsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BulletTagComponent>());

            state.RequireForUpdate<ServerSettingsComponent>();
            state.RequireForUpdate<MatchStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.TryGetSingletonRW<MatchStateComponent>(out var matchState);

            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            // StartNewMatchRequestRpc
            
            foreach (var (_, entity) in SystemAPI.Query<RefRO<StartNewMatchRequestRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);
                if (matchState.ValueRO.State == MatchState.MatchFinished)
                    StartNewMatch(ref state, commandBuffer);
                break;
            }

            // PlayerReadyRpc
            
            foreach (var (_, requestSource, entity) in
                     SystemAPI.Query<RefRO<PlayerReadyRpc>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);
                if (matchState.ValueRO.State == MatchState.WaitingForPlayers)
                {
                    if (SystemAPI.TryGetComponent(requestSource.ValueRO.SourceConnection, out NetworkId networkId))
                    {
                        var playerInfoBuffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
                        playerInfoBuffer.GetPlayerInfo(networkId.Value).Ready = true;
                    }
                }
            }

            // MatchStateComponent processing
            
            var matchTiming = matchState.ValueRO.TimingSettings;

            if (matchState.ValueRO.State == MatchState.WaitingForPlayers)
            {
                matchState.ValueRW.TimeRemaining -= SystemAPI.Time.DeltaTime;

                if (matchState.ValueRO.Round == 1)
                {
                    if (AreAllPlayersReady(ref state) && 
                        matchState.ValueRW.TimeRemaining > matchTiming.RoundFightMessageDelay)
                        matchState.ValueRW.TimeRemaining = matchTiming.RoundFightMessageDelay;
                }

                if (matchState.ValueRW.TimeRemaining <= 0)
                {
                    matchState.ValueRW.State = MatchState.RoundInProgress;
                    _maxTanksSeen = _tanksGroup.CalculateEntityCount();
                    return;
                }
            }

            if (matchState.ValueRO.State == MatchState.RoundInProgress)
            {
                if (IsRoundEnded(ref state, out var tanksLeft))
                {
                    if (tanksLeft == 1 && GetLeftTankPlayerId(ref state, out var winnerNetworkId))
                    {
                        matchState.ValueRW.WinnerNetworkId = winnerNetworkId;

                        var playerInfoBuffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
                        ref var playerInfo = ref playerInfoBuffer.GetPlayerInfo(winnerNetworkId);
                        playerInfo.WinsCount++;

                        var config = SystemAPI.GetSingleton<MatchConfigComponent>();
                        if (playerInfo.WinsCount >= config.RoundsToWin)
                        {
                            matchState.ValueRW.State = MatchState.MatchFinished;
                            matchState.ValueRW.TimeRemaining = matchTiming.MatchResultTime;
                        }
                        else
                        {
                            matchState.ValueRW.State = MatchState.RoundFinished;
                            matchState.ValueRW.TimeRemaining = matchTiming.RoundFinishedTime;
                            matchState.ValueRW.Round++;
                        }
                    }
                    else
                    {
                        // Draw: all remaining tanks were destroyed simultaneously.
                        // Nobody scores a win; advance to the next round.
                        matchState.ValueRW.WinnerNetworkId = 0;
                        matchState.ValueRW.State = MatchState.RoundFinished;
                        matchState.ValueRW.TimeRemaining = matchTiming.RoundFinishedTime;
                        matchState.ValueRW.Round++;
                    }

                    StopMatch(ref state, commandBuffer);
                }
            }

            if (matchState.ValueRO.State == MatchState.RoundFinished)
            {
                matchState.ValueRW.TimeRemaining -= SystemAPI.Time.DeltaTime;
                if (matchState.ValueRW.TimeRemaining <= 0)
                {
                    matchState.ValueRW.State = MatchState.WaitingForPlayers;
                    matchState.ValueRW.TimeRemaining = matchTiming.NewRoundTime;
                    DestroyLevel(ref state, commandBuffer);
                    return;
                }
            }

            if (matchState.ValueRO.State == MatchState.MatchFinished)
            {
                matchState.ValueRW.TimeRemaining -= SystemAPI.Time.DeltaTime;
                if (matchState.ValueRW.TimeRemaining <= 0)
                {
                    StartNewMatch(ref state, commandBuffer);
                }
            }
        }

        private bool AreAllPlayersReady(ref SystemState state)
        {
            int totalPlayers = 0;
            int readyPlayers = 0;
            foreach (var entry in SystemAPI.GetSingletonBuffer<PlayerInfoComponent>())
            {
                if (entry.Disconnected) continue;
                totalPlayers++;
                if (entry.Ready) readyPlayers++;
            }

            return totalPlayers > 0 && readyPlayers == totalPlayers;
        }

        private bool GetLeftTankPlayerId(ref SystemState state, out int networkId)
        {
            foreach (var playerId in SystemAPI.Query<RefRO<PlayerIdComponent>>().WithAll<TankStateComponent>())
            {
                var playerConnection = playerId.ValueRO.PlayerEntity;
                if (!SystemAPI.HasComponent<JoinedPlayerComponent>(playerConnection))
                    continue;

                networkId = SystemAPI.GetComponent<NetworkId>(playerConnection).Value;
                return true;
            }

            networkId = -1;
            return false;
        }

        private bool IsRoundEnded(ref SystemState state, out int tanksLeft)
        {
            tanksLeft = _tanksGroup.CalculateEntityCount();

            // Never started / still solo — round must continue.
            if (_maxTanksSeen <= 1)
                return false;

            if (tanksLeft > 1)
                return false;

            // tanksLeft == 0 or 1: wait for bullets owned by tanks no longer alive.
            if (!IsBulletsFromDestroyedTanksCleared(ref state, tanksLeft))
                return false;

            return true;
        }

        // Returns true when no in-flight bullet remains whose owner is not among the current
        // tanks.
        private bool IsBulletsFromDestroyedTanksCleared(ref SystemState state, int tanksLeft)
        {
            if (_bulletsGroup.IsEmpty)
                return true;

            if (tanksLeft == 0)
                return false; // Any remaining bullet belongs to a dead tank → wait.

            // tanksLeft == 1: collect the survivor's network id, then scan bullets; any
            // bullet whose owner isn't the survivor is a dead man's bullet.
            if (!GetLeftTankPlayerId(ref state, out var survivorNetworkId))
                return false;

            foreach (var ghostOwner in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<BulletTagComponent>())
            {
                if (ghostOwner.ValueRO.NetworkId != survivorNetworkId)
                    return false;
            }

            return true;
        }

        private void StartNewMatch(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            DestroyLevel(ref state, commandBuffer);

            SystemAPI.TryGetSingletonRW<MatchStateComponent>(out var matchState);
            
            matchState.ValueRW.State = MatchState.WaitingForPlayers;
            matchState.ValueRW.TimeRemaining = matchState.ValueRO.TimingSettings.WaitingForPlayersTime;
            matchState.ValueRW.Round = 1;
            matchState.ValueRW.WinnerNetworkId = 0;

            var matchStateEntity = SystemAPI.GetSingletonEntity<MatchStateComponent>();
            var playerInfoBuffer = state.EntityManager.GetBuffer<PlayerInfoComponent>(matchStateEntity);
            for (int i = 0; i < playerInfoBuffer.Length; i++)
            {
                ref var playerInfo = ref playerInfoBuffer.ElementAt(i);
                playerInfo.WinsCount = 0;
                playerInfo.Ready = ConnectionUtils.IsBotNetworkId(playerInfo.NetworkId) ||
                                   (playerInfo.Spectator && !playerInfo.IsHost);
            }
        }

        private void StopMatch(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            foreach (var (_, entity) in SystemAPI.Query<RefRO<BulletTagComponent>>().WithEntityAccess())
                commandBuffer.DestroyEntity(entity);

            var commandTargetLookup = SystemAPI.GetComponentLookup<CommandTarget>();
            var linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>();

            foreach (var (networkId, playerConnection) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithEntityAccess())
            {
                ConnectionUtils.DetachTankFromConnection(
                    playerConnection, commandTargetLookup, linkedEntityGroupLookup);
            }
        }

        private void DestroyLevel(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            commandBuffer.DestroyEntity(_levelGroup, EntityQueryCaptureMode.AtPlayback);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<WallTagComponent>>().WithEntityAccess())
                commandBuffer.DestroyEntity(entity);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<BonusComponent>>().WithEntityAccess())
                commandBuffer.DestroyEntity(entity);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<TankStateComponent>>().WithEntityAccess())
                commandBuffer.DestroyEntity(entity);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<BulletTagComponent>>().WithEntityAccess())
                commandBuffer.DestroyEntity(entity);

            // Detach every connection (real clients and bots alike) from its tank and clear
            // the per-round markers so TankSpawnSystem respawns tanks for the next round.
            // LevelSentTag is only present on real clients, but RemoveComponent is idempotent.
            // This also covers paths that skip StopMatch (e.g. StartNewMatchRequestRpc), which
            // would otherwise leave stale CommandTargets behind.
            var commandTargetLookup = SystemAPI.GetComponentLookup<CommandTarget>();
            var linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>();

            foreach (var (networkId, playerConnection) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithEntityAccess())
            {
                ConnectionUtils.DetachTankFromConnection(
                    playerConnection, commandTargetLookup, linkedEntityGroupLookup);

                commandBuffer.RemoveComponent<LevelSentTag>(playerConnection);
                commandBuffer.RemoveComponent<TankSpawnedTag>(playerConnection);
            }
        }
    }
}