namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>What is wrong with one skill node's prerequisite.</summary>
    public enum SkillGraphProblemKind
    {
        /// <summary>The prerequisite is set but its asset was deleted.</summary>
        PrerequisiteMissing,

        /// <summary>The prerequisite exists but is not part of the same tree.</summary>
        PrerequisiteNotInTree,

        /// <summary>The node lists itself as its own prerequisite.</summary>
        SelfPrerequisite,

        /// <summary>Following prerequisites from this node comes back to it, so it can never unlock.</summary>
        Cycle
    }
}
