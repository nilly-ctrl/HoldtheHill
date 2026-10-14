using System;
using System.Collections.Generic;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>Finds string ids that more than one asset uses. Pure C#.</summary>
    public static class DuplicateIdRules
    {
        /// <summary>
        /// Given (id, owner) pairs, returns each id used by two or more owners with the list of owners.
        /// Empty ids are ignored. Ids compare exactly: saves store them as written.
        /// </summary>
        public static Dictionary<string, List<string>> Find(IEnumerable<KeyValuePair<string, string>> idsAndOwners)
        {
            var byId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (idsAndOwners == null)
            {
                return byId;
            }

            foreach (KeyValuePair<string, string> pair in idsAndOwners)
            {
                if (string.IsNullOrEmpty(pair.Key))
                {
                    continue;
                }

                if (!byId.TryGetValue(pair.Key, out List<string> owners))
                {
                    owners = new List<string>();
                    byId[pair.Key] = owners;
                }

                owners.Add(pair.Value);
            }

            var duplicates = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<string>> entry in byId)
            {
                if (entry.Value.Count > 1)
                {
                    duplicates[entry.Key] = entry.Value;
                }
            }

            return duplicates;
        }
    }
}
