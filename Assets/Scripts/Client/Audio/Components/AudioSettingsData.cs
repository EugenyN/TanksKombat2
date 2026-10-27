using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client audio settings singleton: AudioSource pool prefab, baked from authoring via UnityObjectRef.
    public struct AudioSettingsData : IComponentData
    {
        public UnityObjectRef<GameObject> AudioSourcePrefab;
    }
}
