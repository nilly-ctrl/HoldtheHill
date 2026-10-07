using System;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Game-feel moments the graybox animators already detect, re-announced so other feedback
    /// (sound, for now) can react to the same moments without detecting them a second time.
    /// Keys are the .aseprite names, e.g. "TowerLinear" or "EnemyBrute".
    /// </summary>
    public static class GrayboxFeedback
    {
        public static event Action<string, Vector3> TowerAttacked;
        public static event Action<string, Vector3, int> TowerUpgraded;
        public static event Action<string, Vector3> EnemyHurt;
        public static event Action<string, Vector3> EnemyDied;
        public static event Action<Vector3> ShieldBroke;
        public static event Action<Vector3> HealPulsed;
        public static event Action<Vector3> MineExploded;

        internal static void RaiseTowerAttacked(string key, Vector3 at) => TowerAttacked?.Invoke(key, at);
        internal static void RaiseTowerUpgraded(string key, Vector3 at, int level) => TowerUpgraded?.Invoke(key, at, level);
        internal static void RaiseEnemyHurt(string key, Vector3 at) => EnemyHurt?.Invoke(key, at);
        internal static void RaiseEnemyDied(string key, Vector3 at) => EnemyDied?.Invoke(key, at);
        internal static void RaiseShieldBroke(Vector3 at) => ShieldBroke?.Invoke(at);
        internal static void RaiseHealPulsed(Vector3 at) => HealPulsed?.Invoke(at);
        internal static void RaiseMineExploded(Vector3 at) => MineExploded?.Invoke(at);
    }
}
