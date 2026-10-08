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
            _capture = null;
            Action open = _openProgress; _openProgress = null;
            ShoutBehavior.NotifySceneIllustrationChangedForExternal();
            try { open?.Invoke(); }
            catch (Exception ex) { InformationManager.DisplayMessage(new InformationMessage("[AI画卷] 等待面板打开失败：" + ex.Message)); }
        }

        private static void OpenCustomBattleInput()
        {
            Mission mission = Mission.Current;
            float speed = mission.Scene.TimeSpeed;
            Action restore = () => { if (ReferenceEquals(Mission.Current, mission) && mission.Scene != null) mission.Scene.TimeSpeed = speed; };
            ShoutTextInputPopup.Show("战斗现场", "点击生图：采集当前与反向机位，再由导演推演一幅插画。", "当前模式只提供截图生图，不发送 NPC 喊话。", "",
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
                Vec3 pivot = player.Position + Vec3.Up * 1.3f;
                float subjectRadius = 1.6f;
                if (target != null && target.IsActive() && target.Mission == mission && (target.Position - player.Position).Length < 30f)
                {
                    pivot = (player.Position + target.Position) * 0.5f + Vec3.Up * 1.3f;
                    subjectRadius += (target.Position - player.Position).Length * 0.5f;
                }
                facts = CaptureFacts(player, target, battle);
                dialogue = battle ? "" : ShoutBehavior.CaptureSceneIllustrationDialogueForExternal();
                subject = "mission_" + Guid.NewGuid().ToString("N");
                // Admit the lifetime before acquiring native resources; cleanup is transactional.
                scope = new IllustrationScope(ScreenManager.TopScreen, "general", () => ScopeClosed(scope, capture, mission), campaignOwned: true, missionOwned: true);
                capture = new MissionScreenshotCapture(mission, pivot, player.Frame.rotation.f, subjectRadius, options.AutoCleanTempFiles);
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
            SetStatus(mission, "正在采集两张截图，完成后恢复原画面…");
            // Open only after capture restores the UI/camera. Never put the waiting card into a screenshot.
            _openProgress = () => {
                if (ReferenceEquals(_scope, scope) && ReferenceEquals(Mission.Current, mission) && !mission.MissionEnded
                    && ReferenceEquals(ScreenManager.TopScreen, originScreen) && panelStillOpen?.Invoke() == true)
                    IllustrationCardPopup.ShowForMissionScreenshot(scope, subject, mission, "双截图采集已结束，正在准备画卷…");
            };
            try
            {
                bool started = scope.RunGeneration(subject, null, async token =>
                {
                    using (token.Register(() => IllustratorRuntime.PostCritical(() => capture.Cancel("截图生图请求已取消。"))))
                    {
                        MissionScreenshotPair pair = await capture.Completion.ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        // Hash and decode large native files only inside the bounded worker.
                        if (MissionScreenshotImageCodec.SameImage(pair.Current, pair.Reverse))
                            throw new InvalidOperationException("两张截图相同，未取得有效反向画面；已停止生成。");
                        GenerationDiagnostics.Current?.RecordStage("mission_screenshots_complete", new Newtonsoft.Json.Linq.JObject {
                            ["viewCount"] = 2, ["source"] = "native_bmp_exclusive_file", ["gpu_frame_identity_verified"] = false,
                            ["firstRawBytes"] = pair.Current.Length, ["secondRawBytes"] = pair.Reverse.Length });
                        byte[] current = MissionScreenshotImageCodec.ToPng(pair.Current, token);
                        byte[] reverse = MissionScreenshotImageCodec.ToPng(pair.Reverse, token);
                        string contract = MissionScreenshotRules.Contract(battle);
                        var references = new[] {
                            new IllustrationReferenceImage(Convert.ToBase64String(current), "截图A：以玩家身体朝向为前方，从人物区域前方水平回望的独立机位，不是玩家当前镜头。" + contract, IllustrationReferenceKind.MissionScreenshot),
                            new IllustrationReferenceImage(Convert.ToBase64String(reverse), "截图B：同一冻结瞬间，从人物区域后方水平回望，与A朝向相差180度。与A是同一组人物、同一场景。" + contract, IllustrationReferenceKind.MissionScreenshot)
                        };
                        pair = null; current = null; reverse = null;
                        var plan = new IllustrationPromptPlan(battle ? MissionScreenshotRules.BattleMode : MissionScreenshotRules.ShoutMode, facts, contract, dialogue);
                        IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, scope)) SetStatus(mission, "两张截图已取得，导演正在推演…"); });
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
                if (!started) Finish(scope, capture, mission, "生图任务繁忙，请等待已有任务完成。");
            }
            catch (Exception ex) { Finish(scope, capture, mission, "生图失败：" + ex.Message); }
        }

        private static string CaptureFacts(Agent player, Agent target, bool battle)
        {
            var facts = new StringBuilder();
            facts.AppendLine(battle ? "战斗：固定双截图采集瞬间的动作，不能推演后续战果。" : "场景喊话：已发生对白动作优先，截图提供现场关系。");
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
            capture.Cancel("双截图采集已结束。");
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
