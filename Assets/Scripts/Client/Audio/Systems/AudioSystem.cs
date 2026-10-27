using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client presentation: pooled AudioSource playback driven by StartAudioSource/StopAudioSource events.
    [AutoStaticsCleanup]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class AudioSystem : SystemBase
    {
        private const int PoolSize = 4;
        private readonly List<AudioSource> _pool = new(PoolSize);
        private Dictionary<AudioClipKeys, AudioClip> _clips;
        private Transform _parent;
        private static AudioClipKeys? _lastPlayedClip;

        private int _roundRobinIndex;

        protected override void OnCreate()
        {
            RequireForUpdate<AudioSettingsData>();
        }

        protected override void OnStartRunning()
        {
            _clips = new Dictionary<AudioClipKeys, AudioClip>();
            foreach (var entry in SystemAPI.GetSingletonBuffer<AudioClipElement>())
                _clips[entry.Key] = entry.Clip;

            BuildPool();
        }

        protected override void OnStopRunning()
        {
            DisposePool();
            _clips = null;
            _lastPlayedClip = null;
        }

        protected override void OnDestroy()
        {
            DisposePool();
            _lastPlayedClip = null;
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (stopAudioSource,  e) in SystemAPI.Query<RefRO<StopAudioSource>>().WithEntityAccess())
            {
                foreach (var source in _pool)
                    source.Stop();
                ecb.DestroyEntity(e);
            }

            foreach (var (startAudioSource,  e) in SystemAPI.Query<RefRO<StartAudioSource>>().WithEntityAccess())
            {
                var audioSource = AcquireSource();
                audioSource.clip = _clips[startAudioSource.ValueRO.Key];
                audioSource.Play();
                ecb.DestroyEntity(e);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void BuildPool()
        {
            DisposePool();

            var prefab = SystemAPI.GetSingleton<AudioSettingsData>().AudioSourcePrefab;

            _parent = new GameObject("AudioSources").transform;
            _roundRobinIndex = 0;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = Object.Instantiate<GameObject>(prefab, _parent, false);
                _pool.Add(go.GetComponent<AudioSource>());
            }
        }

        private void DisposePool()
        {
            foreach (var source in _pool)
            {
                if (source != null && source.gameObject != null)
                    Object.Destroy(source.gameObject);
            }
            _pool.Clear();

            if (_parent != null)
            {
                Object.Destroy(_parent.gameObject);
                _parent = null;
            }
        }

        private AudioSource AcquireSource()
        {
            foreach (var source in _pool)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }

            var audioSource = _pool[_roundRobinIndex];
            _roundRobinIndex = (_roundRobinIndex + 1) % _pool.Count;
            return audioSource;
        }

        public static void PlayAudio(EntityCommandBuffer ecb, AudioClipKeys clipKey)
        {
            var entity = ecb.CreateEntity();
#if UNITY_EDITOR
            ecb.SetName(entity, $"PlayAudio({clipKey})");
#endif
            ecb.AddComponent(entity, new StartAudioSource { Key = clipKey });
        }
        
        public static void PlayAudioOnce(EntityCommandBuffer ecb, AudioClipKeys clipKey)
        {
            if (_lastPlayedClip == clipKey)
                return;

            PlayAudio(ecb, clipKey);
            _lastPlayedClip = clipKey;
        }

        public static void PlayAudio(EntityManager em, AudioClipKeys clipKey)
        {
            var entity = em.CreateEntity();
#if UNITY_EDITOR
            em.SetName(entity, $"PlayAudio({clipKey})");
#endif
            em.AddComponentData(entity, new StartAudioSource { Key = clipKey });
        }

        public static void PlayAudioOnce(EntityManager em, AudioClipKeys clipKey, bool stopPrevious = false)
        {
            if (_lastPlayedClip == clipKey)
                return;

            if (stopPrevious)
                StopAudio(em);
            PlayAudio(em, clipKey);
            _lastPlayedClip = clipKey;
        }

        public static void StopAudio(EntityCommandBuffer ecb)
        {
            var entity = ecb.CreateEntity();
#if UNITY_EDITOR
            ecb.SetName(entity, "StopAudio");
#endif
            ecb.AddComponent(entity, new StopAudioSource());
        }

        public static void StopAudio(EntityManager em)
        {
            var entity = em.CreateEntity();
#if UNITY_EDITOR
            em.SetName(entity, "StopAudio");
#endif
            em.AddComponentData(entity, new StopAudioSource());
        }
    }
}
