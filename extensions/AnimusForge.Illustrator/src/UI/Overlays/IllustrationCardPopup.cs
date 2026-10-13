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

using AnimusForge.Illustrator;

using AnimusForge.Illustrator.Context;

using AnimusForge.Illustrator.Core;

using AnimusForge.Illustrator.Engine;

using AnimusForge.Illustrator.UI.Patches;

using Newtonsoft.Json.Linq;

using SandBox.View.Map;



namespace AnimusForge.Illustrator.UI.Overlays

{

    public sealed class IllustrationCardPopup

    {

        private static IllustrationCardPopup _activeInstance;

        private static ConversationIllustrationSessionSnapshot _conversationSession;

        private static long _conversationSessionEpoch;

        private readonly ScreenBase _screen;

        private readonly MovableGauntletLayer _layer;

        private readonly GauntletLayer _backdropLayer;

        private readonly IllustrationCardVM _dataSource;

        private readonly IllustrationScope _scope;

        private readonly string _category;

        private readonly Action<string> _onRegenerate;

        private string _playerRedrawDraft = string.Empty;

        private string _imageEditDraft = string.Empty;

        private CachedIllustrationItem _displayedImage;

        private bool _editingRedrawPrompt;

        private bool _hiddenForSystemUi;

        private readonly string _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);

        private readonly bool _autoFullscreen;

        private string _activeSpriteName;

        private bool _closed;

        private IDisposable _redrawEditor;

        private TaleWorlds.MountAndBlade.Mission _screenshotMission;

        private int _generationCount;

        private int _cacheLoadVersion;

        private IllustrationScope _joinedGeneration;

        private string _joinedSubjectKey, _joinedSessionKey;

        private bool _autoRedrawPending;

        private bool _autoReplyArmed;

        private bool _autoReplyRequested;

        private string _latestAutoPlayerText;

        private string _latestAutoReplyText;

        private List<ConversationContextExtractor.NativeDialogueLine> _latestAutoPriorHistory;

        private string _openingSceneBase64;

        private string _openingSceneSessionKey;

        // One location screenshot per conversation, taken when the player opens the scene

        // illustration, before any Illustrator layer covers the scene; reused by every redraw.

        private static string _sessionOpeningScene;

        private static string _sessionOpeningSceneKey;

        // Map conversations draw their 3D tableau inside the MapConversation layer itself,

        // above our backdrop. These are hidden while an illustration is shown and restored on close.

        private readonly List<Widget> _hiddenMapConversationWidgets = new List<Widget>();



        internal static Widget VisualRoot => _activeInstance?._layer?.UIContext?.Root;

        public static bool IsOpen => _activeInstance != null;



        private sealed class ConversationIllustrationSessionSnapshot

        {

            internal string SessionKey;

            internal string StableHardFacts;

            internal string NearbyPropFacts;

            internal string SceneDirectorNote;

            internal IReadOnlyList<IllustrationReferenceImage> SceneReferences;

            internal IReadOnlyList<IllustrationReferenceImage> CharacterReferences;

            internal IReadOnlyList<IllustrationReferenceImage> GenerationCharacterReferences;

            internal IReadOnlyList<IllustrationReferenceImage> EmblemReferences;

            internal IReadOnlyList<IllustrationReferenceImage> GenerationEmblemReferences;

        }



        private static string ConversationSessionOwnerKey()

        {

            try

            {

                var source = ScreenCaptureHelper.GetConversationSceneCaptureSource();

                if (source != null) return source.SessionOwnerKey;

            }

            catch { }

            return "unavailable:" + (ScreenManager.TopScreen?.GetHashCode() ?? 0);

        }



        private static string ConversationSessionKey(ConversationVisualContext context, string ownerKey = null)

        {

            if (context == null) return string.Empty;

            string main = context.MainHero?.StringId ?? "player";

            string partner = context.InterlocutorHero?.StringId ?? context.InterlocutorCharacter?.StringId ?? "NPC";

            return (ownerKey ?? ConversationSessionOwnerKey()) + "|" + main + "|" + partner;

        }



        private static string FreezeConversationHardFacts(string hardFacts)

        {

            string value = hardFacts ?? string.Empty;

            int dynamicStart = value.IndexOf(IllustratorText.Raw("Residual_IllustrationCardPopup_95b32aa073bc"), StringComparison.Ordinal);

            if (dynamicStart < 0) dynamicStart = value.IndexOf("[Hard facts: current character space and action]", StringComparison.Ordinal);

            return (dynamicStart >= 0 ? value.Substring(0, dynamicStart) : value).Trim();

        }



        private static string ExtractConversationDynamicFacts(string hardFacts)

        {

            string value = hardFacts ?? string.Empty;

            int dynamicStart = value.IndexOf(IllustratorText.Raw("Residual_IllustrationCardPopup_95b32aa073bc"), StringComparison.Ordinal);

            if (dynamicStart < 0) dynamicStart = value.IndexOf("[Hard facts: current character space and action]", StringComparison.Ordinal);

            return dynamicStart >= 0 ? value.Substring(dynamicStart).Trim() : string.Empty;

        }



        private static IllustrationPromptPlan BuildConversationSessionPlan(IllustrationPromptPlan current, ConversationIllustrationSessionSnapshot session, string nearbyPropFacts = null)

        {

            if (session == null) return current;

            var hardFacts = new StringBuilder(session.StableHardFacts ?? string.Empty);

            string dynamicFacts = ExtractConversationDynamicFacts(current?.HardFacts);

            if (!string.IsNullOrWhiteSpace(dynamicFacts)) hardFacts.AppendLine().Append(dynamicFacts);

            string props = !string.IsNullOrWhiteSpace(session.NearbyPropFacts) ? session.NearbyPropFacts : nearbyPropFacts;

            if (!string.IsNullOrWhiteSpace(props)) hardFacts.AppendLine().Append(props.Trim());

            return new IllustrationPromptPlan(current.Mode, hardFacts.ToString(), current.ArtDirection,

                current.DirectorOnlyFacts + (session.SceneDirectorNote ?? string.Empty), current.PlayerRedrawPrompt);

        }



        private IllustrationCardPopup(ScreenBase screen, string movieName, string category, Action<string> onRegenerate, Action onSceneProbe = null, bool autoFullscreen = false, bool missionScreenshot = false)

