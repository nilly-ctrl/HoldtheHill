using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One boss: what the announcement and its name label say, and the prefab that is spawned.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Boss Data", fileName = "BossData")]
    public class GrayboxBossData : ScriptableObject
    {
        [Tooltip("Short id used by the spawn panel and waves, e.g. TitanBeetle.")]
        [SerializeField] private string _id;

        [Tooltip("Name in menus and lists.")]
        [SerializeField] private string _displayName;

        [Tooltip("Text of the announcement banner and the label over the boss. The pixel fonts are capitals only.")]
        [SerializeField] private string _bannerText;

        [Tooltip("The boss that is spawned.")]
        [SerializeField] private GameObject _prefab;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string BannerText => _bannerText;

        public GameObject Prefab => _prefab;
    }
}
