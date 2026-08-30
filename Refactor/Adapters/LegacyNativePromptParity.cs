using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Builds detached Native prompt sections from the final legacy message/string
/// blocks and compares the resulting package with the legacy package. This is
/// a diagnostic seam only: it never rebuilds rule text, invokes a provider, or
/// carries a live game object across an async boundary.
/// </summary>
public static class LegacyNativePromptParity
{
    public static LegacyNativePromptParityResult CompareMainMessages(
        IEnumerable<object> legacyMessages,
        IEnumerable<string> prefixUserSections,
        IEnumerable<string> suffixUserSections,
        string playerText,
        int maxTokens = 4096,
        string model = "legacy-native-parity")
    {
        PromptPackage legacy = LegacyPromptPackageAdapter.FromLegacyMessages(legacyMessages, maxTokens, model);
        List<PromptMessage> expected = legacy.Messages.ToList();
        List<string> prefix = NonEmptySections(prefixUserSections);
        List<string> suffix = NonEmptySections(suffixUserSections);
        List<PromptMessage> history = new List<PromptMessage>();

        bool shapeValid = expected.Count > 0
            && string.Equals(expected[0].Role, "system", StringComparison.OrdinalIgnoreCase)
            && expected.Count >= 1 + prefix.Count + suffix.Count;
        if (shapeValid)
        {
            int historyStart = 1 + prefix.Count;
            int historyEnd = expected.Count - suffix.Count;
            for (int i = historyStart; i < historyEnd; i++)
            {
                history.Add(expected[i]);
            }
        }

        DetachedPromptSections sections = shapeValid
            ? new DetachedPromptSections(
                new[] { expected[0].Content },
                prefix,
                suffix,
                appendCurrentPlayerInput: false)
            : DetachedPromptSections.Empty;
        PromptPackage detached = ComposeMain(sections, history, playerText, maxTokens, model);
        string mismatch = shapeValid ? string.Empty : "legacy_shape";
        bool matches = shapeValid && CompareMessages(legacy, detached, out mismatch);
        return new LegacyNativePromptParityResult(
            "main",
            sections,
            null,
            legacy,
            detached,
            matches ? "match" : BuildMismatch(shapeValid, mismatch, legacy, detached));
    }

    public static LegacyNativePromptParityResult ComparePostprocessBlocks(
        string legacySystemPrompt,
        string legacyUserPrompt,
        int maxTokens = 5000,
        string model = "legacy-native-postprocess-parity")
    {
        DetachedPostprocessPromptSections sections = new DetachedPostprocessPromptSections(
            new[] { legacySystemPrompt ?? string.Empty },
            Array.Empty<string>(),
            new[] { legacyUserPrompt ?? string.Empty },
            appendLatestVisibleReply: false);
        InteractionEnvelope envelope = BuildEnvelope(
            string.Empty,
            Array.Empty<PromptMessage>(),
            DetachedPromptSections.Empty,
            sections);
        PromptPackage detached = new LegacyDetachedPostprocessPromptComposer(maxTokens, model).Compose(
            envelope,
            new RuleSelection(Array.Empty<string>(), Array.Empty<string>()),
            string.Empty,
            string.Empty,
            new PostprocessContext(Array.Empty<string>(), Array.Empty<string>(), new CapabilitySet(Array.Empty<string>())));
        PromptPackage legacy = new PromptPackage(
            new[]
            {
                new PromptMessage("system", legacySystemPrompt ?? string.Empty),
                new PromptMessage("user", legacyUserPrompt ?? string.Empty)
            },
            maxTokens,
            model);
        return new LegacyNativePromptParityResult(
            "postprocess",
            null,
            sections,
            legacy,
            detached,
            CompareMessages(legacy, detached, out string mismatch) ? "match" : BuildMismatch(false, mismatch, legacy, detached));
    }

    public static DetachedInteractionPromptSections BuildAtomicBundle(
        DetachedPromptSections main,
        DetachedPostprocessPromptSections postprocess)
    {
        return new DetachedInteractionPromptSections(main, postprocess);
    }

