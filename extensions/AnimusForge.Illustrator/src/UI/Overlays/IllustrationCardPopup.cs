using System;
using System.Collections.Generic;
using System.Linq;
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
                    popup._dataSource.SetReady(cached.ThemeText);
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
                    popup._dataSource.SetReady(cached.ThemeText);
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
            // 百科肖像清空全部武器槽（用户指定）：武器不入画——事实区与离屏立绘同步无武器，
            // 消除长兵器竖持对姿势构图的锚定；盾牌同属武器槽一并清除，符合"百科不画盾"规则。
            for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
            {
                equipment[slot] = EquipmentElement.Invalid;
            }
            equipmentCode = equipment.CalculateEquipmentCode().ToString();
            HeroVisualProfile profile = HeroVisualExtractor.Extract(hero, equipmentSnapshot: equipment, equipmentSource: "百科当前人物立绘的完整EquipmentCode（不是现场装备或身份推测，武器槽已按百科肖像规则清空）", appearance: appearance);
            string bannerCode = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;

            string hardFacts = profile.BuildVisualSummary(includeMount: false);
            string directorFacts = $"【纪元时间】卡拉迪亚历 {TaleWorlds.CampaignSystem.CampaignTime.Now.GetYear} 年\n" + profile.BuildDirectorOnlyFacts();
            _generationCount++;
            // 跨会话变体去重：_generationCount 是弹窗实例字段，重开弹窗即归零，
            // 必须统计本存档已留存的百科肖像版本，并把已用过的场景母题回传导演避让。
            int priorVersions = 0;
            var usedMotifs = new List<string>();
            string recentActions = string.Empty;
            try
            {
                var priorItems = DiskImageCacheManager.GetAllCachedIllustrations(_scope.CampaignKey)
                    ?.Where(i => string.Equals(i.SubjectKey, $"Hero_{hero.StringId}", StringComparison.OrdinalIgnoreCase)
                              && string.Equals(i.Category, "encyclopedia", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(i => i.CreatedTime)
                    .ToList();
                priorVersions = priorItems?.Count ?? 0;
                recentActions = IllustrationDirection.BuildActionHistory(priorItems);
                foreach (string motif in (priorItems ?? new List<CachedIllustrationItem>())
                    .Select(i => ExtractSceneMotif(i.Prompt))
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct()
                    .Take(3))
                {
                    usedMotifs.Add(motif);
                }
            }
            catch { }
            string artDirection = GenerateDiversePoseDirective();
            if (!string.IsNullOrWhiteSpace(recentActions)) artDirection += "\n" + recentActions;
            if (_generationCount > 1 || priorVersions > 0) artDirection += "\n" + VisualDirectorEngine.BuildRedrawVariationDirective(_generationCount + priorVersions);
            if (usedMotifs.Count > 0) artDirection += $"\n【已用过的场景母题·须避开】：{string.Join("；", usedMotifs)}——结合本次人物行动选择场景与镜头，不仅更换背景。";
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
                var genRefsList = new System.Collections.Generic.List<IllustrationReferenceImage>();
                if (!string.IsNullOrWhiteSpace(base64Image))
                {
                    var r = new IllustrationReferenceImage(base64Image, $"人物【{heroName}】的身份参考图：锁定容貌、发型肤色与实际装备；人物行动、手势、视线和机位由导演重新构思；依据新场景重建人物体积、衣褶、透视与受光，以统一艺术画风完整重绘。", IllustrationReferenceKind.Character);
                    refs.Add(r);
                    genRefsList.Add(r);
                }
                // 纹章由原生渲染导出，导出控件不向屏幕绘制；取消信号贯穿请求
                if (!string.IsNullOrWhiteSpace(bannerCode))
                {
                    string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(bannerCode, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(emblemB64))
                    {
                        var emblemRef = new IllustrationReferenceImage(emblemB64, "该家族真实纹章标准样图：当画面因已确认事实出现盾牌或纹章罩袍时，必须与此一致绘制，严禁编造或改动图腾；没有载体证据时不要添加纹章载体，严禁在普通胸甲表面硬印纹章", IllustrationReferenceKind.Emblem);
                        refs.Add(emblemRef);
                        // 仅当人物实际穿戴明确纹章罩袍布料时，才作为生图垫图；普通甲胄不送垫图，防止模型强行在胸甲金属表面硬印大纹章
                        bool hasHeraldicCloth = profile.HasHeraldicArmor || profile.BannerEquipmentDetails.Count > 0;
                        if (hasHeraldicCloth)
                        {
                            genRefsList.Add(emblemRef);
                        }
                    }
                }
                var direction = await VisualDirectorEngine.CreateDirectionAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                string detailedPrompt = direction.Prompt;
                IllustratorRuntime.Post(() => { if (!_closed) _dataSource.StatusText = "构思完成，正在绘制画卷（等待生图模型返回）..."; });
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)genRefsList;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, genRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, string.IsNullOrWhiteSpace(direction.Title) ? $"{heroName} 纪事肖像" : direction.Title, _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false, theme: direction.Theme, actionSummary: direction.ActionSummary);
                }
                return new GenerationCompletion(result, saved, effectivePrompt);
            }, completion =>
            {
                if (completion.SavedItem != null) DiskImageCacheManager.SetDefault(completion.SavedItem, _scope.CampaignKey);
                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null &&
                    PublishImage(completion.SavedItem, completion.Result.ImageBytes, completion.Prompt))
                {
                    _dataSource.SetReady(completion.SavedItem?.ThemeText ?? "纪事肖像绘制完成");
                }
                else
                {
                    _dataSource.SetReady($"绘制失败: {completion.Result?.ErrorMessage ?? "未能保存图像"}");
                }
            }, error => _dataSource.SetReady($"生成异常: {error}"));
        }

        private static string GenerateDiversePoseDirective()
        {
            return "【百科构图自主推导】：先依据身份、性格与情绪决定人物此刻正在做什么，再推导姿态、手部动作和视线，最后选择镜头与取景。" +
                "站立、坐姿或轻微动作均可；让行动具有清楚的注意对象，不把双手下垂展示装备作为默认构图，不把复杂动作当成创作要求。" +
                "需要坐靠时使用清楚且合理的支撑，避免一边跨坐一边踮脚、扭腰或同时撑扶多处。" +
                "在非具名艺术布景中统一重绘人物与环境；参考近期行动避免重复的展示姿势，站立时同样明确手势和视线所服务的行动。" +
                "单人独立肖像不添加武器、盾牌、旗帜或坐骑，服饰和身份细节按事实保持。";
        }

        /// <summary>从已存提示词中截取【场景空间】开头作为场景母题，用于跨版本去重。</summary>
        private static string ExtractSceneMotif(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;
            const string marker = "【场景空间】";
            int idx = prompt.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            int start = idx + marker.Length;
            int end = prompt.IndexOf("【", start, StringComparison.Ordinal);
            string section = (end > start ? prompt.Substring(start, end - start) : prompt.Substring(start)).Trim();
            if (section.Length > 40) section = section.Substring(0, 40);
            return section.TrimEnd('，', '。', '；', '、', '：', ' ');
        }

        private static string GenerateConversationSceneVariation(ConversationVisualContext context)
        {
            // 构图全权交给导演：只给自由创作授权 + 双人交互事实约束，不再提供预写取景句式。
            return "【构图自由创作】：镜头景别、机位角度、前景运用与双方瞬间姿态由你依据现场事实与近三轮对话氛围全权自由创作，" +
                "不拘泥任何固定构图模板。【双人交互事实】：两人处于面对面真实交谈情境中。";
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
            variation += "\n" + IllustrationDirection.ReadEventActionHistory(_scope.CampaignKey, key, _category);
            if (_generationCount > 1) variation += "\n" + VisualDirectorEngine.BuildRedrawVariationDirective(_generationCount);
            var promptPlan = new IllustrationPromptPlan("最近三轮对话联动的场景插画", convContext.BuildHardFacts(), convContext.BuildArtDirection(variation), convContext.BuildDirectorOnlyFacts());
            TaleWorlds.Library.Debug.Print($"[Illustrator] ConvScene host='{convContext.EnvironmentProfile?.HostSceneDescription ?? ""}' loc='{convContext.EnvironmentProfile?.SpecificLocation ?? ""}' scene='{convContext.EnvironmentProfile?.RealSceneName ?? ""}'");
            TaleWorlds.Library.Debug.Print($"[Illustrator] ConvLight sceneTime={convContext.EnvironmentProfile?.HasSceneTime == true}, time='{convContext.EnvironmentProfile?.TimeOfDay ?? ""}'");
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
            string playerName = player?.Name != null ? player.Name.ToString() : "玩家主角";
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
                        "会面现场的3D实景画面：用于确认可辨认的昼夜、采光、环境布局、站位及当前人物可见装备外观；与人物立绘按身份对应核对，不把背景士兵装备移给会话对象。未读取到场景时间时，以此现场画面判断昼夜；禁止绘制界面、对话框、字幕、名牌与文字", IllustrationReferenceKind.Scene));
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
                        var r = new IllustrationReferenceImage(b64, $"【角色参考图1 - 玩家: {playerName}】这是玩家主角的独家身份与外观参考：仅用于锁定其面部五官轮廓与全身装备形制（包括头盔/战盔护具）；人物位置与姿态由导演依据现场空间关系推导；严禁直接复刻或贴图游戏3D多边形网格、平坦贴图光影与建模质感；必须从零重新进行纯正艺术手绘该人物；严禁篡改其头盔战盔款式，严禁将玩家降格为随从！", IllustrationReferenceKind.Character);
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
                        var r = new IllustrationReferenceImage(b64, $"【角色参考图2 - 对话对象: {partnerName}】这是对话对方的独家身份与外观参考：仅用于锁定其面部五官轮廓与全部装备形制；人物位置与姿态由导演依据现场空间关系推导；严禁直接复刻或贴图游戏3D多边形网格；必须从零重新进行纯正艺术手绘该人物，严禁将此人与玩家混淆，严禁擅自将青年或壮年人物画为白发老翁！", IllustrationReferenceKind.Character);
                        directorRefs.Add(r);
                        genRefs.Add(r);
                    }
                }

                foreach (var spec in emblemSpecs ?? new List<EmblemSpec>())
                {
                    string b64 = await BannerEmblemComposer.ComposeToBase64Async(spec.Code, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        var r = new IllustrationReferenceImage(b64, $"{spec.Side}一方【{spec.Owner}】的真实纹章标准样图：当画面中属于{spec.Side}的一处已确认纹章载体（如盾牌或背景军旗）真实出现时，必须以此一致的形状与配色绘制，严禁编造图腾；没有载体证据时不要添加纹章载体，严禁在普通胸甲金属表面硬印纹章！", IllustrationReferenceKind.Emblem);
                        directorRefs.Add(r);
                        // 仅当该方人物确实身穿纹章罩袍或持有明确纹章盾牌时，才加入生图垫图，防止生图模型在普通金属胸甲上硬印纹章！
                        bool isPlayerSide = spec.Side == "玩家";
                        var targetProfile = isPlayerSide ? convContext.MainHeroProfile : convContext.InterlocutorProfile;
                        bool sideHasHeraldic = targetProfile != null && (targetProfile.HasHeraldicArmor || targetProfile.HasHeraldicShield || targetProfile.BannerEquipmentDetails.Count > 0);
                        if (sideHasHeraldic)
                        {
                            genRefs.Add(r);
                        }
                    }
                }

                var direction = await VisualDirectorEngine.CreateDirectionAsync(promptPlan, directorRefs, options, token).ConfigureAwait(false);
                string detailedPrompt = direction.Prompt;
                IllustratorRuntime.Post(() => { if (!_closed) _dataSource.StatusText = "构思完成，正在绘制画卷（等待生图模型返回）..."; });
                var finalGenRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)genRefs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, finalGenRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, string.IsNullOrWhiteSpace(direction.Title) ? $"与 {partnerName} 的会晤纪事" : direction.Title, _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false, theme: direction.Theme, actionSummary: direction.ActionSummary);
                }
                return new GenerationCompletion(result, saved, effectivePrompt);
            }, completion =>
            {
                if (completion.SavedItem != null) DiskImageCacheManager.SetDefault(completion.SavedItem, _scope.CampaignKey);
                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null &&
                    PublishImage(completion.SavedItem, completion.Result.ImageBytes, completion.Prompt))
                {
                    _dataSource.SetReady(completion.SavedItem?.ThemeText ?? "会晤插画绘制完成");
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
            if (!string.IsNullOrWhiteSpace(item?.Title)) _dataSource.TitleText = item.Title;
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
