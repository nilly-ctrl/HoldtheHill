using System.Collections.Generic;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>A class declaration found in a C# source file.</summary>
    public sealed class DeclaredClass
    {
        public DeclaredClass(string name, bool isAbstract, IReadOnlyList<string> baseNames)
        {
            Name = name;
            IsAbstract = isAbstract;
            BaseNames = baseNames;
        }

        public string Name { get; }

        public bool IsAbstract { get; }

        /// <summary>Simple names of the base class and interfaces as written, namespaces removed.</summary>
        public IReadOnlyList<string> BaseNames { get; }

        /// <summary>True when the simple name appears in the base list.</summary>
        public bool HasBase(string simpleName)
        {
            for (int i = 0; i < BaseNames.Count; i++)
            {
                if (BaseNames[i] == simpleName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
