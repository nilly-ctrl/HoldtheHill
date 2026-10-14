using System;
using System.Collections.Generic;

namespace HoldTheHill.Core.Localization
{
    /// <summary>One key of a <see cref="GrayboxStringTable"/> with its text in each language.</summary>
    [Serializable]
    public class GrayboxStringEntry
    {
        public string key;

        public List<GrayboxLocalizedText> texts = new List<GrayboxLocalizedText>();

        public GrayboxStringEntry()
        {
        }

        public GrayboxStringEntry(string key)
        {
            this.key = key;
        }

        /// <summary>The text for a language, or null when this entry has none (or an empty one).</summary>
        public string GetText(string language)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                GrayboxLocalizedText t = texts[i];
                if (t != null && t.language == language)
                {
                    return string.IsNullOrEmpty(t.text) ? null : t.text;
                }
            }

            return null;
        }

        /// <summary>Sets the text for a language, adding the language slot when needed.</summary>
        public void SetText(string language, string text)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                if (texts[i] != null && texts[i].language == language)
                {
                    texts[i].text = text;
                    return;
                }
            }

            texts.Add(new GrayboxLocalizedText(language, text));
        }
    }
}
