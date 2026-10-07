using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Evaluates and triggers elemental combos (Frost Shatter, Overcharged Chain, Mine Chain Reaction)
    /// upon specific combat status interactions.
    /// </summary>
    public class GrayboxStatusCombos : MonoBehaviour
    {
        public static GrayboxStatusCombos Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            EnemyHealth.Damaged += CheckCombosOnDamage;
        }

        private void OnDisable()
        {
            EnemyHealth.Damaged -= CheckCombosOnDamage;
        }

        private void CheckCombosOnDamage(EnemyHealth enemy, DamageInfo info, float amountApplied)
        {
            if (enemy == null || enemy.IsDead || amountApplied <= 0.05f)
            {
                return;
            }

            // 1. Frost Shatter Combo: Explosive or Heavy Physical hit on a Frost-Slowed Enemy
            if (enemy.SpeedMultiplier < 1.0f && (info.Type == DamageType.Physical || info.Type == DamageType.Fire) && info.Amount >= 12f)
            {
                TriggerFrostShatter(enemy, info);
            }

            // 2. Overcharged Chain Combo: Chain Lightning on a Shielded Enemy
            if (info.Type == DamageType.Lightning)
            {
                var shield = enemy.GetComponent<EnemyShield>();
                if (shield != null && shield.CurrentShield > 0f)
                {
                    TriggerOverchargeChain(enemy, shield, info);
                }
            }
        }

        private void TriggerFrostShatter(EnemyHealth enemy, DamageInfo info)
        {
            float bonusDamage = info.Amount * 1.5f;
            var shatterInfo = new DamageInfo(bonusDamage, gameObject, enemy.transform.position, DamageType.True)
            {
                IsCritical = true
            };

            enemy.TakeDamage(shatterInfo);

            DamageNumberSpawner.ShowText("FROST SHATTER!", DamageNumberKind.True,
                enemy.transform.position + new Vector3(0f, 0.9f, 0f));
        }

        private void TriggerOverchargeChain(EnemyHealth enemy, EnemyShield shield, DamageInfo info)
        {
            float drained = shield.CurrentShield;
            shield.AbsorbDamage(drained); // Drain remaining shield

            float burstDamage = drained * 0.75f + 15f;
            var burstInfo = new DamageInfo(burstDamage, gameObject, enemy.transform.position, DamageType.Lightning)
            {
                IsCritical = true
            };

            enemy.TakeDamage(burstInfo);

            DamageNumberSpawner.ShowText("OVERCHARGE!", DamageNumberKind.Lightning,
                enemy.transform.position + new Vector3(0f, 0.9f, 0f));
        }
    }
}
