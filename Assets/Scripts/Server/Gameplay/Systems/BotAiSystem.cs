using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace TanksKombat2.Server
{
    // Server-only bot AI. Runs on the main thread because decision-making requires cross-entity
    // queries (find nearest enemy/bonus, line-of-fire raycast against other tanks),
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(TankMovementSystem))]
    [UpdateBefore(typeof(TankShootingSystem))]
    [UpdateBefore(typeof(BotMovementSystem))]
    public partial struct BotAiSystem : ISystem
    {
        private LevelDataComponent _level;
        private LevelReachableCellsComponent _reachableCellsComponent;
        private EntityQuery _tankQuery;
        private EntityQuery _bonusQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BotComponent>();
            state.RequireForUpdate<LevelDataComponent>();
            state.RequireForUpdate<MatchStateComponent>();
            state.RequireForUpdate<TankStateComponent>();

            // Each query declares every component we plan to read out of it; otherwise
            // ToComponentDataArray<T> throws InvalidOperationException (T not in query).
            _tankQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<TankStateComponent>(),
                ComponentType.ReadOnly<LocalTransform>());

            _bonusQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BonusComponent>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState) &&
                matchState.State != MatchState.RoundInProgress)
                return;
            
            _level = SystemAPI.GetSingleton<LevelDataComponent>();
            _reachableCellsComponent = SystemAPI.GetSingleton<LevelReachableCellsComponent>();
            var gameplayConfig = SystemAPI.GetSingleton<ServerGameplayConfigComponent>();

            // Snapshot every tank + transform for nearest-enemy / line-of-fire queries.
            var tankStates = _tankQuery.ToComponentDataArray<TankStateComponent>(Allocator.Temp);
            var tankTransforms = _tankQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var tankEntities = _tankQuery.ToEntityArray(Allocator.Temp);
            
            var occupiedCells = new NativeHashSet<int>(tankTransforms.Length, Allocator.Temp);
            for (int i = 0; i < tankTransforms.Length; i++)
                occupiedCells.Add(_level.Index(tankTransforms[i].Position));
            
            var bonusTransforms = _bonusQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var bonusEntities = _bonusQuery.ToEntityArray(Allocator.Temp);

            foreach (var (botRef, inputRef, transformRef, stateRef, entity) in SystemAPI
                         .Query<RefRW<BotComponent>, RefRW<TankPlayerInput>, RefRO<LocalTransform>,
                             RefRO<TankStateComponent>>()
                         .WithEntityAccess())
            {
                ref var bot = ref botRef.ValueRW;
                
                // if our target was destroyed, drop it and re-decide.
                // Runs every frame so a dead target is noticed even mid-move.
                if (bot.TargetEntity != Entity.Null && !IsTargetAlive(ref state, bot.TargetEntity))
                {
                    RequestStop(entity, ref state);
                    bot.TargetEntity = Entity.Null;
                    bot.State = BotAiState.Undefined;
                }

                bot.AiTimer -= SystemAPI.Time.DeltaTime;
                if (bot.AiTimer > 0f)
                    continue;

                bot.AiTimer = bot.AiSpeed;

                MakeDecision(entity, ref state, ref bot, ref inputRef.ValueRW, in transformRef.ValueRO, in stateRef.ValueRO, 
                    in gameplayConfig.BotAiConfig, tankStates, tankTransforms, tankEntities, bonusTransforms, bonusEntities, occupiedCells);

                switch (bot.State)
                {
                    case BotAiState.Pursuit:
                        PursuitUpdate(entity, ref state, ref bot, in stateRef.ValueRO, tankStates, tankTransforms, tankEntities);
                        break;
                    case BotAiState.Attack:
                        break;
                    case BotAiState.Strafe:
                        break;
                    case BotAiState.Escape:
                        EscapeUpdate(entity, ref state, ref bot);
                        break;
                    case BotAiState.CollectBonus:
                        CollectBonusUpdate(entity, ref state, ref bot, bonusTransforms, bonusEntities);
                        break;
                }
            }

            tankStates.Dispose();
            tankTransforms.Dispose();
            tankEntities.Dispose();
            occupiedCells.Dispose();
            bonusTransforms.Dispose();
            bonusEntities.Dispose();
        }

        private void RequestMove(Entity entity, ref SystemState state, ref BotComponent bot, int2 cell, bool getUpClose)
        {
            SystemAPI.SetComponent(entity, new BotMoveRequest { TargetCell = cell, GetUpClose = getUpClose, Stop = false });
            SystemAPI.SetComponentEnabled<BotMoveRequest>(entity, true);
        }

        private void RequestStop(Entity entity, ref SystemState state)
        {
            SystemAPI.SetComponent(entity, new BotMoveRequest { Stop = true });
            SystemAPI.SetComponentEnabled<BotMoveRequest>(entity, true);
        }

        private void MakeDecision(
            Entity entity,
            ref SystemState state,
            ref BotComponent bot,
            ref TankPlayerInput input,
            in LocalTransform transform,
            in TankStateComponent tankState,
            in BotAiConfig aiConfig,
            NativeArray<TankStateComponent> tankStates,
            NativeArray<LocalTransform> tankTransforms,
            NativeArray<Entity> tankEntities,
            NativeArray<LocalTransform> bonusTransforms,
            NativeArray<Entity> bonusEntities,
            NativeHashSet<int> occupiedCells)
        {
            var origin = LevelHelper.WorldToGrid(transform.Position);
            
            TryGetEnemyInLineOfFire(origin, tankStates, tankTransforms, tankEntities, out int fireDir,
                out var enemyInLine, out bool enemyHasBullets);
            
            if (enemyInLine != Entity.Null)
            {
                float attackChanceMod = bot.AttacksInARow * -0.1f;
                float attackChance = math.clamp(aiConfig.AttackChance + attackChanceMod, 0f, 1f);
                float strafeChance = math.clamp(aiConfig.StrafeChance - attackChanceMod, 0f, 1f);
                
                if (CanAttack(in tankState) && bot.Rand.NextFloat() < attackChance)
                {
                    AttackStart(entity, ref state, ref bot, ref input, fireDir);
                    bot.AttacksInARow++;
                    return;
                }
                
                if (!IsInvulnerable(tankState) && enemyHasBullets && 
                    bot.Rand.NextFloat() < strafeChance &&
                    TryFindStrafeCell(origin, fireDir, ref bot.Rand, occupiedCells, out var strafeCell))
                {
                    StrafeStart(entity, ref state, ref bot, strafeCell);
                    bot.AttacksInARow = 0;
                    return;
                }
            }
            
            bot.AttacksInARow = 0;

            if (bot.State == BotAiState.Undefined)
            {
                // Nearest reachable enemy?
                if (CanAttack(in tankState) && 
                    FindNearestTank(entity, origin, tankStates, tankTransforms, tankEntities, out var nearestTankIdx))
                {
                    PursuitStart(ref bot, tankEntities[nearestTankIdx]);
                    return;
                }

                // Nearest bonus?
                if (FindNearestBonus(origin, bonusTransforms, out var nearestBonusIdx))
                {
                    CollectBonusStart(ref bot, bonusEntities[nearestBonusIdx]);
                    return;
                }

                // Escape or think
                bool needEscape = enemyInLine != Entity.Null || bot.Rand.NextFloat() < aiConfig.EscapeChance;
                if (needEscape && FindEscapePoint(origin, ref bot.Rand, in aiConfig, out var escapeCell))
                {
                    EscapeStart(entity, ref state, ref bot, escapeCell);
                }
                else
                {
                    bot.State = BotAiState.Undefined;
                    if (bot.Rand.NextBool())
                        input.SetMove(bot.Rand.NextInt(0, 4));
                    else
                        input.SetDirection(bot.Rand.NextInt(0, 4));
                }
            }
        }

        private void PursuitStart(ref BotComponent bot, Entity target)
        {
            bot.State = BotAiState.Pursuit;
            bot.TargetEntity = target;
        }

        private void PursuitUpdate(Entity entity, ref SystemState state, ref BotComponent bot, in TankStateComponent tankState,
            NativeArray<TankStateComponent> tankStates, NativeArray<LocalTransform> tankTransforms,
            NativeArray<Entity> tankEntities)
        {
            var targetIndex = tankEntities.IndexOf(bot.TargetEntity);

            // Target dead / unreachable / invulnerable -> re-decide.
            if (!CanAttack(in tankState) || targetIndex == -1 || IsInvulnerable(tankStates[targetIndex]))
            {
                bot.State = BotAiState.Undefined;
                bot.TargetEntity = Entity.Null;
                bot.AiTimer = 0; // make decision next frame
                return;
            }

            // Re-plan toward.
            var targetCell = LevelHelper.WorldToGrid(tankTransforms[targetIndex].Position);
            if (!math.all(targetCell == bot.TargetCell))
                RequestMove(entity, ref state, ref bot, targetCell, false);
        }

        private void AttackStart(Entity entity, ref SystemState state, ref BotComponent bot, ref TankPlayerInput input, int fireDir)
        {
            RequestStop(entity, ref state);
            
            bot.State = BotAiState.Undefined;
            bot.TargetEntity = Entity.Null;
            bot.AiTimer = bot.AiSpeed / 3;
            input.SetDirection(fireDir);
            input.SetShoot();
        }
        
        // a single-cell sidestep perpendicular to the enemy's firing line,
        // chosen with ChanceToStrafe. Reuses EscapePoint as the destination cell.
        private void StrafeStart(Entity entity, ref SystemState state, ref BotComponent bot, int2 strafeCell)
        {
            bot.State = BotAiState.Undefined;
            bot.TargetEntity = Entity.Null;
            RequestMove(entity, ref state, ref bot, strafeCell, true);
        }

        private void EscapeStart(Entity entity, ref SystemState state, ref BotComponent bot, int2 escapeCell)
        {
            bot.State = BotAiState.Escape;
            bot.TargetEntity = Entity.Null;
            bot.TargetCell = escapeCell;
            RequestMove(entity, ref state, ref bot, escapeCell, true);
        }

        private void EscapeUpdate(Entity entity, ref SystemState state, ref BotComponent bot)
        {
            if (bot.IsTargetReached)
            {
                RequestStop(entity, ref state);
                bot.State = BotAiState.Undefined;
                return;
            }

            // Re-plan if we somehow drifted off the path.
            if (!bot.IsMovingToward)
                RequestMove(entity, ref state, ref bot, bot.TargetCell, true);
        }

        private void CollectBonusStart(ref BotComponent bot, Entity bonus)
        {
            bot.State = BotAiState.CollectBonus;
            bot.TargetEntity = bonus;
        }

        private void CollectBonusUpdate(Entity entity, ref SystemState state, ref BotComponent bot,
            NativeArray<LocalTransform> bonusTransforms, NativeArray<Entity> bonusEntities)
        {
            if (!bot.IsMovingToward)
            {
                var targetIndex = bonusEntities.IndexOf(bot.TargetEntity);
                if (targetIndex != -1)
                {
                    var targetCell = LevelHelper.WorldToGrid(bonusTransforms[targetIndex].Position);
                    RequestMove(entity, ref state, ref bot, targetCell, true);
                }
            }
        }
        
        private static bool CanAttack(in TankStateComponent tankState)
        {
            return tankState.Bullets > 0 && tankState.InvulnerabilityTimer <= 0f;
        }

        private static bool IsInvulnerable(in TankStateComponent tankState)
        {
            return tankState.InvulnerabilityTimer > 0f;
        }

        // Returns true if any enemy tank shares the bot's row or column with no wall
        // between them. 'fireDir' uses LevelHelper.Directions
        // indices (0=Up, 1=Right, 2=Down, 3=Left).
        private bool TryGetEnemyInLineOfFire(int2 origin,
            NativeArray<TankStateComponent> tankStates, NativeArray<LocalTransform> transforms,
            NativeArray<Entity> entities, out int fireDir, out Entity enemy, out bool hasBullets)
        {
            fireDir = -1;
            enemy = Entity.Null;
            hasBullets = false;

            for (var i = 0; i < tankStates.Length; i++)
            {
                // Skip invulnerable enemies — shooting them is a waste of ammo.
                if (IsInvulnerable(tankStates[i]))
                    continue;
                
                var enemyCell = LevelHelper.WorldToGrid(transforms[i].Position);
                if (enemyCell.x == origin.x && enemyCell.y != origin.y)
                {
                    var dir = enemyCell.y > origin.y ? 0 : 2; // Up / Down
                    if (IsLineClear(origin, enemyCell, vertical: true))
                    {
                        fireDir = dir;
                        enemy = entities[i];
                        hasBullets = tankStates[i].Bullets > 0;
                        return true;
                    }
                }
                else if (enemyCell.y == origin.y && enemyCell.x != origin.x)
                {
                    var dir = enemyCell.x > origin.x ? 1 : 3; // Right / Left
                    if (IsLineClear(origin, enemyCell, vertical: false))
                    {
                        fireDir = dir;
                        enemy = entities[i];
                        hasBullets = tankStates[i].Bullets > 0;
                        return true;
                    }
                }
            }

            return false;
        }

        // Walks the cells strictly between 'from' and 'to' (exclusive) and returns
        // false if any of them is a wall. The two endpoints are assumed walkable.
        private bool IsLineClear(int2 from, int2 to, bool vertical)
        {
            if (vertical)
            {
                var step = to.y > from.y ? 1 : -1;
                for (var y = from.y + step; y != to.y; y += step)
                    if (_level.GetTile(new int2(from.x, y)).IsWall())
                        return false;
            }
            else
            {
                var step = to.x > from.x ? 1 : -1;
                for (var x = from.x + step; x != to.x; x += step)
                    if (_level.GetTile(new int2(x, from.y)).IsWall())
                        return false;
            }

            return true;
        }

        // Picks a one-cell sidestep perpendicular to the firing line.
        // Both sides are tried in random order so either side is picked with
        // equal chance when both are free.
        private bool TryFindStrafeCell(int2 origin, int fireDir, ref Random rand, NativeHashSet<int> occupiedCells, 
            out int2 strafeCell)
        {
            // Perpendicular offsets to the fire direction (0=Up,1=Right,2=Down,3=Left).
            var (first, second) = fireDir == 0 || fireDir == 2
                ? (new int2(-1, 0), new int2(1, 0))
                : (new int2(0, 1), new int2(0, -1));

            // Randomize which side is tried first.
            if (rand.NextBool())
                (first, second) = (second, first);

            var candidates = new FixedList64Bytes<int2> { origin + first, origin + second };

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (!_level.IsWalkable(candidate))
                    continue;
                if (occupiedCells.Contains(_level.Index(candidate)))
                    continue;

                strafeCell = candidate;
                return true;
            }

            strafeCell = origin;
            return false;
        }

        private static bool FindNearestTank(Entity selfEntity, int2 origin,
            NativeArray<TankStateComponent> tankStates, NativeArray<LocalTransform> transforms, 
            NativeArray<Entity> entities, out int bestIdx)
        {
            bestIdx = -1;
            var bestDist = int.MaxValue;

            for (var i = 0; i < tankStates.Length; i++)
            {
                // Never target ourselves.
                if (entities[i] == selfEntity)
                    continue;

                if (tankStates[i].InvulnerabilityTimer > 0f)
                    continue;

                var cell = LevelHelper.WorldToGrid(transforms[i].Position);
                var dist = math.abs(cell.x - origin.x) + math.abs(cell.y - origin.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return bestIdx >= 0;
        }

        private static bool FindNearestBonus(int2 origin, NativeArray<LocalTransform> transforms, out int bestIdx)
        {
            bestIdx = -1;
            var bestDist = int.MaxValue;

            for (var i = 0; i < transforms.Length; i++)
            {
                var cell = LevelHelper.WorldToGrid(transforms[i].Position);
                var dist = math.abs(cell.x - origin.x) + math.abs(cell.y - origin.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return bestIdx >= 0;
        }

        private bool FindEscapePoint(int2 origin, ref Random rand, in BotAiConfig aiConfig, out int2 result)
        {
            for (var attempt = 0; attempt < aiConfig.EscapeAttempts; attempt++)
            {
                var candidate = origin + new int2(
                    rand.NextInt(-aiConfig.EscapeSearchRadius, aiConfig.EscapeSearchRadius + 1),
                    rand.NextInt(-aiConfig.EscapeSearchRadius, aiConfig.EscapeSearchRadius + 1));

                if (math.all(candidate == origin))
                    continue;
                if (!_level.IsWalkable(candidate))
                    continue;
                if (!_reachableCellsComponent.IsReachable(_level.Size, candidate))
                    continue;

                result = candidate;
                return true;
            }

            result = origin;
            return false;
        }
        
        private bool IsTargetAlive(ref SystemState state, Entity target)
        {
            return SystemAPI.GetEntityStorageInfoLookup().Exists(target);
        }
    }
}