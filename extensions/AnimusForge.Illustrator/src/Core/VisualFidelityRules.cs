namespace AnimusForge.Illustrator.Core
{
    internal static class VisualFidelityRules
    {
        internal static readonly string PlayerRedrawDirectorPriority =
            IllustratorText.Get("Fidelity_0") +
            IllustratorText.Get("Fidelity_1") +
            IllustratorText.Get("Fidelity_2") +
            IllustratorText.Get("Fidelity_3") +
            IllustratorText.Get("Fidelity_4") +
            IllustratorText.Get("Fidelity_5");

        internal static readonly string PlayerRedrawImagePriority =
            IllustratorText.Get("Fidelity_6") +
            IllustratorText.Get("Fidelity_7") +
            IllustratorText.Get("Fidelity_8");

        internal static readonly string PlayerRedrawReferenceException =
            IllustratorText.Get("Fidelity_9");

        internal static readonly string DirectedRedrawRepaint =
            IllustratorText.Get("Fidelity_10");

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
                System.Text.RegularExpressions.Regex.Matches(reference.Label ?? "", @"【[^】\r\n]+】"))
            {
                if (match.Value == IllustratorText.Raw("Fidelity_11") || match.Value == IllustratorText.Raw("Fidelity_12") || match.Value == IllustratorText.Raw("Fidelity_13")) break;
                identity.Append(match.Value);
            }
            return identity.ToString() + (character
                ? IllustratorText.Get("Fidelity_14")
                : IllustratorText.Get("Fidelity_15") + reference.Kind + IllustratorText.Get("Fidelity_16")) +
                IllustratorText.Get("Fidelity_17");
        }

        internal static readonly string ClothingStatePriority =
            IllustratorText.Get("Fidelity_18");

        internal static readonly string FreeEquipmentRule =
            IllustratorText.Get("Fidelity_19");

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

        private static readonly System.Text.RegularExpressions.Regex LocalizedEquipmentFactLines =
            new System.Text.RegularExpressions.Regex("(?m)^(?:" + string.Join("|", new[]
            {
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_310")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_311")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_312")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_313")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_314")),
                System.Text.RegularExpressions.Regex.Escape(IllustratorText.Get("Hero_307"))
            }) + ")[^\\r\\n]*(?:\\r?\\n)?", System.Text.RegularExpressions.RegexOptions.Compiled);

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

        internal static readonly string EnvironmentGeometryPriority =
            IllustratorText.Get("Fidelity_24") +
            IllustratorText.Get("Fidelity_25") +
            IllustratorText.Get("Fidelity_26") +
            IllustratorText.Get("Fidelity_27") +
            IllustratorText.Get("Fidelity_28") +
            IllustratorText.Get("Fidelity_29");

        internal static readonly string ConversationLocationGrounding =
            IllustratorText.Get("Fidelity_30") +
            IllustratorText.Get("Fidelity_31") +
            IllustratorText.Get("Fidelity_32") +
            IllustratorText.Get("Fidelity_33");

        internal static readonly string ConversationCrowdCountPriority =
            IllustratorText.Get("Fidelity_34") +
            IllustratorText.Get("Fidelity_35") +
            IllustratorText.Get("Fidelity_36") +
            IllustratorText.Get("Fidelity_37") +
            IllustratorText.Get("Fidelity_38");

        internal static readonly string ConversationActionPriority =
            IllustratorText.Get("Fidelity_39") +
            IllustratorText.Get("Fidelity_40") +
            IllustratorText.Get("Fidelity_41") +
            IllustratorText.Get("Fidelity_42");

        internal static readonly string DirectorAppearanceFidelity =
            IllustratorText.Get("Fidelity_43") +
            IllustratorText.Get("Fidelity_44") +
            IllustratorText.Get("Fidelity_45") +
            IllustratorText.Get("Fidelity_46") +
            IllustratorText.Get("Fidelity_47") +
            IllustratorText.Get("Fidelity_48") +
            IllustratorText.Get("Fidelity_49");

        internal static readonly string CharacterAppearancePriority =
            IllustratorText.Get("Fidelity_50") +
            IllustratorText.Get("Fidelity_51");

        internal static readonly string DirectorQualityFloor =
            IllustratorText.Get("Fidelity_52") +
            IllustratorText.Get("Fidelity_53") +
            IllustratorText.Get("Fidelity_54") +
            IllustratorText.Get("Fidelity_55") +
            IllustratorText.Get("Fidelity_56") +
            IllustratorText.Get("Fidelity_57");

        internal static readonly string ReferenceRepaint =
            IllustratorText.Get("Fidelity_58") +
            IllustratorText.Get("Fidelity_59") +
            IllustratorText.Get("Fidelity_60") +
            IllustratorText.Get("Fidelity_61") +
            IllustratorText.Get("Fidelity_62");

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
