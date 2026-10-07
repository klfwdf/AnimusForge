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
        private bool _refreshQueued;

        private IllustratorGalleryPopup(ScreenBase screen)
        {
            _screen = screen;
            _scope = new IllustrationScope(screen, null, Close, missionOwned: TaleWorlds.CampaignSystem.Campaign.Current == null);
            try
            {
            _dataSource = new IllustratorGalleryPopupVM(Close, _scope.CampaignKey, () => _scope.IsCurrent);
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

        private void OnGenerationUpdated(IllustrationGenerationUpdate update)
        {
            if (_closed || update.Saved == null || update.CampaignKey != _scope.CampaignKey || _refreshQueued) return;
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
