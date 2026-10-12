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
using AnimusForge.Illustrator.UI;

namespace AnimusForge.Illustrator.UI.Patches
{
    public sealed class WeeklyReportIllustrationOverlayVM : ViewModel, global::AnimusForge.IWeeklyIllustrationSink
    {
        private string _spriteName = string.Empty;
        private bool _hasIllustration;
        private bool _isLoading;
        private string _statusText = IllustratorUiText.WeeklyThinking;
        private string _titleText = string.Empty;
        private string _promptText = string.Empty;
        private bool _showPrompt;
        private readonly Action _onRegenerate;
        private readonly Action _onRegenerateWithPrompt;
        private readonly string _redrawText;
        private readonly string _redrawWithPromptText;
        private readonly string _redrawBasedOnImageText;
        private readonly string _copyText;
        private readonly string _galleryText;
        private readonly string _closeText;
        internal Action OnRegenerateBasedOnImage;
        [DataSourceProperty] public bool CanRegenerateBasedOnImage => HasIllustration && !IsLoading && OnRegenerateBasedOnImage != null;
        public void ExecuteRegenerateBasedOnImage() { if (CanRegenerateBasedOnImage) OnRegenerateBasedOnImage(); }

        public WeeklyReportIllustrationOverlayVM(string title, Action onRegenerate)
            : this(title, onRegenerate, null)
        {
        }

        public WeeklyReportIllustrationOverlayVM(string title, Action onRegenerate, Action onRegenerateWithPrompt)
        {
            _titleText = title ?? IllustratorUiText.WeeklyDefaultTitle;
            _onRegenerate = onRegenerate;
            _onRegenerateWithPrompt = onRegenerateWithPrompt;
            _redrawText = IllustratorUiText.Redraw;
            _redrawWithPromptText = IllustratorUiText.RedrawWithPrompt;
            _redrawBasedOnImageText = IllustratorUiText.RedrawBasedOnImage;
            _copyText = IllustratorUiText.Copy;
            _galleryText = IllustratorUiText.Gallery;
            _closeText = IllustratorUiText.Close;
        }

        [DataSourceProperty] public string RedrawText => _redrawText;
        [DataSourceProperty] public string RedrawWithPromptText => _redrawWithPromptText;
        [DataSourceProperty] public string RedrawBasedOnImageText => _redrawBasedOnImageText;
        [DataSourceProperty] public string CopyText => _copyText;
        [DataSourceProperty] public string GalleryText => _galleryText;
        [DataSourceProperty] public string CloseText => _closeText;

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
                    OnPropertyChanged(nameof(CanRegenerateBasedOnImage));
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
                    OnPropertyChanged(nameof(CanRegenerateWithPrompt));
                    OnPropertyChanged(nameof(CanRegenerateBasedOnImage));
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

        [DataSourceProperty]
        public bool CanRegenerateWithPrompt => !IsLoading && _onRegenerateWithPrompt != null;

        public void ExecuteRegenerateWithPrompt()
        {
            if (CanRegenerateWithPrompt) _onRegenerateWithPrompt.Invoke();
        }

