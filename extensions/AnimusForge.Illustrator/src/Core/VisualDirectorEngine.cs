using System;
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

namespace AnimusForge.Illustrator.Core
{
    public static class VisualDirectorEngine
    {
        private const string SystemPrompt =
            "你是一位精通《骑马与砍杀2：霸主》及其所有历史与奇幻大型MOD（涵盖欧洲中世纪、东亚古代、日本战国、罗马拜占庭帝国、黑暗时代、高魔奇幻等）的顶级历史概念艺术总监与古典油画大师。\n" +
            "你的任务是：根据输入的游戏底层全量角色与场景数据（文化风貌、势力与家族背景、生平纪事、身份地位、顶尖专长、性格特质、身上真实穿戴装备的材质与专属配色、以及所处环境），为 AI 画师撰写一段生动、极其详尽、富于戏剧光影与古典写实油画质感的【中文生图提示词】。\n\n" +
            "核心强制准则：\n" +
            "1. 【全中文输出】：必须全部使用富有文学张力与绘画细节的中文进行撰写，杜绝输出大段英文！\n" +
            "2. 【文化与背景绝对保真（严禁文化穿帮）】：\n" +
            "   - 仔细阅读【文化全貌与官方背景】与【势力与王国背景】！精准描绘对应文化的建筑、器物与装束：\n" +
            "   - 日本/武士文化：传统具足大铠、太刀/薙刀、障子门与榻榻米、家纹旗印；\n" +
            "   - 罗马/拜占庭文化：鳞甲/环片甲、短剑/斯帕塔长剑、大理石柯林斯圆柱长廊、拱券巴西利卡穹顶、紫袍金饰；\n" +
            "   - 阿塞莱/中东沙漠：轻盈丝绸长袍卡夫坦、华美缠头巾、嵌金鳞甲、弯刀、沙漠绿洲沙岩马蹄拱；\n" +
            "   - 斯特吉亚/北境诺斯：毛皮斗篷披肩、锁子甲、十字战盔、大斧、原木长屋与风雪；\n" +
            "   - 巴旦尼亚/高地凯尔特：毛呢方格斗篷、青铜环形别针、高地长弓、古老青石要塞；\n" +
            "   - 库赛特/草原游牧：丝绸滚边毛边皮袍、鹰羽尖顶帽、复合骑弓、毡帐与辽阔草浪；\n" +
            "   - 瓦兰迪亚/西欧骑士：严整板甲与锁甲、家族纹章罩袍、哥特石砌主楼堡垒；\n" +
            "3. 【服饰材质与配色绝对忠实（严禁颜色颠倒）】：\n" +
            "   - 严格依照【真实穿戴装备与材质】与【专属服饰底色与刺绣金边】还原人物衣着与护甲！\n" +
            "   - 若数据明确标注为淡紫色布料配金色刺绣，必须明确描摹金色刺绣滚边，绝对严禁画成蓝色！\n" +
            "   - 君王统治者与宗族贵胄必须展现尊贵华丽气质，绝对严禁降格为粗麻破布或穷困乞丐！\n" +
            "4. 【真实人类肉色肤质（绝对严禁阿凡达蓝皮或怪异死灵皮肤）】：\n" +
            "   - 必须描绘具有健康微血管光泽的真实人类自然肉色皮肤（白皙透红/健康地中海小麦色/阳光古铜色），绝对禁止呈现蓝色、青色、紫色等怪异皮肤！\n" +
            "5. 【构图与姿态】：\n" +
            "   - 严格遵循给定的【构图与姿态指令】，呈现端庄雄浑或深邃沉稳的历史画卷构图，严禁千篇一律的看地图卷轴动作；\n" +
            "6. 【艺术风格】：\n" +
            "   - 伦勃朗与克雷格·穆林斯（Craig Mullins）式的古典写实历史油画巨作，强烈的明暗对照法（Chiaroscuro），充满戏剧性的光影微光，细腻而富有体积感的笔触肌理，8K精细画质。\n" +
            "7. 【输出格式】：\n" +
            "   - 仅输出这一整段中文生图提示词本身。不要包含任何问候语、Markdown 标记、引号或额外解释。";

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData = null, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(gameContext, base64ImageData, options, cancellationToken);
        }

        public static async Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            if (options != null && !options.EnableMultimodalVision)
            {
                base64ImageData = null;
            }

