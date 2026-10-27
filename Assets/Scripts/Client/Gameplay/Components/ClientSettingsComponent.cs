using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client-only presentation settings: tile set config reference, per-player color palette and
    // runtime player preferences (mirrored from GameSettings by ClientSettingsInitSystem).
    public struct ClientSettingsComponent : IComponentData
    {
        public UnityObjectRef<TileSetConfig> TileSetConfig;
        public FixedList512Bytes<float4> PlayerColors;
        public bool HealthBars;

        public Color GetColor(int colorIndex)
        {
            var c = PlayerColors[colorIndex % PlayerColors.Length];
            return new Color(c.x, c.y, c.z, c.w);
        }
    }
}
