using System;
using UnityEngine;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// The one owner of game state: which screen is up, whether time runs and how fast. The only
    /// writer of <see cref="Time.timeScale"/>; everything else asks <see cref="IsPlaying"/> or
    /// listens to <see cref="StateChanged"/>.
    /// </summary>
    /// <remarks>
    /// Time runs only in <see cref="GameFlowState.Playing"/>, at the chosen speed, or at the dev
    /// override while one is set. Menus, pause and the run-end screen freeze it. A subclass adds
    /// what a run is (waves, checkpoints, records); this class does not know.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Game Flow/Game Flow Controller")]
    public class GameFlowController : MonoBehaviour
    {
        [Tooltip("State on Play. Playing drops straight into the game; Title goes through the menus.")]
        [SerializeField] private GameFlowState _initialState = GameFlowState.Playing;

        [Tooltip("The fast-forward speeds the player can cycle through, slowest first. The first is normal speed.")]
        [SerializeField] private float[] _speeds = { 1f, 2f, 4f };

        /// <summary>Raised after every state change with (previous, current).</summary>
        public event Action<GameFlowState, GameFlowState> StateChanged;

        /// <summary>Which screen is up.</summary>
        public GameFlowState State { get; private set; }

        /// <summary>True while the game itself is on screen and time runs.</summary>
        public bool IsPlaying => State == GameFlowState.Playing;

        /// <summary>Index into the speed list of the fast-forward chosen.</summary>
        public int SpeedIndex { get; private set; }

        /// <summary>How many speeds there are to cycle through.</summary>
        public int SpeedCount => _speeds.Length;

        /// <summary>The fast-forward multiplier chosen, whether or not time is running now.</summary>
        public float Speed => SpeedAt(SpeedIndex);

        /// <summary>
        /// A speed the dev console asked for, or 0 for none. While set it replaces the chosen
        /// speed whenever the game is playing; pause and the menus still freeze time.
        /// </summary>
        public float SpeedOverride { get; private set; }

        protected virtual void Awake()
        {
            ForceState(_initialState);
        }

        protected virtual void OnDestroy()
        {
            // Leaving Play mode paused or sped up would carry the scale into the editor.
            Time.timeScale = 1f;
        }

        public void Pause()
        {
            if (State == GameFlowState.Playing)
            {
                Enter(GameFlowState.Paused);
            }
        }

        public void Resume()
        {
            if (State == GameFlowState.Paused)
            {
                Enter(GameFlowState.Playing);
            }
        }

        /// <summary>Picks a fast-forward speed by index (clamped) and applies it if time is running.</summary>
        public void SetSpeedIndex(int index)
        {
            SpeedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, _speeds.Length - 1));
            ApplyTimeScale();
        }

        /// <summary>The next speed in the list, round to the first.</summary>
        public void CycleSpeed() => SetSpeedIndex(_speeds.Length == 0 ? 0 : (SpeedIndex + 1) % _speeds.Length);

        /// <summary>The speed at an index (clamped), or 1 if the list is empty.</summary>
        public float SpeedAt(int index)
        {
            return _speeds.Length == 0 ? 1f : _speeds[Mathf.Clamp(index, 0, _speeds.Length - 1)];
        }

        /// <summary>Sets, or with 0 clears, the dev speed override. The flow stays the only writer of <see cref="Time.timeScale"/>.</summary>
        public void SetSpeedOverride(float scale)
        {
            SpeedOverride = Mathf.Clamp(scale, 0f, 20f);
            ApplyTimeScale();
        }

        /// <summary>Moves to a state, applies the time scale and tells listeners if it changed.</summary>
        protected virtual void Enter(GameFlowState next)
        {
            GameFlowState previous = State;
            State = next;
            ApplyTimeScale();
            if (previous != next)
            {
                StateChanged?.Invoke(previous, next);
            }
        }

        /// <summary>Sets the state without telling listeners. For setup before anyone listens.</summary>
        protected void ForceState(GameFlowState state)
        {
            State = state;
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            float speed = SpeedOverride > 0f ? SpeedOverride : Speed;
            Time.timeScale = State == GameFlowState.Playing ? speed : 0f;
        }
    }
}
