using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: turns join/disconnect/destroy RPCs into event log messages.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class EventLogViewSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<MatchStateComponent>(); // make sure scene loaded
        }
        
        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var eventLogView = InGameUIViews.Instance.EventLog;
            
            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);
            var settings = SystemAPI.GetSingleton<ClientSettingsComponent>();

            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<PlayerJoinedRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                var color = settings.GetColor(rpc.ValueRO.ColorIndex);
                eventLogView.SetEventLogMessage($"{rpc.ValueRO.PlayerName.FormatWithColor(color)} joined");
                
                Debug.Log($"[Client] Player '{rpc.ValueRO.PlayerName}' (id={rpc.ValueRO.NetworkId}) joined the game.");
                
                ecb.DestroyEntity(entity);
            }

            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<PlayerDisconnectedRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                var color = settings.GetColor(rpc.ValueRO.ColorIndex);
                eventLogView.SetEventLogMessage($"{rpc.ValueRO.PlayerName.FormatWithColor(color)} disconnected");
                
                Debug.Log($"[Client] Player '{rpc.ValueRO.PlayerName}' (id={rpc.ValueRO.NetworkId}) disconnected.");
                
                ecb.DestroyEntity(entity);
            }
            
            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<TankDestroyedRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                var playerInfoBuffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
                var killerInfo = playerInfoBuffer.GetPlayerInfo(rpc.ValueRO.KillerNetworkId);
                var victimInfo = playerInfoBuffer.GetPlayerInfo(rpc.ValueRO.VictimNetworkId);
                var killerName = killerInfo.PlayerName.FormatWithColor(settings.GetColor(killerInfo.ColorIndex));
                var victimName = victimInfo.PlayerName.FormatWithColor(settings.GetColor(victimInfo.ColorIndex));
                
                eventLogView.SetEventLogMessage($"{killerName} destroyed {victimName}");
      
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
        }
    }
}
