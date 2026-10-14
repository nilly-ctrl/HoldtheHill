#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.GameFlow;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The state behind the cheat commands that need to keep acting after the command has run:
    /// god mode (hill and Warden cannot be hurt) and a custom time speed.
    /// </summary>
    /// <remarks>
    /// God mode watches <see cref="Queen.OnHealthChanged"/> and refills the hill
    /// from inside that event. The hill raises the event before it checks for defeat, so the
    /// refill is what the defeat check sees. Nothing in the game scripts is changed.
    ///
    /// <see cref="GrayboxGameFlow"/> is the only thing that writes <see cref="Time.timeScale"/>.
    /// The speed command hands it an override (<see cref="GrayboxGameFlow.SetSpeedOverride"/>),
    /// which it applies while playing and drops while paused. Without a flow in the scene the
    /// command has nothing to talk to and does nothing.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/DevTools/Dev Tools Cheats")]
    public class DevToolsCheats : MonoBehaviour
    {
        private static float s_speed;
        private GrayboxWardenHealth _wardenHealth;

        public static DevToolsCheats Instance { get; private set; }

        /// <summary>True while hill and Warden are protected.</summary>
        public static bool God { get; private set; }

        /// <summary>The speed set by the speed command, or 0 when none is set.</summary>
        public static float SpeedOverride => s_speed;

        public static void SetGod(bool on) => God = on;

        /// <summary>Sets the time scale while playing. 0 clears the override and the game's own speed returns.</summary>
        public static void SetSpeed(float scale)
        {
            s_speed = Mathf.Clamp(scale, 0f, 20f);
            GrayboxGameFlow flow = FindAnyObjectByType<GrayboxGameFlow>();
            if (flow != null) flow.SetSpeedOverride(s_speed);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_speed = 0f;
            God = false;
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        // The hill, found once; god mode listens to its instance event (no static on the shim).
        private Queen _hill;
        private bool _listening;

        private void OnEnable() => Listen();

        private void Start() => Listen();

        private void Listen()
        {
            if (_listening) return;
            if (_hill == null) _hill = FindAnyObjectByType<Queen>();
            if (_hill == null) return;
            _hill.OnHealthChanged += OnHillHealthChanged;
            _listening = true;
        }

        private void OnDisable()
        {
            if (_listening) _hill.OnHealthChanged -= OnHillHealthChanged;
            _listening = false;
            if (Instance == this) Instance = null;
        }

        private void OnHillHealthChanged(float current, float max)
        {
            if (!God) return;

            Queen hill = _hill;
            if (hill != null && !hill.IsFinished && hill.CurrentHealth < hill.MaxHealth)
            {
                hill.Heal(hill.MaxHealth);
            }
        }

        private void LateUpdate()
        {
            if (!God) return;

            if (_wardenHealth == null && GrayboxWarden.Instance != null) _wardenHealth = GrayboxWarden.Instance.Health;
            if (_wardenHealth != null && !_wardenHealth.IsDowned && _wardenHealth.CurrentHealth < _wardenHealth.MaxHealth)
            {
                _wardenHealth.Heal(_wardenHealth.MaxHealth);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (s_speed > 0f)
            {
                s_speed = 0f;
                GrayboxGameFlow flow = FindAnyObjectByType<GrayboxGameFlow>();
                if (flow != null) flow.SetSpeedOverride(0f);
            }
        }
    }
}
#endif
