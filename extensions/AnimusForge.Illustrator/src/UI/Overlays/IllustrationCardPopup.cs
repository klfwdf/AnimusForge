using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
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
        private int _generationCount;

        internal static Widget VisualRoot => _activeInstance?._layer?.UIContext?.Root;
        public static bool IsOpen => _activeInstance != null;

        private IllustrationCardPopup(ScreenBase screen, string movieName, string category, Action onRegenerate)
        {
            _screen = screen;
            _category = category;
            _dataSource = new IllustrationCardVM(Close, onRegenerate);
            var layer = new MovableGauntletLayer("IllustrationCardOverlay", 4015, false);
            _layer = layer;
            try
            {
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
            catch { Close(); throw; }
        }

        public static void ShowForEncyclopedia(Hero hero, Widget tableauWidget)
        {
            IllustratorRuntime.AssertMainThread();
            if (hero == null || !IllustratorRuntime.IsEnabled("encyclopedia")) return;
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null) return;

            IllustrationCardPopup popup = null;
            try
            {
                _activeInstance?.Close();
                popup = new IllustrationCardPopup(topScreen, "EncyclopediaIllustrationOverlay", "encyclopedia", () =>
                {
                    _activeInstance?.ExecuteEncyclopediaGeneration(hero, tableauWidget);
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
                    popup.ExecuteEncyclopediaGeneration(hero, tableauWidget);
                }
            }
            catch (Exception ex)
            {
                popup?.Close();
                Debug.Print($"[Illustrator] Failed to show encyclopedia illustration overlay: {ex.Message}");
            }
        }

        public static void ShowForConversation(ConversationVisualContext convContext)
        {
            IllustratorRuntime.AssertMainThread();
            if (convContext == null || !IllustratorRuntime.IsEnabled("conversation")) return;
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null) return;

            IllustrationCardPopup popup = null;
            try
            {
                // 截取 3D 场景主体区域（剔除底部对话 UI 条带），保留现场人物站位与周围预制件环境
                string preCapturedBase64 = null;

                var interlocutor = convContext.InterlocutorHero;
                // 会话画面里双方都可能出现纹章载体：对方家族与玩家家族各发一张参考样图（同一代码去重）
                var emblemSpecs = new List<EmblemSpec>();
                AddEmblemSpec(emblemSpecs, interlocutor, "对话对方");
                AddEmblemSpec(emblemSpecs, Hero.MainHero, "玩家");

                _activeInstance?.Close();
                popup = new IllustrationCardPopup(topScreen, "ConversationIllustrationOverlay", "conversation", () =>
                {
                    string redrawBase64 = null;
                    ConversationVisualContext redrawContext = ConversationContextExtractor.ExtractFromCurrentConversation();
                    var current = redrawContext ?? convContext;
                    var redrawEmblems = new List<EmblemSpec>();
                    AddEmblemSpec(redrawEmblems, current.InterlocutorHero, "对话对方");
                    AddEmblemSpec(redrawEmblems, current.MainHero, "玩家");
                    _activeInstance?.ExecuteConversationGeneration(current, redrawBase64, redrawEmblems);
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
                    popup.ExecuteConversationGeneration(convContext, preCapturedBase64, emblemSpecs);
                }
            }
            catch (Exception ex)
            {
                popup?.Close();
                Debug.Print($"[Illustrator] Failed to show conversation illustration overlay: {ex.Message}");
            }
        }

        private void ExecuteEncyclopediaGeneration(Hero hero, Widget tableauWidget)
        {
            try { ExecuteEncyclopediaGenerationCore(hero, tableauWidget); }
            catch (Exception ex) { _dataSource.SetReady("生成准备失败：" + ex.Message); }
        }

        private void ExecuteEncyclopediaGenerationCore(Hero hero, Widget tableauWidget)
        {
            _dataSource.SetLoading("AI画师正在细致描摹人物面相骨相与专属构图...");

            string key = $"Hero_{hero.StringId}";
            string heroName = hero.Name?.ToString() ?? "英雄";

            var portrait = tableauWidget as CharacterTableauWidget;
            if (portrait == null || string.IsNullOrWhiteSpace(portrait.EquipmentCode))
            {
                _dataSource.SetReady("无法读取百科当前展示装备，已停止生成；不会改用猜测的便服或战斗服。");
                return;
            }
            string equipmentCode = portrait.EquipmentCode;
            var appearance = CharacterAppearanceSnapshot.FromTableau(portrait);
            Equipment equipment = Equipment.CreateFromEquipmentCode(equipmentCode);
            HeroVisualProfile profile = HeroVisualExtractor.Extract(hero, equipmentSnapshot: equipment, equipmentSource: "百科当前人物立绘的完整EquipmentCode（不是现场装备或身份推测）");
            string bannerCode = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;

            string hardFacts = profile.BuildVisualSummary() + "\n" + VisualFidelityRules.EncyclopediaPortrait;
            string directorFacts = $"【纪元时间】卡拉迪亚历 {TaleWorlds.CampaignSystem.CampaignTime.Now.GetYear} 年\n" + profile.BuildDirectorOnlyFacts();
            _generationCount++;
            string artDirection = GenerateDiversePoseDirective(hero) + "\n" + VisualFidelityRules.PortraitFraming(_generationCount);
            if (_generationCount > 1) artDirection += "\n本次重绘只适度改变镜头角度、景别或光线，不强制改变姿势，不增加道具。";
            var promptPlan = new IllustrationPromptPlan("人物百科纪事", hardFacts, artDirection, directorFacts);
            var options = IllustratorRuntime.CaptureOptions();
            if (options?.EnableOffscreenRendering != true)
            {
                _dataSource.SetReady("请先开启离屏渲染；本次生图需要人物完整装备立绘，不使用模板或旧截图替代。");
                return;
            }

            _scope.Run(async token =>
            {
                string base64Image = await ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(hero,
                    maxDimension: 768, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true,
                    equipmentCodeOverride: equipmentCode, appearance: appearance).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(base64Image))
                    throw new InvalidOperationException("百科完整装备离屏立绘未取得，已停止生成；请稍后重试。");

                var refs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                if (!string.IsNullOrWhiteSpace(base64Image))
                {
                    refs.Add(new IllustrationReferenceImage(base64Image, $"人物【{heroName}】的身份参考图：仅用于锁定其五官、发型、肤色、装备与服饰或其他实际纹章载体上的家族纹章（仅在画面确有该载体时绘制）；可保留本图中自然放松的姿态；不要复制界面、背景与游戏渲染质感，不要为重新设计构图而发明手持物、撑桌或夸张动作"));
                }
                // 纹章由原生渲染导出，导出控件不向屏幕绘制；取消信号贯穿请求
                if (!string.IsNullOrWhiteSpace(bannerCode))
                {
                    string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(bannerCode, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(emblemB64))
                    {
                        refs.Add(new IllustrationReferenceImage(emblemB64, "该家族真实纹章标准样图：其底色与徽记形状、配色即纹章本体；当画面因已确认事实出现纹章载体时，必须与此一致绘制，严禁编造或改动图腾；没有载体证据时不要添加纹章载体"));
                    }
                }
                string detailedPrompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                IllustratorRuntime.Post(() => { if (!_closed) _dataSource.StatusText = "构思完成，正在绘制画卷（等待生图模型返回）..."; });
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, genRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, $"{heroName} 纪事肖像", _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false);
                }
                return new GenerationCompletion(result, saved, effectivePrompt);
            }, completion =>
            {
                if (completion.SavedItem != null) DiskImageCacheManager.SetDefault(completion.SavedItem, _scope.CampaignKey);
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
            // Portraits are not action scenes. Variation must not invent props, gestures or locations.
            return "人物为视觉中心的克制肖像：自然直立或轻微侧身、肩臂放松；优先近景半身，镜头可在正面与轻侧面适度变化；必须描写可辨认的背景空间、材质和光源，环境低对比但不能低曝光成黑底。不为求变化设计复杂持物动作。";
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

        /// <summary>一枚待合成的纹章参考图：旗帜代码 + 归属方标签（画面归因用）。</summary>
        private sealed class EmblemSpec
        {
            public string Code = string.Empty;
            public string Owner = string.Empty;
            public string Side = string.Empty;
        }

        private static void AddEmblemSpec(List<EmblemSpec> specs, Hero hero, string sideLabel)
        {
            if (hero == null) return;
            string code = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;
            if (string.IsNullOrWhiteSpace(code)) return;
            if (specs.Exists(s => s.Code == code)) return;
            string owner = hero.Clan?.Name != null ? hero.Clan.Name.ToString()
                : (hero.Clan?.Kingdom?.Name != null ? hero.Clan.Kingdom.Name.ToString() : "未知家族");
            specs.Add(new EmblemSpec { Code = code, Owner = owner, Side = sideLabel });
        }

        private void ExecuteConversationGeneration(ConversationVisualContext convContext, string preCapturedBase64 = null, List<EmblemSpec> emblemSpecs = null)
        {
            try { ExecuteConversationGenerationCore(convContext, preCapturedBase64, emblemSpecs); }
            catch (Exception ex) { _dataSource.SetReady("生成准备失败：" + ex.Message); }
        }

        private void ExecuteConversationGenerationCore(ConversationVisualContext convContext, string preCapturedBase64 = null, List<EmblemSpec> emblemSpecs = null)
        {
            _dataSource.SetLoading("AI画师正在分析现场交谈与肢体姿势...");

            string partnerId = convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC";
            string key = $"Conv_{partnerId}";
            // 台词与近三轮对话只进导演（DirectorOnlyFacts）——导演转成画面描述后，生图模型只见视觉文本，不再把台词画进图里
            _generationCount++;
            string variation = GenerateConversationSceneVariation(convContext);
            if (_generationCount > 1) variation += "\n" + VisualDirectorEngine.BuildRedrawVariationDirective(_generationCount);
            var promptPlan = new IllustrationPromptPlan("最近三轮对话联动的场景插画", convContext.BuildHardFacts(), convContext.BuildArtDirection(variation), convContext.BuildDirectorOnlyFacts());
            TaleWorlds.Library.Debug.Print($"[Illustrator] ConvScene host='{convContext.EnvironmentProfile?.HostSceneDescription ?? ""}' loc='{convContext.EnvironmentProfile?.SpecificLocation ?? ""}' scene='{convContext.EnvironmentProfile?.RealSceneName ?? ""}'");
            string partnerName = convContext.InterlocutorHero != null && convContext.InterlocutorHero.Name != null
                ? convContext.InterlocutorHero.Name.ToString()
                : (convContext.InterlocutorCharacter != null && convContext.InterlocutorCharacter.Name != null
                    ? convContext.InterlocutorCharacter.Name.ToString()
                    : "对方");
            var options = IllustratorRuntime.CaptureOptions();
            if (options?.EnableOffscreenRendering != true)
            {
                _dataSource.SetReady("请先开启离屏渲染；本次生图需要人物完整装备立绘，不使用模板或旧截图替代。");
                return;
            }
            Hero interlocutor = convContext.InterlocutorHero;
            bool interlocutorCivilian = convContext.InterlocutorCivilian;
            Hero player = convContext.MainHero;
            bool playerCivilian = convContext.MainHeroCivilian;
            string playerEquipmentCode = convContext.MainHeroProfile?.EquipmentCode;
            string partnerEquipmentCode = convContext.InterlocutorProfile?.EquipmentCode;
            if (player == null || string.IsNullOrWhiteSpace(playerEquipmentCode) || string.IsNullOrWhiteSpace(partnerEquipmentCode))
            {
                _dataSource.SetReady("未能取得双方完整装备快照，已停止生成；不会用兵种模板或另一套服装替代。");
                return;
            }

            _scope.Run(async token =>
            {
                // 参考图分两路：场景实景截图只发导演识图（避免截图质感与UI文字被生图模型复制），
                // 人物立绘同时进生图垫图。
                var directorRefs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                var genRefs = new System.Collections.Generic.List<IllustrationReferenceImage>();
                preCapturedBase64 = await ScreenCaptureHelper.CaptureConversationSceneWithoutUiAsync(token).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(preCapturedBase64))
                {
                    directorRefs.Add(new IllustrationReferenceImage(preCapturedBase64,
                        "会面现场的3D实景画面：仅用于理解双方站位、坐骑、周围真实环境布局与光影方向，画面中禁止出现任何界面元素、对话框、字幕、名牌与文字"));
                }

                // 离屏舞台提取在 scope 内携带 token：关闭/重绘时旧任务立即取消并拆舞台
                Func<Task<string>> playerStage = player == null ? (Func<Task<string>>)null : () => ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(player, useCivilian: playerCivilian, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true, equipmentCodeOverride: playerEquipmentCode, appearance: convContext.MainHeroProfile.Appearance);
                Func<Task<string>> partnerStage = interlocutor != null
                    ? () => ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(interlocutor, interlocutorCivilian, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true, equipmentCodeOverride: partnerEquipmentCode, appearance: convContext.InterlocutorProfile.Appearance)
                    : convContext.InterlocutorCharacter != null ? (Func<Task<string>>)(() => ScreenCaptureHelper.ExtractCharacterPortraitOffscreenAsync(convContext.InterlocutorCharacter, cancellationToken: token, bodyProperties: convContext.InterlocutorBodyProperties, cleanTempFiles: options?.AutoCleanTempFiles == true, equipmentCodeOverride: partnerEquipmentCode, appearance: convContext.InterlocutorProfile.Appearance)) : null;
                if (playerStage != null)
                {
                    string b64 = await playerStage().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(b64)) throw new InvalidOperationException("玩家完整装备离屏立绘失败，已停止生成。");
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        var r = new IllustrationReferenceImage(b64, "对话中玩家主角的身份参考图：仅用于锁定其五官、发型、肤色与装备；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感");
                        directorRefs.Add(r);
                        genRefs.Add(r);
                    }
                }
                if (partnerStage != null)
                {
                    string b64 = await partnerStage().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(b64)) throw new InvalidOperationException("对方完整装备离屏立绘失败，已停止生成。");
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        var r = new IllustrationReferenceImage(b64, $"对话对方【{partnerName}】的身份参考图：仅用于锁定其五官、发型、肤色、装备与服饰或其他实际纹章载体上的家族纹章（仅在画面确有该载体时绘制）；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感");
                        directorRefs.Add(r);
                        genRefs.Add(r);
                    }
                }
                foreach (var spec in emblemSpecs ?? new List<EmblemSpec>())
                {
                    string b64 = await BannerEmblemComposer.ComposeToBase64Async(spec.Code, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        var r = new IllustrationReferenceImage(b64, $"{spec.Side}一方【{spec.Owner}】的真实纹章标准样图：当画面中属于{spec.Side}的一处已确认纹章载体出现时，必须以此一致的形状与配色绘制，严禁编造图腾；没有载体证据时不要添加纹章载体");
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
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, $"与 {partnerName} 的会晤纪事", _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false);
                }
                return new GenerationCompletion(result, saved, effectivePrompt);
            }, completion =>
            {
                if (completion.SavedItem != null) DiskImageCacheManager.SetDefault(completion.SavedItem, _scope.CampaignKey);
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
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, imageBytes);
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
