using System.Collections.Generic;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>One wave: its enemies with their spawn times.</summary>
    public sealed class BalanceWave
    {
        public BalanceWave(string name, IEnumerable<BalanceEnemy> enemies)
        {
            Name = name ?? string.Empty;
            Enemies = new List<BalanceEnemy>(enemies ?? new BalanceEnemy[0]);
        }

        public string Name { get; }

        public List<BalanceEnemy> Enemies { get; }

        public float TotalHp
        {
            get
            {
                float total = 0f;
                foreach (BalanceEnemy enemy in Enemies)
                {
                    total += enemy.Hp;
                }

                return total;
            }
        }

        public int TotalBounty
        {
            get
            {
                int total = 0;
                foreach (BalanceEnemy enemy in Enemies)
                {
                    total += enemy.Bounty;
                }

                return total;
            }
        }
    }
}
