using HoldTheHill.Features.GameFlow;
using UnityEngine;

namespace HoldTheHill.Features.DamageNumbers
{
    /// <summary>
    /// Shows "BREACH! -n HP" where an enemy reaches the <see cref="Queen"/> on the same object.
    /// Lives here, not on the Queen, so the hill knows nothing about damage numbers.
    /// </summary>
    [RequireComponent(typeof(Queen))]
    [AddComponentMenu("Hold the Hill/Breach Text")]
    public class BreachText : MonoBehaviour
    {
        private Queen _queen;

        private void Awake()
        {
            _queen = GetComponent<Queen>();
        }

        private void OnEnable()
        {
            _queen.Breached += Show;
        }

        private void OnDisable()
        {
            _queen.Breached -= Show;
        }

        private static void Show(float damage, Vector3 position)
        {
            DamageNumberSpawner.ShowText($"BREACH! -{Mathf.RoundToInt(damage)} HP", DamageNumberKind.Fire, position);
        }
    }
}
