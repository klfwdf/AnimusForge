using System;
using System.IO;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using Path = System.IO.Path;

namespace AnimusForge.Illustrator.Engine
{
    // A requested capture owns this tableau, its cameras and its snapshot scene.
    // There is no Mission reference and no manual scene tick or texture CPU readback.
    internal sealed class IsolatedPanoramaRenderer
    {
        // Kept independent of native objects so cancellation/face/export ordering
        // can be exercised offline. The renderer serializes access with _gate.
        internal sealed class FaceSequence
        {
            private readonly int _count;
            internal int Face { get; private set; } = -1;
            internal int Generation { get; private set; }
            internal int PaintCount { get; private set; }
            internal int ExportGeneration { get; private set; } = -1;
            internal bool Closed { get; private set; }
            internal bool ExportPending => ExportGeneration >= 0;
            internal bool Ready => !Closed && Face >= 0 && PaintCount >= 3 && !ExportPending;

            internal FaceSequence(int count) { _count = count; }

            internal bool Select(int index)
            {
                if (Closed || ExportPending || index < 0 || index >= _count) return false;
                Face = index;
                Generation++;
                PaintCount = 0;
                return true;
            }

            internal void Painted(int generation)
            {
                if (!Closed && Face >= 0 && generation == Generation) PaintCount++;
            }

            internal bool BeginExport(int index)
            {
                if (!Ready || Face != index) return false;
                ExportGeneration = Generation;
                return true;
            }

            internal void EndExport(bool hasCompletedFile)
            {
                // A timeout/cancel cannot unlock the camera while a native save
                // may still refer to it. That session must be retired instead.
                if (hasCompletedFile) ExportGeneration = -1;
            }

            internal void Close()
            {
                Closed = true;
                Face = -1;
                PaintCount = 0;
                ExportGeneration = -1;
            }
        }

        private readonly object _gate = new object();
        private readonly PanoramaSceneSnapshot _snapshot;
        private readonly FaceSequence _sequence;
        private readonly Camera[] _cameras;
        private readonly string _directory;
        private readonly TaskCompletionSource<bool> _retired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Texture _texture;
        private TableauView _view;
        private bool _sceneTransferred;
        private bool _renderActive;
        private Exception _paintFailure;
        private string _exportPath;

        // This acknowledges submission of native retirement, not a GPU completion fence.
        internal Task Retired => _retired.Task;
        internal bool IsReady
        {
            get
            {
                IllustratorRuntime.AssertMainThread();
                lock (_gate)
                {
                    if (_paintFailure != null) throw new InvalidOperationException("Isolated panorama paint failed.", _paintFailure);
                    return _renderActive && _sequence.Ready;
                }
            }
        }

        private IsolatedPanoramaRenderer(PanoramaSceneSnapshot snapshot, int faceCount)
        {
            _snapshot = snapshot;
            _sequence = new FaceSequence(faceCount);
            _cameras = new Camera[faceCount];
            _directory = Path.Combine(Path.GetTempPath(), "AnimusForgeIllustrator", "panorama_" + Guid.NewGuid().ToString("N"));
        }

        internal static IsolatedPanoramaRenderer Create(PanoramaSceneSnapshot snapshot, MatrixFrame[] frames, int size)
        {
            IllustratorRuntime.AssertMainThread();
            if (snapshot == null || snapshot.Scene == null) throw new ArgumentNullException(nameof(snapshot));
            if (frames == null || frames.Length == 0 || frames.Length > 6) throw new ArgumentException("A panorama requires one to six camera frames.", nameof(frames));
            if (size < 256 || size > 1024) throw new ArgumentOutOfRangeException(nameof(size));
            var renderer = new IsolatedPanoramaRenderer(snapshot, frames.Length);
            try
            {
                Directory.CreateDirectory(renderer._directory);
                TaleWorlds.Library.Debug.Print("[IllustratorPanorama] Creating private cameras and vanilla tableau render target.");
                for (int i = 0; i < frames.Length; i++)
                {
                    var camera = Camera.CreateCamera();
                    renderer._cameras[i] = camera;
                    camera.SetFovHorizontal((float)Math.PI / 2f, 1f, 0.05f, 2000f);
                    camera.Frame = frames[i];
                }
                // Initialize the snapshot's postfx/shadow context before requesting
                // native final-pass dumps. Never change the live scene's settings.
                Scene scene = snapshot.Scene;
                scene.EnsurePostfxSystem();
                scene.SetDofMode(false);
                scene.SetMotionBlurMode(false);
                scene.SetBloom(false);
                scene.SetShadow(true);
                TaleWorlds.Library.Debug.Print("[IllustratorPanorama] Allocating isolated tableau (no copied scene light components).");
                renderer._texture = TableauView.AddTableau("AF_IsolatedPanorama_" + Guid.NewGuid().ToString("N"), renderer.Paint, scene, size, size);
                renderer._view = renderer._texture.TableauView;
                renderer._view.SetEnable(false);
                renderer._view.SetScene(scene);
                snapshot.MarkRendered();
                renderer._sceneTransferred = true;
                renderer._view.SetRenderWithPostfx(true);
                renderer._view.SetPostfxConfigParams(0);
                renderer._view.SetSceneUsesSkybox(false);
                renderer._view.SetSceneUsesShadows(true);
                renderer._view.SetShadowmapResolutionMultiplier(0.5f);
                renderer._view.SetSceneUsesContour(false);
                renderer._view.SetClearColor(0xff000000);
                renderer._view.SetDeleteAfterRendering(false);
                TaleWorlds.Library.Debug.Print("[IllustratorPanorama] Isolated tableau initialized.");
                return renderer;
            }
            catch
            {
                renderer.Restore();
                throw;
            }
        }