        {

            _screen = screen;

            _category = category;

            _autoFullscreen = autoFullscreen;

            _onRegenerate = onRegenerate;

            _dataSource = new IllustrationCardVM(Close, onRegenerate == null ? (Action)null : () => Regenerate(null), onSceneProbe,

                missionScreenshot ? (Action)null : OpenRedrawPromptEditor);

            _dataSource.OnRegenerateBasedOnImage = missionScreenshot ? (Action)null : OpenCurrentImageEditor;

            var layer = new MovableGauntletLayer(autoFullscreen ? "IllustrationFullscreenOverlay" : "IllustrationCardOverlay", autoFullscreen ? 4020 : 4015, false);

            _layer = layer;

            try

            {

            if (autoFullscreen)

            {

                // One passive image layer below the conversation, with controls kept

                // above it. Resolve the host's order once when opening, never per tick.

                _backdropLayer = new GauntletLayer("IllustrationFullscreenBackdrop", ResolveFullscreenBackdropOrder(screen), false);

                _backdropLayer.LoadMovie("ConversationIllustrationFullscreenBackdrop", _dataSource);

                _backdropLayer.InputRestrictions.ResetInputRestrictions();

            }

            var movieIdentifier = layer.LoadMovie(movieName, _dataSource);

            if (!autoFullscreen)

                layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");

            else

                layer.AttachTopFraction(movieIdentifier?.Movie?.RootWidget?.FindChild("TopPanel", includeAllChildren: true), 0.75f);

            // The transparent controls root never consumes the conversation's input;

            // only its actual buttons participate in mouse hit testing.

            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);

            _layer = layer;

            _scope = new IllustrationScope(screen, category, Close, missionOwned: missionScreenshot);

            IllustratorRuntime.GenerationUpdated += OnGenerationUpdated;

            IllustratorRuntime.DefaultImageChanged += OnDefaultImageChanged;



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



        private bool JoinPendingGeneration(string subjectKey, string sessionKey = null)

        {

            var pending = IllustratorRuntime.FindGenerating(_category, subjectKey, sessionKey);

            if (pending == null) return false;

            _joinedGeneration = pending;

            _joinedSubjectKey = subjectKey;

            _joinedSessionKey = sessionKey;

            ++_cacheLoadVersion; // invalidate a cache read that may otherwise overwrite the new result

            _dataSource.SetLoading(IllustratorUiText.WeeklyBackground);

            return true;

        }



        // A presentation-only subscriber. Closing this card never cancels the mission generation owner.

        internal static void ShowForMissionScreenshot(IllustrationScope generation, string subjectKey,

            TaleWorlds.MountAndBlade.Mission mission, string status)

        {

            IllustratorRuntime.AssertMainThread();

            if (generation == null || !generation.IsCurrent || !ReferenceEquals(TaleWorlds.MountAndBlade.Mission.Current, mission)) return;

            IllustrationCardPopup popup = null;

            try

            {

                _activeInstance?.Close();

                var screen = ScreenManager.TopScreen;

                popup = new IllustrationCardPopup(screen, "EncyclopediaIllustrationOverlay", "general", null, missionScreenshot: true);

                popup._screenshotMission = mission;

                popup._joinedGeneration = generation;

                popup._joinedSubjectKey = subjectKey;

                popup._dataSource.TitleText = IllustratorUiText.Gallery;

                popup._dataSource.SetLoading(status + " " + IllustratorUiText.CardAutoSaveHint);

                screen.AddLayer(popup._layer);

                _activeInstance = popup;

            }

            catch { popup?.Close(); throw; }

        }



        internal static void UpdateMissionScreenshotStatus(IllustrationScope generation, string status)

        {

            var popup = _activeInstance;

            if (popup == null || popup._closed || popup._screenshotMission == null || !ReferenceEquals(popup._joinedGeneration, generation)) return;

            popup._dataSource.StatusText = status;

        }



        private void Regenerate(string playerPrompt)

        {

            if (_closed || !_scope.IsCurrent || !ReferenceEquals(_activeInstance, this) || _dataSource.IsLoading) return;

            _onRegenerate?.Invoke(playerPrompt);

        }



        private void OpenCurrentImageEditor()

        {

            if (_editingRedrawPrompt || _displayedImage == null || !_dataSource.CanRegenerateBasedOnImage) return;

            var source = _displayedImage;

            string session = _category == "conversation" ? ConversationSessionOwnerKey() : null;

            _redrawEditor = IllustrationRedrawPromptEditor.Show(_imageEditDraft,

                () => !_closed && _scope.IsCurrent && ReferenceEquals(_activeInstance, this) &&

                    ReferenceEquals(source, _displayedImage) && !_dataSource.IsLoading &&

                    (_category != "conversation" || session == ConversationSessionOwnerKey()),

                prompt => {

                    _imageEditDraft = prompt;

                    var options = IllustratorRuntime.CaptureOptions();

                    if (_autoFullscreen) options = options?.WithSceneImageSize();

                    ++_cacheLoadVersion;

                    _dataSource.SetLoading(IllustratorUiText.WeeklyRedrawLoading);

                    CurrentImageRedraw.Start(_scope, source, prompt, options, session, completion => {

                        if (completion.Saved != null && PublishImage(completion.Saved, completion.Result.ImageBytes, completion.Result.ResolvedPrompt))

                            _dataSource.SetReady(IllustratorUiText.WeeklyRedrawSuccess);

                        else _dataSource.SetReady(IllustratorUiText.WeeklyRedrawFailed(completion.Result?.ErrorMessage ?? IllustratorUiText.CardNotSavedOrDisplayed));

                    }, error => _dataSource.SetReady(IllustratorUiText.WeeklyRedrawFailed(error)));

                }, status => _dataSource.StatusText = status,

                editing => { if (!_closed) { _editingRedrawPrompt = editing; ApplyControlsVisibility(); } }, basedOnImage: true, inputOwner: _layer, isInputOwnerAlive: () => !_closed);

        }



        private void OpenRedrawPromptEditor()

        {

            if (_editingRedrawPrompt) return;

            string ownerKey = _category == "conversation" ? ConversationSessionOwnerKey() : null;

            _redrawEditor = IllustrationRedrawPromptEditor.Show(_playerRedrawDraft,

                () => !_closed && _scope.IsCurrent && ReferenceEquals(_activeInstance, this) && !_dataSource.IsLoading &&

                    (_category != "conversation" || ownerKey == ConversationSessionOwnerKey()),

                prompt => { _playerRedrawDraft = prompt; Regenerate(prompt); },

                status => _dataSource.StatusText = status,

                editing => { if (!_closed) { _editingRedrawPrompt = editing; ApplyControlsVisibility(); } },

                inputOwner: _layer, isInputOwnerAlive: () => !_closed);

        }



        private void OnDefaultImageChanged(CachedIllustrationItem image)

        {

            if (_closed || !_scope.IsCurrent || !ReferenceEquals(_activeInstance, this) || _dataSource.IsLoading ||

                image.CampaignKey != _scope.CampaignKey || image.Category != _category ||

                image.SubjectKey != _displayedImage?.SubjectKey) return;

            ++_cacheLoadVersion;

            if (PublishImage(image)) _dataSource.SetReady(IllustratorUiText.WeeklyDefaultChanged);

        }



        private void OnGenerationUpdated(IllustrationGenerationUpdate update)

        {

            if (_closed || !_scope.IsCurrent || !ReferenceEquals(_activeInstance, this) ||

                !ReferenceEquals(update.Source, _joinedGeneration) || update.CampaignKey != _scope.CampaignKey ||

                update.Category != _category || update.SubjectKey != _joinedSubjectKey || update.SessionKey != _joinedSessionKey) return;

            _joinedGeneration = null;

            if (ReferenceEquals(update.Source, _scope)) return; // attached owner's callback already published it

            ++_cacheLoadVersion;

            if (_category == "conversation" && !(_joinedSessionKey ?? "").StartsWith(ConversationSessionOwnerKey() + "|", StringComparison.Ordinal))

            {

                _dataSource.SetReady(IllustratorUiText.ConversationSessionChanged);

                return;

            }

            if (update.Saved != null && PublishImage(update.Saved))

                _dataSource.SetReady(IllustrationStatusUiText.Cached(update.Saved));

            else _dataSource.SetReady(IllustratorUiText.CardPrepareFailed(update.Error ?? IllustratorUiText.WeeklyLoadFailed));

        }



        private static bool StartCachedImageLoad(IllustrationCardPopup popup, string key, string campaignKey, string category, Action<CachedIllustrationItem, Exception> completed)

        {

            if (popup == null || completed == null) return false;

            int version = ++popup._cacheLoadVersion;

            return IllustratorRuntime.Start(

                () => Task.Run(() => DiskImageCacheManager.LoadImage(key, campaignKey, category)),

                (cached, error) =>

                {

                    if (popup._closed || !popup._scope.IsCurrent || !ReferenceEquals(_activeInstance, popup) || version != popup._cacheLoadVersion) return;

                    completed(cached, error);

                });

        }



        private static int ResolveFullscreenBackdropOrder(ScreenBase screen)

        {

            int conversationOrder = int.MaxValue;

            if (screen?.Layers != null)

                foreach (var layer in screen.Layers)

                {

                    if (layer == null || layer.IsFinalized) continue;

                    string name = layer.Name;

                    if (name == "MissionConversation" || name == "MapConversation" ||

                        name == "AnimusForgeNativeConversationOverlay" || name == "ShoutTextInputPopup")

                        conversationOrder = Math.Min(conversationOrder, layer.InputRestrictions.Order);

                }

            // Native mission conversation is order 49 on both supported game APIs.

            return conversationOrder == int.MaxValue ? 48 : conversationOrder - 1;

        }



        internal static string SelectOpeningSceneReference(string image, string capturedSession, string currentSession)

        {

            return !string.IsNullOrWhiteSpace(image) && !string.IsNullOrWhiteSpace(capturedSession) &&

                !capturedSession.StartsWith("unavailable:", StringComparison.Ordinal) &&

                string.Equals(capturedSession, currentSession, StringComparison.Ordinal) ? image : null;

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

                popup = new IllustrationCardPopup(topScreen, "EncyclopediaIllustrationOverlay", "encyclopedia", prompt =>

                {

                    popup?.ExecuteEncyclopediaGeneration(hero, tableauWidget, prompt);

                });



                string heroName = hero.Name != null ? hero.Name.ToString() : IllustratorUiText.DefaultHeroName;

                popup._dataSource.TitleText = IllustratorUiText.PortraitTitle(heroName);

                topScreen.AddLayer(popup._layer);

                _activeInstance = popup;



                string key = $"Hero_{hero.StringId}";

                if (popup.JoinPendingGeneration(key)) return;

                popup._dataSource.SetLoading(IllustratorUiText.GalleryLoading);

                if (!StartCachedImageLoad(popup, key, popup._scope.CampaignKey, "encyclopedia", (cached, error) =>

                {

                    if (error != null) Debug.Print("[Illustrator] Encyclopedia cache read failed: " + error.Message);

                    if (cached != null && cached.ImageData != null && cached.ImageData.Length > 0 && popup.PublishImage(cached))

                        popup._dataSource.SetReady(IllustrationStatusUiText.Cached(cached, IllustratorRuntime.CaptureOptions()?.StyleFingerprint, checkStyle: true));

                    else

                        popup.ExecuteEncyclopediaGeneration(hero, tableauWidget);

                }))

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



        public static void ShowForConversation(ConversationVisualContext convContext, bool? autoFullscreen = null, bool forceGenerate = false)

        {

            IllustratorRuntime.AssertMainThread();

            if (convContext == null || !IllustratorRuntime.IsEnabled("conversation")) return;

            bool useFullscreen = autoFullscreen ?? (IllustratorSettings.Instance?.ConversationIllustrationUsesFullscreen == true);

            string conversationSessionKey = ConversationSessionKey(convContext);

            lock (typeof(IllustrationCardPopup))

            {

                if (_conversationSession != null && !string.Equals(_conversationSession.SessionKey, conversationSessionKey, StringComparison.Ordinal))

                {

                    _conversationSessionEpoch++;

                    _conversationSession = null;

                }

                if (_sessionOpeningSceneKey != null && !string.Equals(_sessionOpeningSceneKey, conversationSessionKey, StringComparison.Ordinal))

                {

                    _sessionOpeningScene = null;

                    _sessionOpeningSceneKey = null;

                }

            }

            ScreenBase topScreen = ScreenManager.TopScreen;

            if (topScreen == null) return;



            IllustrationCardPopup popup = null;

            try

            {

                // Retain one location-only capture before opening the fullscreen art.

                string preCapturedBase64 = null;

                string openingSceneSessionKey = ConversationSessionKey(convContext);



                var interlocutor = convContext.InterlocutorHero;

                // 会话画面里双方都可能出现纹章载体：对方家族与玩家家族各发一张参考样图（同一代码去重）

                var emblemSpecs = new List<EmblemSpec>();

                AddEmblemSpec(emblemSpecs, interlocutor, IllustratorText.Raw("Residual_IllustrationCardPopup_367d88405654"));

                AddEmblemSpec(emblemSpecs, Hero.MainHero, IllustratorText.Raw("Residual_IllustrationCardPopup_991e63fe2920"));



                // 试采只绑定弹窗打开时的真实 Mission owner，不在点击时接受另一个场景。

                ScreenCaptureHelper.ConversationSceneCaptureSource probeSource = null;

                if (topScreen is TaleWorlds.MountAndBlade.View.Screens.MissionScreen)

                {

                    try { probeSource = ScreenCaptureHelper.GetConversationSceneCaptureSource(); }

                    catch (Exception ex) { Debug.Print("[Illustrator] Scene probe unavailable: " + ex.Message); }

                }

                if (probeSource?.RequiresMissionPanorama != true) probeSource = null;

                var previousPopup = _activeInstance;

                previousPopup?.Close();

                // Capture the location when the player opens the scene illustration (card or

                // fullscreen), once per conversation. Later generations reuse it instead of

                // photographing a screen already covered by the loading panel or backdrop.

                lock (typeof(IllustrationCardPopup))

                    preCapturedBase64 = SelectOpeningSceneReference(_sessionOpeningScene, _sessionOpeningSceneKey, openingSceneSessionKey);

                if (preCapturedBase64 != null)

                {

                    Debug.Print("[Illustrator] Conversation scene capture reused from this conversation session.");

                }

                else if (probeSource == null)

                {

                    // Field (map) conversations use terrain facts and offscreen portraits; no screenshot.

                    Debug.Print("[Illustrator] Conversation scene capture at open: skipped_non_scene_terrain_facts");

                }

                else

                {

                    // Removing a previous layer does not immediately repaint desktop

                    // pixels. Never capture its old painting as the actual environment.

                    string calibrationReason = "previous_overlay_pending_redraw";

                    try

                    {

                        if (previousPopup == null)

                            preCapturedBase64 = ScreenCaptureHelper.CaptureUnobstructedConversationSceneBase64(out calibrationReason);

                    }

                    catch (Exception ex)

                    {

                        calibrationReason = "capture_exception_" + ex.GetType().Name;

                    }

                    if (!string.IsNullOrWhiteSpace(preCapturedBase64))

                    {

                        lock (typeof(IllustrationCardPopup))

                        {

                            _sessionOpeningScene = preCapturedBase64;

                            _sessionOpeningSceneKey = openingSceneSessionKey;

                        }

                    }

                    Debug.Print("[Illustrator] Conversation scene capture at open: " + (string.IsNullOrWhiteSpace(preCapturedBase64) ? calibrationReason : "captured_before_overlay"));

                }

                popup = new IllustrationCardPopup(topScreen, useFullscreen ? "ConversationIllustrationFullscreenOverlay" : "ConversationIllustrationOverlay", "conversation", prompt =>

                {

                    string redrawBase64 = null;

                    ConversationVisualContext redrawContext = ConversationContextExtractor.ExtractFromCurrentConversation();

                    var current = redrawContext ?? convContext;

                    var redrawEmblems = new List<EmblemSpec>();

                    AddEmblemSpec(redrawEmblems, current.InterlocutorHero, IllustratorText.Raw("Residual_IllustrationCardPopup_367d88405654"));

                    AddEmblemSpec(redrawEmblems, current.MainHero, IllustratorText.Raw("Residual_IllustrationCardPopup_991e63fe2920"));

                    popup?.ExecuteConversationGeneration(current, redrawBase64, redrawEmblems, playerRedrawPrompt: prompt);

                }, probeSource == null ? (Action)null : () => popup?.ExecuteIsolatedSceneProbe(probeSource), useFullscreen);

                popup._openingSceneBase64 = preCapturedBase64;

                popup._openingSceneSessionKey = openingSceneSessionKey;



                string partnerName = convContext.InterlocutorHero != null && convContext.InterlocutorHero.Name != null

                    ? convContext.InterlocutorHero.Name.ToString()

                    : (convContext.InterlocutorCharacter != null && convContext.InterlocutorCharacter.Name != null

                        ? convContext.InterlocutorCharacter.Name.ToString()

                        : IllustratorUiText.DefaultPartnerName);



                popup._dataSource.TitleText = useFullscreen ? IllustratorUiText.SceneTitle : IllustratorUiText.ConversationTitle(partnerName);

                if (popup._backdropLayer != null) topScreen.AddLayer(popup._backdropLayer);

                topScreen.AddLayer(popup._layer);

                _activeInstance = popup;



                string partnerId = convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC";

                string key = $"Conv_{partnerId}";

                if (popup.JoinPendingGeneration(key, conversationSessionKey)) return;

                if (!forceGenerate)

                {

                    popup._dataSource.SetLoading(IllustratorUiText.GalleryLoading);

                    if (!StartCachedImageLoad(popup, key, popup._scope.CampaignKey, "conversation", (cached, error) =>

                    {

                        if (error != null) Debug.Print("[Illustrator] Conversation cache read failed: " + error.Message);

                        if (cached != null && cached.ImageData != null && cached.ImageData.Length > 0 && popup.PublishImage(cached))

                        {

                            popup._dataSource.SetReady(IllustrationStatusUiText.Cached(cached, IllustratorRuntime.CaptureOptions()?.StyleFingerprint, checkStyle: true));

                        }

                        else

                        {

                            // Opening the card only prepares the conversation context. The first

                            // generation must come from the player's explicit Regenerate action;

                            // enabling automatic redraw must never issue an entry-time request.

                            popup._dataSource.SetReady(IllustratorUiText.ConversationFirstImageRequired);

                        }

                    }))

                    {

                        popup._dataSource.SetReady(IllustratorUiText.ConversationFirstImageRequired);

                    }

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



        private void ExecuteEncyclopediaGeneration(Hero hero, Widget tableauWidget, string playerRedrawPrompt = null)

        {

            try { ExecuteEncyclopediaGenerationCore(hero, tableauWidget, playerRedrawPrompt); }

            catch (Exception ex) { _dataSource.SetReady(IllustratorUiText.CardPrepareFailed(ex.Message)); }

        }



        private void ExecuteEncyclopediaGenerationCore(Hero hero, Widget tableauWidget, string playerRedrawPrompt = null)

        {

            if (JoinPendingGeneration($"Hero_{hero.StringId}")) return;

            ++_cacheLoadVersion;

            _dataSource.SetLoading(IllustratorUiText.PortraitPreparing);



            string key = $"Hero_{hero.StringId}";

            string heroName = hero.Name?.ToString() ?? IllustratorText.Get("Hero_319");



            var portrait = tableauWidget as CharacterTableauWidget;

            if (portrait == null || string.IsNullOrWhiteSpace(portrait.EquipmentCode))

            {

                _dataSource.SetReady(IllustratorUiText.PortraitEquipmentUnavailable);

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

            HeroVisualProfile profile = HeroVisualExtractor.Extract(hero, equipmentSnapshot: equipment, equipmentSource: IllustratorText.Get("Facts_EquipmentSourcePortrait"), appearance: appearance);

            string bannerCode = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;



            string hardFacts = profile.BuildVisualSummary(includeMount: false);

            string directorFacts = IllustratorText.Format("Facts_CampaignEra", TaleWorlds.CampaignSystem.CampaignTime.Now.GetYear) + profile.BuildDirectorOnlyFacts();

            _generationCount++;

            string baseArtDirection = GenerateDiversePoseDirective();

            var options = IllustratorRuntime.CaptureOptions()?.WithPlayerRedrawRequest(playerRedrawPrompt);

            VisualDirectorEngine.RequirePlayerRedrawDirector(playerRedrawPrompt, options);



            _scope.RunGeneration(key, null, async token =>

            {

                GenerationDiagnostics.Current?.SetSubject(key);

                // Metadata enumeration can scan a large campaign cache. Keep it on the

                // worker before composing redraw history so the game thread stays responsive.

                int priorVersions = 0;

                var usedMotifs = new List<string>();

                string recentActions = string.Empty;

                try

                {

                    var priorItems = await Task.Run(() => DiskImageCacheManager.GetAllCachedIllustrations(_scope.CampaignKey), token).ConfigureAwait(false);

                    priorItems = priorItems

                        ?.Where(i => string.Equals(i.SubjectKey, key, StringComparison.OrdinalIgnoreCase)

                                  && string.Equals(i.Category, "encyclopedia", StringComparison.OrdinalIgnoreCase))

                        .OrderByDescending(i => i.CreatedTime)

                        .ToList();

                    priorVersions = priorItems?.Count ?? 0;

                    recentActions = IllustrationDirection.BuildActionHistory(priorItems);

                    foreach (string motif in (priorItems ?? new List<CachedIllustrationItem>())

                        .Where(i => !IllustrationDirection.IsLocalFallback(i))

                        .Select(i => ExtractSceneMotif(string.Equals(i.Prompt, IllustratorText.Raw("Residual_DiskImageCacheManager_ec79815ca580"), StringComparison.Ordinal) ? IllustratorText.Get("Cache_LocalPromptPlaceholder") : i.Prompt))

                        .Where(m => !string.IsNullOrWhiteSpace(m))

                        .Distinct()

                        .Take(3))

                    {

                        usedMotifs.Add(motif);

                    }

                }

                catch (OperationCanceledException) { throw; }

                catch { }

                string artDirection = baseArtDirection;

                if (!string.IsNullOrWhiteSpace(recentActions)) artDirection += "\n" + recentActions;

                if (_generationCount > 1 || priorVersions > 0) artDirection += "\n" + VisualDirectorEngine.BuildRedrawVariationDirective(_generationCount + priorVersions);

                if (usedMotifs.Count > 0) artDirection += IllustratorText.Format("Facts_UsedMotifsAvoid", string.Join(IllustratorText.Get("UsedMotifListSeparator"), usedMotifs));

                var promptPlan = new IllustrationPromptPlan(IllustratorText.Raw("Director_1020"), hardFacts, artDirection, directorFacts, playerRedrawPrompt);

                // Character portraits are a required reference and are always rendered offscreen.

                CharacterPortraitReferences portraits = await ScreenCaptureHelper.ExtractHeroPortraitReferencesAsync(hero,

                    maxDimension: 768, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true,

                    equipmentCodeOverride: equipmentCode, appearance: appearance).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(portraits.FullBody))

                    throw new InvalidOperationException("Encyclopedia full-equipment offscreen portrait failed. Generation stopped. Inspect portrait_stage_result failureCode.");



                var refs = new System.Collections.Generic.List<IllustrationReferenceImage>();

                var genRefsList = new System.Collections.Generic.List<IllustrationReferenceImage>();

                IllustrationReferenceRouting.AddCharacter(refs, genRefsList, portraits, heroName,

                    IllustratorText.Format("Ref_PortraitIdentity", heroName));

                // 纹章由原生渲染导出，导出控件不向屏幕绘制；取消信号贯穿请求

                if (profile.HasHeraldicArmor && !string.IsNullOrWhiteSpace(bannerCode))

                {

                    string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(bannerCode, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(emblemB64))

                    {

                        var emblemRef = new IllustrationReferenceImage(emblemB64, IllustratorText.Get("Ref_ClanEmblem"), IllustrationReferenceKind.Emblem);

                        refs.Add(emblemRef);

                        // 已在原生采集之前确认实际穿戴纹章载体；普通装备不产生该参考。

                        genRefsList.Add(emblemRef);

                    }

                }

                var direction = await VisualDirectorEngine.CreateDirectionAsync(promptPlan, refs, options, token).ConfigureAwait(false);

                string detailedPrompt = direction.Prompt;

                IllustratorRuntime.Post(() => { if (!_closed && !token.IsCancellationRequested) _dataSource.StatusText = IllustrationStatusUiText.Direction(direction) + " · " + (_category == "encyclopedia" ? IllustratorUiText.PortraitDrawing : IllustratorUiText.ConversationDrawing); });

                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)genRefsList;

                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, genRefs, options, token).ConfigureAwait(false);

                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;

                CachedIllustrationItem saved = null;

                if (result.Success && result.ImageBytes != null)

                {

                    token.ThrowIfCancellationRequested();

                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, string.IsNullOrWhiteSpace(direction.Title) ? IllustratorText.Format("Card_PortraitCacheTitle", heroName) : direction.Title, _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false, theme: direction.Theme, actionSummary: direction.ActionSummary, diagnosticId: result.DiagnosticId, directorStatus: direction.DirectionStatus, directorStatusText: direction.StatusText, directorFallbackReason: direction.FallbackReason, styleFingerprint: options?.StyleFingerprint);

                    if (saved != null) DiskImageCacheManager.PromoteDefaultIfNewest(saved, _scope.CampaignKey);

                }

                return new GenerationCompletion(result, saved, effectivePrompt);

            }, completion =>

            {

                if (completion.SavedItem != null) DiskImageCacheManager.PromoteDefaultIfNewest(completion.SavedItem, _scope.CampaignKey);

                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null &&

                    PublishImage(completion.SavedItem, completion.Result.ImageBytes, completion.Prompt))

                {

                    _dataSource.SetReady(completion.SavedItem != null ? IllustrationStatusUiText.Cached(completion.SavedItem) : IllustratorUiText.PortraitComplete);

                }

                else

                {

                    _dataSource.SetReady(IllustratorUiText.CardDrawFailed(completion.Result?.ErrorMessage ?? IllustratorUiText.WeeklyNotSaved));

                }

            }, error =>

            {

                _dataSource.SetReady(IllustratorUiText.CardGenerationException(error));

            }, completion => completion.SavedItem, completion => completion.Result?.ErrorMessage ?? "Failed to save image");

        }



        private void QueuePendingAutoRedraw()

        {

            if (!_autoReplyArmed || !_autoRedrawPending || _closed) return;

            _autoRedrawPending = false;

            IllustratorRuntime.PostCritical(() =>

            {

                if (ReferenceEquals(_activeInstance, this) && !_closed) AutoRedrawActiveConversation();

            });

        }



        private static string GenerateDiversePoseDirective()

        {

            return IllustratorText.Get("Directive_DiversePose");

        }



        /// <summary>从已存提示词中截取【场景空间】开头作为场景母题，用于跨版本去重。</summary>

        private static string ExtractSceneMotif(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;
            string[] markers = { IllustratorText.Raw("Residual_IllustrationCardPopup_f455d27ac8d7"), "[Scene space]", IllustratorText.Raw("AFI_PROTOCOL_SCENE") };
            int idx = -1;
            int markerLen = 0;
            foreach (var m in markers)
            {
                idx = prompt.IndexOf(m, StringComparison.Ordinal);
                if (idx >= 0) { markerLen = m.Length; break; }
            }
            if (idx < 0) return string.Empty;
            int start = idx + markerLen;
            int end = prompt.IndexOfAny(new[] { '。', '.', '【', '[', '\n', '\r' }, start);
            string section = (end > start ? prompt.Substring(start, end - start) : prompt.Substring(start)).Trim();
            if (section.Length > 40) section = section.Substring(0, 40);
            return section.TrimEnd('；', ';', '。', '.', '，', ',', '、', '：', ':', ' ');
        }



        private static string GenerateConversationSceneVariation(ConversationVisualContext context)

        {

            return IllustratorText.Get("Directive_ConversationSceneVariation");

        }



        /// <summary>一枚待合成的纹章参考图：旗帜代码 + 归属方标签（画面归因用）。</summary>

        private sealed class EmblemSpec

        {

            public string Code = string.Empty;

            public string Owner = string.Empty;

            public bool PlayerSide;

            public bool InterlocutorSide;

            public string Side => PlayerSide && InterlocutorSide ? IllustratorText.Get("Side_Both") : PlayerSide ? IllustratorText.Get("Side_Player") : IllustratorText.Get("Side_Interlocutor");

        }



        private static void AddEmblemSpec(List<EmblemSpec> specs, Hero hero, string sideLabel)

        {

            if (hero == null) return;

            string code = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;

            if (string.IsNullOrWhiteSpace(code)) return;

            var existing = specs.Find(s => s.Code == code);

            if (existing != null)

            {

                existing.PlayerSide |= sideLabel == IllustratorText.Raw("Residual_IllustrationCardPopup_991e63fe2920") || sideLabel == "Player";

                existing.InterlocutorSide |= sideLabel != IllustratorText.Raw("Residual_IllustrationCardPopup_991e63fe2920") && sideLabel != "Player";

                return;

            }

            string owner = hero.Clan?.Name != null ? hero.Clan.Name.ToString()

                : (hero.Clan?.Kingdom?.Name != null ? hero.Clan.Kingdom.Name.ToString() : IllustratorText.Get("Default_UnknownClan"));

            bool isPlr = sideLabel == IllustratorText.Raw("Residual_IllustrationCardPopup_991e63fe2920") || sideLabel == "Player";

            specs.Add(new EmblemSpec { Code = code, Owner = owner, PlayerSide = isPlr, InterlocutorSide = !isPlr });

        }



        private void ExecuteConversationGeneration(ConversationVisualContext convContext, string preCapturedBase64 = null, List<EmblemSpec> emblemSpecs = null, bool automatic = false, string playerRedrawPrompt = null)

        {

            if (!automatic)

            {

                _autoReplyRequested = IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen == true;

                _autoRedrawPending = false;

                _latestAutoPlayerText = null;

                _latestAutoReplyText = null;

                _latestAutoPriorHistory = null;

            }

            try { ExecuteConversationGenerationCore(convContext, preCapturedBase64, emblemSpecs, automatic, playerRedrawPrompt); }

            catch (Exception ex) { _dataSource.SetReady(IllustratorUiText.CardPrepareFailed(ex.Message)); }

        }



        private void ExecuteIsolatedSceneProbe(ScreenCaptureHelper.ConversationSceneCaptureSource source)

        {

            try

            {

                if (_closed || _dataSource.IsLoading || source?.RequiresMissionPanorama != true) return;

                source.EnsureCurrent(System.Threading.CancellationToken.None);

                _dataSource.SetLoading(IllustratorUiText.SceneProbeLoading);

                _scope.Run(async token =>

                {

                    await source.EnsureCurrentAsync(token).ConfigureAwait(false);

                    var diagnostics = GenerationDiagnostics.Current;

                    diagnostics?.SetSubject("isolated-scene-probe");

                    diagnostics?.RecordStage("isolated_scene_probe_start", new JObject

                    {

                        ["views"] = 6, ["faceSize"] = 512, ["radiusMeters"] = 30, ["coverage"] = "360x180",

                        ["providerRequests"] = 0, ["sceneCopy"] = true, ["sourceMissionViews"] = 0

                    });

                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    bool captured = false;

                    try

                    {

                        // Reuse the production private-scene capture and its locks/retirement.

                        // The optional presented-frame reference is not the probe preview.

                        var capture = await ScreenCaptureHelper.CaptureConversationSceneReferencesAsync(source, token).ConfigureAwait(false);

                        await source.EnsureCurrentAsync(token).ConfigureAwait(false);

                        var reference = capture?.References?.FirstOrDefault(image => image?.Kind == IllustrationReferenceKind.ScenePanorama);

                        if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image))

                            throw new InvalidOperationException("The isolated scene did not return a complete panorama reference image.");

                        byte[] imageBytes = Convert.FromBase64String(reference.Base64Image);

                        token.ThrowIfCancellationRequested();

                        if (imageBytes.Length == 0) throw new InvalidOperationException("The isolated scene returned an empty reference mosaic.");

                        diagnostics?.RecordIsolatedSceneProbeImage(imageBytes);

                        captured = true;

                        return imageBytes;

                    }

                    finally

                    {

                        diagnostics?.RecordStage("isolated_scene_probe_end", new JObject

                        {

                            ["totalMs"] = watch.ElapsedMilliseconds, ["panorama"] = captured,

                            ["providerRequests"] = 0, ["sourceMissionViews"] = 0

                        });

                    }

                }, imageBytes =>

                {

                    try

                    {

                        source.EnsureCurrent(System.Threading.CancellationToken.None);

                        string probeNote = IllustratorUiText.SceneProbeNote;

                        if (imageBytes == null || imageBytes.Length == 0 || !PublishImage(null, imageBytes, probeNote))

                        {

                            SetSceneProbeFailure(IllustratorUiText.SceneProbeDisplayFailed);

                            return;

                        }

                        _dataSource.TitleText = IllustratorUiText.SceneProbeTitle;

                        _dataSource.SetReady(IllustratorUiText.SceneProbeComplete);

                    }

                    catch (Exception ex) { SetSceneProbeFailure(ex.Message); }

                }, SetSceneProbeFailure);

            }

