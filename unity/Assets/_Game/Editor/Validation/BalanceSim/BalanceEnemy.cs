namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>One enemy of a wave as the balance model sees it. Plain data, no Unity types.</summary>
    public sealed class BalanceEnemy
    {
        public BalanceEnemy(string name, float spawnTime, float hp, float speed, int bounty, float breachDamage)
        {
            Name = name ?? string.Empty;
            SpawnTime = spawnTime;
            Hp = hp;
            Speed = speed;
            Bounty = bounty;
            BreachDamage = breachDamage;
        }

        public string Name { get; }

        /// <summary>Seconds after the wave starts that it enters the path.</summary>
        public float SpawnTime { get; }

        /// <summary>Health plus shield.</summary>
        public float Hp { get; }

        /// <summary>World units per second.</summary>
        public float Speed { get; }

        /// <summary>Gold paid for killing it.</summary>
        public int Bounty { get; }

        /// <summary>Hill health lost if it reaches the end. The leak points of one leak.</summary>
        public float BreachDamage { get; }
    }
}
