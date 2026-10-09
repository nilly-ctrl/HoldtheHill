using System.Collections.Generic;
using HoldTheHill.Sandbox.NillyCtrl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Interactive IMGUI Skill Tree window toggleable via hotkey 'K' or HUD button.
    /// Displays 3 branches of upgrades with unlock prerequisites, SP balance, and active multipliers.
    /// </summary>
    public class GrayboxSkillTreeUI : MonoBehaviour
    {
        private bool _showWindow = false;
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _btnStyle;

        private void Update()
        {
            if (GrayboxGameFlow.GameplayActive && GrayboxControls.Pressed(GrayboxControls.SkillTree))
            {
                _showWindow = !_showWindow;
            }
        }

        public void ToggleWindow()
        {
            _showWindow = !_showWindow;
        }

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            InitStyles();

            if (!_showWindow || GrayboxSkillTree.Instance == null)
            {
                return;
            }

            float width = 700f;
            float height = 420f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect windowRect = new Rect(x, y, width, height);
            GUI.Box(windowRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 12f, y + 10f, width - 24f, height - 20f));

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b><color=#ffd700>COMMANDER SKILL TREE</color></b>", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>Skill Points:</b> <color=#80ff80>{GrayboxSkillTree.Instance.SkillPoints} SP</color>", _headerStyle);
            if (GUILayout.Button("X", _btnStyle, GUILayout.Width(24), GUILayout.Height(22)))
            {
                _showWindow = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // One column per branch, in catalog order.
            GUILayout.BeginHorizontal();

            List<string> branches = GrayboxSkillTree.Instance.GetBranches();
            for (int i = 0; i < branches.Count; i++)
            {
                if (i > 0)
                {
                    GUILayout.Space(10);
                }

                DrawBranchColumn(branches[i], GrayboxSkillTree.Instance.GetBranchNodes(branches[i]));
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            // Footer Active Stats Readout
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Active Multipliers:</b> Dmg x{GrayboxSkillTree.Instance.DamageMultiplier:0.00} | Spd x{GrayboxSkillTree.Instance.FireRateMultiplier:0.00} | Gold x{GrayboxSkillTree.Instance.BountyMultiplier:0.00} | Cost x{GrayboxSkillTree.Instance.CostMultiplier:0.00}", _cardStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawBranchColumn(string title, List<SkillNode> nodes)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(216));
            GUILayout.Label($"<b><color=#80d0ff>{title.ToUpperInvariant()}</color></b>", _headerStyle);
            GUILayout.Space(4);

            foreach (SkillNode node in nodes)
            {
                string id = node.id;
                bool isUnlocked = node.isUnlocked;
                bool canUnlock = GrayboxSkillTree.Instance.CanUnlock(id);

                if (isUnlocked)
                {
                    GUI.color = new Color(0.4f, 1f, 0.4f);
                }
                else if (canUnlock)
                {
                    GUI.color = new Color(1f, 0.9f, 0.4f);
                }
                else
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                }

                Texture2D icon = GrayboxIcons.Get(node.Data.IconName);
                Color buttonTint = GUI.color;

                GUILayout.BeginHorizontal();
                if (icon != null)
                {
                    Rect slot = GUILayoutUtility.GetRect(GrayboxIcons.Size, GrayboxIcons.Size,
                        GUILayout.Width(GrayboxIcons.Size), GUILayout.Height(GrayboxIcons.Size));

                    // Full colour once owned or affordable; dimmed while locked, with a padlock on top.
                    GUI.color = isUnlocked || canUnlock ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.85f);
                    GUI.DrawTexture(slot, icon);
                    GUI.color = Color.white;
                    if (!isUnlocked && !canUnlock)
                    {
                        Texture2D padlock = GrayboxIcons.Get("UiLockIcon");
                        if (padlock != null)
                        {
                            GUI.DrawTexture(new Rect(slot.x + 8f, slot.y + 8f, 16f, 16f), padlock);
                        }
                    }

                    GUILayout.Space(4);
                    GUI.color = buttonTint;
                }

                string btnText = isUnlocked ? $"{node.name} - OWNED" : $"{node.name} ({node.cost} SP)";
                if (GUILayout.Button(btnText, _btnStyle, GUILayout.Height(32)))
                {
                    if (canUnlock)
                    {
                        GrayboxSkillTree.Instance.TryUnlock(id);
                    }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Label($"<size=10>{node.description}</size>", _cardStyle);
                GUILayout.Space(6);
            }

            GUILayout.EndVertical();
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            _cardStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true,
                wordWrap = true
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
