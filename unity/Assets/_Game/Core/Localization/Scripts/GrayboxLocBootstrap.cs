using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Put one in a scene to hand a <see cref="GrayboxStringTable"/> to <see cref="GrayboxLoc"/>.
    /// Not placed in any scene yet.
    /// </summary>
    public class GrayboxLocBootstrap : MonoBehaviour
    {
        [SerializeField] private GrayboxStringTable _table;
        [SerializeField] private string _startLanguage = GrayboxStringTable.DefaultLanguage;

        private void Awake()
        {
            GrayboxLoc.SetTable(_table);
            GrayboxLoc.Language = _startLanguage;
        }
    }
}
