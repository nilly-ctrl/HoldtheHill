using System;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Static text lookup: <c>GrayboxLoc.Get("hud.wave")</c>. Falls back from the current language
    /// to English, then to the key itself, so a missing string is visible but never throws.
    /// </summary>
    /// <remarks>
    /// Nothing in the existing UI calls this yet. Give it a table with <see cref="SetTable"/> (the
    /// <see cref="GrayboxLocBootstrap"/> component does that in a scene) before the first lookup;
    /// with no table every call returns the key.
    /// </remarks>
    public static class GrayboxLoc
    {
        private static GrayboxStringTable _table;
        private static string _language = GrayboxStringTable.DefaultLanguage;

        /// <summary>Raised after <see cref="Language"/> changes, with the new language code.</summary>
        public static event Action<string> LanguageChanged;

        public static GrayboxStringTable Table => _table;

        /// <summary>Current language code. Setting a different value raises <see cref="LanguageChanged"/>.</summary>
        public static string Language
        {
            get => _language;
            set
            {
                string next = string.IsNullOrEmpty(value) ? GrayboxStringTable.DefaultLanguage : value;
                if (next == _language)
                {
                    return;
                }

                _language = next;
                LanguageChanged?.Invoke(_language);
            }
        }

        public static void SetTable(GrayboxStringTable table)
        {
            _table = table;
        }

        /// <summary>Text for the key in the current language, else English, else the key itself.</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_table != null && _table.TryGet(key, _language, out string text))
            {
                return text;
            }

            return key;
        }

        /// <summary>
        /// Like <see cref="Get(string)"/> with <see cref="string.Format(string, object[])"/> applied.
        /// A bad format string returns the unformatted text instead of throwing.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string text = Get(key);
            if (args == null || args.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        /// <summary>Back to no table and English, and drops listeners. For tests and Enter Play Mode without domain reload.</summary>
        public static void Reset()
        {
            _table = null;
            _language = GrayboxStringTable.DefaultLanguage;
            LanguageChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Reset();
        }
    }
}
