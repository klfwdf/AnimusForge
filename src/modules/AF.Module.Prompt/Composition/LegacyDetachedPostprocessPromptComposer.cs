using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Shared detached composer for action postprocess. Sections are supplied by
/// the channel owner from the existing ActionPostprocess prompt builder, so
/// all rule/tag/runtime semantics remain authoritative in legacy code.
/// </summary>
public sealed class LegacyDetachedPostprocessPromptComposer : IPostprocessPromptComposer
{
    private readonly int _maxTokens;
    private readonly string _model;

    public LegacyDetachedPostprocessPromptComposer(int maxTokens = 4096, string model = "legacy-detached-postprocess")
    {
        _maxTokens = Math.Max(16, maxTokens);
        _model = string.IsNullOrWhiteSpace(model) ? "legacy-detached-postprocess" : model.Trim();
    }

    public PromptPackage Compose(
        InteractionEnvelope envelope,
        RuleSelection selection,
        string visibleReply,
        string rawReply,
        PostprocessContext context)
    {
        if (envelope == null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        DetachedPostprocessPromptSections sections = envelope.PostprocessPromptSections
            ?? DetachedPostprocessPromptSections.Empty;
        List<PromptMessage> messages = new List<PromptMessage>();
        AddSystemSections(messages, sections.SystemSections);
        AddUserSections(messages, sections.PrefixUserSections);
        foreach (PromptMessage historyMessage in envelope.History)
        {
            if (historyMessage != null && !string.IsNullOrWhiteSpace(historyMessage.Content))
            {
                messages.Add(new PromptMessage(NormalizeRole(historyMessage.Role), historyMessage.Content));
            }
        }
        AddUserSections(messages, sections.SuffixUserSections);
        if (sections.AppendLatestVisibleReply && !string.IsNullOrWhiteSpace(visibleReply))
        {
            messages.Add(new PromptMessage("user", "[latest_reply]\n" + visibleReply.Trim()));
        }
        return new PromptPackage(messages, _maxTokens, _model);
    }

    private static void AddSystemSections(List<PromptMessage> messages, IReadOnlyList<string> sections)
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
