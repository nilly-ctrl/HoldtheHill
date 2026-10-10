using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Key -> per-language text. English (<see cref="DefaultLanguage"/>) is the fallback language.
    /// </summary>
    /// <remarks>
    /// Edit it in the Inspector or round-trip it through CSV with
    /// <c>Tools > Hold the Hill > Pipeline > Localization Tools</c>. Game code reads it through
    /// <see cref="GrayboxLoc"/>, never directly.
    /// </remarks>
    [CreateAssetMenu(fileName = "GrayboxStrings", menuName = "Hold the Hill/Graybox String Table")]
    public class GrayboxStringTable : ScriptableObject
    {
        /// <summary>The language every key should have, and the fallback for the others.</summary>
        public const string DefaultLanguage = "en";

        [Tooltip("Languages this table is meant to cover, in the order they appear in CSV exports. 'en' first.")]
        [SerializeField] private List<string> _languages = new List<string> { DefaultLanguage };

        [SerializeField] private List<GrayboxStringEntry> _entries = new List<GrayboxStringEntry>();

        private Dictionary<string, GrayboxStringEntry> _lookup;

        public IReadOnlyList<string> Languages => _languages;

        public IReadOnlyList<GrayboxStringEntry> Entries => _entries;

        public int Count => _entries.Count;

        private void OnValidate()
        {
            _lookup = null;
        }

        /// <summary>Declares a language so exports include its column. No effect when already listed.</summary>
        public void AddLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language) && !_languages.Contains(language))
            {
                _languages.Add(language);
            }
        }

        public bool HasKey(string key) => FindEntry(key) != null;

        public GrayboxStringEntry FindEntry(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (_lookup == null)
            {
                RebuildLookup();
            }

            _lookup.TryGetValue(key, out GrayboxStringEntry entry);
            return entry;
        }

        /// <summary>Text for exactly this language; false when the key or that language's text is missing.</summary>
        public bool TryGetExact(string key, string language, out string text)
        {
            text = FindEntry(key)?.GetText(language);
            return text != null;
        }

        /// <summary>
        /// Text for the language, falling back to English; false when neither has text.
        /// </summary>
        public bool TryGet(string key, string language, out string text)
        {
            if (TryGetExact(key, language, out text))
            {
                return true;
            }

            return TryGetExact(key, DefaultLanguage, out text);
        }

        /// <summary>Adds the key when missing and sets one language's text.</summary>
        public void Set(string key, string language, string text)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language))
            {
                return;
            }

            GrayboxStringEntry entry = FindEntry(key);
            if (entry == null)
            {
                entry = new GrayboxStringEntry(key);
                _entries.Add(entry);
                if (_lookup != null)
                {
                    _lookup[key] = entry;
                }
            }

            entry.SetText(language, text);
            AddLanguage(language);
        }

        public bool Remove(string key)
        {
            GrayboxStringEntry entry = FindEntry(key);
            if (entry == null)
            {
                return false;
            }

            _entries.Remove(entry);
            _lookup = null;
            return true;
        }

        /// <summary>Keys that have no English text, which is what every other language falls back to.</summary>
        public List<string> KeysMissingDefault()
        {
            var result = new List<string>();
            foreach (GrayboxStringEntry entry in _entries)
            {
                if (entry != null && entry.GetText(DefaultLanguage) == null)
                {
                    result.Add(entry.key);
                }
            }

            return result;
        }

        private void RebuildLookup()
        {
            _lookup = new Dictionary<string, GrayboxStringEntry>(_entries.Count);
            foreach (GrayboxStringEntry entry in _entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.key))
                {
                    _lookup[entry.key] = entry; // a duplicate key: the later row wins
                }
            }
        }
    }
}
