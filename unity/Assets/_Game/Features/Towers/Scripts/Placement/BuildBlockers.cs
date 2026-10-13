using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// The <see cref="IBuildBlocker"/>s in the scene right now. Blockers register in OnEnable and
    /// unregister in OnDisable; the placer asks <see cref="Blocked"/> before it builds.
    /// </summary>
    public static class BuildBlockers
    {
        private static readonly List<IBuildBlocker> s_blockers = new List<IBuildBlocker>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_blockers.Clear();

        /// <summary>Every registered blocker, for rules that want to pass them along.</summary>
        public static IReadOnlyList<IBuildBlocker> All => s_blockers;

        public static void Register(IBuildBlocker blocker)
        {
            if (blocker != null && !s_blockers.Contains(blocker))
            {
                s_blockers.Add(blocker);
            }
        }

        public static void Unregister(IBuildBlocker blocker)
        {
            s_blockers.Remove(blocker);
        }

        /// <summary>True if any registered blocker forbids the cell at <paramref name="cellCentre"/>.</summary>
        public static bool Blocked(Vector3 cellCentre)
        {
            for (int i = 0; i < s_blockers.Count; i++)
            {
                if (s_blockers[i].Blocks(cellCentre))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
