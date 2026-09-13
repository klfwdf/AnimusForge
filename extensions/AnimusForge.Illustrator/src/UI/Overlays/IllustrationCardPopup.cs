using System;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Context;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.UI.Overlays
{
    public sealed class IllustrationCardPopup
    {
        private static IllustrationCardPopup _activeInstance;
        private readonly ScreenBase _screen;
        private readonly MovableGauntletLayer _layer;
        private readonly IllustrationCardVM _dataSource;
        private readonly IllustrationScope _scope;
        private readonly string _category;
        private readonly string _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
        private string _activeSpriteName;
        private bool _closed;

        public static bool IsOpen => _activeInstance != null;

        private IllustrationCardPopup(ScreenBase screen, string movieName, string category, Action onRegenerate)
        {
            _screen = screen;
            _category = category;
            _dataSource = new IllustrationCardVM(Close, onRegenerate);
            var layer = new MovableGauntletLayer("IllustrationCardOverlay", 4015, false);
            var movieIdentifier = layer.LoadMovie(movieName, _dataSource);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
            _layer = layer;
            _scope = new IllustrationScope(screen, category, Close);

            try
            {
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            }
            catch
            {
            }
        }

        public static void ShowForEncyclopedia(Hero hero, Widget tableauWidget)
        {
            IllustratorRuntime.AssertMainThread();
            if (hero == null || !IllustratorRuntime.IsEnabled("encyclopedia")) return;
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null) return;

            try
            {
                string preCapturedBase64 = ScreenCaptureHelper.CaptureWidgetBase64(tableauWidget, 768);

                string offscreenTempDir = null;
                string offscreenPrefix = null;
                Task<string> bannerTask = null;
                if (IllustratorRuntime.CaptureOptions()?.EnableOffscreenRendering == true)
                {
                    var view = ScreenCaptureHelper.ResolveTableauView(tableauWidget);
                    if (view != null)
                    {
                        ScreenCaptureHelper.TriggerTableauViewSave(view, out offscreenTempDir, out offscreenPrefix);
                    }
                    var clanBanner = hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner;
                    if (clanBanner != null)
                    {
                        bannerTask = ScreenCaptureHelper.ExtractBannerOffscreenAsync(clanBanner);
                    }
                }

                _activeInstance?.Close();
                var popup = new IllustrationCardPopup(topScreen, "EncyclopediaIllustrationOverlay", "encyclopedia", () =>
                {
                    string redrawDir = null;
                    string redrawPrefix = null;
                    Task<string> redrawBannerTask = null;
                    if (IllustratorRuntime.CaptureOptions()?.EnableOffscreenRendering == true)
                    {
                        var view = ScreenCaptureHelper.ResolveTableauView(tableauWidget);
                        if (view != null)
                        {
                            ScreenCaptureHelper.TriggerTableauViewSave(view, out redrawDir, out redrawPrefix);
                        }
                        var clanBanner = hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner;
                        if (clanBanner != null)
                        {
                            redrawBannerTask = ScreenCaptureHelper.ExtractBannerOffscreenAsync(clanBanner);
                        }
                    }
                    _activeInstance?.ExecuteEncyclopediaGeneration(hero, tableauWidget, preCapturedBase64, redrawDir, redrawPrefix, redrawBannerTask);
                });

                string heroName = hero.Name != null ? hero.Name.ToString() : "英雄";
                popup._dataSource.TitleText = $"【纪事肖像 · {heroName}】";
                topScreen.AddLayer(popup._layer);
                _activeInstance = popup;

                string key = $"Hero_{hero.StringId}";
                var cached = DiskImageCacheManager.LoadImage(key, popup._scope.CampaignKey, "encyclopedia");
                if (cached != null && cached.ImageData != null && cached.ImageData.Length > 0 && popup.PublishImage(cached))
                {
                    popup._dataSource.SetReady("已载入当前存档的默认纪事肖像");
                }
                else
                {
                    popup.ExecuteEncyclopediaGeneration(hero, tableauWidget, preCapturedBase64, offscreenTempDir, offscreenPrefix, bannerTask);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to show encyclopedia illustration overlay: {ex.Message}");
            }
        }

        public static void ShowForConversation(ConversationVisualContext convContext)
        {
            IllustratorRuntime.AssertMainThread();
            if (convContext == null || !IllustratorRuntime.IsEnabled("conversation")) return;
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null) return;

            try
            {
                // 截取 3D 场景主体区域（剔除底部对话 UI 条带），保留现场人物站位与周围预制件环境
                string preCapturedBase64 = ScreenCaptureHelper.CaptureConversationSceneBase64(768);

                // 主线程发起离屏渲染：对方真实3D肖像 + 其家族纹章
                Task<string> portraitTask = null;
                Task<string> bannerTask = null;
                var interlocutor = convContext.InterlocutorHero;
                if (IllustratorRuntime.CaptureOptions()?.EnableOffscreenRendering == true && interlocutor != null)
                {
                    portraitTask = ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(interlocutor, convContext.InterlocutorCivilian);
                    var clanBanner = interlocutor.Clan?.Banner ?? interlocutor.Clan?.Kingdom?.Banner;
                    if (clanBanner != null)
                    {
                        bannerTask = ScreenCaptureHelper.ExtractBannerOffscreenAsync(clanBanner);
                    }
                }

                _activeInstance?.Close();
                var popup = new IllustrationCardPopup(topScreen, "ConversationIllustrationOverlay", "conversation", () =>
                {
                    string redrawBase64 = ScreenCaptureHelper.CaptureConversationSceneBase64(768);
                    Task<string> redrawPortrait = null;
                    Task<string> redrawBanner = null;
                    if (IllustratorRuntime.CaptureOptions()?.EnableOffscreenRendering == true && interlocutor != null)
                    {
                        redrawPortrait = ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(interlocutor, convContext.InterlocutorCivilian);
                        var clanBanner = interlocutor.Clan?.Banner ?? interlocutor.Clan?.Kingdom?.Banner;
                        if (clanBanner != null)
                        {
                            redrawBanner = ScreenCaptureHelper.ExtractBannerOffscreenAsync(clanBanner);
                        }
                    }
                    _activeInstance?.ExecuteConversationGeneration(convContext, redrawBase64, redrawPortrait, redrawBanner);
                });

                string partnerName = convContext.InterlocutorHero != null && convContext.InterlocutorHero.Name != null
                    ? convContext.InterlocutorHero.Name.ToString()
                    : (convContext.InterlocutorCharacter != null && convContext.InterlocutorCharacter.Name != null
                        ? convContext.InterlocutorCharacter.Name.ToString()
                        : "对方");

                popup._dataSource.TitleText = $"【会晤插画 · 与 {partnerName}】";
                topScreen.AddLayer(popup._layer);
                _activeInstance = popup;

                string partnerId = convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC";
                string key = $"Conv_{partnerId}";
                var cached = DiskImageCacheManager.LoadImage(key, popup._scope.CampaignKey, "conversation");
                if (cached != null && cached.ImageData != null && cached.ImageData.Length > 0 && popup.PublishImage(cached))
                {
                    popup._dataSource.SetReady("已载入当前存档的默认会晤插画");
                }
                else
                {
                    popup.ExecuteConversationGeneration(convContext, preCapturedBase64, portraitTask, bannerTask);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to show conversation illustration overlay: {ex.Message}");
            }
        }

        private void ExecuteEncyclopediaGeneration(Hero hero, Widget tableauWidget, string preCapturedBase64 = null, string offscreenTempDir = null, string offscreenPrefix = null, Task<string> bannerTask = null)
        {
            _dataSource.SetLoading("AI画师正在细致描摹人物面相骨相与专属构图...");

            string key = $"Hero_{hero.StringId}";
            string heroName = hero.Name?.ToString() ?? "英雄";

            bool useCivilian = hero.IsNotable || (hero.IsNoncombatant && !hero.IsPartyLeader) || (hero.IsWanderer && hero.PartyBelongedTo == null);
            HeroVisualProfile profile = HeroVisualExtractor.Extract(hero, useCivilian: useCivilian);

            var sb = new StringBuilder();
            sb.AppendLine($"=== 【史诗纪事肖像：{heroName}】 ===");
            sb.AppendLine(GenerateDiversePoseDirective(hero));
            sb.AppendLine("【最高艺术准则】：必须严格还原人物真实面容骨相、胡须发型与尊贵地位，严格依据游戏内实际提取的所属文化风貌、真实穿戴装备（装备名称与材质）进行绘制，严禁张冠李戴！呈现大师级写实油画质感。");
            sb.AppendLine(profile.BuildSummary());
            string contextPrompt = sb.ToString();
            var options = IllustratorRuntime.CaptureOptions();

            _scope.Run(async token =>
            {
                string base64Image = null;
                if (!string.IsNullOrEmpty(offscreenPrefix))
                {
                    base64Image = await ScreenCaptureHelper.WaitForOffscreenFileAsync(offscreenTempDir, offscreenPrefix, timeoutMs: 400, maxDimension: 768).ConfigureAwait(false);
                }
                if (string.IsNullOrWhiteSpace(base64Image)) base64Image = preCapturedBase64;

                var refs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                if (!string.IsNullOrWhiteSpace(base64Image))
                {
                    refs.Add(new IllustrationReferenceImage(base64Image, $"人物【{heroName}】的真实游戏内3D形象（画面中该人物的五官、发型、装备与衣着必须严格依此还原）"));
                }
                if (bannerTask != null)
                {
                    string bannerB64 = await bannerTask.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(bannerB64))
                    {
                        refs.Add(new IllustrationReferenceImage(bannerB64, "该人物所属家族的真实纹章旗帜（画面中一切旗帜、盾徽与罩袍纹章必须严格依此绘制，严禁编造其他纹章）"));
                    }
                }

                string detailedPrompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(contextPrompt, refs, options, token).ConfigureAwait(false);
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, genRefs, options, token).ConfigureAwait(false);
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, detailedPrompt, $"{heroName} 纪事肖像", _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200);
                }
                return new GenerationCompletion(result, saved, detailedPrompt);
            }, completion =>
            {
                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null &&
                    PublishImage(completion.SavedItem, completion.Result.ImageBytes, completion.Prompt))
                {
                    _dataSource.SetReady("纪事肖像绘制完成");
                }
                else
                {
                    _dataSource.SetReady($"绘制失败: {completion.Result?.ErrorMessage ?? "未能保存图像"}");
                }
            }, error => _dataSource.SetReady($"生成异常: {error}"));
        }

        private static string GenerateDiversePoseDirective(Hero hero)
        {
            if (hero == null) return string.Empty;

            // 动态随机因子：融合英雄ID、TickCount与新Guid，确保同一英雄每次点击【重新绘制】都能随机抽取完全不同的古典构图！
            int seed = Math.Abs(hero.StringId.GetHashCode() ^ Environment.TickCount ^ Guid.NewGuid().GetHashCode());

            bool isMonarch = hero.IsFactionLeader || (hero.Clan?.Leader == hero && hero.MapFaction?.Leader == hero);
            bool isFemale = hero.IsFemale;
            bool isNotable = hero.IsNotable;
            bool isWanderer = hero.IsWanderer;
            bool isScholar = hero.IsWanderer && (hero.GetSkillValue(DefaultSkills.Medicine) >= 80 || hero.GetSkillValue(DefaultSkills.Engineering) >= 80 || (hero.Name?.ToString() ?? "").Contains("学者"));

            string cultureName = hero.Culture?.Name != null ? hero.Culture.Name.ToString() : "本文化";
            string clanName = hero.Clan?.Name != null ? hero.Clan.Name.ToString() : "家族";

            string[] poses;

            if (isMonarch)
            {
                poses = new string[]
                {
                    $"【阵营君主专属构图 · 巍峨王座】：人物威严端坐于象征至高统治权的雕花王座之上，身着符合{cultureName}传统的华贵御袍，单手轻抚王座扶手，另一手握持权杖或礼仪佩剑，目光如鹰隼般穿透画面直视观者，背景为饰有{clanName}家族纹章的悬垂织锦挂毯与高耸殿堂穹顶，气吞山河。",
                    $"【阵营君主专属构图 · 视察疆土】：人物傲立于{cultureName}王都要塞宫殿的开阔露台，身披彰显崇高地位的华贵披风，迎着暮色远眺广袤疆土与城廓，身后长风吹卷所属家族{clanName}的纹章王旗，神态冷峻深沉，尽显开辟霸业的王者远谋与帝王孤傲。",
                    $"【阵营君主专属构图 · 御前议政殿堂】：人物昂立于庄严的宫廷议政大厅之中，在熊熊燃烧的殿堂火盆与跃动的橘金暖光映照下，双手自然交叠于腰间佩剑剑柄，侧脸与额骨刚毅沉稳，深邃审视着满堂重臣，庄严肃穆。",
                    $"【阵营君主专属构图 · 凯旋戎装半身】：人物身着精工锻造的御用战甲与战袍，未戴战盔而是任长风拂动发丝，右手握紧战盔托于胸前，远景为被晚霞染红的雄伟要塞城垛与飘扬的战旗，彰显征战四方的王者雄姿。"
                };
            }
            else if (isFemale)
            {
                poses = new string[]
                {
                    $"【贵族名媛专属构图 · 华美雅室】：人物仪态端庄地端坐于{cultureName}贵族殿堂精美的高背雕花座椅上，身着剪裁考究的华贵礼袍，纤手轻搭于膝间，神情雍容娴静而富有深邃智谋，背景透出文艺复兴式深沉暗调光影。",
                    $"【贵族名媛专属构图 · 长廊凭栏回眸】：人物优雅立于城堡拱券长廊的大理石栏杆旁，轻微回首凝视画面，微风拂动发丝与轻盈头纱，背景为薄暮中的群山与城堞，光线柔和细腻，宛如古典大师油画杰作。",
                    $"【贵族名媛专属构图 · 典籍与微光】：人物侧身立于高大的拱形彩绘玻璃花窗旁，手中轻捧一本烫金封面的羊皮纸典籍，彩窗折射下的宝石蓝与琥珀色微光轻洒在发梢与丝绸衣褶上，神态睿智沉思。",
                    $"【巾帼统帅专属构图 · 战甲披风伫立】：人物虽为贵族女性，却身着合体轻便的精工重甲与暗纹斗篷，单臂叉腰，另一手轻扶佩剑，英姿飒爽，目光如炬，尽显巾帼女将的坚韧与统帅风范。"
                };
            }
            else if (isScholar)
            {
                poses = new string[]
                {
                    "【先贤学者专属构图 · 哲思沉吟】：人物身着学者长袍，端坐于石椅上，单肘支于石桌、屈指托腮作深邃哲学沉思状，案几上摊开厚重的古代手札，在单侧斜射的天窗光束下极具质感，杜绝任何轻浮姿态。",
                    "【先贤学者专属构图 · 浑天象仪与古籍】：人物肃立于大型浑天仪与石质藏书架前，手托古老铜制星盘对光端详，周身环绕着散发羊皮纸香气的厚重典籍与油灯，气度博学而孤高。",
                    "【先贤学者专属构图 · 宣谕法度手札】：人物站立挺直，双手庄重展开一份带有古老火漆封印的宪章法令，目光严峻而透彻，如同在辩护真理的首席哲人。",
                    "【先贤学者专属构图 · 独对残阳】：人物倚立于古老修道院或学堂的拱窗前，双臂抱胸，凝望窗外渐渐西沉的落日与远方残垣，神情饱含看尽尘世沧桑的淡泊与冷峻洞察。"
                };
            }
            else if (isWanderer)
            {
                poses = new string[]
                {
                    "【游侠同伴专属构图 · 酒馆石柱小憩】：人物微侧身倚靠在昏暗喧闹的石木酒馆粗石柱旁，身着坚韧皮甲与防雨斗篷，单手大拇指随意别在短剑腰带上，嘴角挂着一丝桀骜不驯的冷笑，鹰隼般锐利的目光打量着全场。",
                    "【游侠同伴专属构图 · 篝火砥砺寒芒】：人物随意盘坐于干燥木箱旁，粗糙双手正握着磨刀石细心打磨一柄精钢匕首，火塘跃动的明亮橘光映照着饱经风霜的面庞与坚实下颚，充满边境猎手警觉的气息。",
                    "【游侠同伴专属构图 · 荒原风霜远眺】：人物肩负复合硬弓与箭袋，立于冷雨初歇的泥泞荒原巨石旁，抬手将兜帽略微推后，深邃坚韧的目光远望风暴聚集的群峰，浑身散发着边境老将的野性与干练。"
                };
            }
            else if (isNotable)
            {
                poses = new string[]
                {
                    "【城镇巨贾专属构图 · 商会会馆】：人物身披奢华毛领锦缎长袍，端坐于雕花木椅上，神态精明沉稳，单手把玩一枚厚重金币，背后是挂着商贸海图与封蜡货单的华美木质护墙板。",
                    "【工匠头领专属构图 · 铁工坊前检视】：人物身穿厚皮革工匠围裙与卷袖衬衫，双臂健硕抱胸站立于锻炉喷涌的暗红火星旁，目光冷峻而自信，尽显掌控全城工坊与行会的宗匠霸气。",
                    "【地下魁首专属构图 · 暗巷阴影对峙】：人物大马金刀坐于木桶之上，身裹黑风衣，双手握着带有兽头手柄的坚木手杖拄于地面，在深沉的光影明暗对比中流露出执掌黑街生死的威慑力。"
                };
            }
            else // 默认常备：封建领主、武将、万夫长、重装骑士
            {
                poses = new string[]
                {
                    "【铁血武将专属构图 · 双手拄大剑立地】：人物身披精铁重甲与厚实战袍，肃立于城堡点将石台，双手紧握一柄长柄重剑剑柄、剑尖直插石缝，雄浑沉稳如山岳，目光冷酷威严，背景为飘扬的家族军旗。",
                    "【铁血武将专属构图 · 单臂抱盔英姿】：三刻半身肖像，人物身着精锻重甲与披肩战袍，左臂英挺地将一顶雕纹开面覆面铁盔挟抱于腰间，右手自然扶在佩剑上，身后露出一角被烽烟笼罩的城楼垛口，风骨卓绝。",
                    "【铁血武将专属构图 · 营帐沙盘前推演】：人物俯身倚在行军大帐的厚木战术台边缘，身披沾染尘土的深色斗篷，神情严峻地审视着前线斥候的军情简报，炭火盆投射出富于戏剧张力的硬朗阴影。",
                    "【铁血武将专属构图 · 城垛风暴哨戒】：人物伫立于狂风呼啸的高耸城垛转角，战袍斗篷被风吹向一侧，手中斜倚一杆带缨长枪，冷眼凝视乌云密布的天际，宛如黑夜中不朽的戍边之盾。"
                };
            }

            string selectedPose = poses[seed % poses.Length];

            var sb = new StringBuilder();
            sb.AppendLine("【构图与姿态指令（绝对禁止模板化动作）】这是一幅古典写实油画风格的史诗英雄肖像！");
            sb.AppendLine("【绝对禁止克隆模板】：严厉禁止画成“坐在木桌前单手扶地图卷轴”的千篇一律动作！必须严格采用以下专属构图：");
            sb.AppendLine(selectedPose);
            return sb.ToString().TrimEnd();
        }

        private void ExecuteConversationGeneration(ConversationVisualContext convContext, string preCapturedBase64 = null, Task<string> portraitTask = null, Task<string> bannerTask = null)
        {
            _dataSource.SetLoading("AI画师正在分析现场交谈与肢体姿势...");

            string partnerId = convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC";
            string key = $"Conv_{partnerId}";
            string contextPrompt = convContext.BuildCompositeContext();
            string partnerName = convContext.InterlocutorHero != null && convContext.InterlocutorHero.Name != null
                ? convContext.InterlocutorHero.Name.ToString()
                : (convContext.InterlocutorCharacter != null && convContext.InterlocutorCharacter.Name != null
                    ? convContext.InterlocutorCharacter.Name.ToString()
                    : "对方");
            var options = IllustratorRuntime.CaptureOptions();

            _scope.Run(async token =>
            {
                var refs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                if (!string.IsNullOrWhiteSpace(preCapturedBase64))
                {
                    refs.Add(new IllustrationReferenceImage(preCapturedBase64, "会面现场的3D实景画面（双方站位、坐骑与周围真实环境布局）"));
                }
                if (portraitTask != null)
                {
                    string b64 = await portraitTask.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        refs.Add(new IllustrationReferenceImage(b64, $"对话对方【{partnerName}】的真实游戏内3D形象（其五官、发型、装备与衣着必须严格依此还原）"));
                    }
                }
                if (bannerTask != null)
                {
                    string b64 = await bannerTask.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        refs.Add(new IllustrationReferenceImage(b64, "对话对方所属家族的真实纹章旗帜（画面中一切旗帜、盾徽与罩袍纹章必须严格依此绘制）"));
                    }
                }

                string detailedPrompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(contextPrompt, refs, options, token).ConfigureAwait(false);
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, genRefs, options, token).ConfigureAwait(false);
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, detailedPrompt, $"与 {partnerName} 的会晤纪事", _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200);
                }
                return new GenerationCompletion(result, saved, detailedPrompt);
            }, completion =>
            {
                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null &&
                    PublishImage(completion.SavedItem, completion.Result.ImageBytes, completion.Prompt))
                {
                    _dataSource.SetReady("会晤插画绘制完成");
                }
                else
                {
                    _dataSource.SetReady($"绘制失败: {completion.Result?.ErrorMessage ?? "未能保存图像"}");
                }
            }, error => _dataSource.SetReady($"生成异常: {error}"));
        }

        private bool PublishImage(CachedIllustrationItem item)
        {
            return item != null && PublishImage(item, item.ImageData, item.Prompt);
        }

        private bool PublishImage(CachedIllustrationItem item, byte[] imageBytes, string prompt)
        {
            string spriteName = !string.IsNullOrWhiteSpace(item?.Key)
                ? item.Key + "_" + _instanceId
                : "Illustration_" + Guid.NewGuid().ToString("N");

            ReleaseActiveSprite();
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, imageBytes, fixColorChannels: IllustratorRuntime.CaptureOptions()?.FixColorChannels ?? true);
            if (sprite == null) return false;
            _activeSpriteName = spriteName;
            _dataSource.SetIllustration(item?.SubjectKey ?? spriteName, spriteName, prompt);
            return true;
        }

        private void ReleaseActiveSprite()
        {
            if (string.IsNullOrWhiteSpace(_activeSpriteName)) return;
            GauntletTextureLoader.ReleaseSprite(_activeSpriteName);
            _activeSpriteName = null;
        }

        private sealed class GenerationCompletion
        {
            public readonly ImageGenerationResult Result;
            public readonly CachedIllustrationItem SavedItem;
            public readonly string Prompt;

            public GenerationCompletion(ImageGenerationResult result, CachedIllustrationItem savedItem, string prompt)
            {
                Result = result;
                SavedItem = savedItem;
                Prompt = prompt;
            }
        }

        public void Close()
        {
            if (_closed) return;
            _closed = true;
            try
            {
                _scope?.Close();
                ReleaseActiveSprite();
                if (_screen != null && _layer != null)
                {
                    _screen.RemoveLayer(_layer);
                }
            }
            catch
            {
            }
            finally
            {
                if (_activeInstance == this)
                {
                    _activeInstance = null;
                }
            }
        }
    }
}
