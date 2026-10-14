using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Core.Audio
{
    /// <summary>One sound cue: its variants, how loud, and how often it may repeat.</summary>
    [Serializable]
    public class SfxCue
    {
        public string Id;
        public AudioClip[] Clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float Volume = 1f;
        [Tooltip("Minimum seconds between plays, so a swarm of hits doesn't turn into noise.")]
        public float Cooldown;
    }

    /// <summary>
    /// The graybox's sound cues, filled from _Game/Shared/Audio/Sfx by CombatTestSceneBuilder.
    /// Cue ids: Fire_&lt;TowerKey&gt;, Death_&lt;EnemyKey&gt;, Hit, Upgrade, Place, Sell, ShieldBreak,
    /// Heal, MineExplode, Burn, Poison, WaveStart, WaveClear, Victory, and the loops BeamLoop,
    /// OrbitLoop and HazardLoop.
    /// </summary>
    public class SfxBank : ScriptableObject
    {
        public List<SfxCue> Cues = new List<SfxCue>();

        public SfxCue Find(string id)
        {
            foreach (SfxCue cue in Cues)
            {
                if (cue.Id == id)
                {
                    return cue;
                }
            }

            return null;
        }
    }
}
