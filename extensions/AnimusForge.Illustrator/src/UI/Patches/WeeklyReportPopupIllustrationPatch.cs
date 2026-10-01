using System;
using TaleWorlds.GauntletUI.BaseTypes;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Context;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI.Gallery;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator.UI.Patches
{
    public sealed class WeeklyReportIllustrationOverlayVM : ViewModel, global::AnimusForge.IWeeklyIllustrationSink
    {
        private string _spriteName = string.Empty;
        private bool _hasIllustration;
        private bool _isLoading;
        private string _statusText = "正在构思本周纪事画卷...";
        private string _titleText = string.Empty;
        private string _promptText = string.Empty;
        private bool _showPrompt;
        private readonly Action _onRegenerate;

        public WeeklyReportIllustrationOverlayVM(string title, Action onRegenerate)
        {
            _titleText = title ?? "帝国纪事油画";
            _onRegenerate = onRegenerate;
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get => _spriteName;
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    OnPropertyChangedWithValue(value, nameof(SpriteName));
                }
            }
        }

        [DataSourceProperty]
        public bool HasIllustration
        {
            get => _hasIllustration;
            set
            {
                if (value != _hasIllustration)
                {
                    _hasIllustration = value;
                    OnPropertyChangedWithValue(value, nameof(HasIllustration));
                }
            }
        }

        [DataSourceProperty]
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value != _isLoading)
                {
                    _isLoading = value;
                    OnPropertyChangedWithValue(value, nameof(IsLoading));
                }
            }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value != _statusText)
                {
                    _statusText = value;
                    OnPropertyChangedWithValue(value, nameof(StatusText));
                }
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, nameof(TitleText));
                }
            }
        }

        [DataSourceProperty]
        public string PromptText
        {
            get => _promptText;
            set
            {
                if (value != _promptText)
                {
                    _promptText = value;
                    OnPropertyChangedWithValue(value, nameof(PromptText));
                }
            }
        }

        [DataSourceProperty]
        public bool ShowPrompt
        {
            get => _showPrompt;
            set
            {
                if (value != _showPrompt)
                {
                    _showPrompt = value;
                    OnPropertyChangedWithValue(value, nameof(ShowPrompt));
                }
            }
        }

        public void ExecuteRegenerate()
        {
            if (IsLoading) return;
            _onRegenerate?.Invoke();
        }

        public void ExecuteTogglePrompt()
        {
            ShowPrompt = !ShowPrompt;
        }

        public void ExecuteCopyPrompt()
        {
            if (string.IsNullOrWhiteSpace(PromptText)) return;
            try
            {
                Input.SetClipboardText(PromptText);
                StatusText = "提示词已复制到剪贴板";
            }
            catch (Exception ex)
            {
                StatusText = "复制失败: " + ex.Message;
            }
        }

        public void ExecuteOpenGallery()
        {
            IllustratorGalleryPopup.Show();
        }

        public void ExecuteClose()
        {
            WeeklyReportPopupIllustrationPatch.CloseOverlay();
        }
    }

    public static class WeeklyReportPopupIllustrationPatch
    {
        internal static Widget VisualRoot => _overlayLayer?.UIContext?.Root;
        private static MovableGauntletLayer _overlayLayer;
        private static WeeklyReportIllustrationOverlayVM _overlayVm;
        // Whichever surface is live: the floating weekly overlay or the inline world-bulletin slot.
        private static global::AnimusForge.IWeeklyIllustrationSink _sink;
        private static global::AnimusForge.WorldBulletinIllustrationVM _bulletinSlot;
        private static CachedIllustrationItem _activeItem;
        private static string _currentEventKey;
        private static WeeklyReportVisualContext _currentContext;
        private static IllustrationScope _scope;
        private static ScreenBase _ownerScreen;
        private static string _activeSpriteName;
        private static bool _closing;
        private static int _redrawCount;

        public static void Patch(Harmony harmony)
        {
            try
            {
                Type targetType = AccessTools.TypeByName("AnimusForge.DevWeeklyReportPopup");
                if (targetType == null)
                {
                    Debug.Print("[Illustrator] DevWeeklyReportPopup type not found; skipping popup hook.");
                    return;
                }

                MethodInfo showMethod = AccessTools.Method(targetType, "Show");
                if (showMethod != null)
                {
                    harmony.Patch(showMethod, postfix: new HarmonyMethod(typeof(WeeklyReportPopupIllustrationPatch), nameof(DevWeeklyReportPopup_Show_Postfix)));
                }

                global::AnimusForge.WorldBulletinPanelIllustrationBridge.AttachSlot = AttachWorldBulletinSlot;

                MethodInfo closeMethod = AccessTools.Method(targetType, "Close");
                if (closeMethod != null)
                {
                    harmony.Patch(closeMethod, postfix: new HarmonyMethod(typeof(WeeklyReportPopupIllustrationPatch), nameof(DevWeeklyReportPopup_Close_Postfix)));
                }

                Debug.Print("[Illustrator] Hooked into DevWeeklyReportPopup successfully.");
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to hook DevWeeklyReportPopup: {ex.Message}");
            }
        }

        public static void DevWeeklyReportPopup_Show_Postfix(bool __result, string titleText, string subtitleText, string bodyText)
        {
            if (!__result)
            {
                return;
            }

            if (!IllustratorRuntime.IsEnabled())
            {
                return;
            }

            try
            {
                AttachOverlay(titleText, subtitleText, bodyText);
            }
            catch (Exception ex)
            {
                CloseOverlay();
                Debug.Print($"[Illustrator] Error attaching overlay to weekly report: {ex.Message}");
            }
        }

        public static void DevWeeklyReportPopup_Close_Postfix()
        {
            CloseOverlay();
        }

        private static void AttachOverlay(string title, string subtitleText, string bodyText)
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null)
            {
                return;
            }

            // 先关旧 overlay，再提取上下文——CloseOverlay 内部会清空 _currentContext，
            // 顺序颠倒会导致新生成被 TriggerRegenerate 因 ctx=null 静默跳过
            CloseOverlay();
            _closing = false;
            _ownerScreen = topScreen;
            IllustrationScope ownerScope = null;
            ownerScope = new IllustrationScope(topScreen, "weekly_report", () => CloseOverlayForScope(ownerScope));
            _scope = ownerScope;
            _currentContext = WeeklyReportContextExtractor.ExtractFromWeeklyReport(title, subtitleText, bodyText);
            _currentEventKey = "weekly_report:" + DiskImageCacheManager.ComputeHash(title + ":" + subtitleText);
            _redrawCount = 0;
            Debug.Print($"[Illustrator] Weekly report popup opened: '{title}' context={(_currentContext != null)}");

            _overlayVm = new WeeklyReportIllustrationOverlayVM(title, TriggerRegenerate);
            _sink = _overlayVm;
            var layer = new MovableGauntletLayer("WeeklyReportIllustrationOverlay", 4010, false);
            _overlayLayer = layer;
            var movieIdentifier = layer.LoadMovie("WeeklyReportIllustrationOverlay", _overlayVm);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
            _overlayLayer = layer;
            topScreen.AddLayer(_overlayLayer);

            BeginCachedLoad("点击【生成纪事插画】绘制本周大事件");
        }

        private static bool AttachWorldBulletinSlot(global::AnimusForge.WorldBulletinIllustrationVM slot, string eventId, string title, string subtitleText, string bodyText)
        {
            if (slot == null) return false;
            bool enabled;
            // IsEnabled asserts the main thread and may throw before the runtime's first tick; treat that as disabled.
            try { enabled = IllustratorRuntime.IsEnabled(); } catch { enabled = false; }
            if (!enabled) return false;
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null) return false;
            try
            {
                CloseOverlay();
                _closing = false;
                _ownerScreen = topScreen;
                IllustrationScope ownerScope = null;
                ownerScope = new IllustrationScope(topScreen, "weekly_report", () => CloseOverlayForScope(ownerScope));
                _scope = ownerScope;
                _currentContext = WeeklyReportContextExtractor.ExtractFromWeeklyReport(title, subtitleText, bodyText);
                // Bulletin ids are unique per issue, so the cache key survives reopening from the chronicle.
                _currentEventKey = "weekly_report:" + DiskImageCacheManager.ComputeHash(string.IsNullOrWhiteSpace(eventId) ? title + ":" + subtitleText : eventId);
                _redrawCount = 0;
                _bulletinSlot = slot;
                _sink = slot;
                slot.TitleText = title ?? "";
                slot.StatusText = "正在为本期快报绘制新插画...";
                slot.OnRegenerate = TriggerRegenerate;
                slot.OnOpenGallery = () => IllustratorGalleryPopup.Show();
                slot.OnDelete = DeleteCurrentBulletinIllustration;
                slot.IsAvailable = true;
                slot.NotifyHandlersChanged();
                Debug.Print($"[Illustrator] World bulletin panel slot attached: '{title}' context={(_currentContext != null)}");
                // Every bulletin opening requests fresh artwork, including archived issues.
                // Keep earlier works in the gallery, but never load them into this slot.
                TriggerRegenerate();
                return true;
            }
            catch (Exception ex)
            {
                CloseOverlay();
                slot.IsAvailable = false;
                Debug.Print($"[Illustrator] Error attaching world bulletin slot: {ex.Message}");
                return false;
            }
        }

        private static void DeleteCurrentBulletinIllustration()
        {
            if (_bulletinSlot == null || _scope == null) return;
            CachedIllustrationItem item = _activeItem;
            if (item == null)
            {
                _bulletinSlot.StatusText = "这张插画尚未存档，无法删除。";
                return;
            }
            if (!DiskImageCacheManager.DeleteItem(item, _scope.CampaignKey))
            {
                _bulletinSlot.StatusText = "删除失败，请到画廊中处理。";
                return;
            }
            if (!string.IsNullOrEmpty(_activeSpriteName))
            {
                GauntletTextureLoader.ReleaseSprite(_activeSpriteName);
                _activeSpriteName = null;
            }
            _activeItem = null;
            _bulletinSlot.ShowPrompt = false;
            _bulletinSlot.SpriteName = string.Empty;
            _bulletinSlot.PromptText = string.Empty;
            _bulletinSlot.HasIllustration = false;
            _bulletinSlot.StatusText = "插画已移入回收区，点击【重绘】重新绘制。";
        }

        private static void BeginCachedLoad(string idleStatus)
        {
            string campaignKey = _scope.CampaignKey;
            string cachedEventKey = _currentEventKey;
            IllustrationScope openedScope = _scope;
            bool autoGenerate = IllustratorSettings.Instance.AutoGenerateWeeklyReportIllustration;
            _sink.IsLoading = true;
            bool cacheLoadStarted = IllustratorRuntime.Start(
                () => Task.Run(() => DiskImageCacheManager.LoadImage(cachedEventKey, campaignKey, "weekly_report")),
                (cached, error) =>
                {
                    if (!ReferenceEquals(_scope, openedScope) || !openedScope.IsCurrent || _sink == null || _redrawCount != 0) return;
                    if (error != null) Debug.Print("[Illustrator] Weekly cache read failed: " + error.Message);
                    var cacheOptions = IllustratorRuntime.CaptureOptions();
                    if (error == null && cached != null && Publish(cached, cached.Prompt))
                    {
                        Debug.Print("[Illustrator] Weekly overlay attached: cached=true");
                        _sink.StatusText = DiskImageCacheManager.CachedDisplayStatus(cached, cacheOptions?.StyleFingerprint);
                        _sink.IsLoading = false;
                    }
                    else if (autoGenerate)
                    {
                        Debug.Print("[Illustrator] Weekly overlay cache miss or style mismatch; auto-generating.");
                        TriggerRegenerate();
                    }
                    else
                    {
                        Debug.Print("[Illustrator] Weekly overlay attached: cached=false, autoGen=false");
                        _sink.StatusText = idleStatus;
                        _sink.IsLoading = false;
                    }
                });
            if (!cacheLoadStarted)
            {
                _sink.StatusText = autoGenerate ? "正在准备最新纪事画卷..." : idleStatus;
                _sink.IsLoading = false;
                if (autoGenerate) TriggerRegenerate();
            }
        }

        private static void TriggerRegenerate()
        {
            try { TriggerRegenerateCore(); }
            catch (Exception ex)
            {
                if (_sink != null) { _sink.IsLoading = false; _sink.StatusText = "生成准备失败：" + ex.Message; }
            }
        }

        private static void TriggerRegenerateCore()
        {
            if (_sink == null || _currentContext == null || _scope == null)
            {
                Debug.Print($"[Illustrator] Weekly regenerate skipped: vm={(_sink != null)} ctx={(_currentContext != null)} scope={(_scope != null)}");
                return;
            }

            _sink.IsLoading = true;
            _sink.HasIllustration = false;
            _sink.StatusText = "正在从本周要闻中选择事件与关键瞬间，构思纪事画卷...";

            string eventKey = _currentEventKey;
            var context = _currentContext;
            // 周报重绘围绕事件叙事变化，不复用百科肖像的动作与镜头变体。
            _redrawCount++;
            string artDirection = context.BuildArtDirection();
            string variation = GenerateWeeklyVariation();
            if (!string.IsNullOrWhiteSpace(variation)) artDirection += "\n" + variation;
            if (_redrawCount > 1) artDirection += "\n" + BuildWeeklyRedrawDirective(_redrawCount);
            string hardFacts = context.BuildHardFacts();
            string directorFacts = context.BuildDirectorOnlyFacts();
            var options = IllustratorRuntime.CaptureOptions();
            if (_bulletinSlot != null)
            {
                options = options?.WithImageSize("1536x1024");
                const string composition = "【快报版式】画幅为横向3:2，围绕本期事件重新创作完整插画；不要照搬旧作品，不要将方图或竖图拉伸为横图。";
                artDirection += "\n" + composition;
                hardFacts += "\n" + composition;
            }
            string campaignKey = _scope.CampaignKey;
            var generationScope = _scope;

            var protagonist = context.ProtagonistHero;
            var appearance = context.ProtagonistProfile?.Appearance;
            string protagonistName = protagonist?.Name?.ToString() ?? "当事人";
            string bannerCode = (protagonist?.Clan?.Banner ?? protagonist?.Clan?.Kingdom?.Banner)?.BannerCode;

            bool started = _scope.Run(async token =>
            {
                Debug.Print("[Illustrator] Weekly generation task started.");
                GenerationDiagnostics.Current?.SetSubject(eventKey);
                string actionHistory = string.Empty;
                try
                {
                    actionHistory = await Task.Run(() => IllustrationDirection.ReadEventActionHistory(campaignKey, eventKey, "weekly_report"), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { Debug.Print("[Illustrator] Weekly action history read failed: " + ex.Message); }
                string workerArtDirection = artDirection;
                if (!string.IsNullOrWhiteSpace(actionHistory)) workerArtDirection += "\n" + actionHistory;
                var promptPlan = new IllustrationPromptPlan("周报历史纪事插画", hardFacts, workerArtDirection, directorFacts);
                var refs = new List<IllustrationReferenceImage>();
                // 离屏舞台提取在 scope 内携带 token：关闭弹窗或重新生成时旧任务立即取消并拆舞台
                Task<CharacterPortraitReferences> portraitStage = null;
                if (options?.EnableOffscreenRendering == true && protagonist != null)
                {
                    portraitStage = ScreenCaptureHelper.ExtractHeroPortraitReferencesAsync(protagonist, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true, appearance: appearance);
                }
                if (portraitStage != null)
                {
                    var portraits = await portraitStage.ConfigureAwait(false);
                    IllustrationReferenceRouting.AddCharacter(refs, null, portraits, protagonistName,
                        $"本期提及人物【{protagonistName}】的可选身份参考：仅在导演选中的事件确实涉及他且需要他入画时，锁定五官、须发和实际穿戴；与同名头肩图属于同一人。供图不指定主角、人数、景别或骑乘动作；正文未选此人时不要把他加入其他事件。人物参与事件的行动与环境按导演纪事描述统一重绘，不复制原立绘姿势、背景和游戏渲染质感。", eventReference: true);
                }
                // 纹章由原生渲染导出，保留完整背景、配色、描边与变换
                if (!string.IsNullOrWhiteSpace(bannerCode))
                {
                    string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(bannerCode, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(emblemB64))
                    {
                        refs.Add(new IllustrationReferenceImage(emblemB64, $"参考人物【{protagonistName}】所属家族的真实纹章标准样图，不代表本期所有事件的阵营：只有所选事件确实涉及该家族且存在有依据的纹章载体时使用，底色、徽记形状和配色须一致；不将该纹章贴给其他参与方，不因提供样图新增盾牌或旗帜载体", IllustrationReferenceKind.EventEmblem));
                    }
                }

                var direction = await VisualDirectorEngine.CreateDirectionAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                string prompt = direction.Prompt;
                IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, generationScope) && !token.IsCancellationRequested && _sink != null) _sink.StatusText = direction.StatusText + "，正在绘制纪事画卷..."; });
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(prompt, genRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? prompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImage(eventKey, result.ImageBytes, effectivePrompt, string.IsNullOrWhiteSpace(direction.Title) ? context.Title : direction.Title, "weekly_report", campaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false, theme: direction.Theme, actionSummary: direction.ActionSummary, diagnosticId: result.DiagnosticId, directorStatus: direction.DirectionStatus, directorStatusText: direction.StatusText, directorFallbackReason: direction.FallbackReason, styleFingerprint: options?.StyleFingerprint);
                    if (saved != null) DiskImageCacheManager.PromoteDefaultIfNewest(saved, campaignKey);
                }
                return new { Result = result, Saved = saved, Prompt = effectivePrompt };
            }, completion =>
            {
                if (completion.Saved != null) DiskImageCacheManager.PromoteDefaultIfNewest(completion.Saved, campaignKey);
                _sink.PromptText = completion.Prompt;
                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null && Publish(completion.Saved, completion.Prompt, completion.Result.ImageBytes))
                {
                    _sink.StatusText = completion.Saved?.DisplayStatusText ?? "【本周纪事油画已绘制完成】";
                }
                else
                {
                    _sink.IsLoading = false;
                    _sink.StatusText = "绘制未成功: " + (completion.Result?.ErrorMessage ?? "未能保存图像");
                }
            }, error =>
            {
                _sink.IsLoading = false;
                _sink.StatusText = "异常: " + error;
            });
            if (!started && _sink != null)
            {
                _sink.IsLoading = false;
            }
        }

        /// <summary>周报纪事画的随机构图变体——同一事件每次生成应有不同取景。</summary>
        private static string GenerateWeeklyVariation()
        {
            // 构图全权交给导演：只给自由创作授权，不再提供预写取景句式。
            return "【构图自由创作】：取景景别、机位角度、叙事瞬间与前景运用由你依据事件要闻与事实区全权自由创作，" +
                "挑选最有叙事力的瞬间，不拘泥任何固定构图模板。";
        }

        private static string BuildWeeklyRedrawDirective(int redrawIndex)
        {
            return $"【纪事重绘 · 第 {redrawIndex} 次绘制】参考本期最近作品的事件与行动摘要，自主选择同一事件的另一可信瞬间、观察位置或叙事重点，也可从本期正文另选一则明确事件。" +
                "保留所选事件的参与方、地点关联和已知结果；不能为变化编造新事件，不能用更换领主展示姿势代替事件叙事。旧作品与人物参考不是发生事实，身份装备只在对应人物实际入画时生效。";
        }

        private static bool Publish(CachedIllustrationItem item, string prompt, byte[] imageBytes = null)
        {
            if (item == null && imageBytes == null) return false;
            if (!string.IsNullOrEmpty(_activeSpriteName))
            {
                GauntletTextureLoader.ReleaseSprite(_activeSpriteName);
                _activeSpriteName = null;
            }
            string spriteName = (item?.Key ?? "weekly_" + Guid.NewGuid().ToString("N")) + "_weekly";
            var bytes = imageBytes ?? item.ImageData;
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, bytes);
            if (sprite == null) return false;
            _activeSpriteName = spriteName;
            _activeItem = item;
            // Providers may ignore requested dimensions. Fit their actual output without stretching.
            _bulletinSlot?.FitImage(sprite.Width, sprite.Height);
            if (!string.IsNullOrWhiteSpace(item?.Title)) _sink.TitleText = item.Title;
            _sink.SpriteName = spriteName;
            _sink.PromptText = prompt;
            _sink.HasIllustration = true;
            _sink.IsLoading = false;
            return true;
        }

        private static void CloseOverlayForScope(IllustrationScope ownerScope)
        {
            if (ownerScope != null && ReferenceEquals(_scope, ownerScope)) CloseOverlay();
        }

        public static void CloseOverlay()
        {
            if (_closing) return;
            _closing = true;
            try
            {
                _scope?.Close();
                if (!string.IsNullOrEmpty(_activeSpriteName))
                {
                    GauntletTextureLoader.ReleaseSprite(_activeSpriteName);
                    _activeSpriteName = null;
                }
                if (_overlayLayer != null && _ownerScreen != null)
                {
                    _ownerScreen.RemoveLayer(_overlayLayer);
                }
            }
            catch
            {
            }
            finally
            {
                _overlayLayer = null;
                _overlayVm = null;
                _sink = null;
                _bulletinSlot = null;
                _activeItem = null;
                _currentContext = null;
                _currentEventKey = null;
                _scope = null;
                _ownerScreen = null;
            }
        }
    }
}
