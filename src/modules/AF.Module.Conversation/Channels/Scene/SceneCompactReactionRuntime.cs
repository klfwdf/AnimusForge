using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimusForge;

// Only detached role messages and speech-rule options cross into model execution.
internal sealed class SceneCompactReactionInput
{
    internal readonly IReadOnlyList<object> Messages;
    internal readonly ConversationSpeechTextOptions SpeechOptions;
    internal SceneCompactReactionInput(IEnumerable<object> messages, ConversationSpeechTextOptions speechOptions)
    {
        Messages = (messages ?? Enumerable.Empty<object>()).ToArray();
        SpeechOptions = speechOptions;
    }
}

internal static class SceneCompactReactionRuntime
{
    internal static Task<string> GenerateAsync(SceneCompactReactionInput input)
    {
        if (input == null || input.Messages.Count == 0) return Task.FromResult("");
        return Task.Run(() => Generate(input));
    }

    private static string Generate(SceneCompactReactionInput input)
    {
        if (!AIConfigHandler.TryCallAuxiliarySimpleDialogue(new List<object>(input.Messages), 80, 0.35f, out var text2, out var error))
        {
            Logger.Log("ShoutBehavior", "[CompactSceneReaction] auxiliary_simple_dialogue failed: " + error);
            return "";
        }
        if (string.IsNullOrWhiteSpace(text2)) return "";
        string text3 = (text2 ?? "").Replace("\r", "").Trim();
        text3 = Regex.Replace(text3, "\\[(?:ACTION:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
        text3 = ShoutUtils.StripNamePrefixedLineSafely(text3, 30);
        text3 = ConversationSpeechTextRules.StripLeakedPromptContentForShout(text3);
        text3 = ConversationSpeechTextRules.StripStageDirectionsForPassiveShout(text3, input.SpeechOptions);
        return text3.Trim();
    }
}
