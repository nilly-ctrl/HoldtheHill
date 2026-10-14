using System;

namespace HoldTheHill.Core.Localization
{
    /// <summary>One translation: the text of a string in one language.</summary>
    [Serializable]
    public class LocalizedText
    {
        /// <summary>Language code such as <c>en</c> or <c>fr</c>.</summary>
        public string language;

        public string text;

        public LocalizedText()
        {
        }

        public LocalizedText(string language, string text)
        {
            this.language = language;
            this.text = text;
        }
    }
}