            catch (Exception ex) { SetSceneProbeFailure(ex.Message); }

        }



        private void SetSceneProbeFailure(string error)

        {

            try

            {

                if (!_closed) _dataSource.SetReady(IllustratorUiText.CardProbeFailed(error));

            }

            catch (Exception ex) { Debug.Print("[Illustrator] Scene probe status failed: " + ex.Message); }

        }



        private void ExecuteConversationGenerationCore(ConversationVisualContext convContext, string preCapturedBase64 = null, List<EmblemSpec> emblemSpecs = null, bool automatic = false, string playerRedrawPrompt = null)

        {

            string pendingKey = "Conv_" + (convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC");

            if (JoinPendingGeneration(pendingKey, ConversationSessionKey(convContext))) return;

            ++_cacheLoadVersion;

            _dataSource.SetLoading(IllustratorUiText.ConversationPreparing);



            string partnerId = convContext.InterlocutorHero?.StringId ?? convContext.InterlocutorCharacter?.StringId ?? "NPC";

            string key = $"Conv_{partnerId}";

            string conversationSessionKey = ConversationSessionKey(convContext);

            ConversationIllustrationSessionSnapshot session;

            long sessionEpoch;

            lock (typeof(IllustrationCardPopup))

            {

                sessionEpoch = _conversationSessionEpoch;

                session = _conversationSession != null && string.Equals(_conversationSession.SessionKey, conversationSessionKey, StringComparison.Ordinal)

                    ? _conversationSession : null;

            }

            // 台词与当前会话最近两轮对话只进导演（DirectorOnlyFacts）——导演转成画面描述后，生图模型只见视觉文本，不再把台词画进图里

            _generationCount++;

            string variation = GenerateConversationSceneVariation(convContext);

            if (_generationCount > 1) variation += "\n" + VisualDirectorEngine.BuildRedrawVariationDirective(_generationCount);

            string hardFacts = convContext.BuildHardFacts();

            string directorFacts = convContext.BuildDirectorOnlyFacts();

            string baseArtDirection = convContext.BuildArtDirection(variation);

            TaleWorlds.Library.Debug.Print($"[Illustrator] ConvScene host='{convContext.EnvironmentProfile?.HostSceneDescription ?? ""}' loc='{convContext.EnvironmentProfile?.SpecificLocation ?? ""}' scene='{convContext.EnvironmentProfile?.RealSceneName ?? ""}'");

            TaleWorlds.Library.Debug.Print($"[Illustrator] ConvLight sceneTime={convContext.EnvironmentProfile?.HasSceneTime == true}, time='{convContext.EnvironmentProfile?.TimeOfDay ?? ""}'");

            string partnerName = convContext.InterlocutorHero != null && convContext.InterlocutorHero.Name != null

                ? convContext.InterlocutorHero.Name.ToString()

                : (convContext.InterlocutorCharacter != null && convContext.InterlocutorCharacter.Name != null

                    ? convContext.InterlocutorCharacter.Name.ToString()

                    : IllustratorText.Get("Conversation_31"));

            // Persist a package template, with its known generic identity alias,
            // so the final UI projection can follow interface language independently.
            string cachePartnerName = convContext.InterlocutorHero?.Name?.ToString()
                ?? convContext.InterlocutorCharacter?.Name?.ToString()
                ?? IllustratorText.Get("Card_DefaultPartnerCacheName");

            // Only the top-screen overlay is a 16:9 frame; the side panel keeps the MCM size.

            var options = IllustratorRuntime.CaptureOptions()?.WithPlayerRedrawRequest(playerRedrawPrompt);

            if (_autoFullscreen) options = options?.WithSceneImageSize();

            VisualDirectorEngine.RequirePlayerRedrawDirector(playerRedrawPrompt, options);

            if (_autoFullscreen)

                baseArtDirection += IllustratorText.Format("Directive_AspectFormat", options?.ImageSize);

            Hero interlocutor = convContext.InterlocutorHero;

            bool interlocutorCivilian = convContext.InterlocutorCivilian;

            Hero player = convContext.MainHero;

            bool playerCivilian = convContext.MainHeroCivilian;

            string playerEquipmentCode = convContext.MainHeroProfile?.EquipmentCode;

            string partnerEquipmentCode = convContext.InterlocutorProfile?.EquipmentCode;

            string playerName = player?.Name != null ? player.Name.ToString() : IllustratorText.Get("Side_Player");

            if (player == null || string.IsNullOrWhiteSpace(playerEquipmentCode) || string.IsNullOrWhiteSpace(partnerEquipmentCode))

            {

                _dataSource.SetReady(IllustratorUiText.ConversationEquipmentUnavailable);

                return;

            }

            var sceneSource = ScreenCaptureHelper.GetConversationSceneCaptureSource();

            if (string.IsNullOrWhiteSpace(preCapturedBase64))

                preCapturedBase64 = SelectOpeningSceneReference(_openingSceneBase64, _openingSceneSessionKey,

                    ConversationSessionKey(convContext, sceneSource.SessionOwnerKey));

            string campaignKey = _scope.CampaignKey;



            _scope.RunGeneration(key, conversationSessionKey, async token =>

            {

                GenerationDiagnostics.Current?.SetSubject(key);

                GenerationDiagnostics.Current?.RecordStage("conversation_generation_trigger", new JObject

                {

                    ["automatic"] = automatic,

                    ["popup"] = _instanceId,

                    ["sessionEpoch"] = sessionEpoch

                });

                GenerationDiagnostics.Current?.RecordStage("conversation_session_cache", new JObject

                {

                    ["hit"] = session != null,

                    ["sessionKey"] = conversationSessionKey,

                    ["reusedPanorama"] = session != null && session.SceneReferences != null,

                    ["reusedCharacterReferences"] = session != null && session.CharacterReferences != null

                });

                string actionHistory = string.Empty;

                try

                {

                    actionHistory = await Task.Run(() => IllustrationDirection.ReadEventActionHistory(campaignKey, key, _category), token).ConfigureAwait(false);

                }

                catch (OperationCanceledException) { throw; }

                catch (Exception ex) { Debug.Print("[Illustrator] Conversation action history read failed: " + ex.Message); }

                string workerArtDirection = baseArtDirection;

                if (!string.IsNullOrWhiteSpace(actionHistory)) workerArtDirection += "\n" + actionHistory;

                var promptPlan = new IllustrationPromptPlan(IllustratorText.Raw("Director_933"), hardFacts, workerArtDirection, directorFacts, playerRedrawPrompt);

                // 真实场景采全景；大地图和部队界面临时谈话只用地形事实。

                var ageEvidence = convContext.InterlocutorAgeSnapshot;

                if (ageEvidence != null)

                    GenerationDiagnostics.Current?.RecordStage("conversation_npc_age", new JObject

                    {

                        ["characterId"] = partnerId,

                        ["agentIndex"] = ageEvidence.AgentIndex.HasValue ? new JValue(ageEvidence.AgentIndex.Value) : JValue.CreateNull(),

                        ["source"] = ageEvidence.Source,

                        ["rawAge"] = ageEvidence.RawAge.HasValue ? new JValue(ageEvidence.RawAge.Value) : JValue.CreateNull(),

                        ["promptAge"] = ageEvidence.Age,

                        ["templateAgeUsed"] = false,

                        ["portraitBodyProperties"] = convContext.InterlocutorBodyProperties

                    });

                var directorRefs = new System.Collections.Generic.List<IllustrationReferenceImage>();

                var genRefs = new System.Collections.Generic.List<IllustrationReferenceImage>();

                ScreenCaptureHelper.ConversationSceneReferenceCapture sceneCapture = null;

                string sceneStatus = IllustratorText.Get("Card_ConversationReferenceReuse");

                IllustrationPromptPlan scenePromptPlan;

                string sceneDirectorNote = string.Empty;

                if (session != null)

                {

                    directorRefs.AddRange(session.SceneReferences ?? Array.Empty<IllustrationReferenceImage>());

                    directorRefs.AddRange(IllustrationReferenceRouting.SinglePortraitSessionReferences(session.CharacterReferences));

                    directorRefs.AddRange(session.EmblemReferences ?? Array.Empty<IllustrationReferenceImage>());

                    genRefs.AddRange(IllustrationReferenceRouting.SinglePortraitSessionReferences(session.GenerationCharacterReferences));

                    genRefs.AddRange(session.GenerationEmblemReferences ?? Array.Empty<IllustrationReferenceImage>());

                    scenePromptPlan = BuildConversationSessionPlan(promptPlan, session);

                }

                else

                {

                    sceneCapture = await ScreenCaptureHelper.CaptureConversationSceneReferencesAsync(sceneSource, token, preCapturedBase64, options?.EnableSceneOffscreenRendering != false).ConfigureAwait(false);

                    directorRefs.AddRange(sceneCapture.References);

                    sceneDirectorNote = sceneCapture.References == null || sceneCapture.References.Count == 0

                        ? (!sceneSource.RequiresMissionPanorama ? IllustratorText.Get("Facts_FieldEnvBasis") : IllustratorText.Get("Facts_EnvRefUnavailable")) + sceneCapture.DirectorNote : string.Empty;

                    scenePromptPlan = new IllustrationPromptPlan(promptPlan.Mode, promptPlan.HardFacts + sceneCapture.NearbyPropFacts, promptPlan.ArtDirection,

                        promptPlan.DirectorOnlyFacts + sceneDirectorNote, promptPlan.PlayerRedrawPrompt);

                    sceneStatus = sceneCapture.StatusText;

                }

                IllustratorRuntime.Post(() => { if (!_closed && !token.IsCancellationRequested) _dataSource.StatusText = session != null ? IllustratorUiText.ConversationReferenceReuse : IllustrationStatusUiText.Scene(sceneStatus) + " · " + IllustratorUiText.PortraitPreparing; });



                var characterRefs = new List<IllustrationReferenceImage>();

                var generationCharacterRefs = new List<IllustrationReferenceImage>();

                var emblemRefs = new List<IllustrationReferenceImage>();

                var generationEmblemRefs = new List<IllustrationReferenceImage>();

                if (session == null)

                {

                    // 离屏舞台提取在 scope 内携带 token：关闭/重绘时旧任务立即取消并拆舞台

                    Func<Task<CharacterPortraitReferences>> playerStage = player == null ? (Func<Task<CharacterPortraitReferences>>)null : () => ScreenCaptureHelper.ExtractHeroPortraitReferencesAsync(player, useCivilian: playerCivilian, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true, equipmentCodeOverride: playerEquipmentCode, appearance: convContext.MainHeroProfile.Appearance);

                    Func<Task<CharacterPortraitReferences>> partnerStage = interlocutor != null

                        ? () => ScreenCaptureHelper.ExtractHeroPortraitReferencesAsync(interlocutor, interlocutorCivilian, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true, equipmentCodeOverride: partnerEquipmentCode, appearance: convContext.InterlocutorProfile.Appearance)

                        : convContext.InterlocutorCharacter != null ? (Func<Task<CharacterPortraitReferences>>)(() => ScreenCaptureHelper.ExtractCharacterPortraitReferencesAsync(convContext.InterlocutorCharacter, cancellationToken: token, bodyProperties: convContext.InterlocutorBodyProperties, cleanTempFiles: options?.AutoCleanTempFiles == true, equipmentCodeOverride: partnerEquipmentCode, appearance: convContext.InterlocutorProfile.Appearance)) : null;

                    if (playerStage != null)

                    {

                        var portraits = await playerStage().ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(portraits.FullBody)) throw new InvalidOperationException("Player full-equipment offscreen portrait failed. Generation stopped.");

                        IllustrationReferenceRouting.AddCharacter(characterRefs, generationCharacterRefs, portraits, playerName,

                            IllustratorText.Format("Ref_ConversationPortraitPlayer", playerName));

                    }

                    if (partnerStage != null)

                    {

                        var portraits = await partnerStage().ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(portraits.FullBody)) throw new InvalidOperationException("Dialogue partner full-equipment offscreen portrait failed. Generation stopped.");

                        IllustrationReferenceRouting.AddCharacter(characterRefs, generationCharacterRefs, portraits, partnerName,

                            IllustratorText.Format("Ref_ConversationPortraitPartner", partnerName));

                    }

                    directorRefs.AddRange(characterRefs);

                    genRefs.AddRange(generationCharacterRefs);



                    foreach (var spec in emblemSpecs ?? new List<EmblemSpec>())

                    {

                        string b64 = await BannerEmblemComposer.ComposeToBase64Async(spec.Code, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(b64)) continue;

                        var r = new IllustrationReferenceImage(b64, IllustratorText.Format("Ref_ConversationEmblem", spec.Side, spec.Owner), IllustrationReferenceKind.Emblem);

                        emblemRefs.Add(r);

                        var playerProfile = convContext.MainHeroProfile;

                        var partnerProfile = convContext.InterlocutorProfile;

                        bool sideHasHeraldic =

                            (spec.PlayerSide && playerProfile != null && (playerProfile.HasHeraldicArmor || playerProfile.HasHeraldicShield || playerProfile.BannerEquipmentDetails.Count > 0)) ||

                            (spec.InterlocutorSide && partnerProfile != null && (partnerProfile.HasHeraldicArmor || partnerProfile.HasHeraldicShield || partnerProfile.BannerEquipmentDetails.Count > 0));

                        if (sideHasHeraldic) generationEmblemRefs.Add(r);

                    }

                    directorRefs.AddRange(emblemRefs);

                    genRefs.AddRange(generationEmblemRefs);



                    token.ThrowIfCancellationRequested();

                    lock (typeof(IllustrationCardPopup))

                    {

                        if (_conversationSessionEpoch == sessionEpoch && _conversationSession == null)

                        {

                            _conversationSession = new ConversationIllustrationSessionSnapshot

                            {

                                SessionKey = conversationSessionKey,

                                StableHardFacts = FreezeConversationHardFacts(promptPlan.HardFacts),

                                NearbyPropFacts = sceneCapture?.NearbyPropFacts ?? string.Empty,

                                SceneDirectorNote = sceneDirectorNote,

                                SceneReferences = new List<IllustrationReferenceImage>(sceneCapture?.References ?? Array.Empty<IllustrationReferenceImage>()),

                                CharacterReferences = new List<IllustrationReferenceImage>(characterRefs),

                                GenerationCharacterReferences = new List<IllustrationReferenceImage>(generationCharacterRefs),

                                EmblemReferences = new List<IllustrationReferenceImage>(emblemRefs),

                                GenerationEmblemReferences = new List<IllustrationReferenceImage>(generationEmblemRefs)

                            };

                        }

                    }

                }



                if (sceneSource != null) await sceneSource.EnsureCurrentAsync(token).ConfigureAwait(false);

                var direction = await VisualDirectorEngine.CreateDirectionAsync(scenePromptPlan, directorRefs, options, token).ConfigureAwait(false);

                string detailedPrompt = direction.Prompt;

                if (options?.EnableReferenceImageForGeneration != false)

                    IllustrationReferenceRouting.AddSceneReferences(genRefs, sceneCapture?.References ?? session?.SceneReferences, direction, token);

                IllustratorRuntime.Post(() => { if (!_closed && !token.IsCancellationRequested) _dataSource.StatusText = (session != null ? IllustratorUiText.ConversationReferenceReuse : IllustrationStatusUiText.Scene(sceneStatus) + " · " + IllustratorUiText.PortraitPreparing) + " · " + IllustrationStatusUiText.Direction(direction) + " · " + IllustratorUiText.ConversationDrawing; });

                var finalGenRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)genRefs;

                var result = await UniversalOpenAiImageClient.GenerateImageAsync(detailedPrompt, finalGenRefs, options, token).ConfigureAwait(false);

                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? detailedPrompt : result.ResolvedPrompt;

                CachedIllustrationItem saved = null;

                if (result.Success && result.ImageBytes != null)

                {

                    token.ThrowIfCancellationRequested();

                    saved = DiskImageCacheManager.SaveImage(key, result.ImageBytes, effectivePrompt, string.IsNullOrWhiteSpace(direction.Title) ? IllustratorText.Format("Card_ConversationCacheTitle", cachePartnerName) : direction.Title, _category, _scope.CampaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false, theme: direction.Theme, actionSummary: direction.ActionSummary, diagnosticId: result.DiagnosticId, directorStatus: direction.DirectionStatus, directorStatusText: direction.StatusText, directorFallbackReason: direction.FallbackReason, styleFingerprint: options?.StyleFingerprint);

                    if (saved != null) DiskImageCacheManager.PromoteDefaultIfNewest(saved, _scope.CampaignKey);

                }

                return new GenerationCompletion(result, saved, effectivePrompt);

            }, completion =>

            {

                // Every successful redraw is the newest conversation image. Promote it

                // before publishing so closing and reopening the card cannot resurrect an

                // older default while an automatic reply redraw is in flight.

                if (completion.SavedItem != null) DiskImageCacheManager.PromoteDefaultIfNewest(completion.SavedItem, _scope.CampaignKey);

                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null &&

                    PublishImage(completion.SavedItem, completion.Result.ImageBytes, completion.Prompt))

                {

                    if (_autoReplyRequested && !_autoReplyArmed)

                    {

                        _autoReplyArmed = true;

                        ConversationIllustrationPatch.LogAutoRedraw("armed popup=" + _instanceId);

                    }

                    _dataSource.SetReady(completion.SavedItem != null ? IllustrationStatusUiText.Cached(completion.SavedItem) : IllustratorUiText.ConversationComplete);

                }

                else

                {

                    _dataSource.SetReady(IllustratorUiText.CardDrawFailed(completion.Result?.ErrorMessage ?? IllustratorUiText.WeeklyNotSaved));

                }

                QueuePendingAutoRedraw();

            }, error =>

            {

                _dataSource.SetReady(IllustratorUiText.CardGenerationException(error));

                QueuePendingAutoRedraw();

            }, completion => completion.SavedItem, completion => completion.Result?.ErrorMessage ?? "Failed to save image");

        }



        private bool PublishImage(CachedIllustrationItem item)

        {

            return item != null && PublishImage(item, item.ImageData, item.Prompt);

        }



        private bool PublishImage(CachedIllustrationItem item, byte[] imageBytes, string prompt)

        {

            string spriteName = !string.IsNullOrWhiteSpace(item?.Key)

                ? item.Key + "_" + _instanceId + "_" + Guid.NewGuid().ToString("N")

                : "Illustration_" + Guid.NewGuid().ToString("N");



            // 无缓存条目的试采使用唯一纹理名；注册失败时保留原来的正式插画。

            // Keep the previous texture alive until replacement registration succeeds.

            var publishClock = System.Diagnostics.Stopwatch.StartNew();

            GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "ui_texture_begin", "bytes=" + (imageBytes?.Length ?? 0));

            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, imageBytes);

            if (sprite == null) { GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "ui_texture_failed", "texture registration returned null; bytes=" + (imageBytes?.Length ?? 0)); return false; }

            if (_activeSpriteName != spriteName) ReleaseActiveSprite();

            _displayedImage = item;

            _dataSource.SetEditableImage(item != null);

            _activeSpriteName = spriteName;

            if (!string.IsNullOrWhiteSpace(item?.Title)) _dataSource.TitleText = IllustratorUiText.ProjectKnownDefaultTitle(item.Title, item.Category);

            _dataSource.SetIllustration(item?.SubjectKey ?? spriteName, spriteName, prompt);

            if (_backdropLayer != null) HideMapConversationSceneUnderIllustration();

            GenerationDiagnostics.WriteDelivery(item?.DiagnosticId, "ui_publish_complete", "sprite=" + spriteName + "; bytes=" + (imageBytes?.Length ?? 0) + "; publishMs=" + publishClock.ElapsedMilliseconds);

            return true;

        }



        // The MapConversation layer (order 205) paints an opaque plate and the full-screen 3D

        // tableau above our backdrop, so a map illustration would stay invisible. Hide the

        // tableau branch and the plates beneath it; the dialogue branch after it stays visible.

        // Runs once per published image, not per frame.

        private void HideMapConversationSceneUnderIllustration()

        {

            if (_hiddenMapConversationWidgets.Count > 0 || _screen?.Layers == null) return;

            foreach (var layer in _screen.Layers)

            {

                if (layer == null || layer.IsFinalized || layer.Name != "MapConversation") continue;

                Widget root = (layer as GauntletLayer)?.UIContext?.Root;

                Widget tableau = root == null ? null : ScreenCaptureHelper.FindChildRecursive(root,

                    w => w.GetType().Name == "MapConversationTableauWidget");

                if (tableau == null) continue;

                Widget branch = tableau;

                while (branch.ParentWidget != null && branch.ParentWidget.GetType().Name != "MapConversationScreenButtonWidget")

                    branch = branch.ParentWidget;

                Widget container = branch.ParentWidget;

                if (container == null)

                {

                    HideMapConversationWidget(tableau);

                    continue;

                }

                int branchIndex = container.GetChildIndex(branch);

                for (int i = 0; i <= branchIndex && i < container.ChildCount; i++)

                    HideMapConversationWidget(container.GetChild(i));

            }

            if (_hiddenMapConversationWidgets.Count > 0)

                Debug.Print("[Illustrator] Map conversation tableau hidden under fullscreen illustration: " + _hiddenMapConversationWidgets.Count);

        }



        private void HideMapConversationWidget(Widget widget)

        {

            if (widget == null || !widget.IsVisible) return;

            widget.IsVisible = false;

            _hiddenMapConversationWidgets.Add(widget);

        }



        private void RestoreMapConversationScene()

        {

            foreach (var widget in _hiddenMapConversationWidgets)

            {

                try { widget.IsVisible = true; }

                catch (Exception ex) { Debug.Print("[Illustrator] Map conversation restore failed: " + ex.GetType().Name); }

            }

            _hiddenMapConversationWidgets.Clear();

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



        private static readonly string[] SystemUiLayerNames = { "EncyclopediaBar", "MapEscapeMenu", "MapCampaignOptions", "MissionEscapeMenu", "MissionOptions" };



        // Called once per application tick; only an active fullscreen conversation overlay does

        // work (a scan of the host screen's few layers), and widgets change only on transitions.

        internal static void TickSystemUiVisibility()

        {

            var instance = _activeInstance;

            if (instance == null || instance._closed) return;

            if (instance._screenshotMission != null && (!ReferenceEquals(TaleWorlds.MountAndBlade.Mission.Current, instance._screenshotMission)

                || instance._screenshotMission.MissionEnded || !instance._scope.IsCurrent))

            {

                instance.Close();

                return;

            }

            if (!instance._autoFullscreen) return;

            bool hide = instance.IsSystemUiCovering();

            if (hide == instance._hiddenForSystemUi) return;

            instance._hiddenForSystemUi = hide;

            instance.ApplyControlsVisibility();

        }



        private bool IsSystemUiCovering()

        {

            if (_screen == null || !ReferenceEquals(ScreenManager.TopScreen, _screen)) return true;

            try

            {

                if (_screen is MapScreen map && (map.EncyclopediaScreenManager?.IsEncyclopediaOpen == true || map.IsEscapeMenuOpened || map.IsInCampaignOptions))

                    return true;

            }

            catch { }

            var layers = _screen.Layers;

            if (layers == null) return false;

            foreach (var layer in layers)

                if (layer != null && !layer.IsFinalized && Array.IndexOf(SystemUiLayerNames, layer.Name) >= 0) return true;

            return false;

        }



        private void ApplyControlsVisibility()

        {

            var root = _layer?.UIContext?.Root;

            if (root != null) root.IsVisible = !_editingRedrawPrompt && !_hiddenForSystemUi;

        }



        public void Close()

        {

            if (_closed) return;

            _closed = true;

            try { _redrawEditor?.Dispose(); } catch (Exception ex) { Debug.Print("[Illustrator] Editor cleanup failed: " + ex.Message); } finally { _redrawEditor = null; }

            _openingSceneBase64 = null;

            _openingSceneSessionKey = null;

            _autoRedrawPending = false;

            _latestAutoPlayerText = null;

            _latestAutoReplyText = null;

            _latestAutoPriorHistory = null;

            try

            {

                IllustratorRuntime.GenerationUpdated -= OnGenerationUpdated;

                IllustratorRuntime.DefaultImageChanged -= OnDefaultImageChanged;

                _joinedGeneration = null;

                if (_scope != null && !_scope.DetachWindowIfGenerating()) _scope.Close();

            }

            catch

            {

            }

            finally

            {

                // A failure finalizing one movie must not leave the fullscreen

                // backdrop attached after the conversation or popup is closed.

                try { RestoreMapConversationScene(); } catch { }

                RemoveOwnedLayer(_layer);

                RemoveOwnedLayer(_backdropLayer);

                try { ReleaseActiveSprite(); } catch { }

                if (_activeInstance == this)

                {

                    _activeInstance = null;

                }

            }

        }



        private void RemoveOwnedLayer(ScreenLayer layer)

        {

            if (_screen == null || layer == null || layer.IsFinalized) return;

            try { _screen.RemoveLayer(layer); }

            catch (Exception ex) { Debug.Print("[Illustrator] Overlay cleanup failed: " + ex.Message); }

        }



        public static void CloseActiveConversation()

        {

            if (_activeInstance != null && string.Equals(_activeInstance._category, "conversation", StringComparison.Ordinal))

                _activeInstance.Close();

        }



        internal static void AutoRedrawActiveConversation()

        {

            IllustratorRuntime.AssertMainThread();

            var instance = _activeInstance;

            if (IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true ||

                instance == null || instance._closed ||

                !string.Equals(instance._category, "conversation", StringComparison.Ordinal)) return;

            if (!instance._scope.IsCurrent || Campaign.Current?.ConversationManager?.IsConversationInProgress != true) return;

            if (!instance._autoReplyArmed)

            {

                // A reply may arrive while the first manual image is still rendering. Keep one

                // pending turn so the successful first image immediately catches up.

                if (instance._autoReplyRequested && instance._dataSource.IsLoading)

                {

                    instance._autoRedrawPending = true;

                    ConversationIllustrationPatch.LogAutoRedraw("queued_first_image popup=" + instance._instanceId);

                }

                return;

            }

            if (instance._dataSource.IsLoading)

            {

                instance._autoRedrawPending = true;

                ConversationIllustrationPatch.LogAutoRedraw("queued_busy popup=" + instance._instanceId);

                return;

            }



            ConversationVisualContext context = ConversationContextExtractor.ExtractFromCurrentConversation();

            if (context == null) return;

            // The main reply is delivered before postprocess commits dialogue history. Use

            // the captured turn so this request cannot illustrate the previous NPC response.

            if (!string.IsNullOrWhiteSpace(instance._latestAutoReplyText))

            {

                context.DialogueSentence = instance._latestAutoReplyText;

                var turn = new List<ConversationContextExtractor.NativeDialogueLine>(instance._latestAutoPriorHistory

                    ?? new List<ConversationContextExtractor.NativeDialogueLine>());

                if (!string.IsNullOrWhiteSpace(instance._latestAutoPlayerText))

                    turn.Add(new ConversationContextExtractor.NativeDialogueLine { Kind = "player", Text = instance._latestAutoPlayerText });

                turn.Add(new ConversationContextExtractor.NativeDialogueLine

                {

                    Kind = "npc", Text = instance._latestAutoReplyText,

                    Speaker = context.InterlocutorHero?.Name?.ToString() ?? context.InterlocutorCharacter?.Name?.ToString() ?? IllustratorText.Get("Conversation_31")

                });

                context.RecentDialogueHistory = ConversationContextExtractor.BuildRecentDialogueHistory(turn, ConversationContextExtractor.RecentDialogueLimit);

            }

            var emblems = new List<EmblemSpec>();

            AddEmblemSpec(emblems, context.InterlocutorHero, IllustratorText.Raw("Residual_IllustrationCardPopup_367d88405654"));

            AddEmblemSpec(emblems, context.MainHero, IllustratorText.Raw("Residual_IllustrationCardPopup_991e63fe2920"));

            instance._autoRedrawPending = false;

            ConversationIllustrationPatch.LogAutoRedraw("start popup=" + instance._instanceId);

            instance.ExecuteConversationGeneration(context, null, emblems, automatic: true);

        }



        internal static Action<string, Hero, CharacterObject> CaptureAutoReplyObserver(string playerText)

        {

            // UI submissions arrive on the game thread. No scene scans or polling: capture

            // a few owner references once per submitted turn, then validate on reply delivery.

            if (!IllustratorRuntime.IsMainThread || IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true) return null;

            var instance = _activeInstance;

            var manager = Campaign.Current?.ConversationManager;

            if (instance == null || instance._closed || instance._category != "conversation" ||

                (!instance._autoReplyArmed && !instance._autoReplyRequested) ||

                !instance._scope.IsCurrent || manager?.IsConversationInProgress != true) return null;

            long epoch = _conversationSessionEpoch;

            var character = manager.OneToOneConversationCharacter;

            // Prefix capture precedes this turn's history commit: never duplicate the delivered reply.

            var priorHistory = ConversationContextExtractor.ReadNativeConversationHistory(260);

            ConversationIllustrationPatch.LogAutoRedraw("request_observed popup=" + instance._instanceId);

            int delivered = 0;

            return (content, targetHero, targetCharacter) =>

            {

                if (string.IsNullOrWhiteSpace(content) || System.Threading.Interlocked.Exchange(ref delivered, 1) != 0) return;

                // A single completion per captured request keeps this queue bounded. Never

                // substitute the latest popup for the one that authorized this request.

                IllustratorRuntime.PostCritical(() =>

                {

                    if (!ReferenceEquals(_activeInstance, instance) || instance._closed ||

                        epoch != _conversationSessionEpoch || !instance._scope.IsCurrent ||

                        !ReferenceEquals(Campaign.Current?.ConversationManager, manager) || !manager.IsConversationInProgress ||

                        !ReferenceEquals(manager.OneToOneConversationCharacter, character) ||

                        !IsSameConversationTarget(manager, character, targetHero, targetCharacter))

                    {

                        ConversationIllustrationPatch.LogAutoRedraw("dropped_stale popup=" + instance._instanceId);

                        return;

                    }

                    if (IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true)

                    {

                        instance._autoRedrawPending = false;

                        ConversationIllustrationPatch.LogAutoRedraw("dropped_disabled popup=" + instance._instanceId);

                        return;

                    }

                    instance._latestAutoPlayerText = playerText;

                    instance._latestAutoReplyText = content;

                    instance._latestAutoPriorHistory = priorHistory;

                    ConversationIllustrationPatch.LogAutoRedraw("reply_received popup=" + instance._instanceId + " chars=" + content.Length);

                    AutoRedrawActiveConversation();

                });

            };

        }



        private static bool IsSameConversationTarget(TaleWorlds.CampaignSystem.Conversation.ConversationManager manager,

            CharacterObject capturedCharacter, Hero targetHero, CharacterObject targetCharacter)

        {

            if (targetCharacter != null && capturedCharacter != null)

                return ReferenceEquals(targetCharacter, capturedCharacter);

            if (targetHero != null)

            {

                try { return ReferenceEquals(targetHero, Hero.OneToOneConversationHero); } catch { }

            }

            return targetCharacter == null && targetHero == null && capturedCharacter == null;

        }



        internal static void ClearConversationSessionCache()

        {

            lock (typeof(IllustrationCardPopup))

            {

                _conversationSessionEpoch++;

                _conversationSession = null;

                _sessionOpeningScene = null;

                _sessionOpeningSceneKey = null;

            }

        }

    }

}
