using System;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Marks an enemy as a boss and says which one. Sits on the boss base prefab, so every
    /// boss variant has it; <see cref="GrayboxBossBanner"/> listens for <see cref="Appeared"/>.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Boss")]
    public class GrayboxBoss : MonoBehaviour
    {
        [SerializeField] private GrayboxBossData _data;

        /// <summary>Raised once for each boss, on its first frame in the scene.</summary>
        public static event Action<GrayboxBoss> Appeared;

        public GrayboxBossData Data => _data;

        private void Start()
        {
            Appeared?.Invoke(this);
        }
    }
}