        internal bool SelectFace(int index)
        {
            IllustratorRuntime.AssertMainThread();
            lock (_gate)
            {
                if (_paintFailure != null || !_sequence.Select(index)) return false;
                _renderActive = true;
                TaleWorlds.Library.Debug.Print($"[IllustratorPanorama] Starting face={index}.");
                _view.SetSaveFinalResultToDisk(false);
                _view.SetCamera(_cameras[index]);
                _view.SetContinuousRendering(true);
                _view.SetEnable(true);
                return true;
            }
        }

        private void Paint(Texture sender, EventArgs args)
        {
            // Called by the native tableau scheduler. A short per-session gate
            // prevents a queued callback from observing released/changing resources.
            lock (_gate)
            {
                if (!_renderActive || _sequence.Closed || _sequence.Face < 0 || ReferenceEquals(sender, null) ||
                    ReferenceEquals(_texture, null) || sender.Pointer != _texture.Pointer) return;
                try
                {
                    int generation = _sequence.Generation;
                    var view = sender.TableauView;
                    view.SetScene(_snapshot.Scene);
                    // The engine may rebind render state between tableaus. Re-submit
                    // this face's immutable camera in every actual paint callback.
                    var camera = _cameras[_sequence.Face];
                    view.SetCamera(camera);
                    view.SetRenderWithPostfx(true);
                    view.SetPostfxConfigParams(0);
                    view.SetSceneUsesSkybox(false);
                    view.SetSceneUsesShadows(true);
                    // The native final-result dump also accesses the shadow pass.
                    // Match the verified character/banner export initialization on
                    // this owned view before counting the callback as ready.
                    Vec3 shadowCenter = camera.Frame.origin;
                    view.SetFocusedShadowmap(true, ref shadowCenter, 100f);
                    view.SetDeleteAfterRendering(false);
                    view.SetContinuousRendering(true);
                    _sequence.Painted(generation);
                }
                catch (Exception ex)
                {
                    // Do not let managed callback failures escape into the engine.
                    // The orchestrator observes this on its next IsReady check and
                    // retires the owned resources on the main thread.
                    _paintFailure = ex;
                    _renderActive = false;
                }
            }
        }

        internal string RequestExport(int index)
        {
            IllustratorRuntime.AssertMainThread();
            lock (_gate)
            {
                if (!_renderActive || _paintFailure != null || !_sequence.BeginExport(index)) return null;
                _exportPath = Path.Combine(_directory, "face_" + index + ".png");
                TaleWorlds.Library.Debug.Print($"[IllustratorPanorama] Exporting prepared face={index}.");
                _view.SetFilePathToSaveResult(_directory + Path.DirectorySeparatorChar);
                _view.SetFileNameToSaveResult(Path.GetFileName(_exportPath));
                _view.SetFileTypeToSave(View.TextureSaveFormat.TextureTypePng);
                _view.SetSaveFinalResultToDisk(true);
                return _exportPath;
            }
        }

        internal void StopExport()
        {
            IllustratorRuntime.AssertMainThread();
            lock (_gate)
            {
                if (_sequence.Closed) return;
                _renderActive = false;
                _view.SetSaveFinalResultToDisk(false);
                _view.SetContinuousRendering(false);
                _view.SetEnable(false);
                // The orchestrator must finish reading the stable PNG before this
                // call. Existence is an additional guard, not a GPU completion claim.
                _sequence.EndExport(HasNonEmptyFile(_exportPath) || HasNonEmptyFile(_exportPath + ".png"));
            }
        }

        private static bool HasNonEmptyFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try { return File.Exists(path) && new FileInfo(path).Length > 0; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        internal void Restore()
        {
            IllustratorRuntime.AssertMainThread();
            lock (_gate)
            {
                if (_sequence.Closed) return;
                _renderActive = false;
                _sequence.Close();
                Exception failure = null;
                try
                {
                    if (_view != null)
                    {
                        RetireStep(() => _view.SetSaveFinalResultToDisk(false), ref failure);
                        RetireStep(() => _view.SetContinuousRendering(false), ref failure);
                        RetireStep(() => _view.SetEnable(false), ref failure);
                        // MarkRendered transfers this isolated scene's ownership.
                        // The native clear queue, not a manual ClearAll, retires it.
                        bool queuedClear = false;
                        RetireStep(() =>
                        {
                            _view.AddClearTask(clearOnlySceneview: !_sceneTransferred);
                            queuedClear = true;
                        }, ref failure);
                        if (_sceneTransferred && queuedClear)
                            RetireStep(() => _snapshot.Scene.ManualInvalidate(), ref failure);
                    }
                    if (!_sceneTransferred) RetireStep(_snapshot.DisposeUnrendered, ref failure);
                }
                finally
                {
                    try
                    {
                        // PaintNeeded is internal. Texture.OnTargetReleased clears
                        // the event natively; closed callbacks return before dereference.
                        // Do not reflectively detach a callback during native rendering.
                        foreach (var camera in _cameras)
                            if (camera != null) RetireStep(camera.ReleaseCamera, ref failure);
                        if (_texture != null) RetireStep(_texture.Release, ref failure);
                    }
                    finally
                    {
                        _texture = null;
                        _view = null;
                        if (failure == null) _retired.TrySetResult(true);
                        else _retired.TrySetException(failure);
                    }
                }
            }
        }

        private static void RetireStep(Action cleanup, ref Exception failure)
        {
            try { cleanup(); }
            catch (Exception ex)
            {
                if (failure == null) failure = ex;
                try { Debug.Print("[Illustrator] Isolated panorama retirement failed: " + ex.GetType().Name); }
                catch { }
            }
        }
    }
}
