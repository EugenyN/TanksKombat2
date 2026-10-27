using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    public static class ConnectionUtils
    {
        public const int MaxSessionPlayers = 8;
        public const int BotNetworkIdBase = 100001;

        public static bool IsBotNetworkId(int networkId) => networkId >= BotNetworkIdBase;
        
        // Detaches the connection's tank: clears CommandTarget.targetEntity and removes
        // the tank's entry from the connection's LinkedEntityGroup.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DetachTankFromConnection(Entity playerConnection, 
            ComponentLookup<CommandTarget> commandTargetLookup, BufferLookup<LinkedEntityGroup> linkedEntityGroupLookup)
        {
            var tankEntity = Entity.Null;
            if (commandTargetLookup.HasComponent(playerConnection))
            {
                var commandTarget = commandTargetLookup[playerConnection];
                tankEntity = commandTarget.targetEntity;
                commandTarget.targetEntity = Entity.Null;
                commandTargetLookup[playerConnection] = commandTarget;
            }

            if (tankEntity == Entity.Null || !linkedEntityGroupLookup.HasBuffer(playerConnection))
                return;

            var linkedEntityGroup = linkedEntityGroupLookup[playerConnection];
            // Real NetCode connections have a self-reference at index 0 — never remove it.
            // Bot connections (BotConnectionsCreateSystem) have no self-reference and
            // store the tank at index 0, so match the tank entry anywhere but skip the
            // self-reference value.
            for (var i = 0; i < linkedEntityGroup.Length; i++)
            {
                var value = linkedEntityGroup[i].Value;
                if (value != tankEntity || value == playerConnection)
                    continue;
                linkedEntityGroup.RemoveAt(i);
                break;
            }
        }
    }
}
