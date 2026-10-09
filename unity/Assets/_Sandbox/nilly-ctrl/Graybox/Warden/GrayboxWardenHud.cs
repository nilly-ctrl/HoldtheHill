using System.Text;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Lists the Warden's abilities down the left edge: key, name, and the cooldown or hold in
    /// progress, then any power-up that is running with its time left. A plain placeholder in the
    /// same IMGUI as the rest of the graybox HUD.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Warden Hud")]
    [RequireComponent(typeof(GrayboxWarden))]
    public class GrayboxWardenHud : MonoBehaviour
    {
        private const float Width = 150f;
        private const float RowHeight = 16f;

        private readonly StringBuilder _text = new StringBuilder();
        private GrayboxWarden _warden;
        private GUIStyle _style;

        private void Awake()
        {
            _warden = GetComponent<GrayboxWarden>();
        }

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive || _warden.Abilities.Count == 0)
            {
                return;
            }

            GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true };

            int powerUps = GrayboxPowerUpEffect.Active.Count;
            float height = (_warden.Abilities.Count + powerUps) * RowHeight + 12f;
            var box = new Rect(8f, (Screen.height - height) * 0.5f, Width, height);
            GUI.Box(box, GUIContent.none);

            for (int i = 0; i < _warden.Abilities.Count; i++)
            {
                GUI.Label(new Rect(box.x + 6f, box.y + 6f + i * RowHeight, Width - 12f, RowHeight), Row(_warden.Abilities[i]), _style);
            }

            for (int i = 0; i < powerUps; i++)
            {
                GrayboxPowerUpEffect effect = GrayboxPowerUpEffect.Active[i];
                float y = box.y + 6f + (_warden.Abilities.Count + i) * RowHeight;
                string colour = ColorUtility.ToHtmlStringRGB(effect.Data.Color);
                GUI.Label(new Rect(box.x + 6f, y, Width - 12f, RowHeight),
                    $"<color=#{colour}>{effect.Data.DisplayName}  {effect.Remaining:0.0}s</color>", _style);
            }
        }

        private string Row(GrayboxWardenAbility ability)
        {
            GrayboxAbilityData data = ability.Data;
            _text.Clear();
            if (data == null)
            {
                return ability.name;
            }

            _text.Append('[').Append(GrayboxControls.Name(data.ControlId)).Append("] ").Append(data.DisplayName);
            if (!ability.IsReady)
            {
                _text.Append("  <color=#ff8080>").Append(ability.CooldownLeft.ToString("0.0")).Append("s</color>");
            }
            else if (ability.HoldProgress > 0f)
            {
                _text.Append("  <color=#80d0ff>").Append(Mathf.RoundToInt(ability.HoldProgress * 100f)).Append("%</color>");
            }
            else if (!ability.CanUse())
            {
                _text.Append("  <color=#808080>-</color>");
            }

            return _text.ToString();
        }
    }
}
