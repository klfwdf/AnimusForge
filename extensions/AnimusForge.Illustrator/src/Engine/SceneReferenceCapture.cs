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

        // Six cameras render a private, frozen environment copy, never Mission.Scene.
        // Presented pixels are an additional lighting/person-position check, not a substitute panorama.
        internal static async Task<IReadOnlyList<IllustrationReferenceImage>> CaptureConversationSceneReferencesAsync(CancellationToken token)
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
            var rawFaces = new List<byte[]>(6);
            bool composed = false;
            PanoramaFrameStats frameStats = null;
            try
            {
                await _stageLock.WaitAsync(captureToken).ConfigureAwait(false);
                stageHeld = true;
                var context = await RunOnGameThreadAsync(() =>
                {
                    var mission = Mission.Current;
                    var screen = ScreenManager.TopScreen as MissionScreen;
                    if (mission?.Scene == null || screen?.CombatCamera == null || !ReferenceEquals(screen.Mission, mission) || !screen.MissionStartedRendering())
                        throw new InvalidOperationException("当前没有可采集全景的已加载场景，请进入实际场景后重试。");
                    frameStats = new PanoramaFrameStats();
                    _activePanoramaFrameStats = frameStats;
                    return Tuple.Create(mission, PanoramaProjection.BuildCameraFrames(screen.CombatCamera.Frame), CaptureUnobstructedConversationSceneBase64());
                }, captureToken).ConfigureAwait(false);
                if (context == null) throw new InvalidOperationException("无法调度全景采集。");
                snapshot = await CreatePanoramaSnapshotAsync(context.Item1, captureToken).ConfigureAwait(false);
                GenerationDiagnostics.Current?.RecordStage("panorama_snapshot_ready", new JObject { ["meshes"] = snapshot.CopiedRoots,
                    ["nodes"] = snapshot.InspectedNodes, ["omittedNonGeometry"] = snapshot.SkippedNonGeometry, ["elapsedMs"] = watch.ElapsedMilliseconds });
                renderer = await RunOnGameThreadAsync(() =>
                {
                    var created = IsolatedPanoramaRenderer.Create(snapshot, context.Item2, 512);
                    _activeIsolatedPanorama = created;
                    _pendingPanoramaSnapshot = null;
                    return created;
                }, captureToken).ConfigureAwait(false);
                if (renderer == null) throw new InvalidOperationException("全景渲染器未建立。");
                GenerationDiagnostics.Current?.RecordStage("panorama_renderer_ready");
                for (int face = 0; face < 6; face++)
                {
                    var faceWatch = Stopwatch.StartNew();
                    GenerationDiagnostics.Current?.RecordStage("panorama_face_start", new JObject { ["face"] = face });
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
                byte[] panorama = PanoramaProjection.Compose(rawFaces);
                token.ThrowIfCancellationRequested();
                var references = new List<IllustrationReferenceImage>
                {
                    new IllustrationReferenceImage(Convert.ToBase64String(panorama),
                        "当前场景实际预制体的360×180度环境全景，来自同一点六个离屏镜头的投影拼接，不是六个房间。" +
                        "全景中央是采集起始前方，左右连接同一空间；用于辨认建筑网格、门窗楼梯、装饰图案和陈设摆放。" +
                        "成图选择导演指定的单一自然机位，不照搬全景展开或球面拉伸。" + snapshot.Notes, IllustrationReferenceKind.ScenePanorama)
                };
                references.AddRange(CreateCapturedSceneReferences(context.Item3));
                composed = true;
                GenerationDiagnostics.Current?.RecordStage("panorama_composed", new JObject { ["faces"] = 6, ["width"] = 2048, ["height"] = 1024,
                    ["copiedRoots"] = snapshot.CopiedRoots, ["skippedAnimated"] = snapshot.SkippedAnimated, ["terrainOmitted"] = snapshot.TerrainOmitted,
                    ["notes"] = snapshot.Notes, ["totalMs"] = watch.ElapsedMilliseconds, ["sourceRoots"] = snapshot.SourceRoots,
                    ["inspectedNodes"] = snapshot.InspectedNodes, ["copyBatches"] = snapshot.Batches,
                    ["copyTotalMs"] = snapshot.TotalMilliseconds, ["copyMaxBatchMs"] = snapshot.MaxBatchMilliseconds });
                TaleWorlds.Library.Debug.Print($"[Illustrator] Isolated prefab panorama composed: faces=6, entities={snapshot.CopiedRoots}, calibration={references.Count > 1}, sourceMissionViews=0");
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
                    GenerationDiagnostics.Current?.RecordStage("scene_capture_end", new JObject { ["faces"] = rawFaces.Count,
                        ["totalMs"] = watch.ElapsedMilliseconds, ["sourceMissionViews"] = 0, ["panorama"] = composed,
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
