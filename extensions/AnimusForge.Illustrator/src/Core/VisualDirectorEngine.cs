using System;
using System.Linq;
using AnimusForge.Illustrator.Engine;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Context;

namespace AnimusForge.Illustrator.Core
{
    public sealed class IllustrationPromptPlan
    {
        public string Mode { get; }
        private string PromptMode
        {
            get
            {
                if (Mode == IllustratorText.Raw("Director_933")) return IllustratorText.Get("ModeConversationCurrentTwo");
                if (Mode == IllustratorText.Raw("Director_934")) return IllustratorText.Get("ModeConversationTwo");
                if (Mode == IllustratorText.Raw("Director_935")) return IllustratorText.Get("ModeConversationOne");
                if (Mode == IllustratorText.Raw("Director_936")) return IllustratorText.Get("ModeConversationThree");
                if (Mode == IllustratorText.Raw("Director_937")) return IllustratorText.Get("WeeklyChronicleMode");
                if (Mode == IllustratorText.Raw("Director_979")) return IllustratorText.Get("ModeGeneric");
                if (Mode == IllustratorText.Raw("Director_1020")) return IllustratorText.Get("ModeEncyclopedia");
                if (Mode == IllustratorText.Raw("ScreenshotRules_836")) return IllustratorText.Get("ModeSceneShout");
                if (Mode == IllustratorText.Raw("ScreenshotRules_837")) return IllustratorText.Get("ModeBattle");
                return Mode;
            }
        }
        internal bool IsConversation => Mode == IllustratorText.Raw("Director_933") || Mode == IllustratorText.Raw("Director_934") || Mode == IllustratorText.Raw("Director_935") || Mode == IllustratorText.Raw("Director_936");
        internal bool IsWeeklyReport => Mode == IllustratorText.Raw("Director_937");
        internal bool IsEncyclopedia => Mode == IllustratorText.Raw("Director_1020");
        internal bool IsMissionScreenshot => MissionScreenshotRules.IsMode(Mode);
        public string HardFacts { get; }
        public string ArtDirection { get; }
        /// <summary>只给导演看的事实（如台词原文/对话历史）——不进最终生图提示词，避免被画成画面文字。</summary>
        public string DirectorOnlyFacts { get; }
        public string PlayerRedrawPrompt { get; }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection)
            : this(mode, hardFacts, artDirection, null)
        {
        }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection, string directorOnlyFacts)
            : this(mode, hardFacts, artDirection, directorOnlyFacts, null)
        {
        }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection, string directorOnlyFacts, string playerRedrawPrompt)
        {
            Mode = (mode ?? string.Empty).Trim();
            HardFacts = VisualFidelityRules.ShareProfileRules((hardFacts ?? string.Empty).Trim());
            ArtDirection = (artDirection ?? string.Empty).Trim();
            DirectorOnlyFacts = (directorOnlyFacts ?? string.Empty).Trim();
            PlayerRedrawPrompt = (playerRedrawPrompt ?? string.Empty).Trim();
        }

        public string BuildDirectorContext()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Mode)) sb.AppendLine(IllustratorText.Get("Director_938") + PromptMode);
            sb.AppendLine("<game_facts>");
            sb.AppendLine(HardFacts);
            sb.AppendLine("</game_facts>");
            if (!string.IsNullOrWhiteSpace(DirectorOnlyFacts))
            {
                sb.AppendLine("<director_only_narrative>");
                sb.AppendLine(DirectorOnlyFacts);
                sb.AppendLine("</director_only_narrative>");
            }
            if (!string.IsNullOrWhiteSpace(ArtDirection))
            {
                sb.AppendLine("<open_art_direction>");
                sb.AppendLine(ArtDirection);
                sb.AppendLine("</open_art_direction>");
            }
            if (!string.IsNullOrWhiteSpace(PlayerRedrawPrompt))
            {
                sb.AppendLine(IllustratorText.Get("Director_939"));
                sb.AppendLine(PlayerRedrawPrompt);
                sb.AppendLine(VisualFidelityRules.PlayerRedrawDirectorPriority);
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static class VisualDirectorEngine
    {
        private static readonly TimeSpan DirectorRequestTimeout = TimeSpan.FromSeconds(240);
        private static readonly string EncyclopediaSystemPrompt = IllustratorText.Get("EncyclopediaDirectorSystem");
        private static readonly string SystemPrompt =
            IllustratorText.Get("Director_940") +
            IllustratorText.Get("Director_941") +
            IllustratorText.Get("Director_942") +
            IllustratorText.Get("Director_943") +
            IllustratorText.Get("Director_944") +
            IllustratorText.Get("Director_945") +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            IllustratorText.Get("Director_946") +
            IllustratorText.Get("Director_947") +
            IllustratorText.Get("Director_948") +
            IllustratorText.Get("Director_949") +
            IllustratorText.Get("Director_950") +
            IllustratorText.Get("Director_951") +
            IllustratorText.Get("Director_952") +
            IllustratorText.Get("Director_953") +
            IllustratorText.Get("Director_954");

        private static readonly string ConversationSystemPrompt =
            IllustratorText.Get("Director_955") +
            IllustratorText.Get("Director_956") +
            VisualFidelityRules.ConversationActionPriority + "\n" +
            VisualFidelityRules.EnvironmentGeometryPriority + "\n" +
            VisualFidelityRules.ConversationLocationGrounding + "\n" +
            VisualFidelityRules.ConversationCrowdCountPriority + "\n" +
            IllustratorText.Get("Director_957") +
            IllustratorText.Get("Director_958") +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            IllustratorText.Get("Director_959") +
            IllustratorText.Get("Director_960") +
            IllustratorText.Get("Director_961") +
            IllustratorText.Get("Director_962") +
            IllustratorText.Get("Director_963") +
            IllustratorText.Get("Director_964") +
            IllustratorText.Get("Director_965");

        // Weekly reports have their own event-led director contract. They never receive
        // the portrait/set-design or current-conversation restrictions above.
        private static readonly string WeeklyReportSystemPrompt =
            IllustratorText.Get("Director_966") +
            IllustratorText.Get("Director_967") +
            IllustratorText.Get("Director_968") +
            IllustratorText.Get("Director_969") +
            IllustratorText.Get("Director_970") +
            IllustratorText.Get("Director_971") +
            IllustratorText.Get("Director_972") +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            IllustratorText.Get("Director_973") +
            IllustratorText.Get("Director_974") +
            IllustratorText.Get("Director_975") +
            IllustratorText.Get("Director_976") +
            IllustratorText.Get("Director_977") +
            IllustratorText.Get("Director_978");

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData = null, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan(IllustratorText.Raw("Director_979"), gameContext, string.Empty), WrapSingle(base64ImageData), options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan(IllustratorText.Raw("Director_980"), gameContext, string.Empty), WrapSingle(base64ImageData), options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan(IllustratorText.Raw("Director_981"), gameContext, string.Empty), referenceImages, options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan(IllustratorText.Raw("Director_982"), gameContext, string.Empty), referenceImages, options, cancellationToken);
        }

        private static System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> WrapSingle(string base64ImageData)
        {
            if (string.IsNullOrWhiteSpace(base64ImageData)) return null;
            return new[] { new IllustrationReferenceImage(base64ImageData, IllustratorText.Get("Director_983"), IllustrationReferenceKind.Scene) };
        }

        public static async Task<string> ExpandToDetailedPromptAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return (await CreateDirectionAsync(plan, referenceImages, options, cancellationToken).ConfigureAwait(false)).Prompt;
        }

        public static async Task<IllustrationDirection> CreateDirectionAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient { Timeout = DirectorRequestTimeout })
                return await CreateDirectionWithClientAsync(plan, referenceImages, options, client, cancellationToken).ConfigureAwait(false);
        }

        // The caller owns client; the same request/parse path is exercised by the offline HTTP audit.
        internal static async Task<IllustrationDirection> CreateDirectionWithClientAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, HttpClient client, CancellationToken cancellationToken)
        {
            plan = plan ?? new IllustrationPromptPlan(IllustratorText.Raw("Director_984"), string.Empty, string.Empty);
            if (!plan.IsMissionScreenshot && options?.PreserveEquipmentFidelity == false)
                plan = new IllustrationPromptPlan(plan.Mode, VisualFidelityRules.WithoutEquipmentFacts(plan.HardFacts),
                    VisualFidelityRules.WithoutEquipmentRules(plan.ArtDirection), plan.DirectorOnlyFacts, plan.PlayerRedrawPrompt);
            GenerationDiagnostics.Current?.RecordStage("prompt_sources", new JObject { ["mode"] = plan.Mode, ["hardFactChars"] = plan.HardFacts.Length, ["artDirectionChars"] = plan.ArtDirection.Length, ["directorOnlyFactChars"] = plan.DirectorOnlyFacts.Length, ["directorRuleSource"] = "visual director system prompt + illustration facts/style; no direct RuleBehaviorPrompts", ["dialogueRulesIndirect"] = plan.IsConversation, ["style"] = options?.SelectedStyle, ["customDirectorRuleChars"] = options?.CustomDirectorPrompt?.Length ?? 0,
                ["customDirectorRulesActive"] = options?.EnableLlmPromptExpansion == true && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl) && !string.IsNullOrWhiteSpace(options.CustomDirectorPrompt),
                ["preserveEquipmentFidelity"] = options?.PreserveEquipmentFidelity != false, ["playerRedrawPromptChars"] = plan.PlayerRedrawPrompt.Length });
            RequirePlayerRedrawDirector(plan.PlayerRedrawPrompt, options);
            if (plan.IsMissionScreenshot)
            {
                RequireMissionScreenshotDirector(options);
                MissionScreenshotRules.RequireReferences(referenceImages);
            }
            if (!plan.IsMissionScreenshot && options != null && !options.EnableMultimodalVision)
            {
                referenceImages = null;
            }
            if (referenceImages != null && referenceImages.Count == 0)
            {
                referenceImages = null;
            }

            int imgCount = referenceImages?.Count ?? 0;
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Starting prompt expansion (MultimodalVision={(options?.EnableMultimodalVision == true ? "ON" : "OFF")}, RefImages={imgCount})...");

            IllustrationDirection direction;
            string fallbackReason = options?.EnableLlmPromptExpansion == false ? "The director is disabled" : "The director interface is not configured";
            DirectorResponse reply = null;
            bool requestedDirector = false;
            try
            {
                if (options != null && options.EnableLlmPromptExpansion && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))
                {
                    requestedDirector = true;
                    GenerationDiagnostics.Current?.RecordStage("director_begin", new JObject { ["model"] = options.DirectorModelName, ["referenceCount"] = referenceImages?.Count ?? 0 });
                    reply = await CallLlmDirectorResponseAsync(plan, options, referenceImages, client, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(reply.FailureReason))
                    {
                        direction = ResolveDirection(reply.Content, plan, options);
                        ApplyResponseMetadata(direction, reply);
                        direction.UsedTextOnlyDirector = referenceImages == null || reply.VisionUnsupported;
                        if (plan.IsMissionScreenshot && (direction.UsedLocalFallback || direction.VisionUnsupported || direction.UsedTextOnlyDirector))
                            fallbackReason = "Free-camera generation requires the vision director to plan the scene: " + direction.FallbackReason;
                        else if (!string.IsNullOrWhiteSpace(plan.PlayerRedrawPrompt) && direction.UsedLocalFallback)
                            fallbackReason = "Redrawing with a prompt cannot use a local composition: " + direction.FallbackReason;
                        else
                            return RecordDirection(direction);
                    }
                    else fallbackReason = reply.FailureReason;
                }
                else
                {
                    TaleWorlds.Library.Debug.Print("[VisualDirector] Chat expansion disabled or unavailable, falling back to rule-based synthesis.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                fallbackReason = "The director request timed out";
            }
            catch (Exception ex)
            {
                // Raw provider errors may contain request text or credentials; diagnostics handle the raw response.
                fallbackReason = "The director request failed (" + ex.GetType().Name + ")";
            }

            if (requestedDirector || plan.IsMissionScreenshot)
            {
                direction = new IllustrationDirection { FallbackReason = fallbackReason };
                ApplyResponseMetadata(direction, reply);
                direction.DirectionStatus = "failed";
                RecordDirection(direction);
                GenerationDiagnostics.Current?.RecordStage("director_failed", new JObject { ["failureCode"] = "director.output_unusable", ["error"] = direction.StatusText });
                throw new InvalidOperationException(direction.StatusText);
            }

            string rulePrompt = SynthesizeRuleBasedPrompt(plan, options);
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Using rule-based prompt ({rulePrompt.Length} chars): {Preview(rulePrompt, 120)}");
            direction = new IllustrationDirection
            {
                Prompt = rulePrompt,
                Theme = IllustratorText.Raw("Director_991"),
                ActionSummary = IllustrationDirection.ExtractActionSummary(rulePrompt),
                DirectionStatus = "local_fallback",
                UsedLocalFallback = true,
                FallbackReason = fallbackReason
            };
            ApplyResponseMetadata(direction, reply);
            return RecordDirection(direction);
        }

        private static IllustrationDirection RecordDirection(IllustrationDirection direction)
        {
            TaleWorlds.Library.Debug.Print($"[VisualDirector] {direction.StatusText}; status={direction.DirectionStatus}, finish_reason={direction.FinishReason}, reason={direction.FallbackReason}, tokens={direction.TotalTokens}; prompt={direction.Prompt.Length} chars.");
            GenerationDiagnostics.Current?.RecordDirection(direction);
            return direction;
        }

        internal static void RequirePlayerRedrawDirector(string playerRedrawPrompt, IllustrationOptions options)
        {
            if (string.IsNullOrWhiteSpace(playerRedrawPrompt) ||
                (options?.EnableLlmPromptExpansion == true && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))) return;
            GenerationDiagnostics.Current?.RecordStage("director_failed", new JObject { ["failureCode"] = "director.player_redraw_unavailable" });
            throw new InvalidOperationException("The director is disabled or unconfigured. Redrawing with a prompt did not start.");
        }

        internal static void RequireMissionScreenshotDirector(IllustrationOptions options)
        {
            if (options?.EnableLlmPromptExpansion != true || string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl)
                || string.IsNullOrWhiteSpace(options.DirectorModelName))
                throw new InvalidOperationException("Scene capture generation requires an enabled and configured vision director. Capture did not start.");
        }

        private static void ApplyResponseMetadata(IllustrationDirection direction, DirectorResponse reply)
        {
            if (reply == null) return;
            direction.FinishReason = reply.FinishReason;
            direction.PromptTokens = reply.PromptTokens;
            direction.CompletionTokens = reply.CompletionTokens;
            direction.TotalTokens = reply.TotalTokens;
            direction.VisionUnsupported = reply.VisionUnsupported;
            if (reply.Truncated) direction.DirectionStatus = "truncated";
            else if (!direction.UsedLocalFallback && reply.VisionUnsupported) direction.DirectionStatus = "vision_unsupported";
            if (reply.VisionUnsupported)
                direction.FallbackReason = "The interface explicitly does not support image input; one text-only conception attempt was made" +
                    (string.IsNullOrWhiteSpace(direction.FallbackReason) ? string.Empty : "; " + direction.FallbackReason);
        }

        internal static string ComposeFinalPrompt(string directorPrompt, string hardFacts = null, bool isSinglePortrait = false, bool isConversation = false, bool isWeeklyReport = false, bool playerRedraw = false, bool preserveEquipment = true)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(directorPrompt))
            {
                sb.AppendLine(directorPrompt.Trim());
            }
            // Original facts still reach the director and identity references.
            // For explicit redraws its reconciled scene is authoritative: appending
            // unedited facts/contracts would reintroduce the superseded details,
            // especially on compact diffusion/DALL-E text encoders.
            if (playerRedraw) return sb.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(hardFacts))
            {
                sb.AppendLine().AppendLine(IllustratorText.Raw("Director_995"));
                sb.Append(VisualFidelityRules.ShareProfileRules(hardFacts.Trim()));
            }
            string contract = VisualFidelityRules.GetEssentialContract(isSinglePortrait, isConversation, isWeeklyReport);
            sb.AppendLine().Append(preserveEquipment ? contract : VisualFidelityRules.WithoutEquipmentRules(contract));
            if (!isSinglePortrait) sb.AppendLine().Append(VisualFidelityRules.ClothingStatePriority);
            return sb.ToString().Trim();
        }

        internal static string ResolveDirectorOutput(string output, IllustrationPromptPlan plan, IllustrationOptions options)
        {
            return ResolveDirection(output, plan, options).Prompt;
        }

        internal static IllustrationDirection ResolveDirection(string output, IllustrationPromptPlan plan, IllustrationOptions options)
        {
            var direction = IllustrationDirection.SplitMetadata(output);
            string fallbackReason;
            direction.Prompt = ResolveDirectorBody(direction.Prompt, plan, options, out fallbackReason);
            if (!string.IsNullOrWhiteSpace(fallbackReason))
            {
                direction.DirectionStatus = "local_fallback";
                direction.UsedLocalFallback = true;
                direction.FallbackReason = fallbackReason;
                direction.Title = string.Empty;
                direction.Theme = IllustratorText.Raw("Director_996");
                direction.ActionSummary = string.Empty;
                direction.SceneYawDegrees = null;
                direction.ScenePitchDegrees = null;
                direction.SceneHorizontalFovDegrees = null;
                direction.AuxiliarySceneYawDegrees = null;
                direction.AuxiliaryScenePitchDegrees = null;
                direction.AuxiliarySceneHorizontalFovDegrees = null;
            }
            bool directorAction = !direction.UsedLocalFallback && !string.IsNullOrWhiteSpace(direction.ActionSummary);
            if (string.IsNullOrWhiteSpace(direction.ActionSummary))
                direction.ActionSummary = IllustrationDirection.ExtractActionSummary(direction.Prompt);
            if (string.IsNullOrWhiteSpace(direction.Theme)) direction.Theme = IllustratorText.Raw("Director_997");
            // Lead the image prompt with the director's positive pose sentence, ahead of the
            // identity references' idle stance. Same echo guard as the body; once per request.
            if (directorAction && plan != null &&
                !NarrativeFactRouter.HasNarrativeEcho(direction.ActionSummary, plan.DirectorOnlyFacts, plan.HardFacts, allowVisualNarration: plan.IsConversation))
                direction.Prompt = IllustratorText.Get("Director_998") + direction.ActionSummary + "\n" + direction.Prompt;
            return direction;
        }

        private static string ResolveDirectorBody(string output, IllustrationPromptPlan plan, IllustrationOptions options, out string fallbackReason)
        {
            fallbackReason = string.Empty;
            output = output ?? string.Empty;
            if (plan?.IsMissionScreenshot == true)
            {
                if (!HasCompleteSceneDescription(output, allowBackgroundOverride: false))
                {
                    fallbackReason = string.IsNullOrWhiteSpace(output) ? "The director returned an empty body" : "The director did not return a complete visual plan";
                    return string.Empty;
                }
                return output.Trim() + IllustratorText.Get("Director_1001") + plan.HardFacts + "\n" +
                    MissionScreenshotRules.Contract(plan.Mode == MissionScreenshotRules.BattleMode);
            }
            // A bulletin is an event illustration. Negated props, quoted event facts and
            // flexible section layouts do not establish an invalid composition. Preserve
            // usable direction and metadata; the image prompt still carries game facts
            // and the selected fidelity contract. Empty bodies keep the existing fallback.
            if (plan?.IsWeeklyReport == true && !string.IsNullOrWhiteSpace(output))
                return ComposeFinalPrompt(output, hardFacts: plan.HardFacts, isWeeklyReport: true,
                    playerRedraw: !string.IsNullOrWhiteSpace(plan.PlayerRedrawPrompt),
                    preserveEquipment: options?.PreserveEquipmentFidelity != false);
            // These keyword checks cannot distinguish an explicitly requested prop
            // from an invented one. Directed redraws use the scoped director policy.
            if (options?.PreserveEquipmentFidelity != false && string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt) &&
                (ViolatesShieldVisibility(output, plan) || ViolatesPortraitComposition(output, plan)))
            {
                fallbackReason = "The director output contains unsupported shields, flags, or an unsuitable portrait action";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Unsupported shield/portrait props rejected; using local portrait fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            if (NarrativeFactRouter.HasNarrativeEcho(output, plan.DirectorOnlyFacts, plan.HardFacts, allowVisualNarration: plan.IsConversation))
            {
                fallbackReason = "The director repeated the narrative instead of translating it into a visual plan";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Narrative echo detected; using local visual-fact fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            bool isSingle = plan?.Mode?.Contains(IllustratorText.Raw("Director_1004")) == true || plan?.Mode?.Contains(IllustratorText.Raw("Director_1005")) == true;
            if (!HasCompleteSceneDescription(output, allowBackgroundOverride: !string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt)))
            {
                fallbackReason = string.IsNullOrWhiteSpace(output) ? "The director returned an empty body" : "The director body lacks complete scene, lighting, or spatial details, or the composition does not meet the requirements";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Missing scene/light/spatial direction; using local scene fallback without retry.");
                // Preserve a short usable visual paraphrase while supplying the missing scene sections.
                // Do not retain equipment lists or long non-conforming output as the main direction.
                // Open art direction is written for the director (redraw quotas, motifs to avoid,
                // "the director decides" guidance); it never reaches the image endpoint.
                if (output.Length <= 120 && System.Text.RegularExpressions.Regex.IsMatch(output, IllustratorText.Raw("Director_1008")) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(output, IllustratorText.Raw("Director_1009"), System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return ComposeFinalPrompt(BuildLocalSceneDirection(plan) + IllustratorText.Get("Director_1010") + output,
                        hardFacts: plan.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true, preserveEquipment: options?.PreserveEquipmentFidelity != false);
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            // The director is responsible for composition, but its prose can omit a visual
            // fact. Keep the authoritative game facts beside the derived direction so the
            // image endpoint does not have to infer age, equipment, emblems or spatial state
            // from a reference image alone.
            return ComposeFinalPrompt(output, hardFacts: plan?.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true,
                playerRedraw: !string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt), preserveEquipment: options?.PreserveEquipmentFidelity != false);
        }

        private static readonly string[] RequiredSectionPatterns = new[]
        {
            IllustratorText.Raw("Director_1011"),
            IllustratorText.Raw("Director_1012"),
            IllustratorText.Raw("Director_1013"),
            IllustratorText.Raw("Director_1014")
        };

        // CN headings and EN stable protocol markers describe the same sections.
        // Cache the four parsers once; do not compile patterns for each image request.
        private static readonly string[] RequiredProtocolMarkers = new[]
        {
            "[[AFI_PROTOCOL_SUBJECT]]", "[[AFI_PROTOCOL_SCENE]]",
            "[[AFI_PROTOCOL_LIGHT]]", "[[AFI_PROTOCOL_SPATIAL]]"
        };
        private static readonly System.Text.RegularExpressions.Regex[] RequiredSectionParsers =
            RequiredSectionPatterns.Select((pattern, index) => new System.Text.RegularExpressions.Regex(
                "(?:【" + pattern + "】|" + System.Text.RegularExpressions.Regex.Escape(RequiredProtocolMarkers[index])
                + @")([\s\S]*?)(?=【(?:" + string.Join("|", RequiredSectionPatterns) + @")】|\[\[AFI_PROTOCOL_|$)",
                System.Text.RegularExpressions.RegexOptions.Compiled)).ToArray();
        private static readonly int[] RequiredSectionMinimum = { 8, 12, 10, 10 };

        internal static bool HasRequiredSceneDescription(string output)
        {
            return HasCompleteSceneDescription(output, allowBackgroundOverride: false);
        }

        private static bool HasCompleteSceneDescription(string output, bool allowBackgroundOverride)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            if (!allowBackgroundOverride && System.Text.RegularExpressions.Regex.IsMatch(output,
                IllustratorText.Raw("Director_1015"),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return false;
            for (int i = 0; i < RequiredSectionParsers.Length; i++)
            {
                var match = RequiredSectionParsers[i].Match(output);
                string value = match.Success ? match.Groups[1].Value.Trim() : string.Empty;
                // Structural guard only: padding and section ratios do not establish
                // action/spatial correctness. Keep a small non-whitespace minimum.
                int length = 0;
                foreach (char character in value)
                    if (!char.IsWhiteSpace(character)) length++;
                if (length < RequiredSectionMinimum[i]) return false;
            }
            return true;
        }

        // Sent directly to the image model: positive wording only, since text encoders
        // read a named object as a request to draw it. Negatives travel separately.
        internal static string BuildLocalSceneDirection(IllustrationPromptPlan plan)
        {
            if (plan?.IsWeeklyReport == true)
            {
                return IllustratorText.Get("Director_1016") +
                    IllustratorText.Get("Director_1017") +
                    IllustratorText.Get("Director_1018") +
                    IllustratorText.Get("Director_1019");
            }
            if (plan?.Mode == IllustratorText.Raw("Director_1020"))
            {
                return IllustratorText.Get("Director_1021") +
                    IllustratorText.Get("Director_1022") +
                    IllustratorText.Get("Director_1023") +
                    IllustratorText.Get("Director_1024");
            }

            bool hasCrowdEvidence = !string.IsNullOrWhiteSpace(plan?.HardFacts) &&
                (plan.HardFacts.IndexOf(IllustratorText.Raw("Director_1025"), StringComparison.Ordinal) >= 0 ||
                 plan.HardFacts.IndexOf(IllustratorText.Get("Environment_101"), StringComparison.Ordinal) >= 0) &&
                plan.HardFacts.IndexOf(IllustratorText.Raw("Director_1026"), StringComparison.Ordinal) < 0 &&
                plan.HardFacts.IndexOf(IllustratorText.Get("Environment_190"), StringComparison.Ordinal) < 0;
            string crowdDirection = hasCrowdEvidence
                ? IllustratorText.Get("Director_1027")
                : IllustratorText.Get("Director_1028");
            return IllustratorText.Get("Director_1029") +
                IllustratorText.Get("Director_1030") + crowdDirection + IllustratorText.Get("Director_1031") +
                IllustratorText.Get("Director_1032") +
                IllustratorText.Get("Director_1033");
        }

        internal static bool ViolatesShieldVisibility(string output, IllustrationPromptPlan plan)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            if (plan?.Mode == IllustratorText.Raw("Director_1034"))
            {
                // 百科模式拦截实际出现、手持、携带或展示盾牌的描述，不误杀“不画盾牌/未持盾”或非盾牌词汇
                if (System.Text.RegularExpressions.Regex.IsMatch(output,
                    IllustratorText.Raw("Director_1035"),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
            }
            return BackShieldPattern.IsMatch(output);
        }

        // A shield carried on someone's back. "身后/背后" are spatial words ("the guard behind
        // the player holds a shield") and 盾 also occurs in 矛盾/后盾/盾徽, so only an explicit
        // carrying verb within the same clause counts.
        private static readonly System.Text.RegularExpressions.Regex BackShieldPattern = new System.Text.RegularExpressions.Regex(
            IllustratorText.Raw("Director_1036") +
            IllustratorText.Raw("Director_1037") +
            @"|shield[^.!?;,\r\n]{0,30}(?:on (?:his|her|their|the) back|slung (?:over|across|on)|strapped to (?:his|her|their|the) back)" +
            @"|(?:slung|strapped)[^.!?;,\r\n]{0,30}shield[^.!?;,\r\n]{0,20}back",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        internal static bool ViolatesPortraitComposition(string output, IllustrationPromptPlan plan)
        {
            if (plan?.Mode != IllustratorText.Raw("Director_1038") || string.IsNullOrWhiteSpace(output)) return false;
            // Natural supported actions are the director's decision, not keyword violations.
            // Keep only the portrait-specific unsupported flag check here.
            foreach (string token in new[] { IllustratorText.Raw("Director_1039"), IllustratorText.Raw("Director_1040") })
                if (output.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>
        /// 重绘变体指令：注入艺术指导区，要求导演刻意切换景别/机位/瞬间，
        /// 避免与上一版雷同构图。redrawIndex 从 2 开始（首次绘制不注入）。
        /// </summary>
        public static string BuildRedrawVariationDirective(int redrawIndex)
        {
            return IllustratorText.Format("Director_1041", redrawIndex) +
                   IllustratorText.Get("Director_1042") +
                   IllustratorText.Get("Director_1043");
        }

        private static string Preview(string value, int maxChars)
        {
            string text = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length <= maxChars ? text : text.Substring(0, maxChars) + "...";
        }

        internal static bool ResolveChatConfigForSnapshot(out string baseUrl, out string apiKey, out string model)
        {
            // Hidden legacy director overrides must never redirect a request away from the main API.
            if (TryGetHostPrimaryChatConfig(out baseUrl, out apiKey, out model)) return true;
            baseUrl = apiKey = model = string.Empty;
            return false;
        }


        private static bool TryGetHostPrimaryChatConfig(out string apiUrl, out string apiKey, out string modelName)
        {
            apiUrl = string.Empty;
            apiKey = string.Empty;
            modelName = string.Empty;

            try
            {
                Type duelSettingsType = AccessTools.TypeByName("AnimusForge.DuelSettings");
                if (duelSettingsType != null)
                {
                    MethodInfo getSettingsMethod = AccessTools.Method(duelSettingsType, "GetSettings");
                    object hostSettings = getSettingsMethod?.Invoke(null, null);
                    if (hostSettings != null)
                    {
                        PropertyInfo apiKeyProp = AccessTools.Property(duelSettingsType, "ApiKey");
                        PropertyInfo apiUrlProp = AccessTools.Property(duelSettingsType, "ApiUrl");
                        MethodInfo getModelMethod = AccessTools.Method(duelSettingsType, "GetEffectiveMainModelName");
                        MethodInfo getEffectiveUrlMethod = AccessTools.Method(duelSettingsType, "GetEffectiveApiUrl", new[] { typeof(string) });

                        apiKey = (apiKeyProp?.GetValue(hostSettings) as string ?? string.Empty).Trim();
                        string rawUrl = (apiUrlProp?.GetValue(hostSettings) as string ?? string.Empty).Trim();

                        if (getEffectiveUrlMethod != null)
                        {
                            apiUrl = (getEffectiveUrlMethod.Invoke(null, new object[] { rawUrl }) as string ?? rawUrl).Trim();
                        }
                        else
                        {
                            apiUrl = rawUrl;
                        }

                        if (getModelMethod != null)
                        {
                            modelName = (getModelMethod.Invoke(hostSettings, null) as string ?? string.Empty).Trim();
                        }

                        if (!string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey))
                        {
                            if (string.IsNullOrWhiteSpace(modelName))
                            {
                                modelName = "gpt-4o-mini";
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[VisualDirector] Failed to reflect host primary chat config: {ex.Message}");
            }

            return false;
        }

        private static string ResolveImageDataUri(string base64ImageData)
        {
            if (string.IsNullOrWhiteSpace(base64ImageData)) return null;
            if (base64ImageData.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return base64ImageData;
            string mimeType = base64ImageData.StartsWith("iVBORw0KGgo") ? "image/png" : "image/jpeg";
            return $"data:{mimeType};base64,{base64ImageData}";
        }

        private static async Task<string> CallLlmDirectorAsync(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient { Timeout = DirectorRequestTimeout })
            {
                var reply = await CallLlmDirectorResponseAsync(plan, options, referenceImages, client, cancellationToken).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(reply.FailureReason) ? reply.Content : string.Empty;
            }
        }

        private sealed class DirectorResponse
        {
            public string Content = string.Empty;
            public string FinishReason = string.Empty;
            public string FailureReason = string.Empty;
            public bool Truncated;
            public bool VisionUnsupported;
            public int? PromptTokens;
            public int? CompletionTokens;
            public int? TotalTokens;
        }

        private static DirectorResponse ParseDirectorResponse(string responseBody)
        {
            JObject json = JObject.Parse(responseBody);
            JToken choice = json["choices"] is JArray choices && choices.Count > 0 ? choices[0] : null;
            JToken message = choice?["message"];
            var reply = new DirectorResponse
            {
                FinishReason = choice?["finish_reason"]?.Type == JTokenType.String ? choice["finish_reason"].Value<string>().Trim() : string.Empty,
                PromptTokens = ReadTokenCount(json["usage"]?["prompt_tokens"]),
                CompletionTokens = ReadTokenCount(json["usage"]?["completion_tokens"]),
                TotalTokens = ReadTokenCount(json["usage"]?["total_tokens"])
            };
            string reason = reply.FinishReason.ToLowerInvariant();
            reply.Truncated = reason == "length" || reason == "max_tokens" || reason == "max_output_tokens";
            // An incomplete/absent response terminates generation; only usable text reaches contract fallback.
            if (reply.Truncated) reply.FailureReason = "The director output was truncated at the token limit. The incomplete body was not used";
            else if (reason == "content_filter" || !string.IsNullOrWhiteSpace(message?["refusal"]?.ToString()))
                reply.FailureReason = "The director refused or filtered this output";
            else if (reason.Length > 0 && reason != "stop" && reason != "end_turn" && reason != "completed")
                reply.FailureReason = "The director did not finish the body normally";

            JToken content = message?["content"];
            if (content?.Type == JTokenType.String) reply.Content = content.Value<string>();
            else if (content is JArray parts)
            {
                var text = new StringBuilder();
                foreach (JToken part in parts)
                    if (part is JObject && part["type"]?.Value<string>() == "text" && part["text"]?.Type == JTokenType.String)
                        text.Append(part["text"].Value<string>());
                reply.Content = text.ToString();
            }
            if (string.IsNullOrWhiteSpace(reply.FailureReason) && string.IsNullOrWhiteSpace(reply.Content))
                reply.FailureReason = "The director returned an empty body";
            return reply;
        }

        private static int? ReadTokenCount(JToken token)
        {
            int value;
            return token != null && int.TryParse(token.ToString(), out value) && value >= 0 ? (int?)value : null;
        }

        private static JObject BuildDirectorPayload(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, bool textFallback)
        {
            string stylePreference = BuildDirectorStylePreference(options);
            int approximateTokens = options != null && options.DirectorApproximateTokens > 0 ? options.DirectorApproximateTokens : 2000;
            string requestText = IllustratorText.Get("Director_1048") +
                (string.IsNullOrWhiteSpace(stylePreference) ? string.Empty : IllustratorText.Get("Director_1049") + stylePreference) + "\n\n" + plan.BuildDirectorContext() +
                IllustratorText.Get("Director_1050") + approximateTokens + IllustratorText.Get("Director_1051");
            if (!string.IsNullOrWhiteSpace(options?.CustomDirectorPrompt))
            {
                requestText += IllustratorText.Get("Director_1052") + options.CustomDirectorPrompt +
                    (!plan.IsMissionScreenshot && options?.PreserveEquipmentFidelity == false ? IllustratorText.Get("Director_1053") : IllustratorText.Get("Director_1054")) +
                    IllustratorText.Get("Director_1055") +
                    IllustratorText.Get("Director_1056");
            }
            if (!string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt))
            {
                var redrawStyle = IllustrationStylePresets.Resolve(options?.SelectedStyle, options?.CustomStylePrompt);
                requestText += IllustratorText.Get("Director_1057") + redrawStyle.NegativePrompt +
                    (redrawStyle.IsCustom ? "\n" + options?.NegativePrompt : string.Empty) +
                    "\n" + VisualFidelityRules.PlayerRedrawDirectorPriority;
            }
            if (!plan.IsMissionScreenshot && options?.PreserveEquipmentFidelity == false) requestText += "\n" + VisualFidelityRules.FreeEquipmentRule;
            if (textFallback)
                requestText += IllustratorText.Get("Director_1058");
            if (referenceImages != null)
            {
                foreach (var reference in referenceImages)
                {
                    if (reference?.Kind != IllustrationReferenceKind.ScenePanorama || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                    requestText += IllustratorText.Get("Director_1059") +
                        IllustratorText.Get("Director_1060") +
                        IllustratorText.Get("Director_1061") +
                        IllustratorText.Get("Director_1062") +
                        IllustratorText.Get("Director_1063") +
                        IllustratorText.Get("Director_1064");
                    break;
                }
            }

            JObject userMessage;
            if (referenceImages != null && referenceImages.Count > 0)
            {
                var content = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = requestText + IllustratorText.Get("Director_1065")
                    }
                };
                foreach (var reference in referenceImages)
                {
                    if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                    if (!string.IsNullOrWhiteSpace(reference.Label))
                    {
                        content.Add(new JObject
                        {
                            ["type"] = "text",
                            ["text"] = IllustratorText.Get("Director_1066") + (plan.IsMissionScreenshot || (string.IsNullOrWhiteSpace(plan.PlayerRedrawPrompt) && options?.PreserveEquipmentFidelity != false) ? reference.Label : VisualFidelityRules.DirectedReferenceLabel(reference))
                        });
                    }
                    content.Add(new JObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JObject
                        {
                            ["url"] = ResolveImageDataUri(reference.Base64Image)
                        }
                    });
                }
                userMessage = new JObject
                {
                    ["role"] = "user",
                    ["content"] = content
                };
            }
            else
            {
                userMessage = new JObject
                {
                    ["role"] = "user",
                    ["content"] = requestText
                };
            }

            string modeSystem = plan.IsEncyclopedia ? EncyclopediaSystemPrompt
                : plan.IsWeeklyReport ? WeeklyReportSystemPrompt
                : plan.IsConversation ? ConversationSystemPrompt : SystemPrompt;
            return new JObject
            {
                ["model"] = options.DirectorModelName,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] =
                        PromptComposerTexts.ApplyOutputLanguage((plan?.IsMissionScreenshot == true ? MissionScreenshotRules.DirectorSystem + "\n" + MissionScreenshotRules.Contract(plan.Mode == MissionScreenshotRules.BattleMode)
                        : options?.PreserveEquipmentFidelity == false ? VisualFidelityRules.WithoutEquipmentRules(modeSystem) : modeSystem) +
                        (string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt) ? string.Empty : "\n" + VisualFidelityRules.PlayerRedrawDirectorPriority) +
                        (plan.IsEncyclopedia ? string.Empty : "\n" + VisualFidelityRules.ClothingStatePriority) +
                        IllustratorText.Get("Director_1067") +
                        IllustratorText.Get("Director_1068") +
                        IllustratorText.Get("Director_1069")) },
                    userMessage
                },
                ["max_tokens"] = options.DirectorApiMaxTokens > 0 ? options.DirectorApiMaxTokens : 25000,
                ["temperature"] = 0.85
            };

        }

        private static async Task<DirectorResponse> CallLlmDirectorResponseAsync(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, HttpClient client, CancellationToken cancellationToken)
        {
            var reply = new DirectorResponse();
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                deadline.CancelAfter(DirectorRequestTimeout);
                string endpoint = ResolveChatEndpoint(options.DirectorApiBaseUrl);
                GenerationDiagnostics.Current?.RegisterSecret(options.DirectorApiKey);
                try
                {
                    // Only an explicit unsupported-image response permits a second request.
                    // Both attempts share one deadline; truncation, empty output and transport errors do not retry.
                    for (int attempt = 0; attempt < (plan.IsMissionScreenshot ? 1 : 2); attempt++)
                    {
                        bool hasImages = referenceImages != null && referenceImages.Count > 0;
                        JObject payload = BuildDirectorPayload(plan, options, referenceImages, reply.VisionUnsupported || !hasImages);
                        GenerationDiagnostics.Current?.RecordDirectorRequest(endpoint, options.DirectorModelName, payload, referenceImages);
                        using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                        {
                            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                            if (!string.IsNullOrWhiteSpace(options.DirectorApiKey))
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.DirectorApiKey);
                            TaleWorlds.Library.Debug.Print($"[VisualDirector] Sending request (model={options.DirectorModelName}, refImages={referenceImages?.Count ?? 0}, attempt={attempt + 1})...");
                            GenerationDiagnostics.Current?.RecordStage("director_http_begin", new JObject { ["endpoint"] = SensitiveLogText.SafeUrl(endpoint), ["referenceCount"] = referenceImages?.Count ?? 0, ["attempt"] = attempt + 1 });
                            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false))
                            {
                                GenerationDiagnostics.Current?.RecordStage("director_http_headers", new JObject { ["httpStatus"] = (int)response.StatusCode });
                                string responseBody = Encoding.UTF8.GetString(await ImagePayload.ReadBoundedAsync(response.Content, 1048576, deadline.Token).ConfigureAwait(false));
                                if (!response.IsSuccessStatusCode)
                                {
                                    GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, string.Empty, (int)response.StatusCode);
                                    if (!plan.IsMissionScreenshot && hasImages && attempt == 0 && ShouldRetryDirectorWithoutImages((int)response.StatusCode, responseBody))
                                    {
                                        reply.VisionUnsupported = true;
                                        referenceImages = null;
                                        continue;
                                    }
                                    reply.FailureReason = DescribeDirectorHttpFailure((int)response.StatusCode, responseBody);
                                    return reply;
                                }
                                DirectorResponse parsed;
                                try { parsed = ParseDirectorResponse(responseBody); }
                                catch
                                {
                                    GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, string.Empty, (int)response.StatusCode);
                                    throw;
                                }
                                GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, parsed.FinishReason, (int)response.StatusCode);
                                GenerationDiagnostics.Current?.RecordDirectorText(parsed.Content);
                                parsed.VisionUnsupported = reply.VisionUnsupported;
                                return parsed;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (OperationCanceledException) { reply.FailureReason = "The director request timed out"; }
                catch (Exception ex) { reply.FailureReason = "The director request or response parsing failed (" + ex.GetType().Name + ")"; }
                return reply;
            }
        }

        private static string DescribeDirectorHttpFailure(int statusCode, string responseBody)
        {
            // Classify known provider failures; raw messages can include credentials or request text.
            string providerMessage = string.Empty;
            try { providerMessage = JObject.Parse(responseBody)?["error"]?["message"]?.Value<string>() ?? string.Empty; }
            catch { }
            if (providerMessage.IndexOf("location is not supported", StringComparison.OrdinalIgnoreCase) >= 0 ||
                providerMessage.IndexOf("unsupported_country_region", StringComparison.OrdinalIgnoreCase) >= 0)
                return "The director service denied access from the current region (HTTP " + statusCode + ")";
            if (statusCode == 401 || statusCode == 403) return "Director authentication or access failed (HTTP " + statusCode + ")";
            if (statusCode == 429) return "The director interface was rate-limited or has insufficient quota (HTTP 429)";
            if (statusCode >= 500) return "The upstream director service is temporarily unavailable (HTTP " + statusCode + ")";
            return "The director interface returned HTTP " + statusCode;
        }

        internal static bool ShouldRetryDirectorWithoutImages(int statusCode, string responseBody)
        {
            if (statusCode != 400 && statusCode != 415 && statusCode != 422 && statusCode != 500 && statusCode != 502 && statusCode != 503) return false;
            string text = (responseBody ?? string.Empty).ToLowerInvariant();
            if (text.Contains("content must be a string") || text.Contains("content should be a string")) return true;
            bool imageContext = text.Contains("image") || text.Contains("vision") || text.Contains("multimodal") || text.Contains("mmproj") || text.Contains(IllustratorText.Raw("Director_1077"));
            bool explicitlyUnsupported = text.Contains("not support") || text.Contains("unsupported") ||
                text.Contains("text-only") || text.Contains("text only") || text.Contains("does not accept") ||
                text.Contains("cannot accept") || text.Contains("only support") || text.Contains(IllustratorText.Raw("Director_1078")) ||
                (text.Contains("mmproj") && (text.Contains("missing") || text.Contains("required")));
            return imageContext && explicitlyUnsupported;
        }

        private static string BuildDirectorStylePreference(IllustrationOptions options)
        {
            return IllustrationStylePresets.Resolve(options?.SelectedStyle, options?.CustomStylePrompt).DirectorPrompt;
        }

        private static string ResolveChatEndpoint(string baseUrl)
        {
            string url = baseUrl.TrimEnd('/');
            if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }
            if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return url + "/chat/completions";
            }
            return url + "/v1/chat/completions";
        }

        private static string SynthesizeRuleBasedPrompt(string rawContext)
        {
            return SynthesizeRuleBasedPrompt(new IllustrationPromptPlan(IllustratorText.Raw("Director_1079"), rawContext, string.Empty), null);
        }

        private static string SynthesizeRuleBasedPrompt(IllustrationPromptPlan plan, IllustrationOptions options)
        {
            // Open art direction addresses the director (motifs to avoid, redraw variation,
            // "the director chooses the camera"); an image model would draw those words.
            // The image client supplies the full selected style once for every route.
            bool isSingle = plan?.Mode?.Contains(IllustratorText.Raw("Director_1080")) == true || plan?.Mode?.Contains(IllustratorText.Raw("Director_1081")) == true;
            return ComposeFinalPrompt(options?.PreserveEquipmentFidelity == false ? VisualFidelityRules.WithoutEquipmentRules(BuildLocalSceneDirection(plan)) : BuildLocalSceneDirection(plan), plan?.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true, preserveEquipment: options?.PreserveEquipmentFidelity != false);
        }
    }
}