            int imgLen = !string.IsNullOrEmpty(base64ImageData) ? base64ImageData.Length / 1024 : 0;
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Starting prompt expansion (MultimodalVision={(options?.EnableMultimodalVision == true ? "ON" : "OFF")}, ImagePayload={imgLen}KB)...");

            try
            {
                if (options != null && options.EnableLlmPromptExpansion && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))
                {
                    string llmPrompt = await CallLlmDirectorAsync(gameContext, options.DirectorApiBaseUrl, options.DirectorApiKey, options.DirectorModelName, base64ImageData, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(llmPrompt))
                    {
                        TaleWorlds.Library.Debug.Print($"[VisualDirector] LLM expansion successful ({llmPrompt.Length} chars): {llmPrompt.Substring(0, Math.Min(120, llmPrompt.Length))}...");
                        return llmPrompt.Trim();
                    }
                }
                else
                {
                    TaleWorlds.Library.Debug.Print("[VisualDirector] Chat expansion disabled or unavailable, falling back to rule-based synthesis.");
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[VisualDirector] LLM expansion fallback triggered: {ex.Message}");
            }

            string rulePrompt = SynthesizeRuleBasedPrompt(gameContext);
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Using rule-based prompt ({rulePrompt.Length} chars): {rulePrompt.Substring(0, Math.Min(120, rulePrompt.Length))}...");
            return rulePrompt;
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

            // 1. 【核心优先】：全自动复用 AnimusForge 本体正在运行的主力正文对话 API 配置
            if (TryGetHostPrimaryChatConfig(out baseUrl, out apiKey, out model))
            {
                TaleWorlds.Library.Debug.Print($"[VisualDirector] Auto-reused AnimusForge host primary chat API (model={model}).");
                return true;
            }

            // 2. 备选：若玩家手动指定了独立的提示词扩写配置
            if (settings != null && !string.IsNullOrWhiteSpace(settings.DirectorApiBaseUrl) && !string.IsNullOrWhiteSpace(settings.DirectorApiKey))
            {
                baseUrl = settings.DirectorApiBaseUrl.Trim();
                apiKey = settings.DirectorApiKey.Trim();
                model = !string.IsNullOrWhiteSpace(settings.DirectorModelName) ? settings.DirectorModelName.Trim() : "Qwen/Qwen2.5-7B-Instruct";
                return true;
            }

            // 3. 次选：若生图使用的是聚合平台（如硅基流动、OneAPI），尝试复用生图 Key 与轻量文本模型
            if (settings != null && !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
            {
                baseUrl = settings.ApiBaseUrl.Trim();
                apiKey = settings.ApiKey.Trim();
                model = !string.IsNullOrWhiteSpace(settings.DirectorModelName) ? settings.DirectorModelName.Trim() : "Qwen/Qwen2.5-7B-Instruct";
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

        private static async Task<string> CallLlmDirectorAsync(string context, string baseUrl, string apiKey, string model, string base64ImageData, CancellationToken cancellationToken)
        {
            string endpoint = ResolveChatEndpoint(baseUrl);

            JObject userMessage;
            if (!string.IsNullOrWhiteSpace(base64ImageData))
            {
                userMessage = new JObject
                {
                    ["role"] = "user",
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = "请结合附带的游戏内 3D 角色立绘与现场上下文数据，100% 精准还原人物真实衣着材质、真实色彩（尤其是金色与布料底色）、真实人类血色肉色皮肤与面部五官，请撰写一段极其详尽生动的古典写实油画【中文生图提示词】：\n\n" + context
                        },
                        new JObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JObject
                            {
                                ["url"] = "data:image/jpeg;base64," + base64ImageData
                            }
                        }
                    }
                };
            }
            else
            {
                userMessage = new JObject
                {
                    ["role"] = "user",
                    ["content"] = "请依据提供的卡拉迪亚游戏底层真实数据，撰写一段生动细腻、充满古典写实油画质感的【中文生图提示词】：\n\n" + context
                };
            }