        public void ExecuteCopyPrompt()
        {
            if (string.IsNullOrWhiteSpace(PromptText)) return;
            try
            {
                Input.SetClipboardText(PromptText);
                StatusText = IllustratorUiText.CardCopySuccess;
            }
            catch (Exception ex)
            {
                StatusText = IllustratorUiText.CardCopyFailed(ex.Message);
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
        private static string _playerRedrawDraft = string.Empty;
        private static string _imageEditDraft = string.Empty;
        private static bool _editingRedrawPrompt;
        private static IDisposable _redrawEditor;
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
                global::AnimusForge.WorldBulletinPanelIllustrationBridge.ShouldPreloadSelection =
                    () => IllustratorRuntime.IsEnabled("weekly_report") &&
                        IllustratorSettings.Instance?.AutoGenerateWeeklyReportIllustration == true;
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

            _overlayVm = new WeeklyReportIllustrationOverlayVM(title, TriggerRegenerate, OpenRedrawPromptEditor);
            _overlayVm.OnRegenerateBasedOnImage = OpenCurrentImageEditor;
            _sink = _overlayVm;
            var layer = new MovableGauntletLayer("WeeklyReportIllustrationOverlay", 4010, false);
            _overlayLayer = layer;
            var movieIdentifier = layer.LoadMovie("WeeklyReportIllustrationOverlay", _overlayVm);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
            _overlayLayer = layer;
            topScreen.AddLayer(_overlayLayer);

            IllustratorRuntime.GenerationUpdated += OnGenerationUpdated;
            IllustratorRuntime.DefaultImageChanged += OnDefaultImageChanged;
            if (!JoinPendingGeneration()) BeginCachedLoad(IllustratorUiText.WeeklyDrawing);
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
                slot.StatusText = IllustratorUiText.WeeklyDrawing;
                slot.OnRegenerate = TriggerRegenerate;
                slot.OnRegenerateBasedOnImage = null;
                slot.OnRegenerateWithPrompt = () => { if (ReferenceEquals(ownerScope, _scope)) OpenRedrawPromptEditor(); };
                slot.OnOpenGallery = () => IllustratorGalleryPopup.Show();
                slot.OnDelete = DeleteCurrentBulletinIllustration;
                slot.IsAvailable = true;
                slot.NotifyHandlersChanged();
                IllustratorRuntime.DefaultImageChanged += OnDefaultImageChanged;
                Debug.Print($"[Illustrator] World bulletin panel slot attached: '{title}' context={(_currentContext != null)}");
                BulletinIllustrationPreloader.Ensure(_currentEventKey, _currentContext,
                    generateOnCacheMiss: IllustratorSettings.Instance?.AutoGenerateWeeklyReportIllustration == true);
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
            if (_bulletinSlot == null || _scope == null || _sink.IsLoading) return;
            CachedIllustrationItem item = _activeItem;
            if (item == null)
            {
                _bulletinSlot.StatusText = IllustratorUiText.WeeklyDeleteUnsaved;
                return;
            }
            if (!DiskImageCacheManager.DeleteItem(item, _scope.CampaignKey))
            {
                _bulletinSlot.StatusText = IllustratorUiText.WeeklyDeleteFailed;
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
            _bulletinSlot.StatusText = IllustratorUiText.WeeklyDeleted;
            BulletinIllustrationPreloader.MarkDeleted(_currentEventKey);
        }

        private static void RefreshPreparedBulletin(string key, GenerationResult result)
        {
            if (_bulletinSlot == null || _scope?.IsCurrent != true || key != _currentEventKey) return;
            var job = BulletinIllustrationPreloader.Find(key);
            if (job == null) return;
            _sink.IsLoading = job.Pending;
            _sink.StatusText = IllustrationStatusUiText.Bulletin(job);
            if (job.Pending) { if (!job.EditingCurrentImage) _sink.HasIllustration = false; return; }
            if (!job.Ready) return;
            if (result != null)
            {
                if (!Publish(result.Saved, result.Prompt, result.Result.ImageBytes)) _sink.StatusText = IllustratorUiText.WeeklyLoadFailed;
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
                        _sink.StatusText = IllustratorUiText.WeeklyLoadFailed;
                    else _sink.StatusText = IllustrationStatusUiText.Cached(cached);
                }))
            {
                _sink.IsLoading = false;
                _sink.StatusText = IllustratorUiText.WeeklyReadBusy;
            }
        }

        private static void BeginCachedLoad(string idleStatus)
        {
            string campaignKey = _scope.CampaignKey;
            string cachedEventKey = _currentEventKey;
            IllustrationScope openedScope = _scope;
            bool autoGenerate = IllustratorRuntime.IsEnabled("weekly_report") &&
                IllustratorSettings.Instance?.AutoGenerateWeeklyReportIllustration == true;
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
                        _sink.StatusText = IllustrationStatusUiText.Cached(cached, cacheOptions?.StyleFingerprint, checkStyle: true);
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
                _sink.StatusText = autoGenerate ? IllustratorUiText.WeeklyThinking : idleStatus;
                _sink.IsLoading = false;
                if (autoGenerate) TriggerRegenerate();
            }
        }

