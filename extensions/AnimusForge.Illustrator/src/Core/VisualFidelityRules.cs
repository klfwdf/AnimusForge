namespace AnimusForge.Illustrator.Core
{
    internal static class VisualFidelityRules
    {
        internal static string SharedReferenceInstructions(System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> references)
        {
            var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>(System.StringComparer.Ordinal);
            var order = new System.Collections.Generic.List<string>();
            for (int i = 0; i < references.Count; i++)
            {
                string rule = ReferenceRoleInstruction(references[i].Kind);
                if (!groups.TryGetValue(rule, out var indices))
                {
                    indices = new System.Collections.Generic.List<int>();
                    groups.Add(rule, indices);
                    order.Add(rule);
                }
                indices.Add(i + 1);
            }
            var result = new System.Text.StringBuilder();
            foreach (string rule in order)
                result.AppendLine(IllustratorText.Format("SharedReferenceRules", string.Join(", ", groups[rule])))
                    .AppendLine(rule);
            return result.ToString();
        }

        // Only known, non-parameterized rules from generated profile fields are shared.
        // Never deduplicate whole facts, user prose, names, or director output.
        internal static string ShareProfileRules(string facts)
        {
            if (string.IsNullOrEmpty(facts)) return facts;
            var shared = new System.Text.StringBuilder();
            string[] keys = { "Hero_458", "Hero_461", "Hero_471", "Hero_472", "Hero_428", "Hero_429", "Hero_457",
                "Hero_323", "Conversation_15", "Hero_425", "Hero_426", "Hero_447" };
            string[] fields = { IllustratorText.Get("Hero_304"), IllustratorText.Get("Hero_309").Split('{')[0], IllustratorText.Get("Hero_311"),
                IllustratorText.Get("Hero_307"), IllustratorText.Get("Hero_312"), IllustratorText.Get("Hero_313") };
            var lines = facts.Split(new[] { '\n' });
            string faceHeading = IllustratorText.Get("Hero_309").Split('{')[0];
            string nestedFaceHeading = IllustratorText.Get("Hero_454");
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith(faceHeading + nestedFaceHeading, System.StringComparison.Ordinal))
                    lines[i] = faceHeading + lines[i].Substring(faceHeading.Length + nestedFaceHeading.Length);
            var seenRules = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach (string key in keys)
            {
                string rule = IllustratorText.Get(key);
                // Templates keep all bound values in place; only their fixed trailing
                // instruction is shared. Never parse names or equipment as instructions.
                int parameterEnd = rule.LastIndexOf('}');
                if (parameterEnd >= 0)
                {
                    int separator = rule.IndexOf(';', parameterEnd + 1);
                    if (separator < 0) separator = rule.IndexOf('；', parameterEnd + 1);
                    if (separator < 0) continue;
                    rule = rule.Substring(separator);
                }
                if (string.IsNullOrEmpty(rule)) continue;
                bool found = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    bool profileField = false;
                    foreach (string field in fields)
                        if (!string.IsNullOrEmpty(field) && lines[i].StartsWith(field, System.StringComparison.Ordinal)) profileField = true;
                    if (!profileField || lines[i].IndexOf(rule, System.StringComparison.Ordinal) < 0) continue;
                    lines[i] = lines[i].Replace(rule, string.Empty);
                    found = true;
                }
                if (found && seenRules.Add(rule)) shared.AppendLine(rule.TrimStart(';', '；', ' '));
            }
            return shared.Length == 0 ? facts : string.Join("\n", lines) + "\n" +
                IllustratorText.Get("SharedProfileRules") + "\n" + shared.ToString().TrimEnd();
        }

        private static readonly PromptLanguageValue<string> PlayerRedrawDirectorPriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_0") +
            IllustratorText.Get("Fidelity_1") +
            IllustratorText.Get("Fidelity_2") +
            IllustratorText.Get("Fidelity_3") +
            IllustratorText.Get("Fidelity_4") +
            IllustratorText.Get("Fidelity_5"));
        internal static string PlayerRedrawDirectorPriority => PlayerRedrawDirectorPriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> PlayerRedrawImagePriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_6") +
            IllustratorText.Get("Fidelity_7") +
            IllustratorText.Get("Fidelity_8"));
        internal static string PlayerRedrawImagePriority => PlayerRedrawImagePriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> PlayerRedrawReferenceExceptionByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_9"));
        internal static string PlayerRedrawReferenceException => PlayerRedrawReferenceExceptionByLanguage.Value;

        private static readonly PromptLanguageValue<string> DirectedRedrawRepaintByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_10"));
        internal static string DirectedRedrawRepaint => DirectedRedrawRepaintByLanguage.Value;

        // Chinese labels use 【...】 headings; English labels use [...] (protocol [[...]] markers excluded).
        private static readonly System.Text.RegularExpressions.Regex IdentityHeadings = new System.Text.RegularExpressions.Regex(
            @"【[^】\r\n]+】|(?<!\[)\[(?!\[)[^\[\]\r\n]+\](?!\])", System.Text.RegularExpressions.RegexOptions.Compiled);

        internal static string DirectedReferenceLabel(IllustrationReferenceImage reference)
        {
            bool character = reference.Kind == IllustrationReferenceKind.Character ||
                reference.Kind == IllustrationReferenceKind.CharacterDetail ||
                reference.Kind == IllustrationReferenceKind.EventCharacter;
            // Keep scene geometry, paired-view A/B and emblem ownership intact.
            if (!character) return reference.Label + PlayerRedrawReferenceException;
            // Keep source role/name headings, not the old equipment-lock prose.
            var identity = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match match in
                IdentityHeadings.Matches(reference.Label ?? ""))
            {
                if (IllustratorText.IsRaw(match.Value, "Fidelity_11") || IllustratorText.IsRaw(match.Value, "Fidelity_12") || IllustratorText.IsRaw(match.Value, "Fidelity_13")) break;
                identity.Append(match.Value);
            }
            return identity.ToString() + (character
                ? IllustratorText.Get("Fidelity_14")
                : IllustratorText.Get("Fidelity_15") + reference.Kind + IllustratorText.Get("Fidelity_16")) +
                IllustratorText.Get("Fidelity_17");
        }

        private static readonly PromptLanguageValue<string> ClothingStatePriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_18"));
        internal static string ClothingStatePriority => ClothingStatePriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> FreeEquipmentRuleByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_19"));
        internal static string FreeEquipmentRule => FreeEquipmentRuleByLanguage.Value;

        internal static string WithoutEquipmentFacts(string facts)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(facts ?? "",
                IllustratorText.Raw("Fidelity_20"), "");
            cleaned = LocalizedEquipmentFactLines.Replace(cleaned, "");
            cleaned = cleaned.Replace(IllustratorText.Raw("Fidelity_21"), "")
                .Replace(IllustratorText.Raw("Fidelity_22"), "")
                .Replace(IllustratorText.Get("Hero_461"), "")
                .Replace(IllustratorText.Get("Hero_459"), "");
            return cleaned;
        }

        private static readonly PromptLanguageValue<System.Text.RegularExpressions.Regex> LocalizedEquipmentFactLinesByLanguage = new PromptLanguageValue<System.Text.RegularExpressions.Regex>(() => new System.Text.RegularExpressions.Regex("(?m)^(?:" + string.Join("|", new[]
            {
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_310")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_311")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_312")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_313")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_314")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_307"))
            }) + ")[^\\r\\n]*(?:\\r?\\n)?", System.Text.RegularExpressions.RegexOptions.Compiled));
        private static System.Text.RegularExpressions.Regex LocalizedEquipmentFactLines => LocalizedEquipmentFactLinesByLanguage.Value;

        // Only fixed rules are filtered; never apply this to model prose or user rules.
        internal static string WithoutEquipmentRules(string rules)
        {
            var result = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match part in
                System.Text.RegularExpressions.Regex.Matches(rules ?? "", @"[^。！？.!?\r\n]+[。！？.!?\r\n]*"))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(part.Value, IllustratorText.Raw("Fidelity_23"), System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                result.Append(part.Value);
            }
            return result.ToString() + "\n" + FreeEquipmentRule;
        }

        private static readonly PromptLanguageValue<string> EnvironmentGeometryPriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_24") +
            IllustratorText.Get("Fidelity_25") +
            IllustratorText.Get("Fidelity_26") +
            IllustratorText.Get("Fidelity_27") +
            IllustratorText.Get("Fidelity_28") +
            IllustratorText.Get("Fidelity_29"));
        internal static string EnvironmentGeometryPriority => EnvironmentGeometryPriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> ConversationLocationGroundingByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_30") +
            IllustratorText.Get("Fidelity_31") +
            IllustratorText.Get("Fidelity_32") +
            IllustratorText.Get("Fidelity_33"));
        internal static string ConversationLocationGrounding => ConversationLocationGroundingByLanguage.Value;

        private static readonly PromptLanguageValue<string> ConversationCrowdCountPriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_34") +
            IllustratorText.Get("Fidelity_35") +
            IllustratorText.Get("Fidelity_36") +
            IllustratorText.Get("Fidelity_37") +
            IllustratorText.Get("Fidelity_38"));
        internal static string ConversationCrowdCountPriority => ConversationCrowdCountPriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> ConversationActionPriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_39") +
            IllustratorText.Get("Fidelity_40") +
            IllustratorText.Get("Fidelity_41") +
            IllustratorText.Get("Fidelity_42"));
        internal static string ConversationActionPriority => ConversationActionPriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> DirectorAppearanceFidelityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_43") +
            IllustratorText.Get("Fidelity_44") +
            IllustratorText.Get("Fidelity_45") +
            IllustratorText.Get("Fidelity_46") +
            IllustratorText.Get("Fidelity_47") +
            IllustratorText.Get("Fidelity_48") +
            IllustratorText.Get("Fidelity_49"));
        internal static string DirectorAppearanceFidelity => DirectorAppearanceFidelityByLanguage.Value;

        private static readonly PromptLanguageValue<string> CharacterAppearancePriorityByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_50") +
            IllustratorText.Get("Fidelity_51"));
        internal static string CharacterAppearancePriority => CharacterAppearancePriorityByLanguage.Value;

        private static readonly PromptLanguageValue<string> DirectorQualityFloorByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_52") +
            IllustratorText.Get("Fidelity_53") +
            IllustratorText.Get("Fidelity_54") +
            IllustratorText.Get("Fidelity_55") +
            IllustratorText.Get("Fidelity_56") +
            IllustratorText.Get("Fidelity_57"));
        internal static string DirectorQualityFloor => DirectorQualityFloorByLanguage.Value;

        private static readonly PromptLanguageValue<string> ReferenceRepaintByLanguage = new PromptLanguageValue<string>(() => IllustratorText.Get("Fidelity_58") +
            IllustratorText.Get("Fidelity_59") +
            IllustratorText.Get("Fidelity_60") +
            IllustratorText.Get("Fidelity_61") +
            IllustratorText.Get("Fidelity_62"));
        internal static string ReferenceRepaint => ReferenceRepaintByLanguage.Value;

        internal static string ReferenceRoleInstruction(IllustrationReferenceKind kind)
        {
            switch (kind)
            {
                case IllustrationReferenceKind.MissionScreenshot:
                    return MissionScreenshotRules.SpatialContract;
                case IllustrationReferenceKind.Character:
                    return CharacterAppearancePriority;
                case IllustrationReferenceKind.CharacterDetail:
                    return IllustratorText.Get("Fidelity_63") + CharacterAppearancePriority;
                case IllustrationReferenceKind.Emblem:
                    return IllustratorText.Get("Fidelity_64");
                case IllustrationReferenceKind.Scene:
                    return IllustratorText.Get("Fidelity_65") + ConversationActionPriority;
                case IllustrationReferenceKind.ScenePanorama:
                    return IllustratorText.Get("Fidelity_66") + EnvironmentGeometryPriority;
                case IllustrationReferenceKind.PairedScenePerspective:
                    return IllustratorText.Get("Fidelity_67") + EnvironmentGeometryPriority;
                case IllustrationReferenceKind.ScenePerspective:
                    return IllustratorText.Get("Fidelity_68") + EnvironmentGeometryPriority;
                case IllustrationReferenceKind.SceneViews:
                    return IllustratorText.Get("Fidelity_69");
                case IllustrationReferenceKind.MapConversationScene:
                    return IllustratorText.Get("Fidelity_70") + ConversationActionPriority;
                case IllustrationReferenceKind.EventCharacter:
                    return IllustratorText.Get("Fidelity_71");
                case IllustrationReferenceKind.EventEmblem:
                    return IllustratorText.Get("Fidelity_72");
                default:
                    return IllustratorText.Get("Fidelity_73");
            }
        }

        // Sent verbatim to the image model after the director body. Keep it positive:
        // prohibited elements (text, collage, extra limbs, ...) travel in the negative
        // channel, which diffusion endpoints receive as a separate negative_prompt field.
        internal static string GetEssentialContract(bool isSinglePortrait, bool isConversation = false, bool isWeeklyReport = false)
        {
            string poseRule = isSinglePortrait
                ? IllustratorText.Get("Fidelity_74")
                : isConversation
                    ? IllustratorText.Get("Fidelity_75") + ConversationActionPriority + "\n" + ConversationLocationGrounding + "\n" + ConversationCrowdCountPriority + "\n"
                    : isWeeklyReport
                        ? IllustratorText.Get("Fidelity_76")
                        : IllustratorText.Get("Fidelity_77");

            return IllustratorText.Raw("Fidelity_78") +
                IllustratorText.Get("Fidelity_79") +
                poseRule +
                IllustratorText.Get("Fidelity_80") +
                IllustratorText.Get("Fidelity_81") +
                IllustratorText.Get("Fidelity_82") +
                IllustratorText.Get("Fidelity_83");
        }

    }
}