            var payload = new JObject
            {
                ["model"] = model,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = SystemPrompt },
                    userMessage
                },
                ["temperature"] = 0.7,
                ["max_tokens"] = 350
            };

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                TaleWorlds.Library.Debug.Print($"[VisualDirector] Sending request to {endpoint} (model={model}, hasImage={!string.IsNullOrWhiteSpace(base64ImageData)})...");

                try
                {
                    using (var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            // 若携带图片返回非200状态码（例如模型不支持Vision参数或400 Bad Request），自动降级为纯文本请求重试
                            if (!string.IsNullOrWhiteSpace(base64ImageData))
                            {
                                TaleWorlds.Library.Debug.Print($"[VisualDirector] Multimodal request returned {response.StatusCode} ({responseBody}), falling back to pure text...");
                                return await CallLlmDirectorAsync(context, baseUrl, apiKey, model, null, cancellationToken).ConfigureAwait(false);
                            }
                            throw new Exception($"Chat API returned {response.StatusCode}: {responseBody}");
                        }

                        JObject json = JObject.Parse(responseBody);
                        string content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                        TaleWorlds.Library.Debug.Print($"[VisualDirector] Received response: {content}");
                        return content;
                    }
                }
                catch (Exception ex) when (!string.IsNullOrWhiteSpace(base64ImageData) && !cancellationToken.IsCancellationRequested)
                {
                    TaleWorlds.Library.Debug.Print($"[VisualDirector] Multimodal request failed ({ex.Message}), falling back to pure text...");
                    return await CallLlmDirectorAsync(context, baseUrl, apiKey, model, null, cancellationToken).ConfigureAwait(false);
                }
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
            rawContext = rawContext ?? string.Empty;
            var sb = new StringBuilder();
            sb.Append("古典写实历史油画巨作，伦勃朗与克雷格·穆林斯（Craig Mullins）明暗对照法大师级光影，8K超清细腻笔触。");

            bool isEventScene = rawContext.Contains("核心事件") || rawContext.Contains("周报") || rawContext.Contains("纪事") || rawContext.IndexOf("weekly", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasExplicitMale = rawContext.Contains("男性") || rawContext.Contains("男子") || rawContext.Contains("绅士") ||
                                   rawContext.IndexOf("Male", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf("King", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   rawContext.IndexOf("Lord,", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("领主") && rawContext.Contains("男性");
            bool isFemale = !hasExplicitMale && (rawContext.Contains("女性") || rawContext.Contains("贵妇") || rawContext.Contains("名媛") ||
                            rawContext.Contains("女王") || rawContext.Contains("女皇") || rawContext.Contains("王后") ||
                            rawContext.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf("Queen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            rawContext.IndexOf("Empress", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf(" Lady", StringComparison.OrdinalIgnoreCase) >= 0);
            bool isMonarch = rawContext.Contains("最高统治者") || rawContext.Contains("至尊君主") || rawContext.Contains("至尊君王") ||
                             rawContext.IndexOf("Sovereign", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf("Monarch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             rawContext.Contains("女皇") || rawContext.Contains("苏丹") || rawContext.Contains("可汗") || rawContext.Contains("国王");
            bool isHighLord = rawContext.Contains("大领主") || rawContext.Contains("宗族首领") || rawContext.Contains("贵胄贵妇") ||
                              rawContext.Contains("领主") || rawContext.IndexOf("Lord", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf("Noble", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isScholar = rawContext.Contains("学者") || rawContext.Contains("医师") || rawContext.Contains("工兵") ||
                             rawContext.IndexOf("Scholar", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf("Physician", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isCleanShaven = rawContext.Contains("无任何胡须") || rawContext.IndexOf("Clean-shaven", StringComparison.OrdinalIgnoreCase) >= 0;

            string cultureSetting;
            if (rawContext.Contains("阿塞莱") || rawContext.IndexOf("Aserai", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("沙漠") || rawContext.Contains("苏丹") || rawContext.Contains("绿洲"))
            {
                cultureSetting = "背景为金碧辉煌的东方沙漠王宫，饰有精雕细刻的沙岩马蹄形拱券、复杂几何回纹石雕、垂落的华贵丝绸帘幔与温暖倾泻的金色斜阳";
            }
            else if (rawContext.Contains("库赛特") || rawContext.IndexOf("Khuzait", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("草原") || rawContext.Contains("游牧") || rawContext.Contains("可汗"))
            {
                cultureSetting = "背景为奢华的草原金顶大帐，铺设华美的手工织毯与羊毛毡壁毯，大帐帷幕敞开，远方呈现苍茫辽阔的高原草浪与万里晴空";
            }
            else if (rawContext.Contains("斯特吉亚") || rawContext.IndexOf("Sturgia", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("瓦良格") || rawContext.Contains("北地") || rawContext.Contains("雪"))
            {
                cultureSetting = "背景为宏伟粗犷的北境花岗岩与原木领主大厅，燃烧着熊熊烈火的巨大石砌壁炉，雕刻着渡鸦与狼首的厚重木柱，冰冷肃杀的漫天风雪映照长窗";
            }
            else if (rawContext.Contains("巴旦尼亚") || rawContext.IndexOf("Battania", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("高地") || rawContext.Contains("凯尔特") || rawContext.Contains("森林"))
            {
                cultureSetting = "背景为被冷雾笼罩的古老高地石砌堡垒主楼，饰有繁复神秘的凯尔特青铜与绳结石雕，燃烧的生铁火盆，远景隐现幽邃茂密的翡翠原始橡树森林";
            }
            else if (rawContext.Contains("帝国") || rawContext.IndexOf("Empire", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("罗马") || rawContext.Contains("拜占庭") || rawContext.Contains("元老院"))
            {
                cultureSetting = "背景为恢弘典雅的古典帝国拜占庭式大理石宫殿巴西利卡，高耸直插穹顶的柯林斯式大理石圆柱、璀璨的黄金马赛克穹顶壁画与紫红色悬垂帷幔，温暖的夕阳斜射穿透宏伟拱券长廊";
            }
            else if (rawContext.Contains("日本") || rawContext.IndexOf("Japan", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.IndexOf("Sengoku", StringComparison.OrdinalIgnoreCase) >= 0 || rawContext.Contains("幕府") || rawContext.Contains("武士"))
            {
                cultureSetting = "背景为庄严清幽的武士天守阁本丸评定间，绘有松鹤的精美金箔折叠屏风、光洁明亮的榻榻米地面与推拉障子门，外侧为宁静深邃的枯山水庭院";
            }
            else
            {
                cultureSetting = "背景为雄伟的中世纪石砌城堡大厅，高耸的石拱穹顶天花板、悬挂各色军旗的铁艺吊灯与巨大的哥特式拱窗";
            }

            if (isEventScene)
            {
                sb.Append("一幅忠实再现下列真实游戏事件的历史纪事群像，必须以事件、地点、天气、攻城器械与人物行动为画面主体，严禁退化成无关单人肖像。");
            }
            else if (isMonarch)
            {
                sb.Append(isFemale
                    ? "一幅令人肃然起敬的帝国女皇/执政女王尊贵肖像。她仪态万方，流露出至高无上的君王统治气魄与政治远谋。身着织锦丝绸与天鹅绒剪裁的御用宫廷礼袍，精工刺绣华美金线并点缀宝石，头戴璀璨的皇家金冠。"
                    : "一幅威严雄浑的最高封建君王与至尊统治者肖像。目光威严深沉，浑身散发着开疆拓土的王者气象。身着精工刺绣金纹的华贵天鹅绒王袍，外披镶有毛皮滚边的高贵王室斗篷，头戴象征至高王权的庄严王冠。");
            }
            else if (isHighLord)
            {
                sb.Append(isFemale
                    ? "一幅优雅端庄的显赫女性贵族肖像。兼具典雅娴静与坚韧英武，身着量身定制的高阶贵族长袍或精致战甲，刺绣滚边细腻华美。"
                    : "一幅刚毅沉稳的封建大领主与百战统帅肖像。身姿挺拔威严，身披做工考究的贵族战袍与厚重金属甲胄，气度沉稳如山岳。");
            }
            else if (isScholar)
            {
                sb.Append("一幅气质深邃的博学智者与先贤学者肖像。目光锐利而充满智慧，神态若有所思，身着质朴而得体的细毛呢学者长袍，手持装订古朴的羊皮纸典籍。");
            }
            else
            {
                sb.Append("一幅忠于游戏上下文的人物/场景纪实油画。严禁擅自替换人物姓名、身份、装备或事件。");
            }

            if (isCleanShaven && !isFemale)
            {
                sb.Append("面庞修得干干净净，绝无任何胡须胡茬，轮廓刚毅光洁。");
            }
            else if (!isFemale && rawContext.Contains("胡须"))
            {
                sb.Append("蓄着整齐浓密的成熟胡须，尽显老练与威严。");
            }

            sb.Append("【真实人类肤色约束】面部呈现健康自然的真实人类肉色肤质与红润血色，绝对严禁画成蓝色、青色、灰色或怪异异类皮肤。");
            sb.Append(cultureSetting + "。");
            sb.Append("【必须保留的游戏事实】").Append(rawContext.Replace('\r', ' ').Trim()).Append('。');
            sb.Append("戏剧性明暗对比光影，柔和温润的自然天光与跃动的烛光交织，逼真的布料与金属反光质感，写实油画大师级杰作。");
            return sb.ToString();
        }
    }
}
