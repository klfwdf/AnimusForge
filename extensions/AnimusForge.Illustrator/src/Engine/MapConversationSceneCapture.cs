using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using SandBox.View.Map;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        // Capture the actual UI/conversation owner before leaving the game thread. A MapScreen
        // can stay open while its conversation ends or changes, so screen identity alone is insufficient.
        internal sealed class ConversationSceneCaptureSource
        {
            private readonly ScreenBase _screen;
            private readonly Mission _mission;
            private readonly MapConversationView _mapView;
            private readonly MapConversationView.MapConversationMission _mapMission;
            private readonly MapConversationTableau _tableau;

            internal bool IsMapConversation => _mapView != null;
            internal bool HasMapTableau => _tableau != null;
            internal Mission Mission => _mission;
            internal MissionScreen MissionScreen => _screen as MissionScreen;
            internal string SessionOwnerKey => IsMapConversation
                ? "map:" + (_mapView?.GetHashCode() ?? 0) + ":" + (_mapMission?.GetHashCode() ?? 0) + ":" + (_tableau?.GetHashCode() ?? 0)
                : "mission:" + (_mission?.GetHashCode() ?? 0) + ":" + (_mission?.Scene?.GetHashCode() ?? 0);

            internal ConversationSceneCaptureSource(MissionScreen screen, Mission mission)
            { _screen = screen; _mission = mission; }

            internal ConversationSceneCaptureSource(MapScreen screen, MapConversationView view)
            {
                _screen = screen;
                _mapView = view;
                _mapMission = view.ConversationMission;
                _tableau = _mapMission.ConversationTableau;
            }

            internal void EnsureCurrent(CancellationToken token)
            {
                IllustratorRuntime.AssertMainThread();
                token.ThrowIfCancellationRequested();
                bool current = _screen != null && !_screen.IsFinalized && ReferenceEquals(ScreenManager.TopScreen, _screen);
                if (IsMapConversation)
                    current = current && _mapView.IsConversationActive &&
                        ReferenceEquals(((MapScreen)_screen).GetMapView<MapConversationView>(), _mapView) &&
                        ReferenceEquals(_mapView.ConversationMission, _mapMission) &&
                        (_tableau == null || ReferenceEquals(_mapMission.ConversationTableau, _tableau));
                else
                    current = current && _mission != null && ReferenceEquals(Mission.Current, _mission) &&
                        ReferenceEquals(MissionScreen?.Mission, _mission) &&
                        _mission.CurrentState != Mission.State.EndingNextFrame && _mission.CurrentState != Mission.State.Over;
                if (!current)
                    throw new InvalidOperationException("本次插画对应的对话或场景已结束、切换，请重新打开场景插画。");
            }

            internal async Task EnsureCurrentAsync(CancellationToken token)
            {
                bool checkedOwner = await RunOnGameThreadAsync(() => { EnsureCurrent(token); return true; }, token).ConfigureAwait(false);
                if (!checkedOwner) throw new InvalidOperationException("场景上下文校验已停止，请重新打开场景插画。");
            }
        }

        internal sealed class ConversationSceneReferenceCapture
        {
            internal IReadOnlyList<IllustrationReferenceImage> References { get; }
            internal string DirectorNote { get; }
            internal string StatusText { get; }
            internal string NearbyPropFacts { get; }

            internal ConversationSceneReferenceCapture(IReadOnlyList<IllustrationReferenceImage> references, string directorNote, string statusText, string nearbyPropFacts = "")
            { References = references; DirectorNote = directorNote; StatusText = statusText; NearbyPropFacts = nearbyPropFacts; }
        }

        internal static ConversationSceneCaptureSource GetConversationSceneCaptureSource()
        {
            IllustratorRuntime.AssertMainThread();
            var screen = ScreenManager.TopScreen;
            if (screen is MissionScreen missionScreen && Mission.Current != null && ReferenceEquals(missionScreen.Mission, Mission.Current))
                return new ConversationSceneCaptureSource(missionScreen, Mission.Current);
            if (screen is MapScreen mapScreen)
            {
                var view = mapScreen.GetMapView<MapConversationView>();
                if (view?.IsConversationActive == true && view.ConversationMission != null)
                    return new ConversationSceneCaptureSource(mapScreen, view);
            }
            throw new InvalidOperationException("当前没有正在显示的任务场景或地图对话，请在会话中重新打开场景插画。");
        }

        private static string MapConversationReferenceNote(bool captured) =>
            "本次采用地图对话独立管线，不是已进入的Mission实景。" +
            (captured
                ? "环境参考只是一张当前地图对话画面的真实截图，显示原生对话Tableau布景的可见部分；不是前后双镜头、30米环境重建、完整战斗地形或全景。保留可见地貌、植被、材质、颜色及空间关系；画外区域没有图像证据。"
                : "本次未取得可用的当前对话环境截图，没有提供环境参考图；只能依据已给出的环境事实和人物参考设计画面，未知的具体地形、陈设与人物现场位置不能声称已经观测。") +
            "对话展示人物的屏幕位置和镜头距离不是双方在野外的实际站位，不从单人特写推断双方距离、高低或朝向。";

        // Read presented pixels only: never access or mutate the tableau's private Scene/Camera,
        // acquire its cached scene, create a second view over it, or hide visible UI.
        private static async Task<ConversationSceneReferenceCapture> CaptureMapConversationSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token, string preCapturedScene = null)
        {
            await SceneCaptureLock.WaitAsync(token).ConfigureAwait(false);
            var watch = Stopwatch.StartNew();
            bool captured = false;
            try
            {
                var result = await RunOnGameThreadAsync(() =>
                {
                    source.EnsureCurrent(token);
                    string image = !string.IsNullOrWhiteSpace(preCapturedScene)
                        ? preCapturedScene
                        : source.HasMapTableau ? CaptureUnobstructedConversationSceneBase64() : null;
                    source.EnsureCurrent(token);
                    captured = !string.IsNullOrWhiteSpace(image);
                    string note = MapConversationReferenceNote(captured);
                    IReadOnlyList<IllustrationReferenceImage> references = captured
                        ? new[] { new IllustrationReferenceImage(image, note +
                            "忽略对话UI、字幕、血条与名牌，不将界面内容画入作品。" + ScreenshotMaskReferenceNote, IllustrationReferenceKind.MapConversationScene) }
                        : Array.Empty<IllustrationReferenceImage>();
                    return new ConversationSceneReferenceCapture(references, note,
                        captured ? "地图对话：仅当前单视角环境参考" : "地图对话：环境截图不可用，使用已知事实与人物参考");
                }, token).ConfigureAwait(false);
                if (result == null) throw new InvalidOperationException("地图对话参考采集调度已停止。");
                return result;
            }
            finally
            {
                SceneCaptureLock.Release();
                GenerationDiagnostics.Current?.RecordStage("scene_capture_end", new JObject
                {
                    ["route"] = "map-conversation", ["coverage"] = captured ? "presented-single-view" : "unavailable",
                    ["environmentReferences"] = captured ? 1 : 0, ["sourceMissionViews"] = 0,
                    ["referenceSheet"] = false, ["panorama"] = false, ["totalMs"] = watch.ElapsedMilliseconds
                });
            }
        }
    }
}