    private static PromptPackage ComposeMain(
        DetachedPromptSections sections,
        IEnumerable<PromptMessage> history,
        string playerText,
        int maxTokens,
        string model)
    {
        InteractionEnvelope envelope = BuildEnvelope(playerText, history, sections, DetachedPostprocessPromptSections.Empty);
        return new LegacyDetachedPromptComposer(maxTokens, model).Compose(
            envelope,
            new RuleSelection(Array.Empty<string>(), Array.Empty<string>()),
            new CapabilitySet(Array.Empty<string>()));
    }

    private static InteractionEnvelope BuildEnvelope(
        string playerText,
        IEnumerable<PromptMessage> history,
        DetachedPromptSections main,
        DetachedPostprocessPromptSections postprocess)
    {
        GameInteractionSnapshot snapshot = new GameInteractionSnapshot(
            new InteractionIdentity("native-parity", InteractionChannel.NativeConversation, "native-parity"),
            new TraceContext("native-parity-trace", 0, 0, "parity", "detached"),
            playerText ?? string.Empty,
            "parity",
            0,
            0,
            Array.Empty<InteractionCandidate>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());
        return new InteractionEnvelope(snapshot, history, main, postprocess);
    }

    private static List<string> NonEmptySections(IEnumerable<string> sections)
    {
        return (sections ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();
    }

    private static bool CompareMessages(PromptPackage expected, PromptPackage actual, out string mismatch)
    {
        mismatch = string.Empty;
        if (expected == null || actual == null || expected.Messages.Count != actual.Messages.Count)
        {
            mismatch = "message_count";
            return false;
        }
        for (int i = 0; i < expected.Messages.Count; i++)
        {
            PromptMessage left = expected.Messages[i];
            PromptMessage right = actual.Messages[i];
            if (!string.Equals(left?.Role, right?.Role, StringComparison.Ordinal)
                || !string.Equals(left?.Content, right?.Content, StringComparison.Ordinal))
            {
                mismatch = "message_" + i;
                return false;
            }
        }
        return true;
    }

    private static string BuildMismatch(bool shapeValid, string mismatch, PromptPackage legacy, PromptPackage detached)
    {
        return (shapeValid ? mismatch : "legacy_shape")
            + ";legacyCount=" + (legacy?.Messages.Count ?? 0)
            + ";detachedCount=" + (detached?.Messages.Count ?? 0);
    }

    public sealed class LegacyNativePromptParityResult
    {
        internal LegacyNativePromptParityResult(
            string stage,
            DetachedPromptSections mainSections,
            DetachedPostprocessPromptSections postprocessSections,
            PromptPackage legacyPackage,
            PromptPackage detachedPackage,
            string comparison)
        {
            Stage = stage;
            MainSections = mainSections;
            PostprocessSections = postprocessSections;
            LegacyPackage = legacyPackage;
            DetachedPackage = detachedPackage;
            Comparison = comparison ?? string.Empty;
        }

        public string Stage { get; }
        public DetachedPromptSections MainSections { get; }
        public DetachedPostprocessPromptSections PostprocessSections { get; }
        public PromptPackage LegacyPackage { get; }
        public PromptPackage DetachedPackage { get; }
        public bool Matches => string.Equals(Comparison, "match", StringComparison.Ordinal);
        public string Comparison { get; }

        public string ToDiagnosticString()
        {
            return "stage=" + Stage
                + " match=" + Matches
                + " comparison=" + Comparison
                + " legacyCount=" + (LegacyPackage?.Messages.Count ?? 0)
                + " detachedCount=" + (DetachedPackage?.Messages.Count ?? 0)
                + " legacyDigest=" + Digest(LegacyPackage)
                + " detachedDigest=" + Digest(DetachedPackage);
        }

        private static string Digest(PromptPackage package)
        {
            StringBuilder value = new StringBuilder();
            foreach (PromptMessage message in package?.Messages ?? Array.Empty<PromptMessage>())
            {
                value.Append(message?.Role ?? string.Empty).Append('\n').Append(message?.Content ?? string.Empty).Append('\n');
            }
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value.ToString())))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
