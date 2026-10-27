#if UNITY_EDITOR
using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Server
{
    // Editor-only debug visualisation of bot paths via Debug.DrawLine. The lines
    // are drawn every server frame with a duration of 0 so they stay alive exactly
    // while the path is active. Toggle 'Enable' at runtime from the inspector or
    // from code to silence the rendering.
    [NoAutoStaticsCleanup]
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct BotPathDebugSystem : ISystem
    {
        private static readonly bool Enable = false;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BotComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!Enable)
                return;

            float offset = 0;
            foreach (var (path, ghostOwner) in SystemAPI.Query<DynamicBuffer<PathStep>, RefRO<GhostOwner>>()
                         .WithAll<BotComponent>())
            {
                if (path.Length == 0)
                    continue;
                
                var color = NetworkIdDebugColorUtility.GetColor(ghostOwner.ValueRO.NetworkId);

                for (var i = 0; i < path.Length; i++)
                {
                    if (i + 1 < path.Length)
                    {
                        var cell1 = LevelHelper.GridToWorld(path[i].Cell) + new float3(offset, offset, 0);
                        var cell2 = LevelHelper.GridToWorld(path[i + 1].Cell) + new float3(offset, offset, 0);
                        Debug.DrawLine(cell1, cell2, color, SystemAPI.Time.DeltaTime);
                    }
                }

                offset += 0.01f;
            }
        }
    }
}
#endif
