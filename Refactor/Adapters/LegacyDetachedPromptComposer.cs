using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Shared detached composer for SceneShout, NativeConversation and Courier.
/// It mirrors the established scene message order without calling any game
/// API or rebuilding prompt text:
/// system blocks -> prefix user blocks -> detached history -> suffix user
/// blocks -> current player input. The channel creates the blocks on the main
/// thread before handing the envelope to this class.
/// </summary>
public sealed class LegacyDetachedPromptComposer : IPromptPackageComposer
{
    private readonly int _maxTokens;
    private readonly string _model;

    public LegacyDetachedPromptComposer(int maxTokens = 4096, string model = "legacy-detached")
    {
        _maxTokens = Math.Max(16, maxTokens);
        _model = string.IsNullOrWhiteSpace(model) ? "legacy-detached" : model.Trim();
    }

    public PromptPackage Compose(
        InteractionEnvelope envelope,
        RuleSelection selection,
        CapabilitySet capabilities)
    {
        if (envelope == null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        List<PromptMessage> messages = new List<PromptMessage>();
        DetachedPromptSections sections = envelope.PromptSections ?? DetachedPromptSections.Empty;
        AddCombinedSystemMessage(messages, sections.SystemSections);
        AddUserSections(messages, sections.PrefixUserSections);

        foreach (PromptMessage historyMessage in envelope.History)
        {
            if (historyMessage != null && !string.IsNullOrWhiteSpace(historyMessage.Content))
            {
                messages.Add(new PromptMessage(NormalizeRole(historyMessage.Role), historyMessage.Content));
            }
        }

        AddUserSections(messages, sections.SuffixUserSections);
        if (sections.AppendCurrentPlayerInput && !string.IsNullOrWhiteSpace(envelope.Snapshot.PlayerText))
        {
            messages.Add(new PromptMessage("user", envelope.Snapshot.PlayerText.Trim()));
        }

        return new PromptPackage(messages, _maxTokens, _model);
    }

    private static void AddCombinedSystemMessage(List<PromptMessage> messages, IReadOnlyList<string> sections)
    {
        List<string> nonEmpty = new List<string>();
        foreach (string section in sections ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(section))
            {
                nonEmpty.Add(section.Trim());
            }
        }
        if (nonEmpty.Count > 0)
        {
            messages.Add(new PromptMessage("system", string.Join(Environment.NewLine + Environment.NewLine, nonEmpty)));
        }
    }

    private static void AddUserSections(List<PromptMessage> messages, IReadOnlyList<string> sections)
    {
        foreach (string section in sections ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(section))
            {
                messages.Add(new PromptMessage("user", section.Trim()));
            }
        }
    }

    private static string NormalizeRole(string role)
    {
        string normalized = (role ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "system" || normalized == "assistant" || normalized == "user"
            ? normalized
            : "user";
    }
}
