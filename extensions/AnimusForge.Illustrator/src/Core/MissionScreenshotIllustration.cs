using System;

using System.Globalization;

using System.Text;

using System.Threading;

using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;

using TaleWorlds.Core;

using TaleWorlds.InputSystem;

using TaleWorlds.Library;

using TaleWorlds.MountAndBlade;

using TaleWorlds.ScreenSystem;

using AnimusForge.Illustrator.Engine;

using AnimusForge.Illustrator.UI;

using AnimusForge.Illustrator.UI.Overlays;



namespace AnimusForge.Illustrator.Core

{

    internal static class MissionScreenshotIllustration

    {

        private static IllustrationScope _scope;

        private static MissionScreenshotCapture _capture;

        private static string _status = "";

        private static Mission _statusMission;

        private static string _shoutKeyName, _menuKeyName;

        private static InputKey _shoutKey = InputKey.T, _menuKey = InputKey.Y;

        private static bool _lastAvailable;

        private static Mission _lastMission;

        private static Action _openProgress;

        internal static void Install()

        {

            ShoutBehavior.SceneIllustrationAvailableHook = () => IllustratorRuntime.IsMainThread && IllustratorRuntime.IsEnabled() && Mission.Current != null;

            ShoutBehavior.SceneIllustrationBusyHook = () => _capture != null && !_capture.Completion.IsCompleted;

            ShoutBehavior.SceneIllustrationStatusHook = () => ReferenceEquals(_statusMission, Mission.Current) ? _status : "";

            ShoutBehavior.SceneIllustrationRequestHook = Request;

            ShoutBehavior.SceneIllustrationGalleryHook = () => UI.Gallery.IllustratorGalleryPopup.Show();

        }



        internal static void Tick()

        {

            bool available = IllustratorRuntime.IsEnabled() && Mission.Current != null;

            if (available != _lastAvailable || !ReferenceEquals(_lastMission, Mission.Current))

            {

                _lastAvailable = available; _lastMission = Mission.Current;

                ShoutBehavior.NotifySceneIllustrationChangedForExternal();

            }

            TickCapture();

            if (_capture != null) return;

            // Campaign owns its existing hotkeys. Custom battles have no CampaignBehavior owner.

            if (Campaign.Current != null || Mission.Current == null || !IllustratorRuntime.IsEnabled()

                || !ShoutBehavior.IsSceneIllustrationBattleForExternal || ShoutTextInputPopup.IsOpen

                || (ScreenManager.FocusedLayer?.IsFocusLayer == true && !ReferenceEquals(ScreenManager.FocusedLayer,

                    (ScreenManager.TopScreen as TaleWorlds.MountAndBlade.View.Screens.MissionScreen)?.SceneLayer))

                || InformationManager.IsAnyInquiryActive()) return;

            var settings = DuelSettings.GetSettings();

            string shout = settings?.ShoutKey, menu = settings?.ShoutSpecialMenuKey;

            if (shout != _shoutKeyName) { _shoutKeyName = shout; if (!Enum.TryParse(shout, true, out _shoutKey)) _shoutKey = InputKey.T; }

            if (menu != _menuKeyName) { _menuKeyName = menu; if (!Enum.TryParse(menu, true, out _menuKey)) _menuKey = InputKey.Y; }

            if (Input.IsKeyPressed(_shoutKey) || Input.IsKeyPressed(_menuKey)) OpenCustomBattleInput();

        }



        private static void TickCapture()

        {

            _capture?.Tick();

            if (_capture?.Completion.IsCompleted != true) return;

            // Only native capture owns the camera gate. Each admitted scope keeps its own

            // immutable screenshots and continues independently on the existing bounded workers.

            var capture = _capture;

            var scope = _scope;

            _capture = null;

            Action open = _openProgress; _openProgress = null;

            ShoutBehavior.NotifySceneIllustrationChangedForExternal();

            if (capture.Completion.IsCanceled)

            { Finish(scope, capture, _statusMission, IllustratorUiText.MissionCancelled); return; }

            if (capture.Completion.IsFaulted)

            { Finish(scope, capture, _statusMission, IllustratorUiText.MissionCaptureFailed(capture.Completion.Exception.GetBaseException().Message)); return; }

            try { open?.Invoke(); }

            catch (Exception ex) { Finish(scope, capture, _statusMission, IllustratorUiText.MissionStartFailed(ex.Message)); }

        }



