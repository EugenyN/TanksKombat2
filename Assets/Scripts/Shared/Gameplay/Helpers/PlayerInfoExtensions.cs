using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Lookup helpers for finding a player's PlayerInfoComponent entry (by NetworkId)
    public static class PlayerInfoExtensions
    {
        public static ref PlayerInfoComponent GetPlayerInfo(this DynamicBuffer<PlayerInfoComponent> playerInfoBuffer, 
            int networkId)
        {
            for (int i = 0; i < playerInfoBuffer.Length; i++)
            {
                if (playerInfoBuffer[i].NetworkId == networkId)
                    return ref playerInfoBuffer.ElementAt(i);
            }

            throw new System.InvalidOperationException($"PlayerInfo with NetworkId={networkId} not found.");
        }
        
        public static bool TryGetPlayerInfoIndex(this DynamicBuffer<PlayerInfoComponent> playerInfoBuffer, 
            int networkId, out int index)
        {
            for (int i = 0; i < playerInfoBuffer.Length; i++)
            {
                if (playerInfoBuffer[i].NetworkId == networkId)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }
}
