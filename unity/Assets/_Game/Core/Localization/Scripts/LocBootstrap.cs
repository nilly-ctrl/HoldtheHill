using UnityEngine;

namespace HoldTheHill.Core.Localization
{
    /// <summary>
    /// Put one in a scene to hand a <see cref="StringTable"/> to <see cref="Loc"/>.
    /// Not placed in any scene yet.
    /// </summary>
    public class LocBootstrap : MonoBehaviour
    {
        [SerializeField] private StringTable _table;
        [SerializeField] private string _startLanguage = StringTable.DefaultLanguage;

        private void Awake()
        {
            Loc.SetTable(_table);
            Loc.Language = _startLanguage;
        }
    }
}
