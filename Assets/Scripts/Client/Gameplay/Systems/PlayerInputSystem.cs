using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Client input: polls keyboard actions (Unity Input System, managed) into the
    // local player's TankPlayerInput ghost. Without [BurstCompile] this ISystem runs on Mono,
    // which is required for the managed Input System polling.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<MatchStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // TryGetSingleton/GetSingleton is a check-only fast path (no auto-complete), so the
            // pending GhostUpdateSystem:UpdateJob write must be completed explicitly before reading.
            state.EntityManager.CompleteDependencyBeforeRO<MatchStateComponent>();
            
            foreach (var playerInput in SystemAPI.Query<RefRW<TankPlayerInput>>().WithAll<GhostOwnerIsLocal>())
                playerInput.ValueRW = default;
            
            var matchState = SystemAPI.GetSingleton<MatchStateComponent>();
            if (matchState.State != MatchState.RoundInProgress)
                return;

            // UI windows are open (menu/options): keep sending an empty command so the
            // click that opened or closes a window never reaches the simulation.
            if (!GameInput.IsPlayerInputAllowed)
                return;

            var inputActions = GameInput.Actions.Player;

            bool moveUp = inputActions.MoveUp.WasPressedThisFrame();
            bool moveRight = inputActions.MoveRight.WasPressedThisFrame();
            bool moveDown = inputActions.MoveDown.WasPressedThisFrame();
            bool moveLeft = inputActions.MoveLeft.WasPressedThisFrame();
            bool shoot = inputActions.Shoot.WasPressedThisFrame();
            bool block = inputActions.Block.IsPressed();

            foreach (var playerInput in SystemAPI.Query<RefRW<TankPlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                ref var inputData = ref playerInput.ValueRW;
                
                if (moveUp) inputData.MoveUp.Set();
                if (moveRight) inputData.MoveRight.Set();
                if (moveDown) inputData.MoveDown.Set();
                if (moveLeft) inputData.MoveLeft.Set();
                if (shoot) inputData.Shoot.Set();
                if (block) inputData.Block.Set();
            }
        }
    }
}