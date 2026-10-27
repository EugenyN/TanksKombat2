using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client audio setup: clip library by key plus the audio source prefab; baked for presentation systems.
    public class AudioSettingsDataAuthoring : MonoBehaviour
    {
        [Tooltip("SFX clips mapped by gameplay event key.")]
        [SerializeField]
        public Dictionary<AudioClipKeys, AudioClip> Clips = new();

        [Tooltip("Prefab of the audio source used to play the clips.")]
        public GameObject AudioSource;
        
        private class Baker : Baker<AudioSettingsDataAuthoring>
        {
            public override void Bake(AudioSettingsDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new AudioSettingsData
                {
                    AudioSourcePrefab = authoring.AudioSource
                });

                var buffer = AddBuffer<AudioClipElement>(entity);
                foreach (var entry in authoring.Clips)
                    buffer.Add(new AudioClipElement { Key = entry.Key, Clip = entry.Value });
            }
        }
    }
}