using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using Newtonsoft.Json.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        private static IsolatedPanoramaRenderer _activeIsolatedPanorama;
        private static PanoramaFrameStats _activePanoramaFrameStats;
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
            "当前玩家视角的单张真实现场画面，不是全景或多个方向。保留可见的墙面材质与配色、门窗楼梯和人物高低关系；" +
            "只对已看见的环境作描述，画面外及被遮挡区域保持未知，不按地点名称重造建筑。" +
            "忽略游戏对话UI、字幕、血条与名牌，不将界面内容画入作品。" + ScreenshotMaskReferenceNote;

        internal static IReadOnlyList<IllustrationReferenceImage> CreateCapturedSceneReferences(string image)
        {
            if (string.IsNullOrWhiteSpace(image)) return Array.Empty<IllustrationReferenceImage>();
            return new[] { new IllustrationReferenceImage(image, SceneReferenceLabel(), IllustrationReferenceKind.Scene) };
        }

        internal static async Task<ConversationSceneReferenceCapture> CaptureConversationSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            await source.EnsureCurrentAsync(token).ConfigureAwait(false);
            GenerationDiagnostics.Current?.RecordStage("scene_capture_route", new JObject
            { ["route"] = source.IsMapConversation ? "map-conversation" : "mission-front-back-30m" });
            if (source.IsMapConversation)
                return await CaptureMapConversationSceneReferencesAsync(source, token).ConfigureAwait(false);
            var references = await CaptureMissionSceneReferencesAsync(source, token).ConfigureAwait(false);
            return new ConversationSceneReferenceCapture(references,
                "本次依据已加载Mission的玩家附近30米静态网格前后双镜头参考；这是局部环境参考，不是完整360度全景。真实当前画面如有附加，用于校验光照与人物关系。",
                "任务场景：附近30米前后双镜头参考");
        }

        // Front/back cameras render a private nearby environment copy, never Mission.Scene.
        // Presented pixels are an additional lighting/person-position check.
        private static async Task<IReadOnlyList<IllustrationReferenceImage>> CaptureMissionSceneReferencesAsync(
            ConversationSceneCaptureSource source, CancellationToken token)
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
            var rawFaces = new List<byte[]>(PanoramaProjection.FrontBackViewCount);
            bool composed = false;
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
                    return Tuple.Create(mission, PanoramaProjection.BuildFrontBackCameraFrames(screen.CombatCamera.Frame), CaptureUnobstructedConversationSceneBase64());
                }, captureToken).ConfigureAwait(false);
                if (context == null) throw new InvalidOperationException("无法调度全景采集。");
                snapshot = await CreatePanoramaSnapshotAsync(context.Item1, captureToken).ConfigureAwait(false);
                GenerationDiagnostics.Current?.RecordStage("panorama_snapshot_ready", new JObject { ["meshes"] = snapshot.CopiedRoots,
                    ["resourceMeshes"] = snapshot.ResourceCopiedComponents,
                    ["nodes"] = snapshot.InspectedNodes, ["omittedNonGeometry"] = snapshot.SkippedNonGeometry, ["elapsedMs"] = watch.ElapsedMilliseconds });
                renderer = await RunOnGameThreadAsync(() =>
                {
                    var created = IsolatedPanoramaRenderer.Create(snapshot, context.Item2, 512, (float)Math.PI * 2f / 3f);
                    _activeIsolatedPanorama = created;
                    _pendingPanoramaSnapshot = null;
                    return created;
                }, captureToken).ConfigureAwait(false);
                if (renderer == null) throw new InvalidOperationException("全景渲染器未建立。");
                GenerationDiagnostics.Current?.RecordStage("panorama_observation_lighting", renderer.ObservationLightingDiagnostics);
                GenerationDiagnostics.Current?.RecordStage("panorama_renderer_ready");
                for (int face = 0; face < PanoramaProjection.FrontBackViewCount; face++)
                {
                    var faceWatch = Stopwatch.StartNew();
                    GenerationDiagnostics.Current?.RecordStage("panorama_face_start", new JObject { ["face"] = face, ["direction"] = face == 0 ? "front" : "back" });
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
                byte[] panorama = PanoramaProjection.ComposeFrontBack(rawFaces);
                token.ThrowIfCancellationRequested();
                var references = new List<IllustrationReferenceImage>
                {
                    new IllustrationReferenceImage(Convert.ToBase64String(panorama),
                        "当前场景实际预制体的前后双视角参考：左图FRONT是采集起始前方，右图BACK是同一点转180度后的后方，每张水平视野120度。" +
                        "两张属于同一空间但并不相邻，不把中间接缝拼成真实墙面；侧面及其他未入镜区域保持未知，不是完整360度全景。" +
                        "用于辨认建筑网格、门窗楼梯、装饰图案和陈设摆放。方向栏不进入作品，最终只画导演指定的单幅自然机位。" + snapshot.Notes, IllustrationReferenceKind.SceneViews)
                };
                references.AddRange(CreateCapturedSceneReferences(context.Item3));
                composed = true;
                GenerationDiagnostics.Current?.RecordStage("panorama_composed", new JObject { ["faces"] = 2, ["layout"] = "front-back-sheet", ["width"] = 1024, ["height"] = 544,
                    ["radiusMeters"] = PanoramaCaptureRadius, ["centerFromPlayer"] = snapshot.CenterFromPlayer,
                    ["excludedByRadius"] = snapshot.SkippedByRadius, ["invalidBounds"] = snapshot.SkippedInvalidBounds,
                    ["copiedRoots"] = snapshot.CopiedRoots, ["resourceMeshes"] = snapshot.ResourceCopiedComponents,
                    ["clearColorRgb"] = "#404040", ["skippedAnimated"] = snapshot.SkippedAnimated, ["terrainOmitted"] = snapshot.TerrainOmitted,
                    ["notes"] = snapshot.Notes, ["totalMs"] = watch.ElapsedMilliseconds, ["sourceRoots"] = snapshot.SourceRoots,
                    ["inspectedNodes"] = snapshot.InspectedNodes, ["copyBatches"] = snapshot.Batches,
                    ["copyTotalMs"] = snapshot.TotalMilliseconds, ["copyMaxBatchMs"] = snapshot.MaxBatchMilliseconds });
                TaleWorlds.Library.Debug.Print($"[Illustrator] Nearby prefab front/back reference composed: views=2, radius=30m, meshes={snapshot.CopiedRoots}, calibration={references.Count > 1}, sourceMissionViews=0");
                return references;
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
                    GenerationDiagnostics.Current?.RecordStage("scene_capture_end", new JObject { ["route"] = "mission-front-back-30m", ["faces"] = rawFaces.Count,
                        ["totalMs"] = watch.ElapsedMilliseconds, ["sourceMissionViews"] = 0, ["referenceSheet"] = composed, ["panorama"] = false,
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
            try { _activeIsolatedPanorama?.Restore(); }
            finally { _activeIsolatedPanorama = null; _activePanoramaFrameStats = null; _pendingPanoramaSnapshot?.DisposeUnrendered(); _pendingPanoramaSnapshot = null; }
        }
    }
}
