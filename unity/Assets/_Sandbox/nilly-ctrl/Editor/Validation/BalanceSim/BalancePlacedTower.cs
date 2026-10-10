namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>A tower the model has built: where its reach sits along the route.</summary>
    public sealed class BalancePlacedTower
    {
        public BalancePlacedTower(BalanceTower definition, float center, float halfReach)
        {
            Definition = definition;
            Center = center;
            HalfReach = halfReach;
        }

        public BalanceTower Definition { get; }

        /// <summary>The point of the route (distance from the entrance) the tower is nearest to.</summary>
        public float Center { get; }

        /// <summary>Half the length of route the tower reaches.</summary>
        public float HalfReach { get; }

        public bool Covers(float position)
        {
            return position >= Center - HalfReach && position <= Center + HalfReach;
        }
    }
}
