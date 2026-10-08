using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator.UI.Gallery
{
    public sealed class IllustratorGalleryPopup
    {
        private static IllustratorGalleryPopup _activeInstance;
        private readonly ScreenBase _screen;
        private readonly MovableGauntletLayer _layer;
        private readonly IllustratorGalleryPopupVM _dataSource;
        private readonly IllustrationScope _scope;
        private bool _closed;
        private IDisposable _redrawEditor;
        private bool _refreshQueued;
        private bool _editingImage, _redrawing;
        private string _redrawDraft = "";
        private IllustrationScope _redrawJob;

        private IllustratorGalleryPopup(ScreenBase screen)
        {
            _screen = screen;
            _scope = new IllustrationScope(screen, null, Close, missionOwned: TaleWorlds.CampaignSystem.Campaign.Current == null);
            try
            {
            _dataSource = new IllustratorGalleryPopupVM(Close, _scope.CampaignKey, () => _scope.IsCurrent);
            _dataSource.RedrawSelected = OpenCurrentImageEditor;
            var layer = new MovableGauntletLayer("IllustratorGalleryPopup", 4020, false);
            _layer = layer;
            var movieIdentifier = layer.LoadMovie("IllustratorGalleryPopup", _dataSource);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "MainPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _layer = layer;
            IllustratorRuntime.GenerationUpdated += OnGenerationUpdated;

            try
            {
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            }
            catch
            {
            }
            }
            catch { Close(); throw; }
        }

        internal static TaleWorlds.GauntletUI.BaseTypes.Widget VisualRoot => _activeInstance?._layer?.UIContext?.Root;
        public static bool IsOpen => _activeInstance != null;

        public static void Show(string focusKey = null)
        {
            IllustratorRuntime.AssertMainThread();
            if (!IllustratorRuntime.IsEnabled()) return;
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null)
            {
                return;
            }

            IllustratorGalleryPopup popup = null;
            try
            {
                _activeInstance?.Close();
                popup = new IllustratorGalleryPopup(topScreen);
                if (!string.IsNullOrWhiteSpace(focusKey))
                {
                    popup._dataSource.SelectByKey(focusKey);
                }
                topScreen.AddLayer(popup._layer);
                _activeInstance = popup;
            }
            catch (Exception ex)
            {
                popup?.Close();
                Debug.Print($"[Illustrator] Failed to open gallery popup: {ex.Message}");
            }
        }

        private void OpenCurrentImageEditor(Engine.CachedIllustrationItem selected)
        {
            if (_closed || _editingImage || _redrawing || selected == null) return;
            _redrawEditor = IllustrationRedrawPromptEditor.Show(_redrawDraft,
                () => !_closed && _scope.IsCurrent && !_redrawing && ReferenceEquals(_dataSource.SelectedImage, selected),
                prompt => {
                    _redrawDraft = prompt;
                    var options = IllustratorRuntime.CaptureOptions();
                    if (selected.Category == "weekly_report" || selected.GenerationMode == "battle_screenshots" || selected.GenerationMode == "scene_shout_screenshots")
                        options = options?.WithSceneImageSize();
                    IllustrationScope job = null;
                    job = new IllustrationScope(_screen, selected.Category, () => {
                        if (!_closed && ReferenceEquals(_redrawJob, job)) { _redrawJob = null; _redrawing = false; _dataSource.SetRedrawing(false); }
                    }, campaignOwned: true, missionOwned: TaleWorlds.CampaignSystem.Campaign.Current == null);
                    _redrawJob = job;
                    _redrawing = true;
                    _dataSource.SetRedrawing(true);
                    _dataSource.StatusText = "正在基于所选画卷重绘，可关闭画廊等待。";
                    try
                    {
                        CurrentImageRedraw.Start(job, selected.CopyMetadata(), prompt, options, null, result => {
                            job.Close();
                            if (_closed) return;
                            _redrawJob = null; _redrawing = false; _dataSource.SetRedrawing(false);
                            if (result.Saved != null) _dataSource.SelectByKey(result.Saved.Key);
                            _dataSource.StatusText = result.Saved != null ? "重绘完成，已保存新画卷。" : "重绘失败：" + result.Result?.ErrorMessage;
                        }, error => {
                            job.Close();
                            if (_closed) return;
                            _redrawJob = null; _redrawing = false; _dataSource.SetRedrawing(false);
                            _dataSource.StatusText = "重绘失败：" + error;
                        });
                    }
                    catch { job.Close(); _redrawJob = null; _redrawing = false; _dataSource.SetRedrawing(false); throw; }
                }, status => { if (!_closed) _dataSource.StatusText = status; }, editing => {
                    if (_closed) return;
                    _editingImage = editing;
                    _layer.UIContext.Root.IsVisible = !editing;
                    _dataSource.SetRedrawing(editing || _redrawing);
                }, basedOnImage: true, inputOwner: _layer, isInputOwnerAlive: () => !_closed);
        }

        private void OnGenerationUpdated(IllustrationGenerationUpdate update)
        {
            if (_closed || _editingImage || update.Saved == null || update.CampaignKey != _scope.CampaignKey || _refreshQueued) return;
            _refreshQueued = true;
            // Coalesce completions, and release the generation worker before admitting a gallery refresh.
            IllustratorRuntime.PostCritical(() =>
            {
                _refreshQueued = false;
                if (!_closed && _scope.IsCurrent) _dataSource.RefreshItems();
            });
        }

        public void Close()
        {
            if (_closed) return;
            _closed = true;
            try { _redrawEditor?.Dispose(); } catch (Exception ex) { Debug.Print("[Illustrator] Editor cleanup failed: " + ex.Message); } finally { _redrawEditor = null; }
            try
            {
                IllustratorRuntime.GenerationUpdated -= OnGenerationUpdated;
                _scope?.Close();
                _dataSource?.DisposeVisuals();
                if (_screen != null && _layer != null)
                {
                    _screen.RemoveLayer(_layer);
                }
            }
            catch
            {
            }
            finally
            {
                if (_activeInstance == this)
                {
                    _activeInstance = null;
                }
            }
        }
    }
}
