using System;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>One translation: the text of a string in one language.</summary>
    [Serializable]
    public class GrayboxLocalizedText
    {
        /// <summary>Language code such as <c>en</c> or <c>fr</c>.</summary>
        public string language;

        public string text;

        public GrayboxLocalizedText()
        {
        }

        public GrayboxLocalizedText(string language, string text)
        {
            this.language = language;
            this.text = text;
        }
    }
}
