using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// A row of tabs, each showing one page. Click a tab, or use the shoulder buttons, Q/E or
    /// Page Up/Down from anywhere on the screen.
    /// </summary>
    [AddComponentMenu("Hold the Hill/UI Kit/Tab Bar")]
    public class UiTabBar : MonoBehaviour
    {
        [Serializable]
        public class Tab
        {
            public UiButton Button;
            public GameObject Page;
        }

        [SerializeField] private List<Tab> _tabs = new List<Tab>();
        [SerializeField] private int _selectedIndex;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private UnityEvent<int> _tabChanged = new UnityEvent<int>();

        private UiScreen _screen;
        private bool _hooked;

        public UnityEvent<int> TabChanged => _tabChanged;
        public int SelectedIndex => _selectedIndex;
        public int Count => _tabs.Count;

        /// <summary>The button of the tab whose page is showing, or null with no tabs.</summary>
        public Selectable SelectedButton
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _tabs.Count || _tabs[_selectedIndex].Button == null) return null;
                return _tabs[_selectedIndex].Button.Button;
            }
        }

        public void AddTab(UiButton button, GameObject page)
        {
            var tab = new Tab { Button = button, Page = page };
            _tabs.Add(tab);
            if (_hooked) Hook(tab, _tabs.Count - 1);
            Apply();
        }

        /// <summary>Shows a tab's page. Out-of-range indexes wrap, so +1/-1 stepping is safe.</summary>
        public void Select(int index)
        {
            if (_tabs.Count == 0) return;

            index = ((index % _tabs.Count) + _tabs.Count) % _tabs.Count;
            bool changed = index != _selectedIndex;
            _selectedIndex = index;
            Apply();
            MoveFocusOffHiddenPage();

            if (changed)
            {
                UiFeedback.Raise(UiCue.Change);
                _tabChanged.Invoke(_selectedIndex);
            }
        }

        // Switching tabs with the shoulder buttons can hide the page the focused control was on.
        // Focus then goes to the new tab's button rather than wherever the screen would default to.
        private void MoveFocusOffHiddenPage()
        {
            EventSystem events = EventSystem.current;
            Selectable button = SelectedButton;
            if (events == null || button == null || !isActiveAndEnabled) return;

            GameObject selected = events.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy) return;

            using (UiFeedback.Mute())
            {
                events.SetSelectedGameObject(button.gameObject);
            }
        }

        private void Awake()
        {
            _screen = GetComponentInParent<UiScreen>(true);
            for (int i = 0; i < _tabs.Count; i++) Hook(_tabs[i], i);
            _hooked = true;
            _selectedIndex = _tabs.Count == 0 ? 0 : Mathf.Clamp(_selectedIndex, 0, _tabs.Count - 1);
            Apply();
        }

        private void Update()
        {
            // Shoulder buttons only steer the tab bar of the screen that has input.
            if (_screen != null && !_screen.IsTop) return;

            int step = UiInput.TabStep();
            if (step != 0) Select(_selectedIndex + step);
        }

        private void Hook(Tab tab, int index)
        {
            if (tab.Button != null) tab.Button.Clicked.AddListener(() => Select(index));
        }

        private void Apply()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                Tab tab = _tabs[i];
                bool active = i == _selectedIndex;
                if (tab.Page != null) tab.Page.SetActive(active);
                if (tab.Button == null) continue;

                var image = tab.Button.GetComponent<Image>();
                Sprite sprite = active ? _activeSprite : _inactiveSprite;
                if (image != null && sprite != null) image.sprite = sprite;
                tab.Button.LabelColour = active ? UiKitStyle.Gold : UiKitStyle.Cream;
            }
        }
    }
}
