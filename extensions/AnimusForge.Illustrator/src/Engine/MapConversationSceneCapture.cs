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
            // MapConversationMission can publish its Tableau one or more frames after the
            // view is created. Keep this mutable so a source captured at conversation open
            // can bind the real Tableau before the first image generation.
            private MapConversationTableau _tableau;

            internal bool IsMapConversation => _mapView != null;
            internal bool HasMapTableau => _tableau != null;
            internal Mission Mission => _mission;
            internal MissionScreen MissionScreen => _screen as MissionScreen;
            // The Tableau is published a few frames after the view, so it is not part of the key:
            // a capture taken when the player opens the illustration must match the later
            // generation. EnsureCurrent still rejects a replaced Tableau.
            internal string SessionOwnerKey => IsMapConversation
                ? "map:" + (_mapView?.GetHashCode() ?? 0) + ":" + (_mapMission?.GetHashCode() ?? 0)
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
                if (IsMapConversation && _tableau == null && _mapMission != null)
                    _tableau = _mapMission.ConversationTableau;
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

        // Field conversations send no screenshot: identity comes from the offscreen portraits,
        // and the tableau's staged close-up is neither the real terrain nor the real stances.
        internal const string MapConversationTerrainNote =
            "本次是野外地图对话，不提供现场截图：人物外观与穿戴以离屏身份立绘为准，环境依据【地貌类型】【当前季节】【现场天气】等地形事实设计。" +
            "按该地貌与季节自主构思可信的野外环境——地形起伏、植被、岩石、水体与天空层次——作为依据地形事实的艺术再现，不指称具体地名或未确认的建筑、营地与人物。" +
            "双方站位、距离、朝向与动作依对话中已发生的叙事决定。";

        // No screen pixels, private Scene or Camera are read on this route.
        private static Task<ConversationSceneReferenceCapture> CaptureMapConversationSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            GenerationDiagnostics.Current?.RecordStage("scene_capture_end", new JObject
            {
                ["route"] = "map-conversation", ["coverage"] = "terrain-facts-only",
                ["environmentReferences"] = 0, ["sourceMissionViews"] = 0,
                ["referenceSheet"] = false, ["panorama"] = false, ["totalMs"] = 0
            });
            return Task.FromResult(new ConversationSceneReferenceCapture(Array.Empty<IllustrationReferenceImage>(),
                MapConversationTerrainNote, "野外对话：按地形事实构图（不截图）"));
        }
    }
}
