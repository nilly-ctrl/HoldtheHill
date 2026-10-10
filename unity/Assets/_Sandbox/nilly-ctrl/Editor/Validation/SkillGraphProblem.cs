namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>One problem found by <see cref="SkillGraphRules"/>.</summary>
    public sealed class SkillGraphProblem
    {
        public SkillGraphProblem(SkillGraphProblemKind kind, string nodeId, string detail)
        {
            Kind = kind;
            NodeId = nodeId;
            Detail = detail;
        }

        public SkillGraphProblemKind Kind { get; }

        public string NodeId { get; }

        public string Detail { get; }
    }
}
