using System;
using UnityEngine;

namespace HoldTheHill.Core.Localization
{
    /// <summary>
    /// Static text lookup: <c>Loc.Get("hud.wave")</c>. Falls back from the current language
    /// to English, then to the key itself, so a missing string is visible but never throws.
    /// </summary>
    /// <remarks>
    /// The menu screens (<c>Graybox/Screens</c>) call this with the English text as the key, so they
    /// read the same with or without a table. Text baked into prefabs by the builders does not go
    /// through it yet. Give it a table with <see cref="SetTable"/> (the
    /// <see cref="LocBootstrap"/> component does that in a scene) before the first lookup;
    /// with no table every call returns the key.
    /// </remarks>
    public static class Loc
    {
        private static StringTable _table;
        private static string _language = StringTable.DefaultLanguage;

        /// <summary>Raised after <see cref="Language"/> changes, with the new language code.</summary>
        public static event Action<string> LanguageChanged;

        public static StringTable Table => _table;

        /// <summary>Current language code. Setting a different value raises <see cref="LanguageChanged"/>.</summary>
        public static string Language
        {
            get => _language;
            set
            {
                string next = string.IsNullOrEmpty(value) ? StringTable.DefaultLanguage : value;
                if (next == _language)
                {
                    return;
                }

                _language = next;
                LanguageChanged?.Invoke(_language);
            }
        }

        public static void SetTable(StringTable table)
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
            _language = StringTable.DefaultLanguage;
            LanguageChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Reset();
        }
    }
}
