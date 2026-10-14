namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>The parts of a skill node the prerequisite rules need, as plain data.</summary>
    public sealed class SkillNodeRecord
    {
        public SkillNodeRecord(string id, string prerequisiteId, bool prerequisiteMissing = false)
        {
            Id = id;
            PrerequisiteId = prerequisiteId;
            PrerequisiteMissing = prerequisiteMissing;
        }

        public string Id { get; }

        /// <summary>Id of the node that has to be unlocked first, or null for a root node.</summary>
        public string PrerequisiteId { get; }

        /// <summary>True when a prerequisite is set on the node but its asset is gone.</summary>
        public bool PrerequisiteMissing { get; }
    }
}
