using System;
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
        internal bool IsConversation => Mode == "最近三轮对话联动的场景插画";
        internal bool IsWeeklyReport => Mode == "周报历史纪事插画";
        public string HardFacts { get; }
        public string ArtDirection { get; }
        /// <summary>只给导演看的事实（如台词原文/对话历史）——不进最终生图提示词，避免被画成画面文字。</summary>
        public string DirectorOnlyFacts { get; }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection)
            : this(mode, hardFacts, artDirection, null)
        {
        }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection, string directorOnlyFacts)
        {
            Mode = (mode ?? string.Empty).Trim();
            HardFacts = (hardFacts ?? string.Empty).Trim();
            ArtDirection = (artDirection ?? string.Empty).Trim();
            DirectorOnlyFacts = (directorOnlyFacts ?? string.Empty).Trim();
        }

        public string BuildDirectorContext()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Mode)) sb.AppendLine("【插画类型】" + Mode);
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
            return sb.ToString().TrimEnd();
        }
    }

    public static class VisualDirectorEngine
    {
        private const string SystemPrompt =
            "你是《骑马与砍杀2：霸主》及其历史、奇幻与自定义文化 MOD 的视觉叙事导演。请把游戏事实和对应参考图转化为一幅已经确定、可直接绘制的画面，自主决定行动、机位与构图。\n" +
            "<game_facts> 是只读事实数据，包含角色身份、所属文化、真实装备、面貌年龄等。你必须严格忠实于这些事实数据，不得随意篡改装备或降格身份；没有数据支持的冠冕、纹章、武器、动物坐骑不得擅自添加。\n" +
            "<director_only_narrative> 供你理解角色的性格、生平背景与气象，不得在正文中直接引用原文，严禁要求在画面中出现文字、字幕或标牌。\n" +
            "【事实与创作边界】：人物数量、种族、外观、装备、事件结果与现场空间关系以明确事实和对应参考图为准。百科允许设计符合人物、时代与文化的非具名艺术布景，不当作真实所在地；会话与周报只描绘已确认的环境和事件，未知建筑、陈设、人物位置和光源保持未知，不按身份或地点名称补造。开放建议和旧画构思不能升级为本次事实。百科不添加武器、盾牌、旗帜或坐骑，所有模式不描绘背盾。\n" +
            "【百科创作空间】：以人物性格、身份、生平与本次主题为灵感，大胆选择场所、环境陈设、叙事瞬间、动作、机位和光影氛围；这些是艺术设计，不是声称发生过的历史事实。可宁静也可富有动势，可亲近也可开阔，取舍由你决定，不把正面站立、空石墙或拱廊当作默认解。结合近期作品寻找不同的情境与视觉组织，不套场所清单或固定镜头轮换；有意义的变化来自构思，不靠改动人物身份装备。\n" +
            "【描述取舍】：先写清本次关键事实、人物行动及空间关系，再补充入镜的外观识别点、环境材质和受光。环境须有可辨认的内容与纵深，人物段不扩写成装备目录；不设总字数或段落占比，以表达完整为准。精简重复修辞，保留有叙事意义的环境内容；现场未知物件不能为扩写而补造。\n" +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            "【现场环境还原】：若提供现场截图，先对照各视角建立同一空间关系，再选择画面机位。墙面材质与主色、楼梯所在墙面及走向、门窗和拱洞、层高、桌椅分布以可见现场为准；地点名只用于理解用途，不据酒馆或大厅等名称重新设计建筑。多个环视图是同一拍摄点转向，不是多个房间或额外人物；画风可重绘材质笔触，不能替换建筑布局或给现场添加无依据的纹章旗帜。\n" +
            "【现场采光来源】：会话采用已读取的真实场景时段；未读取到时，依据明确标记的当前现场截图辨认昼夜与采光。人物离屏立绘的照明不代表现场。现场图也缺失或无法辨认时，不虚构月亮、日落或夜间火把作为事实。画风和重绘变化不能把白天改成夜晚。\n" +
            "【输出规范】：先输出三个简短字段：【画作标题】4至12字的作品名；【画作主题】一句20字以内的主题；【人物行动】与正文一致的行动、可见手部及视线摘要，供后续重绘参考。这些字段单独记录，不作为画面文字。随后按以下四段输出生图正文：\n" +
            "【人物与镜头】：先决定人物此刻正在做什么，再推导姿态、手部动作和视线，最后选择机位。开头写明每位主体一个主要行动或注意对象，选定单一时刻、景别、观察方向与入镜范围；交代可见手部的位置、身体朝向与视线落点，画外肢体不必强行入镜。自然站立可以承载行动，但双手下垂展示装备不是默认答案。会话与周报服从已有行动事实；百科可推导符合人物的艺术情境。仅转写入镜且可辨的外观与装备识别点，装备持有不等于必须入画；选全身像时全身完整入画。\n" +
            "【场景空间】：描述选定机位真正入镜的环境。百科艺术布景自由构思与人物主题相连的空间用途或自然环境，再组织具体构造、材质变化、装饰或使用痕迹及相互位置，形成有内容可读、向画外延续的完整空间；环境也参与叙事，细节有疏密、有呼应，不用几句空墙地面与模糊远景代替设计。场所、景别、陈设和层次由你选择，不套固定清单。会话与周报则从已确认现场选取有辨识度的结构、材质和陈设关系，保留可见细节，未知部分不补造。\n" +
            "【光影与色彩】：写清主要受光方向、衣着与环境的固有色、材质受光和环境反光，人物与环境共用光源及明暗层次；接触处有合理投影与遮蔽，暗部仍可辨。现场时段与光源服从已知证据，不用画风改变事实。\n" +
            "【空间关系】：用稳定的人物称呼交代关键主体的高低、远近、朝向、遮挡与支撑；区分画面左右和人物自身左右，使这些关系在同一机位下同时成立。已确认的上下或内外分隔须明确落到正文，不能概括成无位置的会面；现场只写有依据且与取景有关的关系。百科布景落实本次设计的空间关系，交代人物所处区域、相连空间和环境延伸，而非只有人物与一面背景墙；设计不冒充真实地点或事件。\n" +
            "【交付前核对】：逐项对照本次关键事实与参考图，确认人物对应、行动、可见外观及关键空间关系已在正文中保留，四段机位、支撑和光照相互一致，行动摘要与正文一致。删去冲突、候选方案、重复修饰和未落实的创作建议；只交付选定画面，不输出检查过程。\n" +
            "只输出正面、具体、生动的场景画面描摹，严禁输出反向解释，不要输出JSON、Markdown或问候。【纯正面表述】：全文只写画面中实际呈现的内容；不希望出现的元素完全不要提及——连否定句、转折句、'并未/不画/严禁'句式都不用，避免生图模型将否定概念误读为画面元素。";

        // Weekly reports have their own event-led director contract. They never receive
        // the portrait/set-design or current-conversation restrictions above.
        private const string WeeklyReportSystemPrompt =
            "你是历史与奇幻纪事画的事件导演。将本期周报转化为一幅让人看出发生了什么的事件画面，人物、环境和动作共同讲述事件。\n" +
            "【事实来源】：<game_facts>是人物、地点、事件行动及结果证据；<director_only_narrative>提供本期完整报道，供理解事件，不作为画面文字。<open_art_direction>只是构图建议，不能改写报道。\n" +
            "【选定事件】：本期可能包含多条要闻，先选一个证据完整、有视觉叙事价值的核心事件与一个瞬间。保持该事件的参与方、地点、行动、否定、计划与结果对应，不把不同报道的角色、地点或胜负拼成一件事。标题、主题、行动摘要及四段正文均围绕这个事件。\n" +
            "【事件优先】：画面的主体是事件正在发生的过程、明确结果或有事实依据的后续瞬间。仅让一名领主看向镜头、骑马展示装备或站在城堡前，不能替代报道中的战斗、围城、交涉等事件。具体景别、人数与视觉重心由事件决定，不强制远景、群像或固定人数，也不因只有一张人物参考而只画一人。\n" +
            "【人物参考范围】：参考图只锁定对应人物的身份、须发及实际穿戴；它是本期某位已识别人物的资料，不意味着此人必须成为所选事件主角。所选事件未涉及此人就不强塞入画，未提供参考的其他角色不得复制此人的脸与装备。事件确实包含军队、使团、民众等群体时可按群体事实表现，不虚构特定姓名、精确人数或额外势力。\n" +
            "【纪事艺术再现】：场景依据所选事件与时代文化自主设计，允许有丰富的环境、行动、材质、使用痕迹和光影细节，作为非具名艺术再现而非实测现场。具体建筑与位置没有证据时不声称精确复原，不借用玩家当前地图、城镇或日期天气冒充事件所在地与时段。已确认的地貌、时间、人物关系及结果保持；计划不能画成已完成，围城不能自动画成攻陷。\n" +
            "【道具与纹章】：人物衣甲和武器以资料及事件为准，装备持有不等于正在使用。所有模式不画背盾；明确持盾动作才允许从属盾牌。纹章样图只用于事件中有依据的载体，不能为展示样图增添旗帜或盾牌，普通金属胸甲不硬印徽记。\n" +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            "【输出】：先输出【画作标题】4至12字作品名、【画作主题】一句简述所选事件、【人物行动】事件中主要参与方正在做什么的简短摘要。三项单独记录，不作为画面文字。随后只输出以下四段正面、具体、可直接绘制的画面描述，不写候选方案或解释，不规定总字数和段落比例：\n" +
            "【人物与镜头】先交代事件行动和参与方的互动，再选定同一时刻的观察机位、景别与取景范围。按实际入镜情况转写人物识别点，细节服务事件，不罗列衣甲目录。\n" +
            "【场景空间】环境要让事件可辨认，具体组织发生区域、相关构造、地貌或陈设及材质；保留有叙事价值的细节和空间延伸，自主选择组织方式，不套场所或道具清单。\n" +
            "【光影与色彩】以统一光源、材质受光和环境反光塑造事件氛围，主体与环境同属一幅画，暗部保留内容，光照表达不改变已知时段。\n" +
            "【空间关系】明确所选事件中参与方之间及其与环境的方位、高低、远近、遮挡与接触，所有关系在同一机位下成立；未确认的精确距离不作事实断言。\n" +
            "【交付前核对】：核对所选事件证据、人物归属、行动与结果；画面应能说明发生了什么，而不仅能认出某个人。删除跨事件拼接、姿态与支撑冲突以及重复修辞，只交付修正后的画面，保留艺术创造力，不输出检查过程。";

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData = null, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), WrapSingle(base64ImageData), options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), WrapSingle(base64ImageData), options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), referenceImages, options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), referenceImages, options, cancellationToken);
        }

        private static System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> WrapSingle(string base64ImageData)
        {
            if (string.IsNullOrWhiteSpace(base64ImageData)) return null;
            return new[] { new IllustrationReferenceImage(base64ImageData, "游戏内真实画面参考", IllustrationReferenceKind.Scene) };
        }

        public static async Task<string> ExpandToDetailedPromptAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return (await CreateDirectionAsync(plan, referenceImages, options, cancellationToken).ConfigureAwait(false)).Prompt;
        }

        public static async Task<IllustrationDirection> CreateDirectionAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) })
                return await CreateDirectionWithClientAsync(plan, referenceImages, options, client, cancellationToken).ConfigureAwait(false);
        }

        // The caller owns client; the same request/parse path is exercised by the offline HTTP audit.
        internal static async Task<IllustrationDirection> CreateDirectionWithClientAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, HttpClient client, CancellationToken cancellationToken)
        {
            plan = plan ?? new IllustrationPromptPlan("通用插画", string.Empty, string.Empty);
            if (options != null && !options.EnableMultimodalVision)
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
            string fallbackReason = "导演未启用或接口未配置";
            DirectorResponse reply = null;
            try
            {
                if (options != null && options.EnableLlmPromptExpansion && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))
                {
                    reply = await CallLlmDirectorResponseAsync(plan, options, referenceImages, client, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(reply.FailureReason))
                    {
                        direction = ResolveDirection(reply.Content, plan, options);
                        ApplyResponseMetadata(direction, reply);
                        return RecordDirection(direction);
                    }
                    fallbackReason = reply.FailureReason;
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
                fallbackReason = "导演请求超时";
            }
            catch (Exception ex)
            {
                // Raw provider errors may contain request text or credentials; diagnostics handle the raw response.
                fallbackReason = "导演请求失败（" + ex.GetType().Name + "）";
            }

            string rulePrompt = SynthesizeRuleBasedPrompt(plan, options);
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Using rule-based prompt ({rulePrompt.Length} chars): {Preview(rulePrompt, 120)}");
            direction = new IllustrationDirection
            {
                Prompt = rulePrompt,
                Theme = "人物与情境",
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
                direction.FallbackReason = "接口明确不支持图片输入，已尝试一次文字构思" +
                    (string.IsNullOrWhiteSpace(direction.FallbackReason) ? string.Empty : "；" + direction.FallbackReason);
        }

        internal static string ComposeFinalPrompt(string directorPrompt, string hardFacts = null, bool isSinglePortrait = false, bool isConversation = false, bool isWeeklyReport = false)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(directorPrompt))
            {
                sb.AppendLine(directorPrompt.Trim());
            }
            if (!string.IsNullOrWhiteSpace(hardFacts))
            {
                sb.AppendLine().AppendLine("【不可改写的核心事实】");
                sb.Append(hardFacts.Trim());
            }
            sb.AppendLine().Append(VisualFidelityRules.GetEssentialContract(isSinglePortrait, isConversation, isWeeklyReport));
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
                direction.Theme = "人物与情境";
                direction.ActionSummary = string.Empty;
            }
            if (string.IsNullOrWhiteSpace(direction.ActionSummary))
                direction.ActionSummary = IllustrationDirection.ExtractActionSummary(direction.Prompt);
            if (string.IsNullOrWhiteSpace(direction.Theme)) direction.Theme = "人物与情境";
            return direction;
        }

        private static string ResolveDirectorBody(string output, IllustrationPromptPlan plan, IllustrationOptions options, out string fallbackReason)
        {
            fallbackReason = string.Empty;
            output = output ?? string.Empty;
            if (ViolatesShieldVisibility(output, plan) || ViolatesPortraitComposition(output, plan))
            {
                fallbackReason = "导演输出含无依据盾牌、旗帜或不合要求的肖像动作";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Unsupported shield/portrait props rejected; using local portrait fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            if (NarrativeFactRouter.HasNarrativeEcho(output, plan.DirectorOnlyFacts, plan.HardFacts))
            {
                fallbackReason = "导演输出复述叙事原文，未转化为画面";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Narrative echo detected; using local visual-fact fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            bool isSingle = plan?.Mode?.Contains("百科") == true || plan?.Mode?.Contains("肖像") == true;
            if (!HasRequiredSceneDescription(output))
            {
                fallbackReason = string.IsNullOrWhiteSpace(output) ? "导演返回空正文" : "导演正文缺少完整场景、光线与空间描述，或构图不合要求";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Missing scene/light/spatial direction; using local scene fallback without retry.");
                // Preserve a short usable visual paraphrase while supplying the missing scene sections.
                // Do not retain equipment lists or long non-conforming output as the main direction.
                if (output.Length <= 120 && System.Text.RegularExpressions.Regex.IsMatch(output, "远景|近景|中景|过肩|俯拍|仰拍") &&
                    !System.Text.RegularExpressions.Regex.IsMatch(output, "纯黑|漆黑|全黑|黑色背景|黑幕|black background", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return ComposeFinalPrompt(BuildLocalSceneDirection(plan) + "\n可保留的动作与镜头：" + output +
                        "\n构图方向：" + plan.ArtDirection + "\n画风偏好：" + BuildImageStylePreference(options), hardFacts: plan.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true);
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            return ComposeFinalPrompt(output, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true);
        }

        private static readonly string[] RequiredSectionPatterns = new[]
        {
            @"(?:人物与镜头|人物镜头|角色与镜头|人物与构图)",
            @"(?:场景空间|场景环境|环境空间|空间与环境)",
            @"(?:光线与色彩|光影与色彩|光线与光影|光照与色彩|光影色彩)",
            @"(?:空间关系|空间透视|透视与空间|透视关系|空间与景深)"
        };

        internal static bool HasRequiredSceneDescription(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            if (System.Text.RegularExpressions.Regex.IsMatch(output,
                @"背景[^。！？\r\n]{0,8}(?:纯黑|漆黑|全黑)|纯黑背景|黑幕|(?:pure|solid|pitch)[ -]?black background",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return false;
            int[] minimum = { 8, 12, 10, 10 };
            for (int i = 0; i < RequiredSectionPatterns.Length; i++)
            {
                var match = System.Text.RegularExpressions.Regex.Match(output,
                    "【" + RequiredSectionPatterns[i] + "】([^【]+)");
                string value = match.Success ? match.Groups[1].Value.Trim() : string.Empty;
                // Structural guard only: padding and section ratios do not establish
                // action/spatial correctness. Keep a small non-whitespace minimum.
                int length = 0;
                foreach (char character in value)
                    if (!char.IsWhiteSpace(character)) length++;
                if (length < minimum[i]) return false;
            }
            return true;
        }

        internal static string BuildLocalSceneDirection(IllustrationPromptPlan plan)
        {
            if (plan?.IsWeeklyReport == true)
            {
                return "【人物与镜头】以本期一项已确认事件的行动为画面中心，参与方的活动与相互作用构成主体，人物形象按身份资料对应；取景让事件本身清楚可读。" +
                    "【场景空间】所选事件发生于与其时代文化相容的非具名艺术再现空间，环境构造、材质和使用痕迹服务于事件，不借用玩家当前所在地。" +
                    "【光线与色彩】参与方与环境处于统一照明和色彩关系中，受光、投影与环境反光连贯，暗部保留事件细节，已知时段保持。" +
                    "【空间关系】同一事件的参与方、行动对象与相关环境相互关联，前后遮挡和接触关系自然，保持事实中的高低、位置和事件结果。";
            }
            if (plan?.Mode == "人物百科纪事")
            {
                return "【人物与镜头】人物容貌与现有衣着按事实和身份参考呈现，神情自然，体态与画面支撑关系一致。" +
                    "【场景空间】非具名艺术布景：有细腻纹理的环境色面向后延伸，明暗交界与柔和转折形成可辨认的空间层次，不指代人物真实所在地。" +
                    "【光线与色彩】柔和环境光沿人物与背景连续铺展，受光面与反射填充形成自然过渡，暗部保留材质纹理、固有色与纵深，人物和背景处于统一曝光中。" +
                    "【空间关系】人物与周围空间通过遮挡、景深及色彩过渡自然衔接，近处纹理清楚，远处层次柔和退开。";
            }

            return "【人物与镜头】按事实区人物数量与实际动作选择清楚的中近景关系，装备只按真实快照概括，不列成展示目录。" +
                "【场景空间】采用现场参考图或事实中已确认的环境形体，交代近处地面/空间基面与远处环境的延伸；未知部分保持非地标化的有层次环境色面，不凭空添加建筑、陈设或事件。" +
                "【光线与色彩】遵循已有时间与现场光源，用合理环境光和反射填充保留人物与背景细节；亮部不溢出、暗部可辨认材质，色彩不被整体黑影吞没。" +
                "【空间关系】保留已确认的人物距离、朝向和地形关系，近景、中景与后景用遮挡和景深区分；未知位置不作具体地名或事件断言，背景仍应可辨。";
        }

        internal static bool ViolatesShieldVisibility(string output, IllustrationPromptPlan plan)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            if (plan?.Mode == "人物百科纪事")
            {
                // 百科模式拦截实际出现、手持、携带或展示盾牌的描述，不误杀“不画盾牌/未持盾”或非盾牌词汇
                if (System.Text.RegularExpressions.Regex.IsMatch(output,
                    @"(?<!(?:不|未|无|绝不|并未|禁止|严禁|不画)\s*)(?:手持|持有|佩戴|佩挂|握持|手握|持着|手提|立有|摆放|展示|带着|装备|举着|提着|背负|背着)[^。！？\r\n]{0,20}(?:盾牌|大盾|圆盾|风筝盾|骑兵盾|重盾|塔盾|精铁盾|木盾|皮盾|shield)|(?<!(?:不|未|无|绝不|并未|禁止|严禁|不画)\s*)(?:盾牌|大盾|圆盾|风筝盾|骑兵盾|重盾|塔盾|shield)[^。！？\r\n]{0,20}(?:格外醒目|立在|置于|握在|持在|拿在|背在)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
            }
            return System.Text.RegularExpressions.Regex.IsMatch(output,
                @"(?<!(?:不|未|无|绝不|并未|禁止|严禁|不画)\s*)(?:背负|背着|背在|背后|背上|身后|肩后)[^。！？\r\n]{0,40}盾|(?<!(?:不|未|无|绝不|并未|禁止|严禁|不画)\s*)盾[^。！？\r\n]{0,40}(?:背负|背着|背在|背后|背上|身后|肩后)|shield[^.!?\r\n]{0,60}(?:back|behind|shoulders)|(?:back|behind|shoulders)[^.!?\r\n]{0,60}shield",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        internal static bool ViolatesPortraitComposition(string output, IllustrationPromptPlan plan)
        {
            if (plan?.Mode != "人物百科纪事" || string.IsNullOrWhiteSpace(output)) return false;
            // 只拦截真正违规的夸张动作或大纛举旗，不误杀周围环境道具描述
            foreach (string token in new[] { "一手举旗", "大纛", "手撑桌", "双手撑桌", "夸张扭身", "扭转躯干", "倚案摆拍" })
                if (output.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>
        /// 重绘变体指令：注入艺术指导区，要求导演刻意切换景别/机位/瞬间，
        /// 避免与上一版雷同构图。redrawIndex 从 2 开始（首次绘制不注入）。
        /// </summary>
        public static string BuildRedrawVariationDirective(int redrawIndex)
        {
            return $"【重绘变体 · 第 {redrawIndex} 次绘制】先检查本次行动意图与近期作品，再推导姿态、手部动作、视线和镜头；减少双手下垂展示姿势的重复。" +
                   "会话或事件已有行动事实时保持该事实，以取景或叙事瞬间变化；百科结合近期行动选择不同的自然瞬间，不强迫复杂动作，也不禁止有情境依据的站立；" +
                   "【人物与装备细节绝对锁定】：人物真实装备、相貌、发色与纹章细节须与参考图完全一致，不得因重绘而增减改动！";
        }

        private static string Preview(string value, int maxChars)
        {
            string text = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length <= maxChars ? text : text.Substring(0, maxChars) + "...";
        }

        internal static bool ResolveChatConfigForSnapshot(IllustratorSettings settings, out string baseUrl, out string apiKey, out string model)
        {
            return TryResolveChatConfig(settings, out baseUrl, out apiKey, out model);
        }

        private static bool TryResolveChatConfig(IllustratorSettings settings, out string baseUrl, out string apiKey, out string model)
        {
            baseUrl = string.Empty;
            apiKey = string.Empty;
            model = string.Empty;

            // 1. 若玩家在 MCM 中配置了独立的导演 Base URL，优先使用导演配置
            if (settings != null && !string.IsNullOrWhiteSpace(settings.DirectorApiBaseUrl))
            {
                baseUrl = settings.DirectorApiBaseUrl.Trim();
                apiKey = (settings.DirectorApiKey ?? string.Empty).Trim();
                model = (settings.DirectorModelName ?? string.Empty).Trim();

                // 若 Key 留空，尝试从主模块正文 API 借用
                if (string.IsNullOrWhiteSpace(apiKey) && TryGetHostPrimaryChatConfig(out _, out string hostKey, out _))
                {
                    apiKey = hostKey;
                }
                // 若 Model 留空，尝试从主模块正文 API 借用或使用默认轻量模型
                if (string.IsNullOrWhiteSpace(model))
                {
                    if (TryGetHostPrimaryChatConfig(out _, out _, out string hostModel) && !string.IsNullOrWhiteSpace(hostModel))
                    {
                        model = hostModel;
                    }
                    else
                    {
                        model = "gpt-4o-mini";
                    }
                }
                TaleWorlds.Library.Debug.Print($"[VisualDirector] Using custom visual director API ({baseUrl}, model={model}).");
                return true;
            }

            // 2. 核心默认：导演 Base URL 留空时，全自动复用 AnimusForge 主模块正文对话 API 配置
            if (TryGetHostPrimaryChatConfig(out baseUrl, out apiKey, out string defaultHostModel))
            {
                // 若在 MCM 中指定了独立的导演模型名则使用，否则沿用主模块正文模型
                if (settings != null && !string.IsNullOrWhiteSpace(settings.DirectorModelName))
                {
                    model = settings.DirectorModelName.Trim();
                }
                else
                {
                    model = defaultHostModel;
                }
                TaleWorlds.Library.Debug.Print($"[VisualDirector] Auto-reused AnimusForge host primary chat API ({baseUrl}, model={model}).");
                return true;
            }

            // 3. 次选备用：若生图使用聚合平台（如硅基流动、OneAPI），尝试复用生图 Key 与轻量文本模型
            if (settings != null && !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
            {
                baseUrl = settings.ApiBaseUrl.Trim();
                apiKey = settings.ApiKey.Trim();
                model = !string.IsNullOrWhiteSpace(settings.DirectorModelName) ? settings.DirectorModelName.Trim() : "gpt-4o-mini";
                TaleWorlds.Library.Debug.Print($"[VisualDirector] Fallback-reused image API for director ({baseUrl}, model={model}).");
                return true;
            }

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
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) })
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
            if (reply.Truncated) reply.FailureReason = "导演输出达到令牌上限而截断，未采用残缺正文";
            else if (reason == "content_filter" || !string.IsNullOrWhiteSpace(message?["refusal"]?.ToString()))
                reply.FailureReason = "导演拒绝或过滤了本次输出";
            else if (reason.Length > 0 && reason != "stop" && reason != "end_turn" && reason != "completed")
                reply.FailureReason = "导演未正常结束正文";

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
                reply.FailureReason = "导演返回空正文";
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
            string requestText = "请依据游戏事实构思一个清晰、有变化且可直接绘制的瞬间。开放构图建议可以取舍，不要把建议改写成不存在的事实。" +
                (string.IsNullOrWhiteSpace(stylePreference) ? string.Empty : "\n【画风偏好】" + stylePreference) + "\n\n" + plan.BuildDirectorContext();
            if (textFallback)
                requestText += "\n【参考可用性】本次仅提供文字，图片输入不可用。未被文字确认的人物外观与真实现场细节保持未知，不声称已经看过参考图；艺术布景和事件艺术再现仍按本模式创作边界设计。";

            JObject userMessage;
            if (referenceImages != null && referenceImages.Count > 0)
            {
                var content = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = requestText + "\n参考图只约束其标签所指对象；不要把人物立绘背景或纹章画布误当成事件现场。"
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
                            ["text"] = "【参考图】" + reference.Label
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

            return new JObject
            {
                ["model"] = options.DirectorModelName,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = plan?.IsWeeklyReport == true ? WeeklyReportSystemPrompt : SystemPrompt },
                    userMessage
                },
                ["temperature"] = 0.85,
                ["max_tokens"] = options != null && options.DirectorMaxTokens > 0 ? options.DirectorMaxTokens : 1500
            };

        }

        private static async Task<DirectorResponse> CallLlmDirectorResponseAsync(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, HttpClient client, CancellationToken cancellationToken)
        {
            var reply = new DirectorResponse();
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(120));
                string endpoint = ResolveChatEndpoint(options.DirectorApiBaseUrl);
                GenerationDiagnostics.Current?.RegisterSecret(options.DirectorApiKey);
                try
                {
                    // Only an explicit unsupported-image response permits a second request.
                    // Both attempts share one deadline; truncation, empty output and transport errors do not retry.
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        bool hasImages = referenceImages != null && referenceImages.Count > 0;
                        JObject payload = BuildDirectorPayload(plan, options, referenceImages, reply.VisionUnsupported);
                        GenerationDiagnostics.Current?.RecordDirectorRequest(endpoint, options.DirectorModelName, payload, referenceImages);
                        using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                        {
                            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                            if (!string.IsNullOrWhiteSpace(options.DirectorApiKey))
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.DirectorApiKey);
                            TaleWorlds.Library.Debug.Print($"[VisualDirector] Sending request (model={options.DirectorModelName}, refImages={referenceImages?.Count ?? 0}, attempt={attempt + 1})...");
                            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false))
                            {
                                string responseBody = Encoding.UTF8.GetString(await ImagePayload.ReadBoundedAsync(response.Content, 1048576, deadline.Token).ConfigureAwait(false));
                                if (!response.IsSuccessStatusCode)
                                {
                                    GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, string.Empty);
                                    if (hasImages && attempt == 0 && ShouldRetryDirectorWithoutImages((int)response.StatusCode, responseBody))
                                    {
                                        reply.VisionUnsupported = true;
                                        referenceImages = null;
                                        continue;
                                    }
                                    reply.FailureReason = "导演接口返回 HTTP " + (int)response.StatusCode;
                                    return reply;
                                }
                                DirectorResponse parsed;
                                try { parsed = ParseDirectorResponse(responseBody); }
                                catch
                                {
                                    GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, string.Empty);
                                    throw;
                                }
                                GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, parsed.FinishReason);
                                parsed.VisionUnsupported = reply.VisionUnsupported;
                                return parsed;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (OperationCanceledException) { reply.FailureReason = "导演请求超时"; }
                catch (Exception ex) { reply.FailureReason = "导演请求或响应解析失败（" + ex.GetType().Name + "）"; }
                return reply;
            }
        }

        internal static bool ShouldRetryDirectorWithoutImages(int statusCode, string responseBody)
        {
            if (statusCode != 400 && statusCode != 415 && statusCode != 422 && statusCode != 500 && statusCode != 502 && statusCode != 503) return false;
            string text = (responseBody ?? string.Empty).ToLowerInvariant();
            if (text.Contains("content must be a string") || text.Contains("content should be a string")) return true;
            bool imageContext = text.Contains("image") || text.Contains("vision") || text.Contains("multimodal") || text.Contains("mmproj") || text.Contains("图片");
            bool explicitlyUnsupported = text.Contains("not support") || text.Contains("unsupported") ||
                text.Contains("text-only") || text.Contains("text only") || text.Contains("does not accept") ||
                text.Contains("cannot accept") || text.Contains("only support") || text.Contains("不支持") ||
                (text.Contains("mmproj") && (text.Contains("missing") || text.Contains("required")));
            return imageContext && explicitlyUnsupported;
        }

        private static string BuildDirectorStylePreference(IllustrationOptions options)
        {
            return IllustrationStylePresets.Resolve(options?.SelectedStyle, options?.CustomStylePrompt).DirectorPrompt;
        }

        private static string BuildImageStylePreference(IllustrationOptions options)
        {
            var preset = IllustrationStylePresets.Resolve(options?.SelectedStyle, options?.CustomStylePrompt);
            // Native vivid/natural styles have no image-text override; keep their short descriptive fallback.
            return preset.ImagePrompt ?? preset.DirectorPrompt;
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
            return SynthesizeRuleBasedPrompt(new IllustrationPromptPlan("通用插画", rawContext, string.Empty), null);
        }

        private static string SynthesizeRuleBasedPrompt(IllustrationPromptPlan plan, IllustrationOptions options)
        {
            var sb = new StringBuilder();
            string style = BuildImageStylePreference(options);
            sb.Append(BuildLocalSceneDirection(plan));
            if (!string.IsNullOrWhiteSpace(plan?.ArtDirection))
            {
                sb.AppendLine().Append("可参考但不必逐项照搬的构图方向：").Append(plan.ArtDirection.Trim());
            }
            if (!string.IsNullOrWhiteSpace(style))
            {
                sb.AppendLine().Append("画风偏好：").Append(style).Append('。');
            }
            bool isSingle = plan?.Mode?.Contains("百科") == true || plan?.Mode?.Contains("肖像") == true;
            return ComposeFinalPrompt(sb.ToString(), plan?.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true);
        }
    }
}
