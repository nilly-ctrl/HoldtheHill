using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Something that acts on behalf of another object: a shot or a hazard left by a tower.
    /// Damage it deals is credited to <see cref="Owner"/>, not to the loose object itself.
    /// </summary>
    public interface IOwned
    {
        /// <summary>The object to credit, or null when none was set.</summary>
        GameObject Owner { get; }
    }
}
