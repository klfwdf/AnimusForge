using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using SandBox.Conversation.MissionLogics;
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
            private bool? _requiresMissionPanorama;

            internal bool IsMapConversation => _mapView != null;
            internal bool RequiresMissionPanorama
            {
                get
                {
                    if (!_requiresMissionPanorama.HasValue)
                    {
                        IllustratorRuntime.AssertMainThread();
                        // Resolve once for capture; owner-key checks must not scan behaviors each tick.
                        _requiresMissionPanorama = _mission != null && _mission.GetMissionBehavior<ConversationMissionLogic>() == null;
                    }
                    return _requiresMissionPanorama.Value;
                }
            }
            internal string CaptureRoute => RequiresMissionPanorama ? "mission-panorama-30m"
                : IsMapConversation ? "map-conversation" : "mission-conversation-terrain";
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
            {
                _screen = screen;
                _mission = mission;
            }

            internal ConversationSceneCaptureSource(MapScreen screen, MapConversationView view)
            {
                _screen = screen;
                _requiresMissionPanorama = false;
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
                    throw new InvalidOperationException("The conversation or scene for this illustration ended or changed. Reopen the scene illustration.");
                _ = RequiresMissionPanorama;
            }

            internal async Task EnsureCurrentAsync(CancellationToken token)
            {
                bool checkedOwner = await RunOnGameThreadAsync(() => { EnsureCurrent(token); return true; }, token).ConfigureAwait(false);
                if (!checkedOwner) throw new InvalidOperationException("Scene context validation stopped. Reopen the scene illustration.");
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
            throw new InvalidOperationException("No mission scene or map conversation is currently displayed. Reopen the scene illustration during a conversation.");
        }

        // Map and presentation-Mission conversations send no screenshot: identity comes from the offscreen portraits,
        // and the tableau's staged close-up is neither the real terrain nor the real stances.
        internal static string MapConversationTerrainNote => IllustratorText.Get("MapConversation_TerrainNote");

        // No screen pixels, private Scene or Camera are read on this route.
        private static Task<ConversationSceneReferenceCapture> CaptureMapConversationSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            GenerationDiagnostics.Current?.RecordStage("scene_capture_end", new JObject
            {
                ["route"] = source.CaptureRoute, ["coverage"] = "terrain-facts-only",
                ["environmentReferences"] = 0, ["sourceMissionViews"] = 0,
                ["referenceSheet"] = false, ["panorama"] = false, ["totalMs"] = 0
            });
            return Task.FromResult(new ConversationSceneReferenceCapture(Array.Empty<IllustrationReferenceImage>(),
                MapConversationTerrainNote, IllustratorText.Get("MapConversation_TerrainTitle")));
        }
    }
}