        private static void TriggerRegenerate()
        {
            TriggerRegenerate(null);
        }

        private static void TriggerRegenerate(string playerRedrawPrompt)
        {
            try { TriggerRegenerateCore(playerRedrawPrompt); }
            catch (Exception ex)
            {
                if (_sink != null) { _sink.IsLoading = false; _sink.StatusText = IllustratorUiText.WeeklyPrepareFailed(ex.Message); }
            }
        }

        private static void OpenCurrentImageEditor()
        {
            if (_bulletinSlot != null || _editingRedrawPrompt || _scope == null || _sink == null || _sink.IsLoading || !_sink.HasIllustration || _activeItem == null) return;
            var owner = _scope; var sink = _sink; var source = _activeItem;
            _redrawEditor = IllustrationRedrawPromptEditor.Show(_imageEditDraft,
                () => ReferenceEquals(owner, _scope) && owner.IsCurrent && ReferenceEquals(sink, _sink) &&
                    ReferenceEquals(source, _activeItem) && !sink.IsLoading,
                prompt => {
                    _imageEditDraft = prompt;
                    var options = IllustratorRuntime.CaptureOptions();
                    if (_bulletinSlot != null) options = options?.WithSceneImageSize();
                    ++_redrawCount;
                    sink.IsLoading = true; sink.StatusText = IllustratorUiText.WeeklyRedrawLoading;
                    if (_bulletinSlot != null) { BulletinIllustrationPreloader.EditCurrentImage(source, prompt, options); return; }
                    CurrentImageRedraw.Start(owner, source, prompt, options, null, result => {
                        if (!ReferenceEquals(owner, _scope) || !ReferenceEquals(sink, _sink)) return;
                        if (result.Saved != null && Publish(result.Saved, result.Result.ResolvedPrompt, result.Result.ImageBytes))
                            sink.StatusText = IllustratorUiText.WeeklyRedrawSuccess;
                        else { sink.IsLoading = false; sink.StatusText = IllustratorUiText.WeeklyRedrawFailed(result.Result?.ErrorMessage ?? IllustratorUiText.WeeklyNotSaved); }
                    }, error => { if (ReferenceEquals(owner, _scope)) { sink.IsLoading = false; sink.StatusText = IllustratorUiText.WeeklyRedrawFailed(error); } });
                }, status => sink.StatusText = status,
                editing => {
                    if (!ReferenceEquals(owner, _scope)) return;
                    _editingRedrawPrompt = editing;
                    if (_overlayLayer?.UIContext?.Root != null) _overlayLayer.UIContext.Root.IsVisible = !editing;
                    _bulletinSlot?.SetPromptEditing?.Invoke(editing);
                }, basedOnImage: true, inputOwner: _overlayLayer,
                isInputOwnerAlive: () => !_closing && ReferenceEquals(owner, _scope));
        }

        private static void OpenRedrawPromptEditor()
        {
            if (_editingRedrawPrompt || _scope == null || _sink == null) return;
            var owner = _scope;
            var sink = _sink;
            var context = _currentContext;
            _redrawEditor = IllustrationRedrawPromptEditor.Show(_playerRedrawDraft,
                () => ReferenceEquals(owner, _scope) && owner.IsCurrent && ReferenceEquals(sink, _sink) &&
                    ReferenceEquals(context, _currentContext) && context != null && !sink.IsLoading,
                prompt => { _playerRedrawDraft = prompt; TriggerRegenerate(prompt); },
                status => sink.StatusText = status,
                editing =>
                {
                    if (!ReferenceEquals(owner, _scope)) return;
                    _editingRedrawPrompt = editing;
                    if (_overlayLayer?.UIContext?.Root != null) _overlayLayer.UIContext.Root.IsVisible = !editing;
                    _bulletinSlot?.SetPromptEditing?.Invoke(editing);
                }, inputOwner: _overlayLayer,
                isInputOwnerAlive: () => !_closing && ReferenceEquals(owner, _scope));
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
            _sink.StatusText = IllustratorUiText.WeeklyBackground;
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
            if (update.Saved != null && Publish(update.Saved, update.Saved.Prompt)) _sink.StatusText = IllustrationStatusUiText.Cached(update.Saved);
            else _sink.StatusText = IllustratorUiText.WeeklyRedrawFailed(update.Error ?? IllustratorUiText.WeeklyLoadFailed);
        }

