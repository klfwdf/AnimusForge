using System;

namespace AnimusForge.SceneActions.Core
{
    // Reply markers are shared compatibility data, independent of UI and prompt language.
    public static class SceneActionReplyCompatibility
    {
        public static bool IsFailureReply(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return true;
            foreach (string prefix in SceneActionVocabulary.List("SpeechCompat.FailureZh"))
                if (response.StartsWith(prefix, StringComparison.Ordinal)) return true;
            foreach (string prefix in SceneActionVocabulary.List("SpeechCompat.FailureEn"))
                if (response.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool TryFindOrdinaryTaskBlock(string prompt, out int start, out int length)
        {
            string opening = SceneActionVocabulary.Text("SpeechCompat.OrdinaryStartZh");
            string closing = SceneActionVocabulary.Text("SpeechCompat.OrdinaryEndZh");
            StringComparison comparison = StringComparison.Ordinal;
            start = prompt.IndexOf(opening, comparison);
            length = 0;
            if (start < 0)
            {
                opening = SceneActionVocabulary.Text("SpeechCompat.OrdinaryStartEn");
                closing = SceneActionVocabulary.Text("SpeechCompat.OrdinaryEndEn");
                comparison = StringComparison.OrdinalIgnoreCase;
                start = prompt.IndexOf(opening, comparison);
            }
            if (start < 0) return false;
            int end = prompt.IndexOf(closing, start + opening.Length, comparison);
            if (end < 0) return false;
            length = end + closing.Length - start;
            return true;
        }
    }
}
