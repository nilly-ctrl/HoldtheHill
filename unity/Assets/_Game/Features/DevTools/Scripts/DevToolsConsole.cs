#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// The debug console: a drop-down text box at the top of the screen, opened with the
    /// backquote key or F1. Type <c>help</c> for the commands. Output and history are kept for
    /// the session.
    /// </summary>
    /// <remarks>
    /// Drawn with IMGUI, as the dev tools are not part of the game's uGUI HUD. While it is open
    /// the game's own keys are switched off (<see cref="DevInput"/>) so typing does not play the
    /// game. Esc, backquote or F1 close it.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/DevTools/Dev Tools Console")]
    public class DevToolsConsole : MonoBehaviour
    {
        private const string InputName = "dev-console-input";
        private const int MaxLines = 300;

        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _history = new List<string>();
        private int _historyIndex;
        private string _input = string.Empty;
        private Vector2 _scroll;
        private bool _focusInput;
        private bool _scrollToEnd;

        public static DevToolsConsole Instance { get; private set; }

        public static bool IsOpen { get; private set; }

        /// <summary>The commands this console runs.</summary>
        public DevCommandRegistry Registry { get; set; } = DevCommandRegistry.Default;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            IsOpen = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Print("<color=#ffd966>Hold the Hill dev console.</color> Type <b>help</b>. Keys: ` or F1 console, F2 tuning, F3 pick enemy, F4 perf.");
        }

        private void OnDisable()
        {
            SetOpen(false);
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds a line to the console output (rich text allowed).</summary>
        public void Print(string line)
        {
            foreach (string part in (line ?? string.Empty).Split('\n'))
            {
                _lines.Add(part);
            }

            if (_lines.Count > MaxLines) _lines.RemoveRange(0, _lines.Count - MaxLines);
            _scrollToEnd = true;
        }

        /// <summary>Runs a line as if it had been typed, and prints it and the result.</summary>
        public void Run(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            Print("<color=#8ab4f8>> " + line + "</color>");
            DevCommandResult result = Registry.Execute(line);
            if (!string.IsNullOrEmpty(result.Message))
            {
                Print(result.Ok ? result.Message : "<color=#ff7b72>" + result.Message + "</color>");
            }
        }

        private void SetOpen(bool open)
        {
            if (IsOpen == open && open == DevInput.Locked) return;

            IsOpen = open;
            DevInput.Lock(this, open);
            if (open) _focusInput = true;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.backquoteKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame)
            {
                SetOpen(!IsOpen);
            }
            else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        private void OnGUI()
        {
            if (!IsOpen) return;

            DevGui.Ensure();
            GUI.depth = -1000;
            Event current = Event.current;

            // The key that opened the console must not also be typed into it.
            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.BackQuote || current.character == '`')
                {
                    current.Use();
                    return;
                }

                if (HandleKey(current)) return;
            }

            float height = Mathf.Max(DevGui.Line * 6f, Screen.height * 0.42f);
            var rect = new Rect(0f, 0f, Screen.width, height);
            DevGui.Occupy(rect);
            GUI.Box(rect, GUIContent.none, DevGui.Panel);

            float line = DevGui.Line;
            var scrollRect = new Rect(8f, 6f, Screen.width - 16f, height - line - 16f);
            float contentHeight = 0f;
            var content = new GUIContent();
            float textWidth = scrollRect.width - 24f;
            foreach (string text in _lines)
            {
                content.text = text.Length == 0 ? " " : text;
                contentHeight += DevGui.Label.CalcHeight(content, textWidth);
            }

            var view = new Rect(0f, 0f, textWidth, Mathf.Max(contentHeight, scrollRect.height));
            if (_scrollToEnd && current.type == EventType.Layout)
            {
                _scroll.y = Mathf.Max(0f, view.height - scrollRect.height);
                _scrollToEnd = false;
            }

            _scroll = GUI.BeginScrollView(scrollRect, _scroll, view);
            float y = 0f;
            foreach (string text in _lines)
            {
                content.text = text.Length == 0 ? " " : text;
                float h = DevGui.Label.CalcHeight(content, textWidth);
                GUI.Label(new Rect(0f, y, textWidth, h), content, DevGui.Label);
                y += h;
            }

            GUI.EndScrollView();

            var inputRect = new Rect(8f, height - line - 6f, Screen.width - 16f, line);
            GUI.SetNextControlName(InputName);
            _input = GUI.TextField(inputRect, _input, DevGui.Field);
            if (_focusInput)
            {
                GUI.FocusControl(InputName);
                if (current.type == EventType.Repaint) _focusInput = false;
            }
        }

        // Returns true if the key was used and drawing should stop for this event.
        private bool HandleKey(Event e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    string line = _input;
                    _input = string.Empty;
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (_history.Count == 0 || _history[_history.Count - 1] != line) _history.Add(line);
                        Run(line);
                    }

                    _historyIndex = _history.Count;
                    _focusInput = true;
                    e.Use();
                    return true;

                case KeyCode.UpArrow:
                    if (_history.Count > 0)
                    {
                        _historyIndex = Mathf.Max(0, _historyIndex - 1);
                        _input = _history[_historyIndex];
                    }

                    e.Use();
                    return true;

                case KeyCode.DownArrow:
                    if (_history.Count > 0)
                    {
                        _historyIndex = Mathf.Min(_history.Count, _historyIndex + 1);
                        _input = _historyIndex < _history.Count ? _history[_historyIndex] : string.Empty;
                    }

                    e.Use();
                    return true;

                case KeyCode.Tab:
                    Complete();
                    e.Use();
                    return true;

                case KeyCode.Escape:
                    SetOpen(false);
                    e.Use();
                    return true;
            }

            return false;
        }

        private void Complete()
        {
            string typed = _input.TrimStart();
            if (typed.IndexOf(' ') >= 0) return;

            List<string> matches = Registry.Complete(typed);
            if (matches.Count == 0) return;

            if (matches.Count == 1)
            {
                _input = matches[0] + " ";
                return;
            }

            string common = matches[0];
            foreach (string name in matches)
            {
                int n = 0;
                while (n < common.Length && n < name.Length && char.ToLowerInvariant(common[n]) == char.ToLowerInvariant(name[n])) n++;
                common = common.Substring(0, n);
            }

            _input = common;
            Print(string.Join("   ", matches));
        }
    }
}
#endif
