using System;
using TaleWorlds.GauntletUI.BaseTypes;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json.Linq;
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
                global::AnimusForge.WorldBulletinPanelIllustrationBridge.PrepareIssue = BulletinIllustrationPreloader.Prepare;
                global::AnimusForge.WorldBulletinPanelIllustrationBridge.PrepareSelection = BulletinIllustrationPreloader.PrepareSelection;
                global::AnimusForge.WorldBulletinPanelIllustrationBridge.CancelSelection = BulletinIllustrationPreloader.CancelSelection;
                global::AnimusForge.WorldBulletinPanelIllustrationBridge.AwaitSelection = BulletinIllustrationPreloader.AwaitSelection;
                BulletinIllustrationPreloader.Updated = RefreshPreparedBulletin;

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

            IllustratorRuntime.GenerationUpdated += OnGenerationUpdated;
            if (!JoinPendingGeneration()) BeginCachedLoad("点击【生成纪事插画】绘制本周大事件");
        }

        private static bool AttachWorldBulletinSlot(global::AnimusForge.WorldBulletinIllustrationVM slot, string eventId, string title, string subtitleText, string bodyText, global::AnimusForge.WorldBulletinIllustrationPlan plan)
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
                _currentContext = plan == null ? WeeklyReportContextExtractor.ExtractFromWeeklyReport(title, subtitleText, bodyText) : WeeklyReportContextExtractor.ExtractFromPlan(plan);
                // Bulletin ids are unique per issue, so the cache key survives reopening from the chronicle.
                _currentEventKey = plan == null ? BulletinIllustrationPreloader.KeyFor(eventId, title, subtitleText, bodyText) : BulletinIllustrationPreloader.KeyFor(plan);
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
                BulletinIllustrationPreloader.Ensure(_currentEventKey, _currentContext);
                RefreshPreparedBulletin(_currentEventKey, null);
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
            BulletinIllustrationPreloader.MarkDeleted(_currentEventKey);
        }

        private static void RefreshPreparedBulletin(string key, GenerationResult result)
        {
            if (_bulletinSlot == null || _scope?.IsCurrent != true || key != _currentEventKey) return;
            var job = BulletinIllustrationPreloader.Find(key);
            if (job == null) return;
            _sink.IsLoading = job.Pending;
            _sink.StatusText = job.Status;
            if (job.Pending) { _sink.HasIllustration = false; return; }
            if (!job.Ready) return;
            if (result != null)
            {
                if (!Publish(result.Saved, result.Prompt, result.Result.ImageBytes)) _sink.StatusText = "本期配图加载失败，可点击重绘。";
                return;
            }
            if (_sink.HasIllustration) return;
            var viewScope = _scope;
            int load = ++_redrawCount;
            _sink.IsLoading = true;
            if (!IllustratorRuntime.Start(() => Task.Run(() => DiskImageCacheManager.LoadImage(key, viewScope.CampaignKey, "weekly_report")),
                (cached, error) =>
                {
                    if (!ReferenceEquals(viewScope, _scope) || !viewScope.IsCurrent || _bulletinSlot == null || key != _currentEventKey || load != _redrawCount) return;
                    _sink.IsLoading = false;
                    if (error != null || cached == null || !Publish(cached, cached.Prompt))
                        _sink.StatusText = "本期配图读取失败，可点击重绘。";
                    else _sink.StatusText = cached.DisplayStatusText;
                }))
            {
                _sink.IsLoading = false;
                _sink.StatusText = "配图读取繁忙，请稍后重开本期快报。";
            }
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

        private static IllustrationScope _joinedGeneration;

        private static bool JoinPendingGeneration()
        {
            if (_scope == null || _sink == null || _bulletinSlot != null) return false;
            var pending = IllustratorRuntime.FindGenerating("weekly_report", _currentEventKey);
            if (pending == null) return false;
            _joinedGeneration = pending;
            ++_redrawCount; // invalidate older asynchronous cache callbacks
            _sink.IsLoading = true;
            _sink.StatusText = "画卷正在后台生成，完成后会通知并更新画廊。";
            return true;
        }

        private static void OnGenerationUpdated(IllustrationGenerationUpdate update)
        {
            if (_scope == null || !_scope.IsCurrent || _sink == null || _bulletinSlot != null ||
                !ReferenceEquals(update.Source, _joinedGeneration) || update.CampaignKey != _scope.CampaignKey ||
                update.SubjectKey != _currentEventKey || update.Category != "weekly_report") return;
            _joinedGeneration = null;
            if (ReferenceEquals(update.Source, _scope)) return;
            _sink.IsLoading = false;
            if (update.Saved != null && Publish(update.Saved, update.Saved.Prompt)) _sink.StatusText = update.Saved.DisplayStatusText;
            else _sink.StatusText = "绘制失败：" + (update.Error ?? "面板图像加载失败；可在画廊查看。");
        }

        internal sealed class GenerationResult
        {
            internal ImageGenerationResult Result;
            internal CachedIllustrationItem Saved;
            internal string Prompt;
        }

        private static void TriggerRegenerateCore()
        {
            if (_sink == null || _currentContext == null || _scope == null) return;
            if (_bulletinSlot != null)
            {
                _sink.HasIllustration = false;
                BulletinIllustrationPreloader.Ensure(_currentEventKey, _currentContext, true);
                RefreshPreparedBulletin(_currentEventKey, null);
                return;
            }
            if (JoinPendingGeneration()) return;
            _sink.IsLoading = true;
            _sink.HasIllustration = false;
            _sink.StatusText = "正在构思本周纪事插画...";
            var owner = _scope;
            StartGeneration(owner, _currentContext, _currentEventKey, false, ++_redrawCount,
                result =>
                {
                    if (!ReferenceEquals(owner, _scope) || _sink == null) return;
                    if (result.Result?.Success == true && Publish(result.Saved, result.Prompt, result.Result.ImageBytes))
                        _sink.StatusText = result.Saved?.DisplayStatusText ?? "本周纪事已绘制完成";
                    else { _sink.IsLoading = false; _sink.StatusText = "绘制未成功：" + result.Result?.ErrorMessage; }
                }, error => { if (ReferenceEquals(owner, _scope) && _sink != null) { _sink.IsLoading = false; _sink.StatusText = error; } },
                status => { if (ReferenceEquals(owner, _scope) && _sink != null) _sink.StatusText = status; });
        }

        // One shared generation pipeline for the weekly popup and campaign-owned bulletin jobs.
        internal static bool StartGeneration(IllustrationScope generationScope, WeeklyReportVisualContext context,
            string eventKey, bool bulletin, int redrawCount, Action<GenerationResult> complete, Action<string> fail, Action<string> status)
        {
            string artDirection = context.BuildArtDirection();
            string variation = GenerateWeeklyVariation();
            if (!string.IsNullOrWhiteSpace(variation)) artDirection += "\n" + variation;
            if (redrawCount > 1) artDirection += "\n" + BuildWeeklyRedrawDirective(redrawCount);
            string hardFacts = context.BuildHardFacts();
            string directorFacts = context.BuildDirectorOnlyFacts();
            var options = IllustratorRuntime.CaptureOptions();
            if (bulletin)
            {
                options = options?.WithSceneImageSize();
                string composition = "【快报版式】画幅为横向16:9，目标尺寸" + options?.ImageSize + "，围绕已选定的同一事件重新创作完整插画；不要照搬旧作品，不要将方图或竖图拉伸为横图。";
                artDirection += "\n" + composition;
                hardFacts += "\n" + composition;
            }
            string campaignKey = generationScope.CampaignKey;

            // All names, roles, banners and appearance snapshots were captured on the game thread.
            var people = context.Characters.Take(4).ToArray();

            return generationScope.RunGeneration(eventKey, null, async token =>
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
                var banners = new HashSet<string>(StringComparer.Ordinal);
                // Sequential native stages keep GPU work bounded. No per-frame scan or parallel tableau allocation.
                foreach (var person in people)
                {
                    token.ThrowIfCancellationRequested();
                    CharacterPortraitReferences portraits = null;
                    if (options?.EnableOffscreenRendering == true && person.Hero != null && person.Profile?.Appearance != null)
                        portraits = await ScreenCaptureHelper.ExtractHeroPortraitReferencesAsync(person.Hero,
                            cancellationToken: token, cleanTempFiles: options.AutoCleanTempFiles,
                            appearance: person.Profile.Appearance).ConfigureAwait(false);
                    if (portraits != null)
                        IllustrationReferenceRouting.AddCharacter(refs, null, portraits, person.Name,
                            $"事件人物【{person.Name}】，角色【{person.Role}】。全身与同名头肩图属于同一人，只锁定五官、须发和实际穿戴；不得借给另一方使用。人物行动按已选事件重新创作，不复制参考立绘站姿、背景或游戏渲染质感。相关身份不等于亲临现场，是否入画以事件事实为准。", eventReference: true);
                    GenerationDiagnostics.Current?.RecordStage("weekly_participant_references", new JObject {
                        ["heroId"] = person.HeroId, ["name"] = person.Name, ["role"] = person.Role,
                        ["fullBody"] = !string.IsNullOrWhiteSpace(portraits?.FullBody),
                        ["headDetail"] = !string.IsNullOrWhiteSpace(portraits?.HeadDetail)
                    });
                    if (!string.IsNullOrWhiteSpace(person.BannerCode) && banners.Add(person.BannerCode))
                    {
                        string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(person.BannerCode,
                            cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(emblemB64))
                        {
                            string owners = string.Join("、", people.Where(p => p.BannerCode == person.BannerCode).Select(p => p.Name));
                            refs.Add(new IllustrationReferenceImage(emblemB64,
                                $"人物【{owners}】所属家族纹章参考；仅在该阵营已有依据的载体上使用，保留底色和徽记，不贴给敌方，不因提供参考新增旗帜或盾牌。", IllustrationReferenceKind.EventEmblem));
                        }
                    }
                }

                var direction = await VisualDirectorEngine.CreateDirectionAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                string prompt = direction.Prompt;
                IllustratorRuntime.Post(() => { if (generationScope.IsCurrent && !token.IsCancellationRequested) status?.Invoke(direction.StatusText + "，正在绘制纪事画卷..."); });
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
                return new GenerationResult { Result = result, Saved = saved, Prompt = effectivePrompt };
            }, complete, fail, result => result.Saved, result => result.Result?.ErrorMessage ?? "未能保存图像");
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
            return $"【纪事重绘 · 第 {redrawIndex} 次绘制】参考本期最近作品的事件与行动摘要，自主选择同一事件的另一可信瞬间、观察位置或叙事重点，始终围绕已选事件，不改选其他消息。" +
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
            var publishClock = System.Diagnostics.Stopwatch.StartNew();
            GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "weekly_ui_texture_begin", "bytes=" + (bytes?.Length ?? 0));
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, bytes);
            if (sprite == null) { GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "weekly_ui_texture_failed", "bytes=" + (bytes?.Length ?? 0)); return false; }
            _activeSpriteName = spriteName;
            _activeItem = item;
            if (!string.IsNullOrWhiteSpace(item?.Title)) _sink.TitleText = item.Title;
            _sink.SpriteName = spriteName;
            _sink.PromptText = prompt;
            _sink.HasIllustration = true;
            _sink.IsLoading = false;
            GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "weekly_ui_publish_complete", "sprite=" + spriteName + "; bytes=" + (bytes?.Length ?? 0) + "; publishMs=" + publishClock.ElapsedMilliseconds);
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
                IllustratorRuntime.GenerationUpdated -= OnGenerationUpdated;
                _joinedGeneration = null;
                if (_scope != null && !_scope.DetachWindowIfGenerating()) _scope.Close();
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
