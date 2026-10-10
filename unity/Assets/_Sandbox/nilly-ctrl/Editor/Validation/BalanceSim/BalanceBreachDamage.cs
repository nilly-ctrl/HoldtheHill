namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// The hill health an enemy costs when it leaks, chosen by name. This mirrors the table in
    /// GrayboxBaseHealth.OnAntBreachedBase; specials carry their own number and override it.
    /// </summary>
    public static class BalanceBreachDamage
    {
        public static float ForName(string enemyName)
        {
            string name = enemyName ?? string.Empty;

            if (name.Contains("Brute")) return 25f;
            if (name.Contains("Shielded")) return 12f;
            if (name.Contains("Healer")) return 15f;
            if (name.Contains("Grunt")) return 10f;
            if (name.Contains("Splitter")) return 8f;
            if (name.Contains("Runner")) return 5f;
            if (name.Contains("Swarm")) return 2f;

            return 5f;
        }
    }
}
