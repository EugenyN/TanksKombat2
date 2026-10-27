using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // Registry entry of a session participant, ghosted to all clients via the player-info buffer
    // on the MatchState singleton. Covers everything known about a player between rounds: name,
    // palette color, wins, ready/spectator flags and the Disconnected marker (the entry outlives
    // the connection entity so the scoreboard keeps showing disconnected players).
    public struct PlayerInfoComponent : IBufferElementData
    {
        [GhostField] public int NetworkId;
        [GhostField] public FixedString32Bytes PlayerName;
        [GhostField] public int ColorIndex;
        [GhostField] public int WinsCount;
        [GhostField] public bool Disconnected;
        [GhostField] public bool Ready;
        [GhostField] public bool Spectator;
        [GhostField] public bool IsHost;
    }
}