        internal sealed class GenerationResult
        {
            internal ImageGenerationResult Result;
            internal CachedIllustrationItem Saved;
            internal string Prompt;
        }

        private static void TriggerRegenerateCore(string playerRedrawPrompt = null)
        {
            if (_sink == null || _currentContext == null || _scope == null) return;
            if (_bulletinSlot != null)
            {
                _sink.HasIllustration = false;
                BulletinIllustrationPreloader.Ensure(_currentEventKey, _currentContext, true, playerRedrawPrompt: playerRedrawPrompt);
                RefreshPreparedBulletin(_currentEventKey, null);
                return;
            }
            if (JoinPendingGeneration()) return;
            _sink.IsLoading = true;
            _sink.HasIllustration = false;
            _sink.StatusText = IllustratorUiText.WeeklyThinking;
            var owner = _scope;
            StartGeneration(owner, _currentContext, _currentEventKey, false, ++_redrawCount,
                result =>
                {
                    if (!ReferenceEquals(owner, _scope) || _sink == null) return;
                    if (result.Result?.Success == true && Publish(result.Saved, result.Prompt, result.Result.ImageBytes))
                        _sink.StatusText = result.Saved != null ? IllustrationStatusUiText.Cached(result.Saved) : IllustratorUiText.WeeklyComplete;
                    else { _sink.IsLoading = false; _sink.StatusText = IllustratorUiText.WeeklyRedrawFailed(result.Result?.ErrorMessage ?? IllustratorUiText.WeeklyNotSaved); }
                }, error => { if (ReferenceEquals(owner, _scope) && _sink != null) { _sink.IsLoading = false; _sink.StatusText = error; } },
                status => { if (ReferenceEquals(owner, _scope) && _sink != null) _sink.StatusText = status; }, playerRedrawPrompt);
        }

