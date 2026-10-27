using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Keys for every sound effect the client audio system can play.
    public enum AudioClipKeys
    {
        TankShot,
        TankRespawn,
        TankDestroy,
        TankDamage,
        TakeBonus,
        OutOfAmmo,
        MatchStart,
        RoundFight,
        RoundWin,
        RoundFail,
        BonusSpawn,
        BonusDestroy,
        MatchResult,
        WaitingTick
    }
    
    // Buffer of audio clips (key -> clip) baked onto the settings entity by AudioSettingsDataAuthoring.
    public struct AudioClipElement : IBufferElementData
    {
        public AudioClipKeys Key;
        public UnityObjectRef<AudioClip> Clip;
    }
}