#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A small overlay: FPS, frame time (average and worst over the last 120 frames), counts of
    /// enemies, towers, projectiles and pickups, the garbage allocated last frame, and how many
    /// enemies are still walking the path. Toggled with F4.
    /// </summary>
    /// <remarks>
    /// Frame times are sampled every frame. The object counts search the scene, so they are
    /// refreshed four times a second rather than every frame. GC allocation comes from the
    /// profiler's "GC Allocated In Frame" counter, which is only live in the Editor and in
    /// development builds; the overlay's own text drawing shows up in it.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/DevTools/Dev Perf Hud")]
    public class DevPerfHud : MonoBehaviour
    {
        private const float CountInterval = 0.25f;

        private readonly DevFrameStats _frames = new DevFrameStats(120);
        private readonly List<Tower> _towers = new List<Tower>();
        private readonly List<GrayboxPickup> _pickups = new List<GrayboxPickup>();
        private ProfilerRecorder _gcRecorder;
        private float _nextCount;
        private string _text = string.Empty;
        private long _lastGc;
        private int _enemies;
        private int _projectiles;
        private int _walkers;
        private int _towerCount;
        private int _pickupCount;

        public static DevPerfHud Instance { get; private set; }

        /// <summary>Whether the overlay is drawn. The bootstrap sets the starting value.</summary>
        public static bool Visible { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Visible = false;
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

        private void OnEnable()
        {
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void OnDisable()
        {
            if (_gcRecorder.Valid) _gcRecorder.Dispose();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f4Key.wasPressedThisFrame) Visible = !Visible;

            // Real time, so the numbers stay meaningful at 4x speed and while paused.
            _frames.Add(Time.unscaledDeltaTime);

            if (_gcRecorder.Valid) _lastGc = _gcRecorder.LastValue;

            if (Visible && Time.unscaledTime >= _nextCount)
            {
                _nextCount = Time.unscaledTime + CountInterval;
                Recount();
            }
        }

        private void Recount()
        {
            _enemies = FindObjectsByType<EnemyHealth>().Length;
            _projectiles = FindObjectsByType<Projectile>().Length;

            int walking = 0;
            foreach (EnemyMover mover in FindObjectsByType<EnemyMover>())
            {
                if (!mover.HasFinished) walking++;
            }

            _walkers = walking;

            Tower.GetActive(_towers);
            _towerCount = _towers.Count;
            GrayboxPickup.GetActive(_pickups);
            _pickupCount = _pickups.Count;

            string gc = _gcRecorder.Valid ? FormatBytes(_lastGc) : "n/a";
            _text = string.Format(
                "FPS {0:0}\nframe {1:0.0} ms avg, {2:0.0} ms worst ({3})\nenemies {4}   path agents {5}\ntowers {6}   projectiles {7}   pickups {8}\nGC alloc/frame {9}",
                _frames.Fps, _frames.AverageMs, _frames.WorstMs, _frames.Count,
                _enemies, _walkers, _towerCount, _projectiles, _pickupCount, gc);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            return (bytes / 1024f).ToString("0.0") + " KB";
        }

        private void OnGUI()
        {
            if (!Visible || string.IsNullOrEmpty(_text)) return;

            DevGui.Ensure();
            GUI.depth = -900;
            float width = DevGui.FontSize * 26f;
            float height = DevGui.Line * 5.4f;
            GUI.Box(new Rect(8f, 8f, width, height), GUIContent.none, DevGui.Panel);
            GUI.Label(new Rect(14f, 10f, width - 12f, height - 4f), _text, DevGui.Label);
        }
    }
}
#endif