        // One shared generation pipeline for the weekly popup and campaign-owned bulletin jobs.
        internal static bool StartGeneration(IllustrationScope generationScope, WeeklyReportVisualContext context,
            string eventKey, bool bulletin, int redrawCount, Action<GenerationResult> complete, Action<string> fail, Action<string> status, string playerRedrawPrompt = null)
        {
            string artDirection = context.BuildArtDirection();
            string variation = GenerateWeeklyVariation();
            if (!string.IsNullOrWhiteSpace(variation)) artDirection += "\n" + variation;
            if (redrawCount > 1) artDirection += "\n" + BuildWeeklyRedrawDirective(redrawCount);
            string hardFacts = context.BuildHardFacts();
            string directorFacts = context.BuildDirectorOnlyFacts();
            var options = IllustratorRuntime.CaptureOptions()?.WithPlayerRedrawRequest(playerRedrawPrompt);
            VisualDirectorEngine.RequirePlayerRedrawDirector(playerRedrawPrompt, options);
            if (bulletin)
            {
                options = options?.WithSceneImageSize();
                string composition = IllustratorText.Format("WeeklyBulletinComposition", options?.ImageSize);
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
                var promptPlan = new IllustrationPromptPlan(IllustratorText.Raw("Director_937"), hardFacts, workerArtDirection, directorFacts, playerRedrawPrompt);
                var refs = new List<IllustrationReferenceImage>();
                var banners = new HashSet<string>(StringComparer.Ordinal);
                // Sequential native stages keep GPU work bounded. No per-frame scan or parallel tableau allocation.
                foreach (var person in people)
                {
                    token.ThrowIfCancellationRequested();
                    CharacterPortraitReferences portraits = null;
                    if (person.Hero != null && person.Profile?.Appearance != null)
                        portraits = await ScreenCaptureHelper.ExtractHeroPortraitReferencesAsync(person.Hero,
                            cancellationToken: token, cleanTempFiles: options.AutoCleanTempFiles,
                            appearance: person.Profile.Appearance).ConfigureAwait(false);
                    if (portraits != null)
                        IllustrationReferenceRouting.AddCharacter(refs, null, portraits, person.Name,
                            IllustratorText.Format("WeeklyParticipantReference", person.Name, person.Role), eventReference: true);
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
                                IllustratorText.Format("WeeklyEmblemReference", owners), IllustrationReferenceKind.EventEmblem));
                        }
                    }
                }

                var direction = await VisualDirectorEngine.CreateDirectionAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                string prompt = direction.Prompt;
                IllustratorRuntime.Post(() => { if (generationScope.IsCurrent && !token.IsCancellationRequested) status?.Invoke(IllustrationStatusUiText.Direction(direction) + " · " + IllustratorUiText.WeeklyDrawing); });
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
            }, complete, fail, result => result.Saved, result => result.Result?.ErrorMessage ?? IllustratorUiText.WeeklyNotSaved);
        }

        /// <summary>周报纪事画的随机构图变体——同一事件每次生成应有不同取景。</summary>
        private static string GenerateWeeklyVariation()
        {
            // 构图全权交给导演：只给自由创作授权，不再提供预写取景句式。
            return IllustratorText.Get("WeeklyFreeComposition");
        }

        private static string BuildWeeklyRedrawDirective(int redrawIndex)
        {
            return IllustratorText.Format("WeeklyChronicleRedraw", redrawIndex);
        }

        private static void OnDefaultImageChanged(CachedIllustrationItem image)
        {
            if (_scope?.IsCurrent != true || _sink == null || _sink.IsLoading || image.Category != "weekly_report" ||
                image.CampaignKey != _scope.CampaignKey || image.SubjectKey != _currentEventKey) return;
            ++_redrawCount;
            if (Publish(image, image.Prompt)) _sink.StatusText = IllustratorUiText.WeeklyDefaultChanged;
        }

        private static bool Publish(CachedIllustrationItem item, string prompt, byte[] imageBytes = null)
        {
            if (item == null && imageBytes == null) return false;
            string previousSprite = _activeSpriteName;
            string spriteName = (item?.Key ?? "weekly_" + Guid.NewGuid().ToString("N")) + "_weekly_" + Guid.NewGuid().ToString("N");
            var bytes = imageBytes ?? item.ImageData;
            var publishClock = System.Diagnostics.Stopwatch.StartNew();
            GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "weekly_ui_texture_begin", "bytes=" + (bytes?.Length ?? 0));
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, bytes);
            if (sprite == null) { GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "weekly_ui_texture_failed", "bytes=" + (bytes?.Length ?? 0)); return false; }
            if (!string.IsNullOrEmpty(previousSprite) && previousSprite != spriteName) GauntletTextureLoader.ReleaseSprite(previousSprite);
            _activeSpriteName = spriteName;
            _activeItem = item;
            if (!string.IsNullOrWhiteSpace(item?.Title)) _sink.TitleText = item.Title;
            _sink.SpriteName = spriteName;
            _sink.PromptText = IllustratorUiText.ProjectKnownCachedPrompt(prompt);
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
            try { _redrawEditor?.Dispose(); } catch (Exception ex) { Debug.Print("[Illustrator] Editor cleanup failed: " + ex.Message); } finally { _redrawEditor = null; }
            try
            {
                IllustratorRuntime.GenerationUpdated -= OnGenerationUpdated;
                IllustratorRuntime.DefaultImageChanged -= OnDefaultImageChanged;
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
                if (_bulletinSlot != null) { _bulletinSlot.OnRegenerateWithPrompt = null; _bulletinSlot.OnRegenerateBasedOnImage = null; }
                _imageEditDraft = string.Empty;
                _playerRedrawDraft = string.Empty;
                _editingRedrawPrompt = false;
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
