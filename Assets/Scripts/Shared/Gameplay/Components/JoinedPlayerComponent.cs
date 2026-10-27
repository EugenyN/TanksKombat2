using Unity.Collections;
using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Attached to a player connection entity after a successful join; carries the display name.
    public struct JoinedPlayerComponent : IComponentData
    {
        public FixedString32Bytes PlayerName;
    }
}