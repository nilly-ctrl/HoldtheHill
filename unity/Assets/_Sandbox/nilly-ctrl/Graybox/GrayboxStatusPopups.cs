using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A small word with its icon when a status lands on an enemy: BURN, POISON, SHOCK, FROZEN,
    /// SLOW, and SHIELD when one breaks. It uses the damage-number popups, so the words come out
    /// in the matching colour style and follow the font theme. An effect that is refreshed on the
    /// same enemy does not repeat its word until the quiet time has passed.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Status Popups")]
    public class GrayboxStatusPopups : MonoBehaviour
    {
        [Tooltip("Seconds before the same status on the same enemy shows its word again.")]
        [SerializeField, Min(0f)] private float _quietSeconds = 2.5f;
        [Tooltip("World offset from the enemy's pivot to where the word appears.")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 0.8f);

        // When each (enemy, word) pair may show again.
        private readonly Dictionary<(EnemyHealth, string), float> _nextAllowed =
            new Dictionary<(EnemyHealth, string), float>();

        private void OnEnable()
        {
            EnemyHealth.StatusApplied += OnStatusApplied;
            GrayboxFeedback.ShieldBroke += OnShieldBroke;
        }

        private void OnDisable()
        {
            EnemyHealth.StatusApplied -= OnStatusApplied;
            GrayboxFeedback.ShieldBroke -= OnShieldBroke;
            _nextAllowed.Clear();
        }

        private void OnStatusApplied(EnemyHealth enemy, StatusEffectData status)
        {
            if (enemy == null || !Describe(status, out string word, out DamageNumberKind kind))
            {
                return;
            }

            var key = (enemy, word);
            if (_nextAllowed.TryGetValue(key, out float allowed) && Time.time < allowed)
            {
                return;
            }

            // Old entries are for enemies long gone; drop them now and then.
            if (_nextAllowed.Count > 256)
            {
                _nextAllowed.Clear();
            }

            _nextAllowed[key] = Time.time + _quietSeconds;
            DamageNumberSpawner.ShowText(word, kind, enemy.transform.position + (Vector3)_offset);
        }

        private void OnShieldBroke(Vector3 at)
        {
            DamageNumberSpawner.ShowText($"{PixelGlyphs.IconShield} SHIELD", DamageNumberKind.True, at + (Vector3)_offset);
        }

        // The effect's name says what it is; a nameless one is judged by what it does.
        private static bool Describe(StatusEffectData status, out string word, out DamageNumberKind kind)
        {
            string name = (status.EffectName ?? string.Empty).ToLowerInvariant();
            if (name.Contains("burn") || name.Contains("fire") || name.Contains("scald"))
            {
                word = $"{PixelGlyphs.IconFire} BURN";
                kind = DamageNumberKind.Fire;
            }
            else if (name.Contains("poison") || name.Contains("acid") || name.Contains("venom"))
            {
                word = $"{PixelGlyphs.IconPoison} POISON";
                kind = DamageNumberKind.Poison;
            }
            else if (name.Contains("shock") || name.Contains("stun") || name.Contains("lightning"))
            {
                word = $"{PixelGlyphs.IconBolt} SHOCK";
                kind = DamageNumberKind.Lightning;
            }
            else if (name.Contains("frost") || name.Contains("chill") || name.Contains("freeze") || name.Contains("ice"))
            {
                word = $"{PixelGlyphs.IconFrost} FROZEN";
                kind = DamageNumberKind.True;
            }
            else if (status.SlowMultiplier < 1f)
            {
                word = "SLOW";
                kind = DamageNumberKind.True;
            }
            else if (status.DamagePerTick > 0f)
            {
                word = $"{PixelGlyphs.IconPoison} POISON";
                kind = DamageNumberKind.Poison;
            }
            else
            {
                word = null;
                kind = DamageNumberKind.Physical;
                return false;
            }

            return true;
        }
    }
}
