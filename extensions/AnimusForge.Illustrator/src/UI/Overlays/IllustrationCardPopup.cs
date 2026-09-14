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
                string bannerCode = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;
                if (IllustratorRuntime.CaptureOptions()?.EnableOffscreenRendering == true)
                {
                    var view = ScreenCaptureHelper.ResolveTableauView(tableauWidget);
                    if (view != null)
                    {
                        ScreenCaptureHelper.TriggerTableauViewSave(view, out offscreenTempDir, out offscreenPrefix);
                    }
                }

                _activeInstance?.Close();
                var popup = new IllustrationCardPopup(topScreen, "EncyclopediaIllustrationOverlay", "encyclopedia", () =>
                {
                    string redrawDir = null;
                    string redrawPrefix = null;
                    if (IllustratorRuntime.CaptureOptions()?.EnableOffscreenRendering == true)
                    {
                        var view = ScreenCaptureHelper.ResolveTableauView(tableauWidget);
                        if (view != null)
                        {
                            ScreenCaptureHelper.TriggerTableauViewSave(view, out redrawDir, out redrawPrefix);
                        }
                    }
                    _activeInstance?.ExecuteEncyclopediaGeneration(hero, tableauWidget, preCapturedBase64, redrawDir, redrawPrefix, bannerCode);
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
                    popup.ExecuteEncyclopediaGeneration(hero, tableauWidget, preCapturedBase64, offscreenTempDir, offscreenPrefix, bannerCode);
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

                var interlocutor = convContext.InterlocutorHero;
                string bannerCode = (interlocutor?.Clan?.Banner ?? interlocutor?.Clan?.Kingdom?.Banner)?.BannerCode;

                _activeInstance?.Close();
                var popup = new IllustrationCardPopup(topScreen, "ConversationIllustrationOverlay", "conversation", () =>
                {
                    string redrawBase64 = ScreenCaptureHelper.CaptureConversationSceneBase64(768);
                    ConversationVisualContext redrawContext = ConversationContextExtractor.ExtractFromCurrentConversation();
                    _activeInstance?.ExecuteConversationGeneration(redrawContext ?? convContext, redrawBase64, bannerCode);
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
                    popup.ExecuteConversationGeneration(convContext, preCapturedBase64, bannerCode);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to show conversation illustration overlay: {ex.Message}");
            }
        }

        private void ExecuteEncyclopediaGeneration(Hero hero, Widget tableauWidget, string preCapturedBase64 = null, string offscreenTempDir = null, string offscreenPrefix = null, string bannerCode = null)
        {
            _dataSource.SetLoading("AI画师正在细致描摹人物面相骨相与专属构图...");

            string key = $"Hero_{hero.StringId}";
            string heroName = hero.Name?.ToString() ?? "英雄";

            bool useCivilian = hero.IsNotable || (hero.IsNoncombatant && !hero.IsPartyLeader) || (hero.IsWanderer && hero.PartyBelongedTo == null);
            HeroVisualProfile profile = HeroVisualExtractor.Extract(hero, useCivilian: useCivilian);

            string hardFacts = profile.BuildSummary();
            string artDirection = GenerateDiversePoseDirective(hero);
            var promptPlan = new IllustrationPromptPlan("人物百科纪事", hardFacts, artDirection);
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
                    refs.Add(new IllustrationReferenceImage(base64Image, $"人物【{heroName}】的身份参考图：仅用于锁定其五官、发型、肤色、装备与盾面/罩袍上的家族纹章（旗帜徽记依此纹样绘制）；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感，构图与画风必须重新设计"));
                }
                string detailedPrompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                IllustratorRuntime.Post(() => { if (!_closed) _dataSource.StatusText = "构思完成，正在绘制画卷（等待生图模型返回）..."; });
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, genRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, $"{heroName} 纪事肖像", _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: true);
                }
                return new GenerationCompletion(result, saved, effectivePrompt);
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
            string[] compositions =
            {
                "环境占主导的远景人物肖像，让建筑、道路或地貌交代人物所处世界，人物不必正对镜头",
                "三分之二侧身中景，人物刚刚转头或停下动作，形成被历史瞬间捕捉的感觉",
                "低机位仰拍，但保持自然比例，用天空、穹顶或旗帜形成留白",
                "高位俯拍人物穿过庭院、街巷、营地或大厅，让空间动线成为叙事主体",
                "近距离面部与上半身肖像，以细微眼神、呼吸和手势表达身份，不额外添加道具",
                "从门框、柱廊、帐帘或树枝之间观察人物，形成自然前景层次",
                "人物位于画面边缘，视线投向画外事件，留下大片环境与悬念空间",
                "逆光或剪影式构图，仍保留足够面部与装备细节供辨认",
                "行进中的瞬间：迈步、下马、登阶或穿过人群，但只使用事实中已有的装备与坐骑",
                "安静停顿的瞬间：人物在窗边、火光旁或露天阴影中思考，避免固定的地图桌动作",
                "半身侧面与远处活动形成双层叙事，让人物身份通过环境关系而不是夸张姿势表现",
                "广角近景，前景是人物真实装备细节，中后景只选择一两个与其经历有关的环境元素",
                "对称构图但加入自然动作和不完全居中，让庄重感不等于僵硬站桩",
                "非对称动态构图，利用披风、光线、人群或建筑线条形成方向感，不凭空添加物件",
                "从同伴或侍从视角观察人物，形成有距离感的纪实肖像，背景人物只作轻微陪衬",
                "天气和光线保持克制，以人物参考图和游戏事实为核心，不强制使用黄昏、火盆或戏剧逆光"
            };
            string roleHint = hero.IsFactionLeader
                ? "可通过空间尺度、周围人的距离或礼仪秩序表现统治身份，不必固定使用王座、王冠或权杖"
                : hero.IsNotable
                    ? "可让人物与其真实职业环境发生联系，但不要凭职业猜测衣着和手持物"
                    : hero.IsWanderer
                        ? "可强调旅途感、临时停留或观察环境，不必固定放在酒馆与篝火旁"
                        : "可在肖像、环境人物和行动瞬间之间自由取舍";
            return "本次可优先尝试以下构图方向，也可由导演根据人物事实选择更合适的方案：" + compositions[seed % compositions.Length] + "。" + roleHint + "。";
        }

        private static string GenerateConversationSceneVariation(ConversationVisualContext context)
        {
            string[] variations =
            {
                "使用宽幅环境双人镜头，人物在真实场景中保持可辨识，但环境可以承担主要叙事",
                "使用玩家肩后看向对方的过肩镜头，让对方当前神情和手势成为重点",
                "使用对方肩后看向玩家的反向过肩镜头，体现双方地位和空间距离",
                "采用两人侧面同框的横向构图，以视线和身体朝向表现关系",
                "采用三分之二侧面中景，捕捉一句话刚说完后的停顿，不要求人物看向镜头",
                "采用较低机位，让建筑或天空参与构图，但人物比例保持自然",
                "采用轻微俯视，展示双方、随行者和附近真实道具的空间关系",
                "让一名人物处于近景边缘，另一名人物位于中景，形成有纵深的对话镜头",
                "聚焦双方手势、握缰、扶剑或放松姿态等实际可见细节，面部仍保持可辨认",
                "选择转身、迈步、下马或准备离开的过渡瞬间；若现场事实不支持这些动作则改用安静停顿",
                "利用门框、柱廊、帐帘、树木或街道摊位形成前景，但只采用真实场景中存在的元素",
                "以当前现场实景参考图为构图骨架，允许改变镜头高度和焦段，不照抄界面裁剪",
                "采用更亲近的双人半身镜头，突出最近三轮对话造成的情绪变化",
                "采用保持距离的广角镜头，让沉默、戒备或外交礼节通过留白体现",
                "从随行者视角观察会面，让背景人物虚化，双方关系清晰",
                "用自然现场光塑造层次，不强制黄昏、火把、逆光或对称站桩"
            };
            int seed = Math.Abs(Environment.TickCount ^ Guid.NewGuid().GetHashCode() ^ (context?.DialogueSentence ?? string.Empty).GetHashCode());
            return "本次镜头变化建议：" + variations[seed % variations.Length] + "。这只是构图选项，若与游戏事实冲突应舍弃。";
        }

        private void ExecuteConversationGeneration(ConversationVisualContext convContext, string preCapturedBase64 = null, string bannerCode = null)
        {
            _dataSource.SetLoading("AI画师正在分析现场交谈与肢体姿势...");

            string partnerId = convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC";
            string key = $"Conv_{partnerId}";
            var promptPlan = new IllustrationPromptPlan("最近三轮对话联动的场景插画", convContext.BuildHardFacts(), convContext.BuildArtDirection(GenerateConversationSceneVariation(convContext)));
            string partnerName = convContext.InterlocutorHero != null && convContext.InterlocutorHero.Name != null
                ? convContext.InterlocutorHero.Name.ToString()
                : (convContext.InterlocutorCharacter != null && convContext.InterlocutorCharacter.Name != null
                    ? convContext.InterlocutorCharacter.Name.ToString()
                    : "对方");
            var options = IllustratorRuntime.CaptureOptions();
            Hero interlocutor = convContext.InterlocutorHero;
            bool interlocutorCivilian = convContext.InterlocutorCivilian;

            _scope.Run(async token =>
            {
                // 参考图分两路：场景实景截图只发导演识图（避免截图质感与UI文字被生图模型复制），
                // 人物立绘同时进生图垫图。
                var directorRefs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                var genRefs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                if (!string.IsNullOrWhiteSpace(preCapturedBase64))
                {
                    directorRefs.Add(new IllustrationReferenceImage(preCapturedBase64,
                        "会面现场的3D实景画面：仅用于理解双方站位、坐骑、周围真实环境布局与光影方向，画面中禁止出现任何界面元素、对话框、字幕、名牌与文字"));
                }

                // 离屏舞台提取在 scope 内携带 token：关闭/重绘时旧任务立即取消并拆舞台
                Task<string> playerStage = null;
                Task<string> partnerStage = null;
                if (options?.EnableOffscreenRendering == true)
                {
                    if (Hero.MainHero != null)
                    {
                        playerStage = ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(Hero.MainHero, useCivilian: true, cancellationToken: token);
                    }
                    if (interlocutor != null)
                    {
                        partnerStage = ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(interlocutor, interlocutorCivilian, cancellationToken: token);
                    }
                    else if (convContext.InterlocutorCharacter != null)
                    {
                        partnerStage = ScreenCaptureHelper.ExtractCharacterPortraitOffscreenAsync(convContext.InterlocutorCharacter, cancellationToken: token);
                    }
                }
                if (playerStage != null)
                {
                    string b64 = await playerStage.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        var r = new IllustrationReferenceImage(b64, "对话中玩家主角的身份参考图：仅用于锁定其五官、发型、肤色与装备；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感");
                        directorRefs.Add(r);
                        genRefs.Add(r);
                    }
                }
                if (partnerStage != null)
                {
                    string b64 = await partnerStage.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        var r = new IllustrationReferenceImage(b64, $"对话对方【{partnerName}】的身份参考图：仅用于锁定其五官、发型、肤色、装备与盾面/罩袍上的家族纹章（旗帜徽记依此纹样绘制）；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感");
                        directorRefs.Add(r);
                        genRefs.Add(r);
                    }
                }

                string detailedPrompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(promptPlan, directorRefs, options, token).ConfigureAwait(false);
                IllustratorRuntime.Post(() => { if (!_closed) _dataSource.StatusText = "构思完成，正在绘制画卷（等待生图模型返回）..."; });
                var finalGenRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)genRefs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, finalGenRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, $"与 {partnerName} 的会晤纪事", _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: true);
                }
                return new GenerationCompletion(result, saved, effectivePrompt);
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
