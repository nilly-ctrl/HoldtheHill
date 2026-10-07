using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Fills <see cref="GrayboxSfxBank"/> from _Sandbox/nilly-ctrl/Audio/Sfx. Each cue names a
    /// sound family; every numbered variant of it (Name01, Name02...) goes in, in order.
    /// Change a pick here and rebuild the graybox to hear a different family.
    /// </summary>
    internal static class GrayboxSfxBankBuilder
    {
        private const string SfxRoot = "Assets/_Sandbox/nilly-ctrl/Audio/Sfx";
        private const string BankPath = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxSfxBank.asset";

        // id, family path under Sfx/, volume, cooldown (s)
        private static readonly (string Id, string Family, float Volume, float Cooldown)[] Cues =
        {
            ("Fire_TowerLinear", "TowerFire/FireAcidSpit", 0.55f, 0.05f),
            ("Fire_TowerHoming", "TowerFire/FireStingerDart", 0.55f, 0.05f),
            ("Fire_TowerMortar", "TowerFire/FireMortar", 0.7f, 0.05f),
            ("Fire_TowerRicochet", "TowerFire/FirePebbleSling", 0.55f, 0.05f),
            ("Fire_TowerChain", "ChainLightning", 0.6f, 0.05f),
            ("Fire_TowerFrostAura", "TowerFire/FireIceShard", 0.6f, 0.05f),
            ("Fire_TowerKnockback", "TowerFire/FireSonicPulse", 0.7f, 0.05f),
            ("Fire_TowerMineLayer", "Place/PlaceDirtThump", 0.5f, 0.05f),
            ("Upgrade", "Upgrade/UpgradeArpeggioUp", 0.9f, 0f),
            ("Place", "Place/PlaceAnthillRise", 0.9f, 0f),
            ("Sell", "TowerSell", 0.9f, 0f),
            ("Hit", "EnemyHit", 0.35f, 0.06f),
            ("Death_EnemyRunner", "Death/DeathShellCrunch", 0.7f, 0.04f),
            ("Death_EnemyGrunt", "Death/DeathBeetleCrack", 0.7f, 0.04f),
            ("Death_EnemyBrute", "Death/DeathBruteCollapse", 0.9f, 0.04f),
            ("Death_EnemyShielded", "Death/DeathPowerDown", 0.7f, 0.04f),
            ("Death_EnemySplitter", "Death/DeathCentipedeRattle", 0.8f, 0.04f),
            ("Death_EnemySwarm", "Death/DeathSquish", 0.5f, 0.06f),
            ("Death_EnemyHealer", "Death/DeathChitterScreech", 0.7f, 0.04f),
            ("ShieldBreak", "Death/DeathShatter", 0.8f, 0.05f),
            ("Heal", "Upgrade/UpgradeShimmerSparkle", 0.35f, 0.3f),
            ("MineExplode", "Death/DeathSmallExplode", 0.9f, 0.05f),
            ("Burn", "StatusBurn", 0.35f, 0.3f),
            ("Poison", "StatusPoison", 0.35f, 0.4f),
            ("WaveStart", "Wave/WaveWarDrums", 1f, 0f),
            ("WaveClear", "WaveClear/WaveClearColonyCelebrate", 1f, 0f),
            ("Victory", "Victory", 1f, 0f),
            ("BeamLoop", "BeamLoop", 0.35f, 0f),
            ("OrbitLoop", "OrbitFieldLoop", 0.2f, 0f),
            ("HazardLoop", "GroundHazardLoop", 0.3f, 0f),
        };

        [MenuItem("Tools/Hold the Hill/Rebuild Graybox Sfx Bank")]
        public static GrayboxSfxBank Build()
        {
            var bank = AssetDatabase.LoadAssetAtPath<GrayboxSfxBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<GrayboxSfxBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            bank.Cues = new List<GrayboxSfxCue>();
            foreach ((string id, string family, float volume, float cooldown) in Cues)
            {
                AudioClip[] clips = Variants(family);
                if (clips.Length == 0)
                {
                    Debug.LogWarning($"[Graybox] No sound files for cue {id} ({SfxRoot}/{family}*.wav).");
                }

                bank.Cues.Add(new GrayboxSfxCue { Id = id, Clips = clips, Volume = volume, Cooldown = cooldown });
            }

            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Graybox] Sfx bank: {bank.Cues.Count} cues, {bank.Cues.Sum(c => c.Clips.Length)} clips -> {BankPath}");
            return AssetDatabase.LoadAssetAtPath<GrayboxSfxBank>(BankPath);
        }

        // "TowerFire/FireAcidSpit" -> TowerFire/FireAcidSpit01.wav ... 05; "TowerSell" -> TowerSell.wav.
        private static AudioClip[] Variants(string family)
        {
            string folder = SfxRoot + "/" + Path.GetDirectoryName(family)?.Replace('\\', '/');
            folder = folder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return System.Array.Empty<AudioClip>();
            }

            string stem = Path.GetFileName(family);
            var pattern = new Regex("^" + Regex.Escape(stem) + @"(\d\d)?\.wav$");

            return AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == folder && pattern.IsMatch(Path.GetFileName(p)))
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .ToArray();
        }
    }
}