        private static void OpenCustomBattleInput()

        {

            Mission mission = Mission.Current;

            float speed = mission.Scene.TimeSpeed;

            Action restore = () => { if (ReferenceEquals(Mission.Current, mission) && mission.Scene != null) mission.Scene.TimeSpeed = speed; };

            ShoutTextInputPopup.Show(IllustratorUiText.MissionBattleTitle, IllustratorUiText.MissionInputHint, IllustratorUiText.MissionInputPlaceholder, "",

                _ => { restore(); InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.MissionNoDialogue)); }, restore,

                enableIllustration: true);

        }



        private static void SetStatus(Mission mission, string status)

        {

            _statusMission = ReferenceEquals(Mission.Current, mission) ? mission : null;

            _status = status;

            IllustrationCardPopup.UpdateMissionScreenshotStatus(_scope, status);

            ShoutBehavior.NotifySceneIllustrationChangedForExternal();

        }



        private static void Request(Func<bool> panelStillOpen)

        {

            IllustratorRuntime.AssertMainThread();

            if (_capture != null && !_capture.Completion.IsCompleted) { InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.MissionCapturing)); return; }

            Mission mission = Mission.Current;

            var originScreen = ScreenManager.TopScreen;

            IllustrationOptions options;

            MissionScreenshotCapture capture = null;

            IllustrationScope scope = null;

            string facts, dialogue, subject;

            bool battle = ShoutBehavior.IsSceneIllustrationBattleForExternal;

            try

            {

                if (!IllustratorRuntime.IsEnabled() || mission?.MainAgent == null) throw new InvalidOperationException("No player scene is available for screenshot illustration, or image generation is disabled.");

                options = IllustratorRuntime.CaptureOptions()?.ForMissionScreenshot();

                VisualDirectorEngine.RequireMissionScreenshotDirector(options);

                if (string.IsNullOrWhiteSpace(options.ApiBaseUrl)) throw new InvalidOperationException("Configure the image API first.");

                Agent player = mission.MainAgent;

                Agent target = ShoutBehavior.GetScenePresentationWheelTargetForExternal();

                facts = CaptureFacts(player, target, battle);

                dialogue = battle ? "" : ShoutBehavior.CaptureSceneIllustrationDialogueForExternal();

                subject = "mission_" + Guid.NewGuid().ToString("N");

                // Admit the lifetime before acquiring native resources; cleanup is transactional.

                scope = new IllustrationScope(ScreenManager.TopScreen, "general", () => ScopeClosed(scope, capture, mission), campaignOwned: true, missionOwned: true);

                capture = new MissionScreenshotCapture(mission, options.AutoCleanTempFiles);

            }

            catch (Exception ex)

            {

                capture?.Cancel("Screenshot initialization failed.");

                scope?.Close();

                SetStatus(mission, IllustratorUiText.MissionFailed(ex.Message));

                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.MissionNotice(_status)));

                return;

            }



            _capture = capture;

            _scope = scope;

            SetStatus(mission, IllustratorUiText.MissionFraming);

            // No worker/API admission while the player composes or chooses another shot.

            _openProgress = () => {

                var pair = capture.Completion.GetAwaiter().GetResult();

                StartGeneration(scope, capture, mission, originScreen, panelStillOpen, subject, facts, dialogue, battle, options, pair);

            };

        }



        private static void StartGeneration(IllustrationScope scope, MissionScreenshotCapture capture, Mission mission,

            ScreenBase originScreen, Func<bool> panelStillOpen, string subject, string facts, string dialogue, bool battle,

            IllustrationOptions options, MissionScreenshotPair captured)

        {

            try

            {

                bool started = scope.RunGeneration(subject, null, async token =>

                {

                    using (token.Register(() => IllustratorRuntime.PostCritical(() => capture.Cancel(IllustratorText.Raw("Residual_MissionScreenshotIllustration_e6c0b8df9d40")))))

                    {

                        MissionScreenshotPair pair = captured; captured = null;

                        token.ThrowIfCancellationRequested();

                        // Hash and decode large native files only inside the bounded worker.

                        if (pair.Reverse != null && MissionScreenshotImageCodec.SameImage(pair.Current, pair.Reverse))

                            throw new InvalidOperationException("The two captures are identical. Capture the scene again. Generation stopped.");

                        GenerationDiagnostics.Current?.RecordStage("mission_screenshots_complete", new Newtonsoft.Json.Linq.JObject {

                            ["viewCount"] = pair.Count, ["source"] = "native_bmp_exclusive_file", ["gpu_frame_identity_verified"] = false,

                            ["firstRawBytes"] = pair.Current.Length, ["secondRawBytes"] = pair.Reverse?.Length ?? 0 });

                        byte[] current = MissionScreenshotImageCodec.ToPng(pair.Current, token);

                        string contract = MissionScreenshotRules.Contract(battle);

                        var references = new System.Collections.Generic.List<IllustrationReferenceImage> {

                            new IllustrationReferenceImage(Convert.ToBase64String(current), IllustratorText.Get("Screenshot_RefA") + contract, IllustrationReferenceKind.MissionScreenshot)

                        };

                        if (pair.Reverse != null)

                        {

                            byte[] additional = MissionScreenshotImageCodec.ToPng(pair.Reverse, token);

                            references.Add(new IllustrationReferenceImage(Convert.ToBase64String(additional), IllustratorText.Get("Screenshot_RefB") + contract, IllustrationReferenceKind.MissionScreenshot));

                        }

                        pair = null; current = null;

                        var plan = new IllustrationPromptPlan(battle ? MissionScreenshotRules.BattleMode : MissionScreenshotRules.ShoutMode, facts, contract, dialogue);

                        IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, scope)) SetStatus(mission, IllustratorUiText.MissionDirecting); });

                        var direction = await VisualDirectorEngine.CreateDirectionAsync(plan, references, options, token).ConfigureAwait(false);

                        IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, scope)) SetStatus(mission, IllustratorUiText.MissionGenerating); });

                        var image = await UniversalOpenAiImageClient.GenerateImageAsync(direction.Prompt, references, options, token).ConfigureAwait(false);

                        if (!image.Success || image.ImageBytes == null) throw new InvalidOperationException(image.ErrorMessage ?? "Image generation did not return an image.");

                        token.ThrowIfCancellationRequested();

                        var saved = DiskImageCacheManager.SaveImageWithEditMetadata(subject, image.ImageBytes,

                            string.IsNullOrWhiteSpace(image.ResolvedPrompt) ? direction.Prompt : image.ResolvedPrompt,

                            string.IsNullOrWhiteSpace(direction.Title) ? (battle ? IllustratorUiText.MissionBattleTitle : IllustratorUiText.MissionShoutTitle) : direction.Title,

                            "general", scope.CampaignKey, options.MaxCacheCount, makeDefault: true, allowImplicitDefault: false,

                            theme: direction.Theme, actionSummary: direction.ActionSummary, diagnosticId: image.DiagnosticId,

                            directorStatus: direction.DirectionStatus, directorStatusText: direction.StatusText,

                            directorFallbackReason: direction.FallbackReason, styleFingerprint: options.StyleFingerprint,

                            generationMode: battle ? "battle_screenshots" : "scene_shout_screenshots");

                        if (saved == null) throw new InvalidOperationException("The generated image could not be saved to the gallery.");

                        // Disk save returns metadata only. Carry this request's bytes to the waiting

                        // card subscriber; never decode/read the cache on the UI thread.

                        saved.ImageData = image.ImageBytes;

                        return saved;

                    }

                }, saved =>

                {

                    Finish(scope, capture, mission, IllustratorUiText.MissionSaved);

                }, error => Finish(scope, capture, mission, IllustratorUiText.MissionFailed(error)), value => value, _ => "The generated image could not be saved.");

                if (!started) { Finish(scope, capture, mission, IllustratorUiText.MissionBusy); return; }

                TryShowProgress(scope, subject, mission, originScreen, panelStillOpen);

            }

            catch (Exception ex) { Finish(scope, capture, mission, IllustratorUiText.MissionFailed(ex.Message)); }

        }



        private static void TryShowProgress(IllustrationScope scope, string subject, Mission mission,

            ScreenBase originScreen, Func<bool> panelStillOpen)

        {

            try

            {

                if (ReferenceEquals(_scope, scope) && ReferenceEquals(Mission.Current, mission) && !mission.MissionEnded

                    && ReferenceEquals(ScreenManager.TopScreen, originScreen) && panelStillOpen?.Invoke() == true)

                    IllustrationCardPopup.ShowForMissionScreenshot(scope, subject, mission, IllustratorUiText.MissionPreparing);

            }

            catch (Exception ex)

            {

                // Presentation is optional after admission. Never cancel or resubmit the paid worker here.

                Debug.Print("[Illustrator] Mission progress UI unavailable: " + ex.GetType().Name);

                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.MissionPanelUnavailable));

            }

        }



        private static string CaptureFacts(Agent player, Agent target, bool battle)

        {

            var facts = new StringBuilder();

            facts.AppendLine(battle ? IllustratorText.Get("Screenshot_FactsBattle") : IllustratorText.Get("Screenshot_FactsSceneShout"));

            facts.AppendLine(IllustratorText.Format("Screenshot_FactsPlayer", player.Name));

            facts.AppendLine(IllustratorText.Format("Screenshot_FactsPlayerLook", Format(player.LookDirection.x) + "," + Format(player.LookDirection.y) + "," + Format(player.LookDirection.z)));

            if (target != null && target.IsActive() && target.Mission == player.Mission)

            {

                Vec3 relative = target.Position - player.Position;

                facts.AppendLine(IllustratorText.Format("Screenshot_FactsTarget", target.Name, Format(relative.Length), Format(relative.z)));

                facts.AppendLine(IllustratorText.Format("Screenshot_FactsTargetLook", Format(target.LookDirection.x) + "," + Format(target.LookDirection.y) + "," + Format(target.LookDirection.z)));

            }

            else facts.AppendLine(IllustratorText.Get("Screenshot_FactsNoTarget"));

            return facts.ToString();

        }

        private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static void Finish(IllustrationScope scope, MissionScreenshotCapture capture, Mission mission, string status)

        {

            capture.Cancel("Free framing concluded.");

            if (ReferenceEquals(_scope, scope))

            {

                SetStatus(mission, status);

                _scope = null; _capture = null; _openProgress = null;

                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.MissionNotice(status)));

            }

            scope.Close();

        }

        private static void ScopeClosed(IllustrationScope scope, MissionScreenshotCapture capture, Mission mission)

        {

            capture?.Cancel("Screenshot generation cancelled (generation disabled or host switched).");

            if (!ReferenceEquals(_scope, scope)) return;

            _scope = null; _capture = null; _openProgress = null;

            SetStatus(mission, IllustratorUiText.MissionScopeCancelled);

            InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.MissionNotice(_status)));

        }

        internal static void Reset()

        {

            _capture?.Cancel("Host switched; capture terminated.");

            _capture = null;

            _openProgress = null;

            _scope?.Close();

            _scope = null;

            _status = ""; _statusMission = null;

            ShoutBehavior.NotifySceneIllustrationChangedForExternal();

        }

    }

}
