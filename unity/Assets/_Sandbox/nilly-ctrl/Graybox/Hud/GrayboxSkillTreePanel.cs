using System.Collections.Generic;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The skill tree window over the field: one column per branch, a button per skill, the
    /// points left and the multipliers in force. Opened and closed with the skill tree key
    /// (<see cref="GrayboxRunHotkeys"/>) or its Close button. Replaces the IMGUI window.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Skill Tree Panel")]
    public class GrayboxSkillTreePanel : GrayboxHudPanel
    {
        [SerializeField] private TMP_Text _points;
        [SerializeField] private TMP_Text _footer;
        [SerializeField] private UiButton _close;

        [Tooltip("Made once per skill in the catalog.")]
        [SerializeField] private GrayboxSkillNodeRow _rowPrefab;

        [Tooltip("Holds one column per branch, laid out side by side.")]
        [SerializeField] private RectTransform _columnsRoot;

        [Tooltip("An inactive line copied to head each column.")]
        [SerializeField] private TMP_Text _branchTitleTemplate;

        private readonly List<GrayboxSkillNodeRow> _rows = new List<GrayboxSkillNodeRow>();
        private readonly List<GameObject> _columns = new List<GameObject>();
        private GrayboxSkillTree _builtFor;
        private int _pointsShown = -1;
        private float _damageShown = -1f;
        private float _rateShown = -1f;
        private float _bountyShown = -1f;
        private float _costShown = -1f;

        /// <summary>True while the window is up. Toggled by the skill tree key.</summary>
        public static bool Open { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Open = false;

        public override bool WantsToShow => Open && GrayboxSkillTree.Instance != null;

        public IReadOnlyList<GrayboxSkillNodeRow> Rows => _rows;

        private void Awake()
        {
            OnClick(_close.Button, () => Open = false);
        }

        public override void Refresh()
        {
            GrayboxSkillTree tree = GrayboxSkillTree.Instance;
            if (tree != _builtFor || _rows.Count != tree.GetAllNodes().Count)
            {
                Rebuild(tree);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                GrayboxSkillNodeRow row = _rows[i];
                row.Show(row.Node.isUnlocked, tree.CanUnlock(row.Node.id));
            }

            if (tree.SkillPoints != _pointsShown)
            {
                _pointsShown = tree.SkillPoints;
                _points.text = $"Skill points  {tree.SkillPoints} SP";
            }

            if (tree.DamageMultiplier != _damageShown || tree.FireRateMultiplier != _rateShown
                || tree.BountyMultiplier != _bountyShown || tree.CostMultiplier != _costShown)
            {
                _damageShown = tree.DamageMultiplier;
                _rateShown = tree.FireRateMultiplier;
                _bountyShown = tree.BountyMultiplier;
                _costShown = tree.CostMultiplier;
                _footer.text = $"Damage x{_damageShown:0.00}   Fire rate x{_rateShown:0.00}   Food x{_bountyShown:0.00}   Cost x{_costShown:0.00}";
            }
        }

        private void Rebuild(GrayboxSkillTree tree)
        {
            foreach (GameObject column in _columns)
            {
                Destroy(column);
            }

            _columns.Clear();
            _rows.Clear();
            _builtFor = tree;

            foreach (string branch in tree.GetBranches())
            {
                var column = new GameObject(branch, typeof(RectTransform)) { layer = gameObject.layer };
                column.transform.SetParent(_columnsRoot, false);
                var layout = column.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                TMP_Text title = Instantiate(_branchTitleTemplate, column.transform);
                title.gameObject.SetActive(true);
                title.text = branch.ToUpperInvariant();

                foreach (SkillNode node in tree.GetBranchNodes(branch))
                {
                    GrayboxSkillNodeRow row = Instantiate(_rowPrefab, column.transform);
                    string id = node.id;
                    row.Bind(node, () =>
                    {
                        tree.TryUnlock(id);
                        ReleaseFocus();
                    });
                    _rows.Add(row);
                }

                _columns.Add(column);
            }
        }
    }
}
