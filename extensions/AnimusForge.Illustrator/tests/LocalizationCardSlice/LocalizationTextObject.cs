using System.Collections.Generic;

namespace TaleWorlds.Localization
{
    // Fixture only: product text uses the real Bannerlord loader, verified separately.
    internal sealed class TextObject
    {
        private readonly string _text;
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        public TextObject(string text) { _text = text.Substring(text.IndexOf('}') + 1); }
        public void SetTextVariable(string name, string value) { _variables[name] = value; }
        public override string ToString()
        {
            string text = _text;
            foreach (var pair in _variables) text = text.Replace("{" + pair.Key + "}", pair.Value);
            return text;
        }
    }
}
