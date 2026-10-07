using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// A menu screen or dialog. Open screens form a stack: only the top one takes input, it keeps
    /// one of its controls focused at all times, and back/cancel goes to it alone.
    /// </summary>
    /// <remarks>
    /// A screen is open exactly while its GameObject is active, so <see cref="Show"/> and
    /// <see cref="Hide"/> are just SetActive and a screen left active in a scene opens itself.
    /// Keeping something focused matters for keyboard and gamepad: a click on empty space
    /// deselects everything in stock uGUI, after which the arrow keys do nothing.
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Hold the Hill/UI Kit/Screen")]
    public class UiScreen : MonoBehaviour
    {
        private static readonly List<UiScreen> s_stack = new List<UiScreen>();

        // The frame a back press was last handled, so a dialog closing does not also close
        // the screen under it with the same press.
        private static int s_backFrame = -1;

        [Tooltip("Focused when the screen opens. The first control found if left empty.")]
        [SerializeField] private Selectable _firstSelected;

        [Tooltip("Back/cancel closes the screen. Turn off for a screen with nowhere to go back to.")]
        [SerializeField] private bool _closeOnBack = true;

        [SerializeField] private UnityEvent _opened = new UnityEvent();
        [SerializeField] private UnityEvent _closed = new UnityEvent();
        [SerializeField] private UnityEvent _backRequested = new UnityEvent();

        private readonly List<Selectable> _found = new List<Selectable>();
        private readonly List<List<Selectable>> _rows = new List<List<Selectable>>();
        private int _navigationSignature;

        private CanvasGroup _group;
        private GameObject _lastSelected;
        private bool _selectPending;
        private int _openedFrame = -1;

        /// <summary>The screen taking input, or null when no screen is open.</summary>
        public static UiScreen Top => s_stack.Count > 0 ? s_stack[s_stack.Count - 1] : null;

        public static int OpenCount => s_stack.Count;

        public bool IsOpen => s_stack.Contains(this);
        public bool IsTop => Top == this;

        public bool CloseOnBack
        {
            get => _closeOnBack;
            set => _closeOnBack = value;
        }

        public Selectable FirstSelected
        {
            get => _firstSelected;
            set => _firstSelected = value;
        }

        public UnityEvent Opened => _opened;
        public UnityEvent Closed => _closed;

        /// <summary>Raised when back/cancel is pressed on this screen, before it closes.</summary>
        public UnityEvent BackRequested => _backRequested;

        /// <summary>Same moment as <see cref="BackRequested"/>, for code that wants a plain delegate.</summary>
        public event Action BackPressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_stack.Clear();
            s_backFrame = -1;
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Does what pressing back does: tells listeners, then closes if set to.</summary>
        public void Back()
        {
            s_backFrame = Time.frameCount;
            UiFeedback.Raise(UiCue.Back);
            _backRequested.Invoke();
            BackPressed?.Invoke();
            if (_closeOnBack) Hide();
        }

        private void OnEnable()
        {
            _group = GetComponent<CanvasGroup>();
            s_stack.Remove(this);
            s_stack.Add(this);
            _openedFrame = Time.frameCount;
            _selectPending = true;
            _navigationSignature = 0;
            RefreshStack();
            UiFeedback.Raise(UiCue.Open);
            _opened.Invoke();
        }

        private void OnDisable()
        {
            if (!s_stack.Remove(this)) return;

            RefreshStack();
            UiScreen top = Top;
            if (top != null)
            {
                top._selectPending = true; // back to whatever it had focused
            }
            else if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            UiFeedback.Raise(UiCue.Close);
            _closed.Invoke();
        }

        private void Update()
        {
            if (!IsTop) return;

            RefreshNavigation();
            KeepFocus();

            // Not on the frame it opened: the press that opened it must not also close it.
            if (Time.frameCount != _openedFrame && Time.frameCount != s_backFrame && UiInput.BackPressed())
            {
                Back();
            }
        }

        private void KeepFocus()
        {
            EventSystem events = EventSystem.current;
            if (events == null) return;

            GameObject selected = events.currentSelectedGameObject;
            bool mine = selected != null && selected.activeInHierarchy && selected.transform.IsChildOf(transform);

            if (mine && !_selectPending)
            {
                _lastSelected = selected;
                return;
            }

            _selectPending = false;
            Selectable target = Usable(_lastSelected) ?? Usable(_firstSelected != null ? _firstSelected.gameObject : null) ?? FirstUsable();
            if (target == null) return;

            // Code is moving focus here, not the player, so no focus sound.
            using (UiFeedback.Mute())
            {
                events.SetSelectedGameObject(target.gameObject);
            }

            _lastSelected = target.gameObject;
        }

        private Selectable Usable(GameObject candidate)
        {
            if (candidate == null || !candidate.activeInHierarchy || !candidate.transform.IsChildOf(transform)) return null;

            var selectable = candidate.GetComponent<Selectable>();
            return selectable != null && selectable.IsInteractable() ? selectable : null;
        }

        private Selectable FirstUsable()
        {
            foreach (Selectable selectable in GetComponentsInChildren<Selectable>(false))
            {
                if (selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None) return selectable;
            }

            return null;
        }

        // ---------- navigation ----------

        // Links the screen's controls for keyboard and gamepad: up and down walk the rows in
        // hierarchy order, left and right move within a side-by-side row (tabs, a button row).
        //
        // Unity's automatic navigation picks the nearest control in the pressed direction, which
        // in a settings list skips rows: a slider's hit area is only the track on the right, so
        // "down" from a tab on the left jumps past it to the next full-width row.
        private void RefreshNavigation()
        {
            GetComponentsInChildren(false, _found);

            int signature = 17;
            for (int i = _found.Count - 1; i >= 0; i--)
            {
                if (!Navigable(_found[i]))
                {
                    _found.RemoveAt(i);
                }
                else
                {
                    signature = unchecked(signature * 31 + _found[i].GetHashCode());
                }
            }

            // Rebuilt only when the set of usable controls changes (a tab's page swapped in,
            // a button disabled), not every frame.
            if (signature == _navigationSignature) return;
            _navigationSignature = signature;

            _rows.Clear();
            Transform rowParent = null;
            foreach (Selectable selectable in _found)
            {
                Transform parent = selectable.transform.parent;
                bool sideBySide = parent != null && parent.GetComponent<HorizontalLayoutGroup>() != null;
                if (sideBySide && parent == rowParent)
                {
                    _rows[_rows.Count - 1].Add(selectable);
                }
                else
                {
                    _rows.Add(new List<Selectable> { selectable });
                    rowParent = sideBySide ? parent : null;
                }
            }

            for (int r = 0; r < _rows.Count; r++)
            {
                List<Selectable> row = _rows[r];
                for (int i = 0; i < row.Count; i++)
                {
                    Selectable item = row[i];
                    item.navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit,
                        // A lone control keeps left/right for itself (slider, stepper).
                        selectOnLeft = i > 0 ? row[i - 1] : null,
                        selectOnRight = i < row.Count - 1 ? row[i + 1] : null,
                        selectOnUp = r > 0 ? Pick(_rows[r - 1], item) : null,
                        selectOnDown = r < _rows.Count - 1 ? Pick(_rows[r + 1], item) : null,
                    };
                }
            }
        }

        private static bool Navigable(Selectable selectable)
        {
            return selectable.isActiveAndEnabled && selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None;
        }

        // Which control of a row to land on when arriving from another row.
        private static Selectable Pick(List<Selectable> row, Selectable from)
        {
            if (row.Count == 1) return row[0];

            // Into a tab bar: the tab whose page is showing, wherever it sits.
            var tabs = row[0].transform.parent.GetComponent<UiTabBar>();
            if (tabs != null && tabs.SelectedButton != null && row.Contains(tabs.SelectedButton)) return tabs.SelectedButton;

            Selectable best = row[0];
            float bestDistance = float.MaxValue;
            foreach (Selectable candidate in row)
            {
                float distance = Mathf.Abs(candidate.transform.position.x - from.transform.position.x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        // Only the top screen takes input. The ones under it ignore the pointer, and the keyboard
        // and gamepad cannot reach them because focus is held inside the top screen. They are
        // not marked non-interactable: that would redraw every control in its disabled state.
        private static void RefreshStack()
        {
            for (int i = 0; i < s_stack.Count; i++)
            {
                UiScreen screen = s_stack[i];
                if (screen == null || screen._group == null) continue;

                screen._group.blocksRaycasts = i == s_stack.Count - 1;
            }
        }
    }
}
