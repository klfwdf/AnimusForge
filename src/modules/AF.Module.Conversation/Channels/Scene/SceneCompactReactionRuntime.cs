using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimusForge;

// Only detached role messages and speech-rule options cross into model execution.
internal sealed class SceneCompactReactionInput : IDisposable
{
    internal readonly IReadOnlyList<object> Messages;
    internal readonly ConversationSpeechTextOptions SpeechOptions;
    internal readonly long Generation;
    internal readonly int SessionId;
    internal readonly string Actor;
    internal ConversationPromptCache.Request Cache;
    internal SceneCompactReactionInput(IEnumerable<object> messages, ConversationSpeechTextOptions speechOptions,
        long generation = 0, int sessionId = 0, string actor = null)
    {
        Messages = (messages ?? Enumerable.Empty<object>()).ToArray();
        SpeechOptions = speechOptions;
        Generation = generation; SessionId = sessionId; Actor = actor;
    }
    internal void Accept(string reply) => Cache?.Accept(reply);
    public void Dispose() => Cache?.Dispose();
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
        input.Cache = ConversationPromptCache.Begin(new List<object>(input.Messages), "scene_auxiliary", input.Generation, input.SessionId, input.Actor);
        if (!AIConfigHandler.TryCallAuxiliarySimpleDialogue(input.Cache.Messages, 80, 0.35f, out var text2, out var error))
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
