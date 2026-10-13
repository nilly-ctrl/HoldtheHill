using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// A subclass of BasicEnemy that represents a Silverfish.
    /// The Silverfish enemy can be instantly defeated when clicked by the player.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SilverfishEnemy : BasicEnemy
    {
        /// <summary>
        /// Instantly defeats the Silverfish when clicked.
        /// Requires a Collider2D on the GameObject to detect mouse clicks.
        /// </summary>
        private void OnMouseDown()
        {
            if (IsDead)
            {
                return;
            }

            // Instantly deplete all remaining health
            TakeDamage(CurrentHealth);
        }
    }
}
