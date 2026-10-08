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
            { Finish(scope, capture, _statusMission, "本次取景已取消，未发送生图请求。"); return; }
            if (capture.Completion.IsFaulted)
            { Finish(scope, capture, _statusMission, "取景失败：" + capture.Completion.Exception.GetBaseException().Message); return; }
            try { open?.Invoke(); }
            catch (Exception ex) { Finish(scope, capture, _statusMission, "生图启动失败：" + ex.Message); }
        }

        private static void OpenCustomBattleInput()
        {
            Mission mission = Mission.Current;
            float speed = mission.Scene.TimeSpeed;
            Action restore = () => { if (ReferenceEquals(Mission.Current, mission) && mission.Scene != null) mission.Scene.TimeSpeed = speed; };
            ShoutTextInputPopup.Show("战斗现场", "点击生图：自由取景，Enter 截取一至两张，确认后由导演推演插画。", "当前模式只提供截图生图，不发送 NPC 喊话。", "",
                _ => { restore(); InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 当前自定义战斗没有可用的交流链路。")); }, restore,
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
            if (_capture != null && !_capture.Completion.IsCompleted) { InformationManager.DisplayMessage(new InformationMessage("[AI画卷] 正在采集现场，截图完成后即可再次生图。")); return; }
            Mission mission = Mission.Current;
            var originScreen = ScreenManager.TopScreen;
            IllustrationOptions options;
            MissionScreenshotCapture capture = null;
            IllustrationScope scope = null;
            string facts, dialogue, subject;
            bool battle = ShoutBehavior.IsSceneIllustrationBattleForExternal;
            try
            {
                if (!IllustratorRuntime.IsEnabled() || mission?.MainAgent == null) throw new InvalidOperationException("当前没有可供截图生图的玩家场景，或生图尚未启用。");
                options = IllustratorRuntime.CaptureOptions()?.ForMissionScreenshot();
                VisualDirectorEngine.RequireMissionScreenshotDirector(options);
                if (string.IsNullOrWhiteSpace(options.ApiBaseUrl)) throw new InvalidOperationException("请先配置生图 API。");
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
                capture?.Cancel("截图启动失败。");
                scope?.Close();
                SetStatus(mission, "生图失败：" + ex.Message);
                InformationManager.DisplayMessage(new InformationMessage("[AI画卷] " + _status));
                return;
            }

            _capture = capture;
            _scope = scope;
            SetStatus(mission, "自由取景：Enter 截图，最多两张；确认后才发送生成请求。");
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
                    using (token.Register(() => IllustratorRuntime.PostCritical(() => capture.Cancel("截图生图请求已取消。"))))
                    {
                        MissionScreenshotPair pair = captured; captured = null;
                        token.ThrowIfCancellationRequested();
                        // Hash and decode large native files only inside the bounded worker.
                        if (pair.Reverse != null && MissionScreenshotImageCodec.SameImage(pair.Current, pair.Reverse))
                            throw new InvalidOperationException("两张截图相同，请重新取景；已停止生成。");
                        GenerationDiagnostics.Current?.RecordStage("mission_screenshots_complete", new Newtonsoft.Json.Linq.JObject {
                            ["viewCount"] = pair.Count, ["source"] = "native_bmp_exclusive_file", ["gpu_frame_identity_verified"] = false,
                            ["firstRawBytes"] = pair.Current.Length, ["secondRawBytes"] = pair.Reverse?.Length ?? 0 });
                        byte[] current = MissionScreenshotImageCodec.ToPng(pair.Current, token);
                        string contract = MissionScreenshotRules.Contract(battle);
                        var references = new System.Collections.Generic.List<IllustrationReferenceImage> {
                            new IllustrationReferenceImage(Convert.ToBase64String(current), "截图A：玩家自由取景的现场参考，不规定最终构图。" + contract, IllustrationReferenceKind.MissionScreenshot)
                        };
                        if (pair.Reverse != null)
                        {
                            byte[] additional = MissionScreenshotImageCodec.ToPng(pair.Reverse, token);
                            references.Add(new IllustrationReferenceImage(Convert.ToBase64String(additional), "截图B：玩家补充拍摄的同一冻结现场，不要求与A反向；不是另一个时间或另一组人物，不规定最终构图。" + contract, IllustrationReferenceKind.MissionScreenshot));
                        }
                        pair = null; current = null;
                        var plan = new IllustrationPromptPlan(battle ? MissionScreenshotRules.BattleMode : MissionScreenshotRules.ShoutMode, facts, contract, dialogue);
                        IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, scope)) SetStatus(mission, "现场截图已确认，导演正在推演…"); });
                        var direction = await VisualDirectorEngine.CreateDirectionAsync(plan, references, options, token).ConfigureAwait(false);
                        IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, scope)) SetStatus(mission, "导演已完成，正在生成插画；可关闭面板…"); });
                        var image = await UniversalOpenAiImageClient.GenerateImageAsync(direction.Prompt, references, options, token).ConfigureAwait(false);
                        if (!image.Success || image.ImageBytes == null) throw new InvalidOperationException(image.ErrorMessage ?? "生图未返回图像。");
                        token.ThrowIfCancellationRequested();
                        var saved = DiskImageCacheManager.SaveImageWithEditMetadata(subject, image.ImageBytes,
                            string.IsNullOrWhiteSpace(image.ResolvedPrompt) ? direction.Prompt : image.ResolvedPrompt,
                            string.IsNullOrWhiteSpace(direction.Title) ? (battle ? "战斗现场" : "场景喊话") : direction.Title,
                            "general", scope.CampaignKey, options.MaxCacheCount, makeDefault: true, allowImplicitDefault: false,
                            theme: direction.Theme, actionSummary: direction.ActionSummary, diagnosticId: image.DiagnosticId,
                            directorStatus: direction.DirectionStatus, directorStatusText: direction.StatusText,
                            directorFallbackReason: direction.FallbackReason, styleFingerprint: options.StyleFingerprint,
                            generationMode: battle ? "battle_screenshots" : "scene_shout_screenshots");
                        if (saved == null) throw new InvalidOperationException("成图未能保存到画廊。");
                        // Disk save returns metadata only. Carry this request's bytes to the waiting
                        // card subscriber; never decode/read the cache on the UI thread.
                        saved.ImageData = image.ImageBytes;
                        return saved;
                    }
                }, saved =>
                {
                    Finish(scope, capture, mission, "插画已保存到画廊。");
                }, error => Finish(scope, capture, mission, "生图失败：" + error), value => value, _ => "成图保存失败。");
                if (!started) { Finish(scope, capture, mission, "生图任务繁忙，请等待已有任务完成。"); return; }
                TryShowProgress(scope, subject, mission, originScreen, panelStillOpen);
            }
            catch (Exception ex) { Finish(scope, capture, mission, "生图失败：" + ex.Message); }
        }

        private static void TryShowProgress(IllustrationScope scope, string subject, Mission mission,
            ScreenBase originScreen, Func<bool> panelStillOpen)
        {
            try
            {
                if (ReferenceEquals(_scope, scope) && ReferenceEquals(Mission.Current, mission) && !mission.MissionEnded
                    && ReferenceEquals(ScreenManager.TopScreen, originScreen) && panelStillOpen?.Invoke() == true)
                    IllustrationCardPopup.ShowForMissionScreenshot(scope, subject, mission, "现场截图已确认，正在准备画卷…");
            }
            catch (Exception ex)
            {
                // Presentation is optional after admission. Never cancel or resubmit the paid worker here.
                Debug.Print("[Illustrator] Mission progress UI unavailable: " + ex.GetType().Name);
                InformationManager.DisplayMessage(new InformationMessage("[AI画卷] 等待面板打开失败，生成仍在后台继续，完成后可到画廊查看。"));
            }
        }

        private static string CaptureFacts(Agent player, Agent target, bool battle)
        {
            var facts = new StringBuilder();
            facts.AppendLine(battle ? "战斗：固定取景冻结瞬间的动作，不能推演后续战果。" : "场景喊话：已发生对白动作优先，截图提供现场关系。");
            facts.AppendLine("玩家：" + player.Name);
            facts.AppendLine("玩家现场朝向向量：" + Format(player.LookDirection.x) + "," + Format(player.LookDirection.y) + "," + Format(player.LookDirection.z));
            if (target != null && target.IsActive() && target.Mission == player.Mission)
            {
                Vec3 relative = target.Position - player.Position;
                facts.AppendLine("框选交流对象：" + target.Name + "；与玩家的实测距离 " + Format(relative.Length) + " 米；相对高差 " + Format(relative.z) + " 米。");
                facts.AppendLine("目标朝向向量：" + Format(target.LookDirection.x) + "," + Format(target.LookDirection.y) + "," + Format(target.LookDirection.z));
            }
            else facts.AppendLine("没有指定 NPC 主目标，画面主体与在场关系依据截图，不补造目标或人物。");
            return facts.ToString();
        }
        private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private static void Finish(IllustrationScope scope, MissionScreenshotCapture capture, Mission mission, string status)
        {
            capture.Cancel("自由取景已结束。");
            if (ReferenceEquals(_scope, scope))
            {
                SetStatus(mission, status);
                _scope = null; _capture = null; _openProgress = null;
                InformationManager.DisplayMessage(new InformationMessage("[AI画卷] " + status));
            }
            scope.Close();
        }
        private static void ScopeClosed(IllustrationScope scope, MissionScreenshotCapture capture, Mission mission)
        {
            capture?.Cancel("截图生图已取消（生图关闭或宿主切换）。");
            if (!ReferenceEquals(_scope, scope)) return;
            _scope = null; _capture = null; _openProgress = null;
            SetStatus(mission, "截图生图已取消（生图关闭或宿主切换）。");
            InformationManager.DisplayMessage(new InformationMessage("[AI画卷] " + _status));
        }
        internal static void Reset()
        {
            _capture?.Cancel("生图宿主已切换，采集终止。");
            _capture = null;
            _openProgress = null;
            _scope?.Close();
            _scope = null;
            _status = ""; _statusMission = null;
            ShoutBehavior.NotifySceneIllustrationChangedForExternal();
        }
    }
}
