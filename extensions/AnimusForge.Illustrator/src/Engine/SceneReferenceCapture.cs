using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        private static IsolatedPanoramaRenderer _activeIsolatedPanorama;
        private static PanoramaFrameStats _activePanoramaFrameStats;
        private static readonly string[] PanoramaDirections = { "front", "right", "back", "left", "up", "down" };
        private sealed class PanoramaFrameStats
        {
            internal int Count;
            internal double TotalMs, MaxMs;
        }

        internal static void ObservePanoramaFrame(float dt)
        {
            var stats = _activePanoramaFrameStats;
            if (stats == null || dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            double milliseconds = dt * 1000d;
            stats.Count++;
            stats.TotalMs += milliseconds;
            stats.MaxMs = Math.Max(stats.MaxMs, milliseconds);
        }
        internal static string SceneReferenceLabel() =>
            "当前玩家视角的单张真实现场画面，不是全景或多个方向。仅用于判断当前位置与环境定位，保留可见的墙面材质与配色、门窗楼梯和环境高低关系；" +
            "只对已看见的环境作描述，画面外及被遮挡区域保持未知，不按地点名称重造建筑。" +
            "人物行动优先采用最近2条对话中的已发生叙事，不采用截图里的站姿、手势、朝向或视线；人物身份细节仍按对应人物参考，不把界面头像或画廊作品当作现场人物。" +
            "忽略游戏对话UI、字幕、血条与名牌，不将界面内容画入作品。" + ScreenshotMaskReferenceNote;

        internal static IReadOnlyList<IllustrationReferenceImage> CreateCapturedSceneReferences(string image)
        {
            if (string.IsNullOrWhiteSpace(image)) return Array.Empty<IllustrationReferenceImage>();
            return new[] { new IllustrationReferenceImage(image, SceneReferenceLabel(), IllustrationReferenceKind.Scene) };
        }

        internal static async Task<ConversationSceneReferenceCapture> CaptureConversationSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token, string preCapturedScene = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            await source.EnsureCurrentAsync(token).ConfigureAwait(false);
            GenerationDiagnostics.Current?.RecordStage("scene_capture_route", new JObject
            { ["route"] = source.IsMapConversation ? "map-conversation" : "mission-panorama-30m" });
            if (source.IsMapConversation)
                return await CaptureMapConversationSceneReferencesAsync(source, token, preCapturedScene).ConfigureAwait(false);
            return await CaptureMissionSceneReferencesAsync(source, token, preCapturedScene).ConfigureAwait(false);
        }

        // Six cubemap directions render one private copy, never Mission.Scene.
        // Presented pixels are an additional lighting/person-position check.
        private static async Task<ConversationSceneReferenceCapture> CaptureMissionSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token, string preCapturedScene = null)
        {
            await SceneCaptureLock.WaitAsync(token).ConfigureAwait(false);
            var watch = Stopwatch.StartNew();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(25));
            var captureToken = deadline.Token;
            bool stageHeld = false;
            PanoramaSceneSnapshot snapshot = null;
            IsolatedPanoramaRenderer renderer = null;
            var exportedPaths = new List<string>();
            var rawFaces = new List<byte[]>(PanoramaProjection.FaceCount);
            bool composed = false;
            string calibrationReason = "not_attempted";
            PanoramaFrameStats frameStats = null;
            try
            {
                await _stageLock.WaitAsync(captureToken).ConfigureAwait(false);
                stageHeld = true;
                var context = await RunOnGameThreadAsync(() =>
                {
                    source.EnsureCurrent(captureToken);
                    var mission = source.Mission;
                    var screen = source.MissionScreen;
                    if (mission?.Scene == null || screen?.CombatCamera == null || !ReferenceEquals(screen.Mission, mission) || !screen.MissionStartedRendering())
                        throw new InvalidOperationException("当前任务场景尚未完成渲染，暂时无法采集附近环境，请稍后重试。");
                    frameStats = new PanoramaFrameStats();
                    _activePanoramaFrameStats = frameStats;
                    MatrixFrame sourceFrame = screen.CombatCamera.Frame;
                    Vec3 combatCameraOrigin = sourceFrame.origin;
                    string cameraOriginSource = "combat_camera";
                    Vec3 cameraOrigin = sourceFrame.origin;
                    var player = mission.MainAgent;
                    if (player != null && player.IsActive())
                    {
                        Vec3 eye = player.GetEyeGlobalPosition();
                        if (!float.IsNaN(eye.x) && !float.IsNaN(eye.y) && !float.IsNaN(eye.z) &&
                            !float.IsInfinity(eye.x) && !float.IsInfinity(eye.y) && !float.IsInfinity(eye.z))
                        {
                            // Keep the live camera's orientation, but put every panorama face
                            // at the player's eye. This is a zero-offset viewpoint, not a zero-
                            // meter geometry selection: the surrounding static copy remains 30 m.
                            sourceFrame.origin = eye;
                            cameraOrigin = eye;
                            cameraOriginSource = "player_eye";
                        }
                    }
                    GenerationDiagnostics.Current?.RecordStage("panorama_camera_origin", new JObject
                    {
                        ["source"] = cameraOriginSource,
                        ["origin"] = new JArray(cameraOrigin.x, cameraOrigin.y, cameraOrigin.z),
                        ["combatCameraOrigin"] = new JArray(combatCameraOrigin.x, combatCameraOrigin.y, combatCameraOrigin.z),
                        ["playerPosition"] = player == null ? null : new JArray(player.Position.x, player.Position.y, player.Position.z),
                        ["distanceFromPlayerBaseMeters"] = player == null ? (double?)null : player.Position.Distance(cameraOrigin),
                        ["addedEyeOffsetMeters"] = cameraOriginSource == "player_eye" ? (double?)0 : null,
                        ["selectionRadiusMeters"] = PanoramaCaptureRadius
                    });
                    string calibration = string.IsNullOrWhiteSpace(preCapturedScene)
                        ? CaptureUnobstructedConversationSceneBase64(out calibrationReason)
                        : preCapturedScene;
                    if (!string.IsNullOrWhiteSpace(preCapturedScene)) calibrationReason = "pre_captured_before_overlay";
                    return Tuple.Create(mission, PanoramaProjection.BuildCameraFrames(sourceFrame), calibration, cameraOriginSource);
                }, captureToken).ConfigureAwait(false);
                if (context == null) throw new InvalidOperationException("无法调度全景采集。");
                GenerationDiagnostics.Current?.RecordStage("scene_calibration", new JObject
                { ["attempt"] = 1, ["reason"] = calibrationReason, ["available"] = !string.IsNullOrWhiteSpace(context.Item3) });
                snapshot = await CreatePanoramaSnapshotAsync(context.Item1, captureToken).ConfigureAwait(false);
                GenerationDiagnostics.Current?.RecordStage("panorama_snapshot_ready", new JObject { ["meshes"] = snapshot.CopiedRoots,
                    ["resourceMeshes"] = snapshot.ResourceCopiedComponents,
                    ["nodes"] = snapshot.InspectedNodes, ["omittedNonGeometry"] = snapshot.SkippedNonGeometry, ["elapsedMs"] = watch.ElapsedMilliseconds });
                renderer = await RunOnGameThreadAsync(() =>
                {
                    var created = IsolatedPanoramaRenderer.Create(snapshot, context.Item2, 512, (float)Math.PI / 2f);
                    _activeIsolatedPanorama = created;
                    _pendingPanoramaSnapshot = null;
                    return created;
                }, captureToken).ConfigureAwait(false);
                if (renderer == null) throw new InvalidOperationException("全景渲染器未建立。");
                GenerationDiagnostics.Current?.RecordStage("panorama_observation_lighting", renderer.ObservationLightingDiagnostics);
                GenerationDiagnostics.Current?.RecordStage("panorama_camera_coverage", renderer.CameraDiagnostics);
                GenerationDiagnostics.Current?.RecordStage("panorama_renderer_ready");
                for (int face = 0; face < PanoramaProjection.FaceCount; face++)
                {
                    var faceWatch = Stopwatch.StartNew();
                    GenerationDiagnostics.Current?.RecordStage("panorama_face_start", new JObject { ["face"] = face, ["direction"] = PanoramaDirections[face] });
                    bool selected = await RunOnGameThreadAsync(() => renderer.SelectFace(face), captureToken).ConfigureAwait(false);
                    if (!selected) throw new InvalidOperationException("全景镜头切换被未完成的导出阻止。");
                    bool ready = false;
                    for (int attempt = 0; attempt < 90 && !ready; attempt++)
                    {
                        var frames = await RunOnGameThreadAsync(() => IllustratorRuntime.AfterFramesAsync(2, captureToken), captureToken).ConfigureAwait(false);
                        if (frames == null) throw new InvalidOperationException("无法等待全景渲染帧。");
                        await frames.ConfigureAwait(false);
                        ready = await RunOnGameThreadAsync(() => renderer.IsReady, captureToken).ConfigureAwait(false);
                    }
                    if (!ready) throw new InvalidOperationException("全景镜头未完成渲染，没有发送不完整参考图。");
                    string path = await RunOnGameThreadAsync(() => renderer.RequestExport(face), captureToken).ConfigureAwait(false);
                    if (path == null) throw new InvalidOperationException("全景导出未就绪。");
                    exportedPaths.Add(path);
                    byte[] bytes = await ReadPanoramaFaceAsync(path, captureToken).ConfigureAwait(false);
                    if (bytes == null) throw new InvalidOperationException("全景导出未取得完整图片，没有用重复视角代替。");
                    await RunOnGameThreadAsync(() => { renderer.StopExport(); return true; }, captureToken).ConfigureAwait(false);
                    rawFaces.Add(bytes);
                    GenerationDiagnostics.Current?.RecordPanoramaFace(face, bytes);
                    GenerationDiagnostics.Current?.RecordStage("panorama_face", new JObject { ["face"] = face, ["bytes"] = bytes.Length, ["captureMs"] = faceWatch.ElapsedMilliseconds });
                }
                // Rendering is stopped by StopExport. Projection and encoding run on this background worker.
                captureToken.ThrowIfCancellationRequested();
                byte[] panorama = PanoramaProjection.ComposeWithCancellation(rawFaces, 2048, 1024, captureToken);
                captureToken.ThrowIfCancellationRequested();
                string currentScene = context.Item3;
                if (string.IsNullOrWhiteSpace(currentScene))
                {
                    // One bounded retry within the existing deadline, without touching UI/camera.
                    currentScene = await RunOnGameThreadAsync(() =>
                    {
                        source.EnsureCurrent(captureToken);
                        return CaptureUnobstructedConversationSceneBase64(out calibrationReason);
                    }, captureToken).ConfigureAwait(false);
                    GenerationDiagnostics.Current?.RecordStage("scene_calibration", new JObject
                    { ["attempt"] = 2, ["reason"] = calibrationReason, ["available"] = !string.IsNullOrWhiteSpace(currentScene) });
                }
                string calibrationNote = string.IsNullOrWhiteSpace(currentScene)
                    ? "本次未取得真实当前画面；当前位置依据已有文字事实，不补造未知位置。人物动作优先采用对话中的已发生叙事，不能把本副本的补光或无人状态当作现场。"
                    : "附加的当前截图仅用于判断当前位置与环境定位，人物动作优先采用对话中的已发生叙事。";
                var references = new List<IllustrationReferenceImage>
                {
                    new IllustrationReferenceImage(Convert.ToBase64String(panorama),
                        "当前场景独立静态副本的360×180度环境全景：同一点前后左右上下六个90度镜头，经球面映射合成一张展开图，不是六个房间或六张拼贴。" +
                        "中央是采集起始前方，左右边缘在后方相接，顶部为上方、底部为下方；展开边缘与两极的拉伸不是建筑变形。" +
                        "这张图只约束场景环境：用于辨认建筑、家具、门窗楼梯、材质与摆放；不得从全景推断人物的脸、发型、年龄、服装、姿态或人数。人物以对应身份参考图和事实为准。" +
                        "最终依导演选定机位绘制单幅正常透视作品，不照搬全景展开布局。" + snapshot.Notes + calibrationNote, IllustrationReferenceKind.ScenePanorama)
                };
                references.AddRange(CreateCapturedSceneReferences(currentScene));
                composed = true;
                GenerationDiagnostics.Current?.RecordStage("panorama_composed", new JObject { ["faces"] = PanoramaProjection.FaceCount,
                    ["layout"] = "equirectangular", ["width"] = 2048, ["height"] = 1024, ["horizontalDegrees"] = 360, ["verticalDegrees"] = 180,
                    ["radiusMeters"] = PanoramaCaptureRadius, ["centerFromPlayer"] = snapshot.CenterFromPlayer,
                    ["excludedByRadius"] = snapshot.SkippedByRadius, ["invalidBounds"] = snapshot.SkippedInvalidBounds,
                    ["copiedRoots"] = snapshot.CopiedRoots, ["resourceMeshes"] = snapshot.ResourceCopiedComponents,
                    ["clearColorRgb"] = "#404040", ["skippedAnimated"] = snapshot.SkippedAnimated, ["terrainOmitted"] = snapshot.TerrainOmitted,
                    ["notes"] = snapshot.Notes, ["totalMs"] = watch.ElapsedMilliseconds, ["sourceRoots"] = snapshot.SourceRoots,
                    ["inspectedNodes"] = snapshot.InspectedNodes, ["copyBatches"] = snapshot.Batches,
                    ["copyTotalMs"] = snapshot.TotalMilliseconds, ["copyMaxBatchMs"] = snapshot.MaxBatchMilliseconds,
                    ["cameraOriginSource"] = context.Item4 });
                TaleWorlds.Library.Debug.Print($"[Illustrator] Nearby prefab panorama composed: views=6, coverage=360x180, radius=30m, meshes={snapshot.CopiedRoots}, calibration={references.Count > 1}, sourceMissionViews=0");
                return new ConversationSceneReferenceCapture(references,
                    (context.Item4 == "player_eye" ? "本次以玩家眼位为零附加偏移视点。" : "未取得有效玩家眼位，本次保留当前相机位置并已记录回退。") +
                    "依据附近30米的独立静态环境副本，采集六个90度方向后投影为360度水平、180度垂直的全景。全方向视野不代表所有几何已覆盖；全景只约束环境，真实当前截图如有附加，仅用于当前位置与环境定位。",
                    "任务场景：附近30米全景参考可用" + (string.IsNullOrWhiteSpace(currentScene) ? "；当前位置截图未取得" : string.Empty), snapshot.NearbyPropFacts);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            { throw new TimeoutException("环境全景采集超过时间预算，已停止；没有发送不完整环境图。"); }
            finally
            {
                try
                {
                    if (renderer != null)
                    {
                        if (!renderer.Retired.IsCompleted) IllustratorRuntime.PostCritical(() =>
                        {
                            try { renderer.Restore(); }
                            finally
                            {
                                if (ReferenceEquals(_activeIsolatedPanorama, renderer)) _activeIsolatedPanorama = null;
                                if (ReferenceEquals(_activePanoramaFrameStats, frameStats)) _activePanoramaFrameStats = null;
                            }
                        });
                        try { await renderer.Retired.ConfigureAwait(false); }
                        catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Panorama retirement: " + ex.Message); }
                    }
                    else if (snapshot != null)
                    {
                        if (!snapshot.Retirement.IsCompleted) IllustratorRuntime.PostCritical(snapshot.DisposeUnrendered);
                        await snapshot.Retirement.ConfigureAwait(false);
                    }
                }
                finally
                {
                    foreach (string path in exportedPaths)
                        foreach (string candidate in new[] { path, path + ".png" })
                            try { File.Delete(candidate); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    if (stageHeld) _stageLock.Release();
                    SceneCaptureLock.Release();
                    if (ReferenceEquals(_activePanoramaFrameStats, frameStats)) _activePanoramaFrameStats = null;
                    GenerationDiagnostics.Current?.RecordStage("scene_capture_end", new JObject { ["route"] = "mission-panorama-30m", ["faces"] = rawFaces.Count,
                        ["totalMs"] = watch.ElapsedMilliseconds, ["sourceMissionViews"] = 0, ["referenceSheet"] = false, ["panorama"] = composed,
                        ["sampledFrames"] = frameStats?.Count ?? 0, ["maxApplicationFrameMs"] = frameStats?.MaxMs ?? 0,
                        ["meanApplicationFrameMs"] = frameStats == null || frameStats.Count == 0 ? 0 : frameStats.TotalMs / frameStats.Count });
                }
            }
        }

        private static async Task<byte[]> ReadPanoramaFaceAsync(string path, CancellationToken token)
        {
            long previousLength = -1;
            for (int attempt = 0; attempt < 35; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var file = new FileInfo(path);
                    if (!file.Exists) file = new FileInfo(path + ".png");
                    if (file.Exists && file.Length > 0)
                    {
                        if (file.Length > ImagePayload.MaxBytes) return null;
                        if (file.Length == previousLength) return ImagePayload.ReadFile(file.FullName);
                        previousLength = file.Length;
                    }
                }
                catch (IOException) { }
                catch (ArgumentException) { }
                await Task.Delay(80, token).ConfigureAwait(false);
            }
            return null;
        }

        internal static void CancelIsolatedPanorama()
        {
            IllustratorRuntime.AssertMainThread();
            PanoramaBatchPump.CancelActive();
            try { _activeIsolatedPanorama?.Restore(); }
            finally { _activeIsolatedPanorama = null; _activePanoramaFrameStats = null; _pendingPanoramaSnapshot?.DisposeUnrendered(); _pendingPanoramaSnapshot = null; }
        }
    }
}
