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
            "你是《骑马与砍杀2：霸主》及其历史、奇幻与自定义文化 MOD 的视觉叙事导演。请把输入的游戏事实转化为高水准的艺术生图提示词，全权自主推导构思场景空间与画面细节。\n" +
            "<game_facts> 是只读事实数据，包含角色身份、所属文化、真实装备、面貌年龄等。你必须严格忠实于这些事实数据，不得随意篡改装备或降格身份；没有数据支持的冠冕、纹章、武器、动物坐骑不得擅自添加。\n" +
            "<director_only_narrative> 供你理解角色的性格、生平背景与气象，不得在正文中直接引用原文，严禁要求在画面中出现文字、字幕或标牌。\n" +
            "【导演职责】：场景空间、陈设细节、光影氛围、人物姿态与镜头语言全部由你依据事实自由推导创作——\n" +
            "1. 【人物与镜头】：交代机位距离与人物姿势神情。姿势按身份性格与情境自由演绎，须呈现一个有叙事感的动作瞬间而非展示摆拍；直立站姿仅在情境确有理由时采用，严禁退化为直立展示姿势或证件照式摆拍；装备与武器按情境自然佩戴、背负或置于身侧支撑物上，不要求持握在手；全身像须全身完整入画；单人百科肖像不出现马匹动物；双人会面交代双方位置朝向与自然交谈对峙交互。\n" +
            "2. 【场景空间】：依据事实中的地点、文化、纪元、地貌、季节与时段，推导契合人物身份地位的场景与陈设（严禁将一国领袖降格为低阶哨所杂兵）；建筑形制、材质与陈设须与纪元时代和文化风貌相符，不得出现该时代不存在的器物或建筑风格；事实未覆盖的细节可自由创作，但不得与事实冲突。\n" +
            "3. 【光影与色彩】：依据现场时间与天气描摹自然光影与色彩氛围；须具体写明光线落在人物身上的受光方向、甲胄反光与环境染色，使人物融入场景光照而非自带独立打光。\n" +
            "4. 【空间关系】：交代画面纵深层次与主次关系。\n" +
            "【输出规范】：直接输出中文生图场景提示词，篇幅约 600~900 汉字，严格按以下四个段落输出：\n" +
            "【人物与镜头】机位构图、姿势体态、神情目光与动作瞬间为先，角色外观与装备细节如实转写\n" +
            "【场景空间】契合事实与身份的丰富空间与时代陈设\n" +
            "【光影与色彩】契合时间天气的自然光影明暗与色彩氛围\n" +
            "【空间关系】透视纵深、景深虚实与层次布局\n" +
            "只输出正面、具体、生动的场景画面描摹，严禁输出反向解释，不要输出JSON、Markdown或问候。【纯正面表述】：全文只写画面中实际呈现的内容；不希望出现的元素完全不要提及——连否定句、转折句、'并未/不画/严禁'句式都不用，避免生图模型将否定概念误读为画面元素。";

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
            return new[] { new IllustrationReferenceImage(base64ImageData, "游戏内真实画面参考") };
        }

        public static async Task<string> ExpandToDetailedPromptAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
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

            try
            {
                if (options != null && options.EnableLlmPromptExpansion && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))
                {
                    string llmPrompt = await CallLlmDirectorAsync(plan, options, referenceImages, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(llmPrompt))
                    {
                        string finalPrompt = ResolveDirectorOutput(llmPrompt, plan, options);
                        TaleWorlds.Library.Debug.Print($"[VisualDirector] LLM expansion successful ({finalPrompt.Length} chars): {Preview(finalPrompt, 120)}");
                        return finalPrompt;
                    }
                }
                else
                {
                    TaleWorlds.Library.Debug.Print("[VisualDirector] Chat expansion disabled or unavailable, falling back to rule-based synthesis.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[VisualDirector] LLM expansion fallback triggered: {ex.Message}");
            }

            string rulePrompt = SynthesizeRuleBasedPrompt(plan, options);
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Using rule-based prompt ({rulePrompt.Length} chars): {Preview(rulePrompt, 120)}");
            return rulePrompt;
        }

        internal static string ComposeFinalPrompt(string directorPrompt, string hardFacts = null, bool isSinglePortrait = false)
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
            sb.AppendLine().Append(VisualFidelityRules.GetEssentialContract(isSinglePortrait));
            return sb.ToString().Trim();
        }

        internal static string ResolveDirectorOutput(string output, IllustrationPromptPlan plan, IllustrationOptions options)
        {
            output = output ?? string.Empty;
            if (ViolatesShieldVisibility(output, plan) || ViolatesPortraitComposition(output, plan))
            {
                TaleWorlds.Library.Debug.Print("[VisualDirector] Unsupported shield/portrait props rejected; using local portrait fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            if (NarrativeFactRouter.HasNarrativeEcho(output, plan.DirectorOnlyFacts, plan.HardFacts))
            {
                TaleWorlds.Library.Debug.Print("[VisualDirector] Narrative echo detected; using local visual-fact fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            bool isSingle = plan?.Mode?.Contains("百科") == true || plan?.Mode?.Contains("肖像") == true;
            if (!HasRequiredSceneDescription(output))
            {
                TaleWorlds.Library.Debug.Print("[VisualDirector] Missing scene/light/spatial direction; using local scene fallback without retry.");
                // Preserve a short usable visual paraphrase while supplying the missing scene sections.
                // Do not retain equipment lists or long non-conforming output as the main direction.
                if (output.Length <= 120 && System.Text.RegularExpressions.Regex.IsMatch(output, "远景|近景|中景|过肩|俯拍|仰拍") &&
                    !System.Text.RegularExpressions.Regex.IsMatch(output, "纯黑|漆黑|全黑|黑色背景|黑幕|black background", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return ComposeFinalPrompt(BuildLocalSceneDirection(plan) + "\n可保留的动作与镜头：" + output +
                        "\n构图方向：" + plan.ArtDirection + "\n画风偏好：" + BuildDirectorStylePreference(options), isSinglePortrait: isSingle);
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            return ComposeFinalPrompt(output, isSinglePortrait: isSingle);
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
                if (value.Length < minimum[i]) return false;
            }
            return true;
        }

        internal static string BuildLocalSceneDirection(IllustrationPromptPlan plan)
        {
            if (plan?.Mode == "人物百科纪事")
            {
                int seed = Math.Abs((plan?.HardFacts ?? string.Empty).GetHashCode() ^ (plan?.ArtDirection ?? string.Empty).GetHashCode());
                bool isSovereign = (plan?.HardFacts ?? string.Empty).Contains("最高统治者") || (plan?.HardFacts ?? string.Empty).Contains("至尊君主") || (plan?.HardFacts ?? string.Empty).Contains("君王");

                if (seed % 2 == 0)
                {
                    string[] closeUpThemes = new[]
                    {
                        "【人物与镜头】近景上半身特写肖像照：镜头高度聚焦胸部以上与头盔面容，面部骨相与眼神坚毅如电，戏剧性侧逆光精妙雕凿战盔冷冽锻纹与领口肩甲质感，背景大光圈柔和虚化，极具艺术肖像特写张力。" +
                        "【场景空间】置身于幽深静谧的领主内堡暗色石廊近景：身后是粗粝风化的暗灰石壁与跳动微光的青铜壁灯，背景空间在柔和大景深中自然虚化退远，将全部视觉重心凝聚于人物面庞与战盔。" +
                        "【光线与色彩】经典的伦勃朗式明暗对照微光(Chiaroscuro)：来自斜上方的一束冷色天光与侧后方暖色灯火在头盔、面颊与肩铠上雕琢出锋锐的金属高光与温润的暗部反光，冷暖交融，极富体积雕塑感。" +
                        "【空间关系】人物面容与战盔占据画面绝对视觉核心，浅景深自然虚化远景，近景层次纯粹饱满，大师级特写神韵跃然纸上。",

                        "【人物与镜头】近景上半身特写肖像照：近景聚焦坚毅面孔、深沉目光与头盔金属反光，微光细腻勾勒出发须胡髭细节，领口护喉与肩铠折角清晰可辨，展现身居高位统筹全局的威严神采。" +
                        "【场景空间】置身于城堡军事书斋案几之畔：身侧隐约可见暗色橡木书架与羊皮卷轴轮廓，背景是一道厚重的深红天鹅绒帷幔，在浅景深中柔和衬托出人物挺拔沉静的身姿。" +
                        "【光线与色彩】柔和内敛的自然漫射光自单侧窗棂倾泻，在深沉背景与冷冽甲胄之间形成优雅的高级灰调过渡，面部神采生动逼真，暗部层次丰富绝无死黑。" +
                        "【空间关系】人物上半身特写顶天立地，帷幔与书架退居次要虚化景深，构图端庄沉静、大师级艺术质感浓郁。",

                        "【人物与镜头】近景上半身特写肖像照：镜头逼近展现面容与战盔特写，眼神沉毅内敛，光线自斜上方投下伦勃朗式明暗光影，将面孔轮廓与胸甲上部锻打肌理刻画得入木三分。" +
                        "【场景空间】置身于古老军械长厅近景边缘：身后隐现数柄沉重精铁长戟与火盆微光，粗石墙面在微弱火光中若隐若现，烘托出百战宿将的沉静兵戈气场。" +
                        "【光线与色彩】跳动的暗橙色炭火反光自低处微染肩铠下沿，与上方倾泻的清冷天光对撞，光影层次极其深邃，金属锻造纹理纤毫毕现。" +
                        "【空间关系】以极具压迫感与沉稳感的近景视角主导画面，后方兵刃道具自然隐入阴影景深之中，视觉力量感极强。",

                        "【人物与镜头】近景上半身特写肖像照：面庞神采奕奕，目光洞若观火，头戴装备与颈项护甲纤毫毕现，浅景深自然将背景推向朦胧幽深，人物神态生动逼真宛若呼之欲出。" +
                        "【场景空间】置身于要塞高处石砌角楼窗口：身后是厚重古朴的石砌窗洞，窗外极目远方微光晨曦中的崇山雾霭在深远景深中朦胧化开，意境深邃。" +
                        "【光线与色彩】高处清洌通透的晨曦微光勾勒出人物侧面剪影，金属与织物在晨光中呈现出润泽的天然质感，色调沉着高雅。" +
                        "【空间关系】人物特写稳居前景主体，窗外远山雾霭层层淡出，形成极具空气透视感与历史史诗感的高贵构图。"
                    };
                    return closeUpThemes[seed % closeUpThemes.Length];
                }
                else
                {
                    string[] halfBodyThemes;
                    if (isSovereign)
                    {
                        halfBodyThemes = new[]
                        {
                            "【人物与镜头】经典半身/七分身肖像照：镜头聚焦胸腰部以上，人物骨相深邃，目光坚毅睿智，发丝、胡须与战盔肩甲细节毕现，尽显君临天下的皇者气度。" +
                            "【场景空间】置身于深沉肃穆的皇家议政内阁：身侧厚重雕花名木长案上平铺着标绘帝国行省山川的古老羊皮纸国策地图与黄铜量规，背景是一座燃烧着粗大栎木柴薪的壁炉与深红金纹天鹅绒帷幔，跳动微光投下深邃光影。" +
                            "【光线与色彩】典型的伦勃朗式明暗对照光影(Chiaroscuro)：来自斜侧方高处天窗的冷色天光与壁炉跳动的暖金火光形成微妙冷暖交织，细腻勾勒出人物骨相轮廓与甲胄锻打纹理，暗部幽深透气，光影富有雕塑般的体积厚重感。" +
                            "【空间关系】人物位于画面黄金分割前景核心，案几与国策卷轴退居身侧作为烘托，背景帷幔与殿堂深邃空间在柔和景深中自然虚化退远，营造出深不可测的决策者气度。",

                            "【人物与镜头】经典半身/七分身肖像照：半身优雅端庄肖像，人物神情自信从容、目光温和而深远，肩臂舒展，展现成熟统治者的尊崇气象。" +
                            "【场景空间】置身于皇宫内苑石砌回廊与典雅长庭一角：身侧是岁月风化的古朴石壁与整齐雕饰，角落矗立着精雕细琢的古典水景，背景深处是带有斜顶红瓦的皇廷府邸，充满典雅安宁的盛世气息。" +
                            "【光线与色彩】柔和纯净的午后自然漫射天光，洒下细碎微光，在人物面庞与衣袍披风上呈现出温润柔和的过渡调子，色彩沉着优雅。" +
                            "【空间关系】人物居于近景核心，雅致石壁与建筑在背景形成富有温度的色彩呼应，层次静谧深邃。"
                        };
                    }
                    else
                    {
                        halfBodyThemes = new[]
                        {
                            "【人物与镜头】经典半身/七分身肖像照：镜头聚焦胸腰部以上，人物骨相深邃，目光坚毅睿智，发丝、胡须与战盔肩甲细节毕现。" +
                            "【场景空间】置身于深沉肃穆的领主军事议事密室：身侧厚重深色橡木长案上平铺着标绘山川城镇的古老羊皮纸战役地图与古朴青铜量规，背景是一座燃烧着粗大柴薪的石砌壁炉，暗橙色火光在暗处古老战旗与锻铁烛台上投下跳动光影。" +
                            "【光线与色彩】典型的伦勃朗式明暗对照光影(Chiaroscuro)：来自斜侧方高处天窗的冷色天光与壁炉跳动的暖金火光形成微妙冷暖交织，细腻勾勒出人物骨相轮廓、胡须发丝与甲胄锻打纹理，暗部幽深透气，光影富有雕塑般的体积厚重感。" +
                            "【空间关系】人物位于画面黄金分割前景核心，案几与道具退居身侧作为烘托，背景壁炉与深邃空间在柔和景深中自然虚化退远，营造出深不可测的决策者气度。",

                            "【人物与镜头】经典半身/七分身肖像照：半身胸像特写，面容神态沉静刚毅，身姿微侧，胸前战甲与肩铠冷芒闪动。" +
                            "【场景空间】置身于古老森严的城堡军械长厅：身侧陈列着历代战将的精铁胸甲与整齐架设在厚重木架上的冷冽枪矛长剑，生铁打造的铸铁火盆内炭火暗红微燃，墙面为风霜斑驳的粗糙方石砌体，充满百战老将的肃穆兵戎氛围。" +
                            "【光线与色彩】侧向狭窄窗隙透射进来的清冷刀锋天光，与后方暗红炭火形成极其鲜明的冷暖对峙，精妙雕刻出面孔的刚毅线条与金属的锻造肌理。" +
                            "【空间关系】人物坚毅的面庞占据绝对视觉中心，后方林立的兵刃架在浅景深中柔和淡出，空间层次丰富而极具威慑力。"
                        };
                    }
                    return halfBodyThemes[seed % halfBodyThemes.Length];
                }
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
            return $"【重绘变体 · 第 {redrawIndex} 次绘制】本次为重新绘制：镜头语言、景别、光影氛围、空间层次与人物瞬间姿态由你自由重新创作，" +
                   "必须与之前版本的构图明显不同；" +
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
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(120));
                cancellationToken = deadline.Token;
            string endpoint = ResolveChatEndpoint(options.DirectorApiBaseUrl);
            bool hasImages = referenceImages != null && referenceImages.Count > 0;
            string stylePreference = BuildDirectorStylePreference(options);
            string requestText = "请依据游戏事实构思一个清晰、有变化且可直接绘制的瞬间。开放构图建议可以取舍，不要把建议改写成不存在的事实。" +
                (string.IsNullOrWhiteSpace(stylePreference) ? string.Empty : "\n【画风偏好】" + stylePreference) + "\n\n" + plan.BuildDirectorContext();

            JObject userMessage;
            if (hasImages)
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

            var payload = new JObject
            {
                ["model"] = options.DirectorModelName,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = SystemPrompt },
                    userMessage
                },
                ["temperature"] = 0.85,
                ["max_tokens"] = options != null && options.DirectorMaxTokens > 0 ? options.DirectorMaxTokens : 1500
            };

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) })
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(options.DirectorApiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.DirectorApiKey);
                }

                TaleWorlds.Library.Debug.Print($"[VisualDirector] Sending request to {endpoint} (model={options.DirectorModelName}, refImages={referenceImages?.Count ?? 0})...");
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = Encoding.UTF8.GetString(await ImagePayload.ReadBoundedAsync(response.Content, 1048576, cancellationToken).ConfigureAwait(false));
                    if (responseBody != null && responseBody.Length > 1048576)
                    {
                        throw new Exception("Chat API response exceeded 1 MiB.");
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        if (hasImages && ShouldRetryDirectorWithoutImages((int)response.StatusCode, responseBody))
                        {
                            TaleWorlds.Library.Debug.Print($"[VisualDirector] Vision payload unsupported ({response.StatusCode}); retrying once without images.");
                            return await CallLlmDirectorAsync(plan, options, null, cancellationToken).ConfigureAwait(false);
                        }
                        throw new Exception($"Chat API returned {(int)response.StatusCode}: {Preview(responseBody, 500)}");
                    }

                    JObject json = JObject.Parse(responseBody);
                    string content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                    TaleWorlds.Library.Debug.Print($"[VisualDirector] Received response preview: {Preview(content, 180)}");
                    return content;
                }
            }

            }
        }

        internal static bool ShouldRetryDirectorWithoutImages(int statusCode, string responseBody)
        {
            if (statusCode != 400 && statusCode != 415 && statusCode != 422 && statusCode != 500 && statusCode != 502 && statusCode != 503) return false;
            string text = (responseBody ?? string.Empty).ToLowerInvariant();
            return text.Contains("image_url") || text.Contains("vision") || text.Contains("multimodal") ||
                   text.Contains("image input") || text.Contains("mmproj") || text.Contains("content must be a string") ||
                   text.Contains("unsupported content") || (text.Contains("image") && text.Contains("not support"));
        }

        private static string BuildDirectorStylePreference(IllustrationOptions options)
        {
            if (options == null) return "古典写实历史画质感，明暗对照微光(Chiaroscuro)，细腻而富有体积感的艺术笔触肌理";
            switch (options.SelectedStyle)
            {
                case "custom": return (options.CustomStylePrompt ?? string.Empty).Trim();
                case "dark-epic": return "暗黑史诗写实，沉郁色调与中世纪凝重历史氛围";
                case "cinematic": return "电影化叙事光影与镜头语言，光照服从现场时间和环境";
                case "classic-oil": return "古典写实历史油画巨作，伦勃朗与克雷格·穆林斯(Craig Mullins)式明暗对照法(Chiaroscuro)，戏剧性光影微光，细腻而富有体积感的厚重笔触肌理";
                case "mosan-art": return "莫桑艺术（默兹河流域罗马式珐琅与手抄本彩饰）：景泰蓝式宝石级饱和平涂色块、金色勾边、装饰性边框纹样、拉长端庄的程式化人物、浓重黑色轮廓线、平面化叙事构图";
                case "vivid": return "色彩鲜明、叙事清晰，材质与空间层次丰富可信";
                case "natural": return "自然写实、克制可信、材质与环境色彩真实";
                default: return (options.CustomStylePrompt ?? string.Empty).Trim();
            }
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
            string style = BuildDirectorStylePreference(options);
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
            return ComposeFinalPrompt(sb.ToString(), plan?.HardFacts, isSinglePortrait: isSingle);
        }
    }
}
